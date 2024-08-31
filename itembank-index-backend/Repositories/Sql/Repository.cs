using itembank_index_backend.Models.Settings;
using System.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace itembank_index_backend.Repositories.Sql;

public class Repository<T> : IRepository<T>
{
    protected readonly AppSettings _appSettings;
    protected readonly string _connectionString;
    protected readonly SqlConnection _conn;
    protected readonly ILogger<Repository<T>> _logger;

    // default constructor
    public Repository(IOptions<AppSettings> appSettings, ILogger<Repository<T>> logger)
    {
        _appSettings = appSettings.Value;
        _logger = logger;
        _connectionString = Environment.GetEnvironmentVariable(EnvironmentVariables.MSSQL_CONNECTION_STRING) ??
                            _appSettings.MsSqlConnectionString;
        // bypass certificate check, not recommended for production
        _connectionString += "TrustServerCertificate=True;";
    }

    public async Task<int> PingAsync()
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString + "Connection Timeout=30;"))
            {
                await connection.OpenAsync();
                return 200;
            }
        }
        catch (SqlException e)
        {
            _logger.LogError(e.ToString());
            return 500;
        }
    }
}