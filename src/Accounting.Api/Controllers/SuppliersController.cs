using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/companies/{companyId:int}/[controller]")]
public sealed class SuppliersController : ControllerBase
{
    private readonly AccountingDbContext _db;

    public SuppliersController(AccountingDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierDto>>> List(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.Suppliers
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Code)
            .Select(x => new SupplierDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                AccountsPayableAccountId = x.AccountsPayableAccountId,
                AccountsPayableAccountCode = x.AccountsPayableAccount != null ? x.AccountsPayableAccount.Code : null,
                AccountsPayableAccountName = x.AccountsPayableAccount != null ? x.AccountsPayableAccount.Name : null,
                CurrencyCode = x.CurrencyCode,
                IsActive = x.IsActive,
                ContactName = x.ContactName,
                Phone = x.Phone,
                Email = x.Email,
                PhysicalAddress1 = x.PhysicalAddress1,
                PhysicalAddress2 = x.PhysicalAddress2,
                PhysicalAddress3 = x.PhysicalAddress3,
                PhysicalCity = x.PhysicalCity,
                PostalAddress1 = x.PostalAddress1,
                PostalAddress2 = x.PostalAddress2,
                PostalAddress3 = x.PostalAddress3,
                PostalCode = x.PostalCode,
                TaxNumber = x.TaxNumber,
                RegistrationNumber = x.RegistrationNumber,
                CreditLimit = x.CreditLimit,
                OnHold = x.OnHold
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create(int companyId, [FromBody] UpsertSupplierRequest request, CancellationToken cancellationToken)
    {
        if (!await _db.Companies.AsNoTracking().AnyAsync(c => c.Id == companyId, cancellationToken))
            return NotFound("Company not found.");

        var code = (request.Code ?? "").Trim();
        var name = (request.Name ?? "").Trim();
        if (code.Length == 0 || name.Length == 0)
            return BadRequest("Code and name are required.");

        if (await _db.Suppliers.AnyAsync(x => x.CompanyId == companyId && x.Code == code, cancellationToken))
            return Conflict("A supplier with this code already exists.");

        if (request.AccountsPayableAccountId is int apId)
        {
            var apOk = await _db.LedgerAccounts.AsNoTracking()
                .AnyAsync(a => a.Id == apId && a.CompanyId == companyId && a.IsPostable, cancellationToken);
            if (!apOk)
                return BadRequest("AP ledger account must exist for this company and be postable.");
        }

        var entity = new Supplier
        {
            CompanyId = companyId,
            Code = code,
            Name = name,
            AccountsPayableAccountId = request.AccountsPayableAccountId,
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "USD" : request.CurrencyCode.Trim().ToUpperInvariant(),
            IsActive = request.IsActive,
            ContactName = TrimOrNull(request.ContactName),
            Phone = TrimOrNull(request.Phone),
            Email = TrimOrNull(request.Email),
            PhysicalAddress1 = TrimOrNull(request.PhysicalAddress1),
            PhysicalAddress2 = TrimOrNull(request.PhysicalAddress2),
            PhysicalAddress3 = TrimOrNull(request.PhysicalAddress3),
            PhysicalCity = TrimOrNull(request.PhysicalCity),
            PostalAddress1 = TrimOrNull(request.PostalAddress1),
            PostalAddress2 = TrimOrNull(request.PostalAddress2),
            PostalAddress3 = TrimOrNull(request.PostalAddress3),
            PostalCode = TrimOrNull(request.PostalCode),
            TaxNumber = TrimOrNull(request.TaxNumber),
            RegistrationNumber = TrimOrNull(request.RegistrationNumber),
            CreditLimit = request.CreditLimit,
            OnHold = request.OnHold,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Suppliers.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, await MapDtoAsync(entity.Id, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SupplierDto>> Update(int companyId, int id, [FromBody] UpsertSupplierRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
        if (entity == null)
            return NotFound();

        var code = (request.Code ?? "").Trim();
        var name = (request.Name ?? "").Trim();
        if (code.Length == 0 || name.Length == 0)
            return BadRequest("Code and name are required.");

        if (await _db.Suppliers.AnyAsync(x => x.CompanyId == companyId && x.Code == code && x.Id != id, cancellationToken))
            return Conflict("A supplier with this code already exists.");

        if (request.AccountsPayableAccountId is int apId)
        {
            var apOk = await _db.LedgerAccounts.AsNoTracking()
                .AnyAsync(a => a.Id == apId && a.CompanyId == companyId && a.IsPostable, cancellationToken);
            if (!apOk)
                return BadRequest("AP ledger account must exist for this company and be postable.");
        }

        entity.Code = code;
        entity.Name = name;
        entity.AccountsPayableAccountId = request.AccountsPayableAccountId;
        entity.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "USD" : request.CurrencyCode.Trim().ToUpperInvariant();
        entity.IsActive = request.IsActive;
        entity.ContactName = TrimOrNull(request.ContactName);
        entity.Phone = TrimOrNull(request.Phone);
        entity.Email = TrimOrNull(request.Email);
        entity.PhysicalAddress1 = TrimOrNull(request.PhysicalAddress1);
        entity.PhysicalAddress2 = TrimOrNull(request.PhysicalAddress2);
        entity.PhysicalAddress3 = TrimOrNull(request.PhysicalAddress3);
        entity.PhysicalCity = TrimOrNull(request.PhysicalCity);
        entity.PostalAddress1 = TrimOrNull(request.PostalAddress1);
        entity.PostalAddress2 = TrimOrNull(request.PostalAddress2);
        entity.PostalAddress3 = TrimOrNull(request.PostalAddress3);
        entity.PostalCode = TrimOrNull(request.PostalCode);
        entity.TaxNumber = TrimOrNull(request.TaxNumber);
        entity.RegistrationNumber = TrimOrNull(request.RegistrationNumber);
        entity.CreditLimit = request.CreditLimit;
        entity.OnHold = request.OnHold;
        entity.ModifiedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(await MapDtoAsync(id, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int companyId, int id, CancellationToken cancellationToken)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
        if (entity == null)
            return NotFound();

        var hasRefs = await _db.SupplierInvoices.AnyAsync(i => i.SupplierId == id, cancellationToken)
            || await _db.PurchaseOrders.AnyAsync(o => o.SupplierId == id, cancellationToken)
            || await _db.CashbookTransactions.AnyAsync(t => t.SupplierId == id, cancellationToken);

        if (hasRefs)
        {
            entity.IsActive = false;
            entity.ModifiedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { deactivated = true, message = "Supplier has related documents; deactivated instead of deleted." });
        }

        _db.Suppliers.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private async Task<SupplierDto> MapDtoAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Suppliers.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new SupplierDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                AccountsPayableAccountId = x.AccountsPayableAccountId,
                AccountsPayableAccountCode = x.AccountsPayableAccount != null ? x.AccountsPayableAccount.Code : null,
                AccountsPayableAccountName = x.AccountsPayableAccount != null ? x.AccountsPayableAccount.Name : null,
                CurrencyCode = x.CurrencyCode,
                IsActive = x.IsActive,
                ContactName = x.ContactName,
                Phone = x.Phone,
                Email = x.Email,
                PhysicalAddress1 = x.PhysicalAddress1,
                PhysicalAddress2 = x.PhysicalAddress2,
                PhysicalAddress3 = x.PhysicalAddress3,
                PhysicalCity = x.PhysicalCity,
                PostalAddress1 = x.PostalAddress1,
                PostalAddress2 = x.PostalAddress2,
                PostalAddress3 = x.PostalAddress3,
                PostalCode = x.PostalCode,
                TaxNumber = x.TaxNumber,
                RegistrationNumber = x.RegistrationNumber,
                CreditLimit = x.CreditLimit,
                OnHold = x.OnHold
            })
            .FirstAsync(cancellationToken);
    }
}
