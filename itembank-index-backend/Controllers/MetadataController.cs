using itembank_index_backend.Services;
using itembank_index_backend.Utils;
using Microsoft.AspNetCore.Mvc;

namespace itembank_index_backend.Controllers;

[ApiController]
[Route("[controller]")]
public class MetadataController : ControllerBase
{
    private readonly MetadataService _metadataService;

    public MetadataController(MetadataService metadataService)
    {
        _metadataService = metadataService;
    }

    [HttpGet("usertype", Name = "UserType")]
    public async Task<dynamic> UserType([FromQuery] string subjectId, [FromQuery] List<int> years)
    {
        var result = await _metadataService.GetUserTypeRawAsync(subjectId, years);
        return Http.HandleNullResponse(result);
    }

    [HttpGet("source", Name = "Source")]
    public async Task<dynamic> Source([FromQuery] string subjectId, [FromQuery] List<int> years)
    {
        var result = await _metadataService.GetSourceRawAsync(subjectId, years);
        return Http.HandleNullResponse(result);
    }
}