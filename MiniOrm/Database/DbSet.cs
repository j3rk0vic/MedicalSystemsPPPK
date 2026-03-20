using MiniOrm.Attributes;
using Npgsql;

namespace MiniOrm.Database;

public class DbSet<T> where T : class
{
    protected readonly DbContext _context;

    public DbSet(DbContext context)
    {
        _context = context;
    }
}