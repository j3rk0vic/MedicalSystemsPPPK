using System.Linq.Expressions;
  using System.Reflection;                                                          
  using MiniOrm.Attributes;                                                         
  using Npgsql;
                                                                                    
  namespace MiniOrm.Database;

  public class QueryBuilder<T> where T : class
  {
      private readonly DbContext _context;
      private readonly NpgsqlConnection? _sharedConnection;
      private readonly NpgsqlTransaction? _transaction;                             
   
      private readonly List<string> _whereConditions = new();                       
      private readonly List<NpgsqlParameter> _parameters = new();
      private int _paramCounter = 0;

      private string? _orderByColumn;                                               
      private bool _orderByDescending;
                                                                                    
      internal QueryBuilder(DbContext context, NpgsqlConnection? sharedConnection = 
  null, NpgsqlTransaction? transaction = null)
      {
          _context = context;
          _sharedConnection = sharedConnection;
          _transaction = transaction;                                               
      }
                                                                                    
      // --- Fluent methods ---

      public QueryBuilder<T> Where(Expression<Func<T, bool>> predicate)             
      {
          ParseExpression(predicate.Body);                                          
          return this; // returns itself so you can keep chaining
      }                                                                             
   
      public QueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)   
      {           
          _orderByColumn = GetColumnName(keySelector);
          _orderByDescending = false;
          return this;                                                              
      }
                                                                                    
      public QueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> 
  keySelector)
      {
          _orderByColumn = GetColumnName(keySelector);
          _orderByDescending = true;
          return this;                                                              
      }
                                                                                    
      // --- Execution ---

      public List<T> ToList()
      {
          var sql = BuildSql();
          var (connection, owned) = AcquireConnection();
                                                                                    
          try
          {                                                                         
              using var cmd = new NpgsqlCommand(sql, connection);
              if (_transaction != null) cmd.Transaction = _transaction;

              foreach (var param in _parameters)
                  cmd.Parameters.Add(param);
                                                                                    
              using var reader = cmd.ExecuteReader();
              var results = new List<T>();                                          
              while (reader.Read())
                  results.Add(MapReaderToEntity(reader));

              return results;
          }
          finally
          {
              if (owned) connection.Dispose();
          }                                                                         
      }
                                                                                    
      // --- SQL Builder ---

      private string BuildSql()
      {
          var sql = $"SELECT * FROM {GetTableName()}";
                                                                                    
          if (_whereConditions.Any())
              sql += $" WHERE {string.Join(" AND ", _whereConditions)}";            
                  
          if (_orderByColumn != null)
              sql += $" ORDER BY {_orderByColumn} {(_orderByDescending ? "DESC" : 
  "ASC")}";                                                                         
   
          return sql;                                                               
      }           

      // --- Expression Tree Parser ---

      private void ParseExpression(Expression expression)
      {
          if (expression is not BinaryExpression binary)
              throw new NotSupportedException($"Expression type {expression.NodeType} is not supported.");
                                                                                    
          // Handle p.X == 1 && p.Y == 2  (recursively parse both sides)            
          if (binary.NodeType == ExpressionType.AndAlso)
          {                                                                         
              ParseExpression(binary.Left);
              ParseExpression(binary.Right);
              return;
          }

          // Handle p.Property == value / p.Property > value / etc.                 
          var memberExpr = (binary.Left as MemberExpression) ?? (binary.Right as
  MemberExpression)                                                                 
              ?? throw new NotSupportedException("Where clause must compare a property to a value.");                                                           
                  
          var valueExpr = binary.Left is MemberExpression ? binary.Right :          
  binary.Left;    

          var column = GetColumnFromMember(memberExpr);                             
          var value  = GetValueFromExpression(valueExpr);
          var op     = GetSqlOperator(binary.NodeType);                             
                  
          var paramName = $"@p{_paramCounter++}";
          _whereConditions.Add($"{column} {op} {paramName}");
          _parameters.Add(new NpgsqlParameter(paramName, value ?? DBNull.Value));   
      }
                                                                                    
      private string GetColumnFromMember(MemberExpression member)                   
      {
          var property = typeof(T).GetProperty(member.Member.Name)                  
              ?? throw new Exception($"Property {member.Member.Name} not found on {typeof(T).Name}.");                                                              
   
          var columnAttr = property.GetCustomAttribute<ColumnAttribute>()           
              ?? throw new Exception($"Property {member.Member.Name} has no [Column] attribute.");

          return columnAttr.Name;                                                   
      }
                                                                                    
      private static object? GetValueFromExpression(Expression expression)
      {
          // Compiles the right-hand side of the condition and evaluates it
          // Works for constants, variables, method calls, etc.                     
          return Expression.Lambda(expression).Compile().DynamicInvoke();
      }                                                                             
                  
      private static string GetSqlOperator(ExpressionType type) => type switch      
      {           
          ExpressionType.Equal              => "=",
          ExpressionType.NotEqual           => "!=",
          ExpressionType.GreaterThan        => ">",                                 
          ExpressionType.GreaterThanOrEqual => ">=",
          ExpressionType.LessThan           => "<",                                 
          ExpressionType.LessThanOrEqual    => "<=",
          _ => throw new NotSupportedException($"Operator {type} is not supported in Where clause.")
      };                                                                            
                  
      private string GetColumnName<TKey>(Expression<Func<T, TKey>> keySelector)     
      {
          var member = (MemberExpression)keySelector.Body;                          
          var property = typeof(T).GetProperty(member.Member.Name)!;
          var columnAttr = property.GetCustomAttribute<ColumnAttribute>()!;
          return columnAttr.Name;                                                   
      }
                                                                                    
      // --- Helpers (same as DbSet) ---

      private (NpgsqlConnection connection, bool owned) AcquireConnection()         
      {
          if (_sharedConnection != null) return (_sharedConnection, false);         
          return (_context.CreateConnection(), true);
      }

      private string GetTableName()
      {
          var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>()
              ?? throw new Exception($"Entity {typeof(T).Name} is missing [Table] attribute.");
          return tableAttr.Name;                                                    
      }           

      private T MapReaderToEntity(NpgsqlDataReader reader)                          
      {
          var entity = Activator.CreateInstance<T>();                               
          var properties = typeof(T).GetProperties()
              .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null)
              .ToList();

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
              catch (IndexOutOfRangeException) { }                                  
          }       

          return entity;
      }
  }
