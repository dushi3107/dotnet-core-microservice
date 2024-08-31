
using itembank_index_backend.Models.Resources.Sql;
using itembank_index_backend.Models.Settings;
using itembank_index_backend.Repositories.Sql;
using Microsoft.Extensions.Options;

namespace itembank_index_backend.Services;

public class MetadataService
{
    private readonly AppSettings _appSettings;
    private readonly MetadataRepository _metadataRepositorySql;
    
    public MetadataService(IOptions<AppSettings> appSettings, MetadataRepository metadataRepositorySql)
    {
        _appSettings = appSettings.Value;
        _metadataRepositorySql = metadataRepositorySql;
    }

    /**
     * gets raw data from sql
     */
    public async Task<List<MetadataResourceSql>> GetUserTypeRawAsync(string subjectId, List<int> years)
    {
        return await _metadataRepositorySql.GetMetadataByTypeAsync(subjectId, "usertype", years);
    }
    
    /**
     * gets raw data from sql
     */
    public async Task<List<MetadataResourceSql>> GetSourceRawAsync(string subjectId, List<int> years)
    {
        return await _metadataRepositorySql.GetMetadataByTypeAsync(subjectId, "source", years);
    }
}