using itembank_index_backend.Models.Settings;
using Microsoft.Extensions.Options;

namespace itembank_index_backend.Repositories.Sql;

public class EmptyRepository : Repository<bool>
{
    public EmptyRepository(IOptions<AppSettings> appSettings, ILogger<Repository<bool>> logger) : base(
        appSettings, logger)
    {
    }
}