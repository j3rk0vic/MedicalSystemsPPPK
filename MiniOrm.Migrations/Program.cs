using System.Reflection;
using MiniOrm.Migrations;                                        

const string ConnectionString = "Host=ep-cold-thunder-agonl6uv-pooler.c-2.eu-central-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_2S5oxbmXDBwq;SSLMode=Require;Trust Server Certificate=true";
                                                                 
const string MigrationsFolder = "Migrations";
 
if (args.Length == 0)                                            
{               
    PrintHelp();
    return;
}

var runner   = new MigrationRunner(ConnectionString);            
var command  = args[0].ToLower();
                                                                 
switch (command)
{
    case "add":
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: dotnet run -- add <MigrationName>");                                               
            return;
        }                                                        
        var generator = new MigrationGenerator();
        generator.Generate(args[1], MigrationsFolder);           
        break;
                                                                 
    case "run": 
        runner.Run(LoadMigrations());
        break;

    case "rollback":                                             
        runner.Rollback(LoadMigrations());
        break;                                                   
                
    case "status":
        runner.PrintStatus(LoadMigrations());
        break;

    default:
        Console.WriteLine($"Unknown command: '{command}'");
        PrintHelp();                                             
        break;
}

static List<IMigration> LoadMigrations()
{                                                                
    return Assembly.GetExecutingAssembly()
        .GetTypes()                                              
        .Where(t => typeof(IMigration).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)                                 
        .Select(t => (IMigration)Activator.CreateInstance(t)!)
        .OrderBy(m => m.Name)                                    
        .ToList();
}                                                                
                
static void PrintHelp()
{
    Console.WriteLine();
    Console.WriteLine("MiniOrm Migrations");
    Console.WriteLine("──────────────────");
    Console.WriteLine("  dotnet run -- add <Name>   Generate a new migration file");                                            
    Console.WriteLine("  dotnet run -- run          Apply all pending migrations");                                       
    Console.WriteLine("  dotnet run -- rollback     Revert the last applied migration");
    Console.WriteLine("  dotnet run -- status       Show which migrations are applied");                                        
    Console.WriteLine();
}