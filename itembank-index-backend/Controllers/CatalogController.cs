using itembank_index_backend.Models.Resources.Sql;
using itembank_index_backend.Services;
using itembank_index_backend.Utils;
using Microsoft.AspNetCore.Mvc;

namespace itembank_index_backend.Controllers;

[ApiController]
[Route("[controller]")]
public class CatalogController
{
    private readonly CatalogService _catalogService;
    
    public CatalogController(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }
    
    [HttpPost("detail", Name = "Detail")]
    public async Task<dynamic> CatalogDetail([FromBody] List<string> ids)
    {
        var result = await _catalogService.GetCatalogDetailRawResourceAsync(ids);
        return Http.HandleNullResponse(result);
    }
    
    [HttpGet("")]
    public async Task<dynamic> Catalog([FromQuery] string subjectId, [FromQuery] List<int> years)
    {
        var result = await _catalogService.GetCatalogRawResourceAsync(subjectId, years);
        return Http.HandleNullResponse(result);
    }
}