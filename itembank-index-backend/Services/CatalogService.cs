
using itembank_index_backend.Models.Resources.Sql;
using itembank_index_backend.Models.Settings;
using itembank_index_backend.Repositories.Sql;
using Microsoft.Extensions.Options;

namespace itembank_index_backend.Services;

public class CatalogService
{
    private readonly AppSettings _appSettings;
    private readonly CatalogRepository _catalogRepositorySql;
    
    public CatalogService(IOptions<AppSettings> appSettings, CatalogRepository catalogRepositorySql)
    {
        _appSettings = appSettings.Value;
        _catalogRepositorySql = catalogRepositorySql;
    }

    /**
     * gets raw data from sql
     */
    public async Task<List<CatalogDetailResourceSql>> GetCatalogDetailRawResourceAsync(List<string> ids)
    {
        return await _catalogRepositorySql.GetCatalogDetailAsync(ids);
    }

    /**
     * gets raw data from sql
     */
    public async Task<List<MetadataResourceSql>> GetCatalogRawResourceAsync(string subjectId, List<int> years)
    {
        return await _catalogRepositorySql.GetCatalogAsync(subjectId, years);
    }
}