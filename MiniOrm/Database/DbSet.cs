using System.Linq.Expressions;
using System.Reflection;
using MiniOrm.Attributes;
using Npgsql;

namespace MiniOrm.Database;

public class DbSet<T> where T : class
{
    protected readonly DbContext _context;

    private readonly NpgsqlConnection? _sharedConnection;
    private readonly NpgsqlTransaction? _transaction;

    public DbSet(DbContext context)
    {
        _context = context;
    }

    internal DbSet(DbContext context, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _context = context;
        _sharedConnection = connection;
        _transaction = transaction;
    }

    private (NpgsqlConnection connection, bool owned) AcquireConnection()
    {
        if (_sharedConnection != null)
            return (_sharedConnection, false);
        return (_context.CreateConnection(), true);
    }

    private NpgsqlCommand CreateCommand(string sql, NpgsqlConnection connection)
    {
        var cmd = new NpgsqlCommand(sql, connection);
        if (_transaction != null)
            cmd.Transaction = _transaction;
        return cmd;
    }

    public List<T> GetAll()
    {
        var sql = $"SELECT * FROM {GetTableName()}";
        var (connection, owned) = AcquireConnection();                                
        try                                         
        {                                           
            using var command = CreateCommand(sql,connection);                                        
            using var reader = command.ExecuteReader();                            
                  
            var results = new List<T>();            
            while (reader.Read())
                results.Add(MapReaderToEntity(reader));
            
            return results;                         
        }       
        finally                                     
        {
            if (owned) 
                connection.Dispose();
        }
    }

    public T? GetById(int id)
    {
        var pkColumn = GetPrimaryKeyColumnName();   
        var sql = $"SELECT * FROM {GetTableName()} WHERE {pkColumn} = @id";                            
                                                      
        var (connection, owned) = AcquireConnection();
        try                                         
        {       
            using var command = CreateCommand(sql, connection);                                        
            command.Parameters.AddWithValue("id", id);                                                
            using var reader = command.ExecuteReader();                            
                  
            if (reader.Read())                      
                return MapReaderToEntity(reader);
                                                      
            return null;
        }                                           
        finally                                     
        {
            if (owned) 
                connection.Dispose();        
        } 
    }

    public void Insert(T entity)
    {
        var properties = GetColumnProperties(excludePrimaryKey: true);       
        var columnNames = properties.Select(p =>    
            p.GetCustomAttribute<ColumnAttribute>()!.Name);     
        var paramNames = properties.Select(p => "@" + p.GetCustomAttribute<ColumnAttribute>()!.Name);   
        var sql = $"INSERT INTO {GetTableName()} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", paramNames)}) RETURNING id";
                                                      
        var (connection, owned) = AcquireConnection();
        try                                         
        {       
            using var command = CreateCommand(sql, connection);                                        
                                                      
            foreach (var property in properties)    
            {   
                var columnAttr = property.GetCustomAttribute<ColumnAttribute>()!;    
                var value = property.GetValue(entity) ?? DBNull.Value;          
                command.Parameters.AddWithValue("@" + columnAttr.Name, value);                          
            }   
                                                      
            var newId = command.ExecuteScalar();
                                                      
            GetPrimaryKeyProperty()?.SetValue(entity, Convert.ToInt32(newId));
        }                                           
        finally 
        {
            if (owned) 
                connection.Dispose();        
        } 
    }

    public void Update(T entity)
    {
        var pkColumn = GetPrimaryKeyColumnName();   
        var pkProperty = GetPrimaryKeyProperty()!;  
        var pkValue = pkProperty.GetValue(entity);  
                                                      
        var properties = GetColumnProperties(excludePrimaryKey: true);       
        var setClauses = properties.Select(p =>
        {                                           
            var colAttr = p.GetCustomAttribute<ColumnAttribute>()!;           
            return $"{colAttr.Name} = @{colAttr.Name}";                                   
        });     
        var sql = $"UPDATE {GetTableName()} SET {string.Join(", ", setClauses)} WHERE {pkColumn} = @pk";
                                                      
        var (connection, owned) = AcquireConnection();
        try                                         
        {       
            using var command = CreateCommand(sql, connection);                                        
                                                      
            foreach (var property in properties)    
            {   
                var columnAttr = property.GetCustomAttribute<ColumnAttribute>()!;    
                var value = property.GetValue(entity) ?? DBNull.Value;          
                command.Parameters.AddWithValue("@" + columnAttr.Name, value);                          
            }   
                                                      
            command.Parameters.AddWithValue("@pk", pkValue);
            command.ExecuteNonQuery();
        }                                           
        finally
        {                                           
            if (owned) 
                connection.Dispose();
        }
    }

    public void Delete(int id)
    {
        var pkColumn = GetPrimaryKeyColumnName();   
        var sql = $"DELETE FROM {GetTableName()} WHERE {pkColumn} = @id";                            
                                                      
        var (connection, owned) = AcquireConnection();
        try                                         
        {       
            using var command = CreateCommand(sql, connection);                                        
            command.Parameters.AddWithValue("@id", id);                                                
            command.ExecuteNonQuery();
        }                                           
        finally                                     
        {                                           
            if (owned) 
                connection.Dispose();        
        } 
    }

    private string GetTableName()
    {
        var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>() ??
                        throw new Exception($"Entity {typeof(T).Name} is missing [Table] attribute.");
        return tableAttr.Name;
    }

    private string GetPrimaryKeyColumnName()
    {
        var pkProperty = GetPrimaryKeyProperty() ?? throw new Exception($"ENtity {typeof(T).Name} has no [PrimaryKey] property.");
        var columnAttr = pkProperty.GetCustomAttribute<ColumnAttribute>() ??
                         throw new Exception($"Primary key property has no [Column] attribute.");
        return columnAttr.Name;
    }

    private PropertyInfo? GetPrimaryKeyProperty()
    {
        return typeof(T).GetProperties().FirstOrDefault(p =>
            p.GetCustomAttribute<PrimaryKeyAttribute>() != null);
    }

    private List<PropertyInfo> GetColumnProperties(bool excludePrimaryKey = false)
    {
        return typeof(T).GetProperties()
            .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null)
            .Where(p => !excludePrimaryKey || p.GetCustomAttribute<PrimaryKeyAttribute>() == null)
            .ToList();
    }

    private T MapReaderToEntity(NpgsqlDataReader reader)
    {
        var entity = Activator.CreateInstance<T>();
        var properties = GetColumnProperties();

        foreach (var property in properties)
        {
            var columnAttr = property.GetCustomAttribute<ColumnAttribute>()!;

            try
            {
                var ordinal = reader.GetOrdinal(columnAttr.Name);
                if (reader.IsDBNull(ordinal)) continue;
                
                var value = reader.GetValue(ordinal);
                
                if (property.PropertyType.IsEnum)
                    property.SetValue(entity, Enum.ToObject(property.PropertyType, value));
                else
                    property.SetValue(entity, Convert.ChangeType(value, property.PropertyType));
            }
            catch (IndexOutOfRangeException)
            {
                // column not in result, skip
            }
        }

        return entity;
    }
    
    public QueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
        => new QueryBuilder<T>(_context, _sharedConnection,                           
            _transaction).Where(predicate);                                                   
   
    public QueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)       
        => new QueryBuilder<T>(_context, _sharedConnection,
            _transaction).OrderBy(keySelector);                                               
   
    public QueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>>          
        keySelector)    
        => new QueryBuilder<T>(_context, _sharedConnection,
            _transaction).OrderByDescending(keySelector);
}