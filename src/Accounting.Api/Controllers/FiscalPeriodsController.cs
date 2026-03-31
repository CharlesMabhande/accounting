using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>Fiscal period open/close (period locking for posting).</summary>
[ApiController]
[Route("api/companies/{companyId:int}/[controller]")]
public sealed class FiscalPeriodsController : ControllerBase
{
    private readonly IFiscalPeriodControlService _periods;

    public FiscalPeriodsController(IFiscalPeriodControlService periods)
    {
        _periods = periods;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FiscalPeriodDto>>> List(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _periods.ListAsync(companyId, cancellationToken);
        return Ok(rows);
    }

    [HttpPost("{periodId:int}/close")]
    public async Task<IActionResult> Close(int companyId, int periodId, CancellationToken cancellationToken)
    {
        var result = await _periods.ClosePeriodAsync(companyId, periodId, cancellationToken);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{periodId:int}/reopen")]
    public async Task<IActionResult> Reopen(int companyId, int periodId, CancellationToken cancellationToken)
    {
        var result = await _periods.ReopenPeriodAsync(companyId, periodId, cancellationToken);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
