using System.Data.SqlClient;
using Dapper;
using itembank_index_backend.Models.Resources.Sql;
using itembank_index_backend.Models.Settings;
using Microsoft.Extensions.Options;

namespace itembank_index_backend.Repositories.Sql;

public class ItemRepository : Repository<ItemResourceSql>
{
    public ItemRepository(IOptions<AppSettings> appSettings,
        ILogger<Repository<ItemResourceSql>> logger) : base(
        appSettings, logger)
    {
    }

    public async Task<List<ItemResourceSql>> GetItemsAsync(List<string> ids)
    {
        try
        {
            const string queryString = $@"
SELECT [ItemYears].Year AS applicableYear
    ,[BodiesOfKnowledge].Id AS bodyOfKnowledgeId
    ,[BodiesOfKnowledge].Code AS bodyOfKnowledgeCode
    ,[BodiesOfKnowledge].Name AS bodyOfKnowledgeName
    ,[BodiesOfKnowledge].SubjectId AS bodyOfKnowledgeSubjectId
    ,[BodiesOfKnowledge].InitiationYear AS bodyOfKnowledgeInitiationYear
    ,[BodiesOfKnowledge].FinalYear AS bodyOfKnowledgeFinalYear

    ,[Items].Content AS content
    ,[Items].Correctness AS correctness
    ,[Items].CreatedOn AS createdOn
    ,[Items].Difficulty AS difficulty
    ,[Items].Fidelity AS fidelity
    ,[Items].Id AS id
    ,[Items].Metadata AS metadata
    ,[Items].OnlineReadiness AS onlineReadiness
    ,[Items].ResourceLinks AS resourceLinks
    ,[Items].Solution AS solution
    ,[Items].SubjectIds AS subjectIds
    ,[Items].UpdatedOn AS updatedOn

    ,[Questions].AnsweringMethod
    ,[Questions].QuestionIndex
FROM Items
LEFT JOIN ItemYears
ON Items.Id = ItemYears.ItemId
LEFT JOIN BodiesOfKnowledge
ON BodiesOfKnowledge.Id = ItemYears.BodyOfKnowledgeId
LEFT JOIN Questions
ON Questions.ItemId = Items.Id
WHERE Items.Id IN @Ids
ORDER BY Items.Id, Questions.QuestionIndex";
            using (var conn = new SqlConnection(_connectionString))
            {
                var result = await conn.QueryAsync<ItemResourceSql>(
                    sql: queryString,
                    param: new
                    {
                        Ids = ids,
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