using Accounting.Application.DTOs;
using Accounting.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>Import master data from a legacy SQL database (e.g. <c>belts</c>).</summary>
[ApiController]
[Route("api/companies/{companyId:int}/import")]
public sealed class CompanyImportController : ControllerBase
{
    private readonly BeltsImportService _import;

    public CompanyImportController(BeltsImportService import)
    {
        _import = import;
    }

    [HttpPost("belts")]
    public async Task<ActionResult<BeltsImportResultDto>> ImportFromBelts(
        int companyId,
        [FromBody] BeltsImportRequest? body,
        CancellationToken cancellationToken)
    {
        var req = body ?? new BeltsImportRequest();
        var result = await _import.ImportAsync(companyId, req, cancellationToken).ConfigureAwait(false);
        if (result.Message == "Company not found.")
            return NotFound(result);
        return Ok(result);
    }
}
