using System.Linq.Expressions;
  using System.Reflection;                                                          
  using MiniOrm.Attributes;                                                         
  using Npgsql;
                                                                                    
  namespace MiniOrm.Database;

  public class QueryBuilder<T> where T : class
  {
      private readonly List<string> _includes = new();
      
      private readonly DbContext _context;
      private readonly NpgsqlConnection? _sharedConnection;
      private readonly NpgsqlTransaction? _transaction;                             
   
      private readonly List<string> _whereConditions = new();                       
      private readonly List<NpgsqlParameter> _parameters = new();
      private int _paramCounter = 0;

      private string? _orderByColumn;                                               
      private bool _orderByDescending;
                                                                                    
      internal QueryBuilder(DbContext context, NpgsqlConnection? sharedConnection = null, NpgsqlTransaction? transaction = null)
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
                                                                                    
      public QueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
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
              var results = new List<T>();
                                                                                    
              using (var cmd = new NpgsqlCommand(sql, connection))
              {                                                                         
                  if (_transaction != null) cmd.Transaction = _transaction;
                  foreach (var param in _parameters)
                      cmd.Parameters.Add(param);

                  using var reader = cmd.ExecuteReader();                               
                  while (reader.Read())
                      results.Add(MapReaderToEntity(reader));                           
              } // reader is closed here — connection is still open
                                                                                    
              LoadIncludes(results, connection);                                                                         
                  
              return results;
          }
          finally                                                                       
          {
              if (owned) connection.Dispose();                                          
          }           
      }
      
      private void LoadIncludes(List<T> entities, NpgsqlConnection connection)
      {                                                                                 
          if (entities.Count == 0) return;
                                                                                    
          foreach (var propertyName in _includes)
          {
              var property = typeof(T).GetProperty(propertyName)
                             ?? throw new Exception($"Property {propertyName} not found on {typeof(T).Name}.");
                                                                                    
              var belongsTo = property.GetCustomAttribute<BelongsToAttribute>();
              var hasOne    = property.GetCustomAttribute<HasOneAttribute>();
              var hasMany   = property.GetCustomAttribute<HasManyAttribute>();          
   
              if (belongsTo != null) 
                  LoadBelongsTo(entities, property, belongsTo, connection);    
              else if (hasOne != null) 
                  LoadHasOne (entities, property, hasOne, connection);    
              else if (hasMany != null) 
                  LoadHasMany  (entities, property, hasMany, connection);
              else throw new Exception($"Property {propertyName} has no navigation attribute ([BelongsTo], [HasOne], [HasMany]).");                                  
          }
      }
      
      private void LoadBelongsTo(List<T> entities, PropertyInfo navProperty, BelongsToAttribute attr, NpgsqlConnection connection)                             
  {
                                                                           
      var fkProp = typeof(T).GetProperties()
          .FirstOrDefault(p => p.GetCustomAttribute<ColumnAttribute>()?.Name ==     
  attr.ForeignKeyColumn)                                                            
          ?? throw new Exception($"No property with [Column(\"{attr.ForeignKeyColumn}\")] found on {typeof(T).Name}.");               
                  
      // Collect all unique FK values (e.g. all distinct patient_id values)         
      var ids = entities
          .Select(e => fkProp.GetValue(e))                                          
          .Where(v => v != null)
          .Select(v => (int)v!)                                                     
          .Distinct()                                                               
          .ToArray();
                                                                                    
      if (ids.Length == 0) return;

      // SELECT * FROM patients WHERE id = ANY(@ids)                                
      var relatedTable = GetTableNameForType(attr.RelatedType);
      var relatedPkColumn = GetPrimaryKeyColumnForType(attr.RelatedType);           
      var sql = $"SELECT * FROM {relatedTable} WHERE {relatedPkColumn} = ANY(@ids)";
                                                                                    
      var relatedEntities = ExecuteQueryForType(attr.RelatedType, sql, ids, connection);                                                                      
                  
      // Build a dictionary: related entity's PK → entity object                    
      var relatedPkProp = GetPrimaryKeyPropertyForType(attr.RelatedType);
      var dict = relatedEntities.ToDictionary(e => (int)relatedPkProp.GetValue(e)!);
                  
      // Assign to each main entity                                                 
      foreach (var entity in entities)
      {                                                                             
          var fkValue = (int)fkProp.GetValue(entity)!;
          if (dict.TryGetValue(fkValue, out var related))                           
              navProperty.SetValue(entity, related);
      }                                                                             
  } 
      
      private void LoadHasOne(List<T> entities, PropertyInfo navProperty,               
          HasOneAttribute attr, NpgsqlConnection connection)                                
      {
          var ourPkProp = GetPrimaryKeyProperty()!;                                     
                  
          var ids = entities                                                            
              .Select(e => (int)ourPkProp.GetValue(e)!)
              .Distinct()                                                               
              .ToArray();

          if (ids.Length == 0) return;

          // SELECT * FROM medical_records WHERE patient_id = ANY(@ids)                 
          var relatedTable = GetTableNameForType(attr.RelatedType);
          var sql = $"SELECT * FROM {relatedTable} WHERE {attr.ForeignKeyColumn} = ANY(@ids)";                                                                       
   
          var relatedEntities = ExecuteQueryForType(attr.RelatedType, sql, ids,         
              connection);    

          // Build dictionary: FK value on related entity → related entity              
          var relatedFkProp = attr.RelatedType.GetProperties()
                                  .FirstOrDefault(p => p.GetCustomAttribute<ColumnAttribute>()?.Name ==     
                                                       attr.ForeignKeyColumn)                                                            
                              ?? throw new Exception($"No property with [Column(\"{attr.ForeignKeyColumn}\")] on {attr.RelatedType.Name}.");              
                  
          var dict = relatedEntities.ToDictionary(e => (int)relatedFkProp.GetValue(e)!);
   
          // Assign to each main entity                                                 
          foreach (var entity in entities)
          {
              var pkValue = (int)ourPkProp.GetValue(entity)!;
              if (dict.TryGetValue(pkValue, out var related))
                  navProperty.SetValue(entity, related);                                
          }
      }
                                                                                    
      // --- SQL Builder ---

      private string BuildSql()
      {
          var sql = $"SELECT * FROM {GetTableName()}";
                                                                                    
          if (_whereConditions.Any())
              sql += $" WHERE {string.Join(" AND ", _whereConditions)}";            
                  
          if (_orderByColumn != null)
              sql += $" ORDER BY {_orderByColumn} {(_orderByDescending ? "DESC" : "ASC")}";                                                                         
   
          return sql;                                                               
      }    
      
      private void LoadHasMany(List<T> entities, PropertyInfo navProperty,              
  HasManyAttribute attr, NpgsqlConnection connection)                               
  {
      var ourPkProp = GetPrimaryKeyProperty()!;                                     
                  
      var ids = entities
          .Select(e => (int)ourPkProp.GetValue(e)!)
          .Distinct()                                                               
          .ToArray();
                                                                                    
      if (ids.Length == 0) return;

      // SELECT * FROM examinations WHERE patient_id = ANY(@ids)
      var relatedTable = GetTableNameForType(attr.RelatedType);
      var sql = $"SELECT * FROM {relatedTable} WHERE {attr.ForeignKeyColumn} = ANY(@ids)";
                                                                                    
      var relatedEntities = ExecuteQueryForType(attr.RelatedType, sql, ids, connection);

      // Group related entities by their FK value                                   
      var relatedFkProp = attr.RelatedType.GetProperties()
          .FirstOrDefault(p => p.GetCustomAttribute<ColumnAttribute>()?.Name ==     
  attr.ForeignKeyColumn)                                                            
          ?? throw new Exception($"No property with [Column(\"{attr.ForeignKeyColumn}\")] on {attr.RelatedType.Name}.");              
                  
      var grouped = relatedEntities
          .GroupBy(e => (int)relatedFkProp.GetValue(e)!)
          .ToDictionary(g => g.Key, g => g.ToList());                               
   
      // Assign typed List<RelatedType> to each main entity                         
      foreach (var entity in entities)
      {                                                                             
          var pkValue = (int)ourPkProp.GetValue(entity)!;
          var items = grouped.TryGetValue(pkValue, out var list) ? list : new       
  List<object>(); 

          // We can't just assign List<object> — we need List<Examination> etc.     
          // Use reflection to create the correctly typed list
          var typedList =                                                           
  Activator.CreateInstance(typeof(List<>).MakeGenericType(attr.RelatedType))!;
          var addMethod = typedList.GetType().GetMethod("Add")!;
          foreach (var item in items)                                               
              addMethod.Invoke(typedList, new[] { item });
                                                                                    
          navProperty.SetValue(entity, typedList);
      }
  }
      
      private List<object> ExecuteQueryForType(Type type, string sql, int[] ids,        
  NpgsqlConnection connection)
  {
      using var cmd = new NpgsqlCommand(sql, connection);
      if (_transaction != null) cmd.Transaction = _transaction;                     
      cmd.Parameters.AddWithValue("@ids", ids);
                                                                                    
      using var reader = cmd.ExecuteReader();
      var results = new List<object>();
      while (reader.Read())                                                         
          results.Add(MapReaderToType(reader, type));
      return results;                                                               
  }               

  private static object MapReaderToType(NpgsqlDataReader reader, Type type)
  {
      var entity = Activator.CreateInstance(type)!;
      var properties = type.GetProperties()                                         
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
                  property.SetValue(entity, Enum.ToObject(property.PropertyType,    
  value));                                                                          
              else
                  property.SetValue(entity, Convert.ChangeType(value,               
  property.PropertyType));
          }
          catch (IndexOutOfRangeException) { }
      }

      return entity;
  }
                                                                                    
  private static string GetTableNameForType(Type type)
  {                                                                                 
      var attr = type.GetCustomAttribute<TableAttribute>()
          ?? throw new Exception($"Type {type.Name} is missing [Table] attribute.");
      return attr.Name;                                                             
  }
                                                                                    
  private static string GetPrimaryKeyColumnForType(Type type)
  {
      var prop = GetPrimaryKeyPropertyForType(type);
      return prop.GetCustomAttribute<ColumnAttribute>()!.Name;                      
  }
                                                                                    
  private static PropertyInfo GetPrimaryKeyPropertyForType(Type type)
  {
      return type.GetProperties().FirstOrDefault(p =>                               
  p.GetCustomAttribute<PrimaryKeyAttribute>() != null)
          ?? throw new Exception($"Type {type.Name} has no [PrimaryKey] property.");
  }                                                                                 
   
  private PropertyInfo? GetPrimaryKeyProperty()                                     
  {               
      return typeof(T).GetProperties().FirstOrDefault(p =>
          p.GetCustomAttribute<PrimaryKeyAttribute>() != null);
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
          var memberExpr = (binary.Left as MemberExpression) ?? (binary.Right as MemberExpression)                                                                 
              ?? throw new NotSupportedException("Where clause must compare a property to a value.");                                                           
                  
          var valueExpr = binary.Left is MemberExpression ? binary.Right : binary.Left;    

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
      
      public QueryBuilder<T> Include(Expression<Func<T, object?>> navigationProperty)
      {                                                                                 
          // Handle nullable types — the compiler wraps them in a Convert expression
          Expression body = navigationProperty.Body;                                    
          if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
              body = unary.Operand;                                                     
                  
          var member = (MemberExpression)body;                                          
          _includes.Add(member.Member.Name);
          return this;
      }
  }
