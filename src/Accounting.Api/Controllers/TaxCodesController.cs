using Accounting.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/companies/{companyId:int}/[controller]")]
public sealed class TaxCodesController : ControllerBase
{
    private readonly ITaxCodeQueryService _service;

    public TaxCodesController(ITaxCodeQueryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List(int companyId, CancellationToken cancellationToken)
    {
        return Ok(await _service.ListAsync(companyId, cancellationToken));
    }
}
