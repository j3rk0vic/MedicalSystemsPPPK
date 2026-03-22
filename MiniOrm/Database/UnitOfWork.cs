using Npgsql;

namespace MiniOrm.Database;

public class UnitOfWork
{
    private readonly DbContext _context;
    private readonly NpgsqlConnection _connection;
    private NpgsqlTransaction _transaction;

    public UnitOfWork(DbContext context)
    {
        _context = context;
        _connection = context.CreateConnection();
        _transaction = _connection.BeginTransaction();
    }

    public DbSet<T> Set<T>() where T : class 
        => new DbSet<T>(_context, _connection, _transaction);

    public void Commit()
    {
        _transaction.Commit();
        _transaction.Dispose();
        _transaction = _connection.BeginTransaction();
    }

    public void Rollback()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _transaction = _connection.BeginTransaction();
    }

    public void Dispose()
    {
        _transaction.Dispose();
        _connection.Dispose();
    }
    
}