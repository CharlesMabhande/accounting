using Accounting.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ReportingController : ControllerBase
{
    private readonly IReportingService _reporting;

    public ReportingController(IReportingService reporting)
    {
        _reporting = reporting;
    }

    [HttpGet("companies/{companyId:int}/trial-balance")]
    public async Task<IActionResult> TrialBalance(int companyId, [FromQuery] DateOnly asOf, CancellationToken cancellationToken)
    {
        var rows = await _reporting.GetTrialBalanceAsync(companyId, asOf, cancellationToken);
        return Ok(rows);
    }

    [HttpGet("companies/{companyId:int}/ledger/{ledgerAccountId:int}")]
    public async Task<IActionResult> Ledger(
        int companyId,
        int ledgerAccountId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var rows = await _reporting.GetLedgerAsync(companyId, ledgerAccountId, from, to, cancellationToken);
        return Ok(rows);
    }

    /// <summary>Excel trial balance (OpenXML workbook via DocumentFormat.OpenXml).</summary>
    [HttpGet("companies/{companyId:int}/trial-balance/export")]
    public async Task<IActionResult> ExportTrialBalance(
        int companyId,
        [FromQuery] DateOnly asOf,
        CancellationToken cancellationToken)
    {
        var bytes = await _reporting.ExportTrialBalanceExcelAsync(companyId, asOf, cancellationToken);
        var name = $"TrialBalance-{companyId}-{asOf:yyyy-MM-dd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }
}
