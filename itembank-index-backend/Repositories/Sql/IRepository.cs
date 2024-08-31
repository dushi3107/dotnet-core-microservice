namespace itembank_index_backend.Repositories.Sql;

public interface IRepository<T>
{
    Task<int> PingAsync();
}