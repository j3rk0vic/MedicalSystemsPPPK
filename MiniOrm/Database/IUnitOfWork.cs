namespace MiniOrm.Database;

public interface IUnitOfWork
{
    DbSet<T> Set<T>() where T : class;

    void Commit();
    void Rollback();
}