using Accounting.Application.DTOs;
using Accounting.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/companies/{companyId:int}/[controller]")]
public sealed class ChartOfAccountsController : ControllerBase
{
    private readonly AccountingDbContext _db;

    public ChartOfAccountsController(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>Postable ledger accounts for bank AR/AP links and other master data.</summary>
    [HttpGet("postable-accounts")]
    public async Task<ActionResult<IReadOnlyList<LedgerAccountOptionDto>>> PostableAccounts(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.LedgerAccounts
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.IsPostable)
            .OrderBy(a => a.Code)
            .Select(a => new LedgerAccountOptionDto
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<object>>> Get(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.LedgerAccounts
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .OrderBy(a => a.Code)
            .Select(a => new
            {
                a.Id,
                a.Code,
                a.Name,
                AccountType = a.AccountType.ToString(),
                a.IsPostable,
                a.CurrencyCode
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }
}
