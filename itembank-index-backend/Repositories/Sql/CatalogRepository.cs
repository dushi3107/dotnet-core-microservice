using System.Data.SqlClient;
using Dapper;
using itembank_index_backend.Models.Resources.Sql;
using itembank_index_backend.Models.Settings;
using Microsoft.Extensions.Options;

namespace itembank_index_backend.Repositories.Sql;

public class CatalogRepository : Repository<CatalogResourceSql>
{
    public CatalogRepository(IOptions<AppSettings> appSettings,
        ILogger<Repository<CatalogResourceSql>> logger) : base(
        appSettings, logger)
    {
    }
    
    public async Task<List<MetadataResourceSql>> GetCatalogAsync(string subjectId, List<int> years)
    {
        try
        {
            const string queryString = $@"
SELECT Id
      , Year
      , CONCAT(GroupingCode, ' ', GroupingName,' - ', SubjectName, BaseName, Version, ' - ', Year, N'年') AS Name
  FROM Catalogs
  WHERE SubjectId = @SubjectId AND Year IN @Years AND Enabled = 1
  ORDER BY Year DESC";
            using (var conn = new SqlConnection(_connectionString))
            {
                var result = await conn.QueryAsync<MetadataResourceSql>(
                    sql: queryString,
                    param: new
                    {
                        SubjectId = subjectId,
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

    public async Task<List<CatalogDetailResourceSql>> GetCatalogDetailAsync(List<string> ids)
    {
        try
        {
            const string queryString = $@"
SELECT [DocItem].ItemId
    , [Catalogs].Id AS Id
    , MAX([Catalogs].GroupingCode) AS GroupCode
    , MAX(CONCAT([Catalogs].SubjectName, [Catalogs].BaseName, [Catalogs].Version, ' ', [Catalogs].Year, '年')) AS Name
    , MAX([Catalogs].Year) AS Year
FROM DocumentItems AS DocItem
LEFT JOIN Documents AS Doc
ON Doc.Id = DocItem.DocumentId
LEFT JOIN DocumentRepositories AS Repo
ON Repo.Id = Doc.DocumentRepositoryId
LEFT JOIN Catalogs
ON Catalogs.Id = Repo.CatalogId
WHERE DocItem.ItemId IN @Ids
GROUP BY DocItem.ItemId, Catalogs.Id";
            using (var conn = new SqlConnection(_connectionString))
            {
                var result = await conn.QueryAsync<CatalogDetailResourceSql>(
                    sql: queryString,
                    param: new
                    {
                        Ids = ids
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