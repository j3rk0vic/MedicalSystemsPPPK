using System.Reflection;
using MedicalSystem.Web.Data;
using MiniOrm.Attributes;                                        
using MiniOrm.Database;
                                                                   
namespace MiniOrm.Migrations;

public class MigrationGenerator                                  
{
    private readonly TableBuilder _tableBuilder = new();         
                
    public void Generate(string migrationName, string outputFolder)
    {
        var entityTypes = GetEntityTypesInDependencyOrder();
                                                                 
        // Build Up SQL: CREATE TABLE for each entity            
        var upStatements = entityTypes                           
            .Select(t => _tableBuilder.GenerateCreateTableSql(t) + ";")                                                           
            .ToList();
                                                                 
        // Build Down SQL: DROP TABLE in REVERSE order (respect FK constraints)
        var downStatements = entityTypes                         
            .AsEnumerable()
            .Reverse()
            .Select(t =>
            {
                var tableAttr = t.GetCustomAttribute<TableAttribute>()!;                         
                return $"DROP TABLE IF EXISTS {tableAttr.Name} CASCADE;";                                                       
            })  
            .ToList();                                           
                
        // Timestamp prefix ensures alphabetical order = chronological order
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var migrationId = $"{timestamp}_{migrationName}";                    
        var className   = $"Migration_{migrationId}";        
        var fileName    = $"{migrationId}.cs";                       
        var filePath  = Path.Combine(outputFolder, fileName);    
   
        // Indent SQL nicely inside the verbatim string          
        var upSql   = string.Join("\n        ", upStatements);
        var downSql = string.Join("\n        ", downStatements); 
                
        var content =                                            
            $@"namespace MiniOrm.Migrations.Migrations;
                                                                             
            public class {className} : IMigration
            {{
                public string Name => ""{migrationId}"";
                                                                             
                public string Up() => @""
                    {upSql}                                                  
                "";                                                          
             
                public string Down() => @""                                  
                    {downSql}
                "";
            }}
            ";
        Directory.CreateDirectory(outputFolder);
        File.WriteAllText(filePath, content);                    
   
        Console.WriteLine($"Migration created: {filePath}");     
        Console.WriteLine("Next step: dotnet build, then dotnet run -- run");                                                    
    }
                                                                 
    // Uses reflection on MedicalDbContext to discover entity types
    // Order of DbSet properties in MedicalDbContext = dependency order                                                           
    private static List<Type> GetEntityTypesInDependencyOrder()
    {                                                            
        return typeof(MedicalDbContext)
            .GetProperties()                                     
            .Where(p =>
                p.PropertyType.IsGenericType &&                  
                p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))                                                 
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToList();                                           
    }           
}
