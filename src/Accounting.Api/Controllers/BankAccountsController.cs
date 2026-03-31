using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/companies/{companyId:int}/[controller]")]
public sealed class BankAccountsController : ControllerBase
{
    private readonly AccountingDbContext _db;

    public BankAccountsController(AccountingDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BankAccountDto>>> List(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.BankAccounts
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Code)
            .Select(x => new BankAccountDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                LedgerAccountId = x.LedgerAccountId,
                LedgerAccountCode = x.LedgerAccount.Code,
                LedgerAccountName = x.LedgerAccount.Name,
                CurrencyCode = x.CurrencyCode,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<BankAccountDto>> Create(int companyId, [FromBody] UpsertBankAccountRequest request, CancellationToken cancellationToken)
    {
        if (!await _db.Companies.AsNoTracking().AnyAsync(c => c.Id == companyId, cancellationToken))
            return NotFound("Company not found.");

        var code = (request.Code ?? "").Trim();
        var name = (request.Name ?? "").Trim();
        if (code.Length == 0 || name.Length == 0)
            return BadRequest("Code and name are required.");

        if (await _db.BankAccounts.AnyAsync(x => x.CompanyId == companyId && x.Code == code, cancellationToken))
            return Conflict("A bank account with this code already exists.");

        var ledgerOk = await _db.LedgerAccounts.AsNoTracking()
            .AnyAsync(a => a.Id == request.LedgerAccountId && a.CompanyId == companyId && a.IsPostable, cancellationToken);
        if (!ledgerOk)
            return BadRequest("Ledger account must exist for this company and be postable.");

        var entity = new BankAccount
        {
            CompanyId = companyId,
            Code = code,
            Name = name,
            LedgerAccountId = request.LedgerAccountId,
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "USD" : request.CurrencyCode.Trim().ToUpperInvariant(),
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.BankAccounts.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = await _db.BankAccounts.AsNoTracking()
            .Where(x => x.Id == entity.Id)
            .Select(x => new BankAccountDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                LedgerAccountId = x.LedgerAccountId,
                LedgerAccountCode = x.LedgerAccount.Code,
                LedgerAccountName = x.LedgerAccount.Name,
                CurrencyCode = x.CurrencyCode,
                IsActive = x.IsActive
            })
            .FirstAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BankAccountDto>> Update(int companyId, int id, [FromBody] UpsertBankAccountRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.BankAccounts.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
        if (entity == null)
            return NotFound();

        var code = (request.Code ?? "").Trim();
        var name = (request.Name ?? "").Trim();
        if (code.Length == 0 || name.Length == 0)
            return BadRequest("Code and name are required.");

        if (await _db.BankAccounts.AnyAsync(x => x.CompanyId == companyId && x.Code == code && x.Id != id, cancellationToken))
            return Conflict("A bank account with this code already exists.");

        var ledgerOk = await _db.LedgerAccounts.AsNoTracking()
            .AnyAsync(a => a.Id == request.LedgerAccountId && a.CompanyId == companyId && a.IsPostable, cancellationToken);
        if (!ledgerOk)
            return BadRequest("Ledger account must exist for this company and be postable.");

        entity.Code = code;
        entity.Name = name;
        entity.LedgerAccountId = request.LedgerAccountId;
        entity.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "USD" : request.CurrencyCode.Trim().ToUpperInvariant();
        entity.IsActive = request.IsActive;
        entity.ModifiedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        var dto = await _db.BankAccounts.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new BankAccountDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                LedgerAccountId = x.LedgerAccountId,
                LedgerAccountCode = x.LedgerAccount.Code,
                LedgerAccountName = x.LedgerAccount.Name,
                CurrencyCode = x.CurrencyCode,
                IsActive = x.IsActive
            })
            .FirstAsync(cancellationToken);

        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int companyId, int id, CancellationToken cancellationToken)
    {
        var entity = await _db.BankAccounts.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
        if (entity == null)
            return NotFound();

        if (await _db.CashbookTransactions.AnyAsync(t => t.BankAccountId == id, cancellationToken))
        {
            entity.IsActive = false;
            entity.ModifiedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { deactivated = true, message = "Bank account has cashbook history; it was deactivated instead of deleted." });
        }

        _db.BankAccounts.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
