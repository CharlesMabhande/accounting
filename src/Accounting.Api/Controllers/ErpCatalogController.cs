using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>Global ERP catalog: currencies and capability summary.</summary>
[ApiController]
[Route("api/erp")]
public sealed class ErpCatalogController : ControllerBase
{
    private readonly IErpPlatformService _erp;

    public ErpCatalogController(IErpPlatformService erp)
    {
        _erp = erp;
    }

    [HttpGet("currencies")]
    public Task<IReadOnlyList<CurrencyDto>> Currencies(CancellationToken cancellationToken) =>
        _erp.ListCurrenciesAsync(cancellationToken);

    [HttpPost("currencies")]
    public async Task<IActionResult> UpsertCurrency([FromBody] UpsertCurrencyRequest request, CancellationToken cancellationToken)
    {
        var r = await _erp.UpsertCurrencyAsync(request, cancellationToken);
        return r.Success ? Ok(new { id = r.Data }) : BadRequest(new { errors = r.Errors });
    }

    [HttpDelete("currencies/{id:int}")]
    public async Task<IActionResult> DeleteCurrency(int id, CancellationToken cancellationToken)
    {
        var r = await _erp.DeleteCurrencyAsync(id, cancellationToken);
        return r.Success ? NoContent() : BadRequest(new { errors = r.Errors });
    }

    [HttpGet("capabilities")]
    public ActionResult<IReadOnlyList<ErpCapabilityDto>> Capabilities() =>
        Ok(_erp.GetErpCapabilities());
}
