using System.Data.SqlClient;
using Dapper;
using itembank_index_backend.Models.Resources.Sql;
using itembank_index_backend.Models.Settings;
using Microsoft.Extensions.Options;

namespace itembank_index_backend.Repositories.Sql;

public class MetadataRepository : Repository<MetadataResourceSql>
{
    public MetadataRepository(IOptions<AppSettings> appSettings,
        ILogger<Repository<MetadataResourceSql>> logger) : base(
        appSettings, logger)
    {
    }
    
    public async Task<List<MetadataResourceSql>> GetMetadataByTypeAsync(string subjectId, string type, List<int> years)
    {
        try
        {
            const string queryString = $@"
SELECT [Values].[Name]
    ,[Values].[Id]
    ,[Values].[Type]
    ,[ViewMembers].[OrderIndex]
    ,[Views].[Year]
FROM [SubjectCustomMetadataViews] AS [Views]
INNER JOIN [SubjectCustomMetadataViewMembers] AS [ViewMembers]
ON  [ViewMembers].SubjectCustomMetadataViewId = [Views].[Id]
INNER JOIN [CustomMetadataValues] AS [Values]
ON [Values].[Id] = [ViewMembers].[CustomMetadataValueId]
WHERE [Views].[SubjectId] = @SubjectId AND [Views].[Type] = @Type AND [Views].[Year] IN @Years
ORDER BY [Views].[Year] DESC, [ViewMembers].[OrderIndex] ASC";
            using (var conn = new SqlConnection(_connectionString))
            {
                var result = await conn.QueryAsync<MetadataResourceSql>(
                    sql: queryString,
                    param: new
                    {
                        SubjectId = subjectId,
                        Type = type,
                        Years = years
                    });
                return result.ToList();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            throw;
        }
    }
}