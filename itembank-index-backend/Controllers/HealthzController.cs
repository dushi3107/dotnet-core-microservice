using System.Data.SqlClient;
using RepositoryElastic = itembank_index_backend.Repositories.Elasticsearch;
using RepositorySql = itembank_index_backend.Repositories.Sql;
using Microsoft.AspNetCore.Mvc;

namespace itembank_index_backend.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("[controller]")]
public class HealthzController : ControllerBase
{
    private readonly RepositoryElastic.EmptyRepository _repositoryElastic;
    private readonly RepositorySql.EmptyRepository _repositorySql;

    public HealthzController(RepositoryElastic.EmptyRepository repositoryElastic,
        RepositorySql.EmptyRepository repositorySql)
    {
        _repositoryElastic = repositoryElastic;
        _repositorySql = repositorySql;
    }

    [HttpGet]
    public async Task<IActionResult> Healthz()
    {
        try
        {
            var statusCode = await _repositoryElastic.PingAsync();
            var statusCode2 = await _repositorySql.PingAsync();

            return statusCode != 200 || statusCode2 != 200 ? new StatusCodeResult(500) : new StatusCodeResult(200);
        }
        catch (Exception)
        {
            return new StatusCodeResult(500);
        }
    }
}