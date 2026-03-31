using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/companies/{companyId:int}/[controller]")]
public sealed class CustomersController : ControllerBase
{
    private readonly AccountingDbContext _db;
    private readonly IReportingService _reporting;

    public CustomersController(AccountingDbContext db, IReportingService reporting)
    {
        _db = db;
        _reporting = reporting;
    }

    /// <summary>Customer statement (posted invoices and receipts in period) — print from desktop.</summary>
    [HttpGet("{id:int}/statement")]
    public async Task<ActionResult<CustomerStatementDto>> GetStatement(
        int companyId,
        int id,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var fromDate = from ?? new DateOnly(toDate.Year, 1, 1);
        var dto = await _reporting.GetCustomerStatementAsync(companyId, id, fromDate, toDate, cancellationToken);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> List(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.Customers
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Code)
            .Select(x => new CustomerDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                AccountsReceivableAccountId = x.AccountsReceivableAccountId,
                AccountsReceivableAccountCode = x.AccountsReceivableAccount != null ? x.AccountsReceivableAccount.Code : null,
                AccountsReceivableAccountName = x.AccountsReceivableAccount != null ? x.AccountsReceivableAccount.Name : null,
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
    public async Task<ActionResult<CustomerDto>> Create(int companyId, [FromBody] UpsertCustomerRequest request, CancellationToken cancellationToken)
    {
        if (!await _db.Companies.AsNoTracking().AnyAsync(c => c.Id == companyId, cancellationToken))
            return NotFound("Company not found.");

        var code = (request.Code ?? "").Trim();
        var name = (request.Name ?? "").Trim();
        if (code.Length == 0 || name.Length == 0)
            return BadRequest("Code and name are required.");

        if (await _db.Customers.AnyAsync(x => x.CompanyId == companyId && x.Code == code, cancellationToken))
            return Conflict("A customer with this code already exists.");

        if (request.AccountsReceivableAccountId is int arId)
        {
            var arOk = await _db.LedgerAccounts.AsNoTracking()
                .AnyAsync(a => a.Id == arId && a.CompanyId == companyId && a.IsPostable, cancellationToken);
            if (!arOk)
                return BadRequest("AR ledger account must exist for this company and be postable.");
        }

        var entity = new Customer
        {
            CompanyId = companyId,
            Code = code,
            Name = name,
            AccountsReceivableAccountId = request.AccountsReceivableAccountId,
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

        _db.Customers.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, await MapDtoAsync(entity.Id, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Update(int companyId, int id, [FromBody] UpsertCustomerRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
        if (entity == null)
            return NotFound();

        var code = (request.Code ?? "").Trim();
        var name = (request.Name ?? "").Trim();
        if (code.Length == 0 || name.Length == 0)
            return BadRequest("Code and name are required.");

        if (await _db.Customers.AnyAsync(x => x.CompanyId == companyId && x.Code == code && x.Id != id, cancellationToken))
            return Conflict("A customer with this code already exists.");

        if (request.AccountsReceivableAccountId is int arId)
        {
            var arOk = await _db.LedgerAccounts.AsNoTracking()
                .AnyAsync(a => a.Id == arId && a.CompanyId == companyId && a.IsPostable, cancellationToken);
            if (!arOk)
                return BadRequest("AR ledger account must exist for this company and be postable.");
        }

        entity.Code = code;
        entity.Name = name;
        entity.AccountsReceivableAccountId = request.AccountsReceivableAccountId;
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
        var entity = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
        if (entity == null)
            return NotFound();

        var hasRefs = await _db.CustomerInvoices.AnyAsync(i => i.CustomerId == id, cancellationToken)
            || await _db.SalesOrders.AnyAsync(o => o.CustomerId == id, cancellationToken)
            || await _db.CashbookTransactions.AnyAsync(t => t.CustomerId == id, cancellationToken);

        if (hasRefs)
        {
            entity.IsActive = false;
            entity.ModifiedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { deactivated = true, message = "Customer has related documents; deactivated instead of deleted." });
        }

        _db.Customers.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private async Task<CustomerDto> MapDtoAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Customers.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CustomerDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                AccountsReceivableAccountId = x.AccountsReceivableAccountId,
                AccountsReceivableAccountCode = x.AccountsReceivableAccount != null ? x.AccountsReceivableAccount.Code : null,
                AccountsReceivableAccountName = x.AccountsReceivableAccount != null ? x.AccountsReceivableAccount.Name : null,
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
