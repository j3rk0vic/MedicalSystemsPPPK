using Npgsql;
                                                                   
namespace MiniOrm.Migrations;

public class MigrationRunner
{
    private readonly string _connectionString;

    public MigrationRunner(string connectionString)              
    {
          _connectionString = connectionString;                    
    }           

    private NpgsqlConnection CreateConnection()
    {
        var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        return conn;
    }                                                            
   
    // Creates __migrations table if it doesn't exist yet        
    public void EnsureMigrationsTable()
    {
        using var conn = CreateConnection();
        using var cmd = new NpgsqlCommand(@"
            CREATE TABLE IF NOT EXISTS __migrations (            
                id         SERIAL PRIMARY KEY,
                name       VARCHAR(255) NOT NULL UNIQUE,         
                applied_at TIMESTAMP NOT NULL DEFAULT NOW()
            )", conn);
        cmd.ExecuteNonQuery();
    }                                                            
   
    // Returns names of all migrations already applied, in order 
    public List<string> GetAppliedMigrations()
    {                                                            
        using var conn = CreateConnection();
        using var cmd = new NpgsqlCommand(                       
            "SELECT name FROM __migrations ORDER BY applied_at ASC", conn);                                                     
        using var reader = cmd.ExecuteReader();
                                                                 
        var applied = new List<string>();
        while (reader.Read())
            applied.Add(reader.GetString(0));
        return applied;
    }

    // Applies all pending migrations in order                   
    public void Run(List<IMigration> allMigrations)
    {                                                            
        EnsureMigrationsTable();
        var applied = GetAppliedMigrations();
                                                                 
        var pending = allMigrations
            .Where(m => !applied.Contains(m.Name))               
            .OrderBy(m => m.Name)                                                    
            .ToList();
                                                                   
        if (pending.Count == 0)
        {
            Console.WriteLine("No pending migrations. Database is up to date.");
            return;
        }

        foreach (var migration in pending)                       
        {
            Console.Write($"Applying {migration.Name}... ");     
                  
            using var conn = CreateConnection();
            using var tx = conn.BeginTransaction();

            try
            {
                // Execute the Up SQL inside a transaction
                using var upCmd = new NpgsqlCommand(migration.Up(), conn, tx);
                upCmd.ExecuteNonQuery();                         
                  
                // Record the migration as applied               
                using var trackCmd = new NpgsqlCommand(
                    "INSERT INTO __migrations (name) VALUES (@name)", conn, tx);
                trackCmd.Parameters.AddWithValue("@name", migration.Name);                                                 
                trackCmd.ExecuteNonQuery();
                                                                   
                tx.Commit();
                Console.WriteLine("Done.");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                Console.WriteLine($"FAILED: {ex.Message}");
                throw;
            }                                                    
        }       
    }                                                            
   
    // Reverts the last applied migration                        
    public void Rollback(List<IMigration> allMigrations)
    {
        EnsureMigrationsTable();
        var applied = GetAppliedMigrations();
                                                                   
        if (applied.Count == 0)
        {                                                        
            Console.WriteLine("Nothing to rollback. No migrations have been applied.");
            return;
        }

        var lastAppliedName = applied.Last();                    
   
        var migration = allMigrations.FirstOrDefault(m => m.Name == lastAppliedName)
            ?? throw new Exception($"Migration class for '{lastAppliedName}' not found in assembly. Was it deleted?");    
   
        Console.Write($"Rolling back {migration.Name}... ");     
                  
        using var conn = CreateConnection();
        using var tx = conn.BeginTransaction();
                                                                   
        try
        {                                                        
            // Execute the Down SQL inside a transaction
            using var downCmd = new NpgsqlCommand(migration.Down(), conn, tx);
            downCmd.ExecuteNonQuery();

            // Remove from tracking table                        
            using var deleteCmd = new NpgsqlCommand( "DELETE FROM __migrations WHERE name = @name", conn, tx);      
            deleteCmd.Parameters.AddWithValue("@name", migration.Name);                                                 
            deleteCmd.ExecuteNonQuery();
                                                                   
            tx.Commit();
            Console.WriteLine("Done.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            Console.WriteLine($"FAILED: {ex.Message}");
            throw;                                               
        }
    }                                                            
                  
    // Shows all migrations and whether they have been applied
    public void PrintStatus(List<IMigration> allMigrations)
    {                                                            
        EnsureMigrationsTable();
        var applied = GetAppliedMigrations();                    
                  
        Console.WriteLine("Migration status:");
        Console.WriteLine($"{"Status",-12} {"Name"}");
        Console.WriteLine(new string('-', 60));                  
                                                                   
        foreach (var m in allMigrations.OrderBy(m => m.Name))    
        {                                                        
            var status = applied.Contains(m.Name) ? "[applied]" : "[pending]";
          Console.WriteLine($"{status,-12} {m.Name}");
        }                                                        
   
        if (!allMigrations.Any())                                
            Console.WriteLine("  No migrations found.");
    }                                                            
}
