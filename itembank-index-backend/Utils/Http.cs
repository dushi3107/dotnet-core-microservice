using Microsoft.AspNetCore.Mvc;

namespace itembank_index_backend.Utils;

public class Http
{
    public static readonly BadRequestObjectResult ItembankSearchBadRequest = new(@"The following required conditions must be filled at least one:
        1. SubjectId
        2. ItemYears
        3. SearchTexts
        4. MustSearchTexts
        5. Ids");
    public static readonly NotFoundResult NotFound = new();
    public static readonly StatusCodeResult InternalServerError = new(StatusCodes.Status500InternalServerError);

    public static dynamic HandleNullResponse(dynamic response)
    {
        return response == null ? NotFound : response;
    }
}