using Npgsql;

namespace MiniOrm.Database;

public abstract class DbContext
{
    protected readonly PostgresConnectionFactory _connectionFactory;

    protected DbContext(string connectionString)
    {
        _connectionFactory = new PostgresConnectionFactory(connectionString);
    }

    public NpgsqlConnection CreateConnection()
    {
        return _connectionFactory.Create();
    }
    
    public TableBuilder CreateTableBuilder()
    {
        return new TableBuilder();
    }
}