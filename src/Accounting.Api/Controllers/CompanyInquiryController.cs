using Accounting.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Api.Controllers;

/// <summary>Read-only workspace lists for desktop / UI grids (company-scoped).</summary>
[ApiController]
[Route("api/companies/{companyId:int}/inquiry")]
public sealed class CompanyInquiryController : ControllerBase
{
    private readonly AccountingDbContext _db;

    public CompanyInquiryController(AccountingDbContext db)
    {
        _db = db;
    }

    [HttpGet("journal-entries")]
    public async Task<IActionResult> JournalEntries(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.JournalEntries
            .AsNoTracking()
            .Where(j => j.CompanyId == companyId)
            .OrderByDescending(j => j.EntryDate)
            .Take(500)
            .Select(j => new
            {
                j.Id,
                j.EntryNumber,
                j.AuditTrailNumber,
                j.EntryDate,
                j.Reference,
                j.Description,
                Status = j.Status.ToString(),
                SourceModule = j.SourceModule.ToString(),
                j.PostedAtUtc
            })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("customer-invoices")]
    public async Task<IActionResult> CustomerInvoices(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.CustomerInvoices
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.DocumentDate)
            .Take(500)
            .Select(x => new
            {
                x.Id,
                x.DocumentNumber,
                x.AuditTrailNumber,
                x.DocumentDate,
                CustomerCode = x.Customer.Code,
                CustomerName = x.Customer.Name,
                Status = x.Status.ToString(),
                x.SubTotal,
                x.TaxAmount,
                x.Total
            })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("supplier-invoices")]
    public async Task<IActionResult> SupplierInvoices(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.SupplierInvoices
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.DocumentDate)
            .Take(500)
            .Select(x => new
            {
                x.Id,
                x.DocumentNumber,
                x.AuditTrailNumber,
                x.DocumentDate,
                SupplierCode = x.Supplier.Code,
                SupplierName = x.Supplier.Name,
                Status = x.Status.ToString(),
                x.SubTotal,
                x.TaxAmount,
                x.Total
            })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("sales-orders")]
    public async Task<IActionResult> SalesOrders(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.SalesOrders
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.OrderDate)
            .Take(500)
            .Select(x => new
            {
                x.Id,
                x.OrderNumber,
                x.OrderDate,
                CustomerCode = x.Customer.Code,
                Status = x.Status.ToString()
            })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("purchase-orders")]
    public async Task<IActionResult> PurchaseOrders(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.OrderDate)
            .Take(500)
            .Select(x => new
            {
                x.Id,
                x.OrderNumber,
                x.OrderDate,
                SupplierCode = x.Supplier.Code,
                Status = x.Status.ToString()
            })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("goods-received-notes")]
    public async Task<IActionResult> GoodsReceivedNotes(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.GoodsReceivedNotes
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.ReceivedDate)
            .Take(500)
            .Select(x => new
            {
                x.Id,
                x.GrnNumber,
                x.ReceivedDate,
                SupplierCode = x.Supplier.Code,
                WarehouseCode = x.Warehouse.Code,
                Status = x.Status.ToString()
            })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("cashbook-transactions")]
    public async Task<IActionResult> CashbookTransactions(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.CashbookTransactions
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.TransactionDate)
            .Take(500)
            .Select(x => new
            {
                x.Id,
                BankCode = x.BankAccount.Code,
                x.AuditTrailNumber,
                x.TransactionDate,
                x.Reference,
                x.Description,
                x.Amount,
                x.IsReceipt,
                Status = x.Status.ToString()
            })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("fixed-assets")]
    public async Task<IActionResult> FixedAssets(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.FixedAssets
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.AssetNumber)
            .Take(500)
            .Select(x => new
            {
                x.Id,
                x.AssetNumber,
                x.Description,
                x.AcquisitionDate,
                x.Cost,
                x.IsDisposed
            })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("payroll-runs")]
    public async Task<IActionResult> PayrollRuns(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.PayrollRuns
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.PeriodEnd)
            .Take(200)
            .Select(x => new
            {
                x.Id,
                x.RunNumber,
                x.PeriodStart,
                x.PeriodEnd,
                x.IsPosted,
                x.GrossWages,
                x.TaxWithheld,
                x.NetPay
            })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("cost-centres")]
    public async Task<IActionResult> CostCentres(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.CostCentres
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, x.Name, x.IsActive })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("project-jobs")]
    public async Task<IActionResult> ProjectJobs(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.ProjectJobs
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, x.Name, x.IsClosed })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("customers")]
    public async Task<IActionResult> Customers(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.Customers
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, x.Name, x.CurrencyCode, x.IsActive })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("suppliers")]
    public async Task<IActionResult> Suppliers(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.Suppliers
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, x.Name, x.CurrencyCode, x.IsActive })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("bank-accounts")]
    public async Task<IActionResult> BankAccounts(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.BankAccounts
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, x.Name, x.CurrencyCode, x.IsActive })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("stock-movements")]
    public async Task<IActionResult> StockMovements(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.StockMovements
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.MovementDate)
            .Take(500)
            .Select(x => new
            {
                x.Id,
                WarehouseCode = x.Warehouse.Code,
                StockCode = x.StockItem.Code,
                StockDescription = x.StockItem.Description,
                MovementType = x.MovementType.ToString(),
                x.MovementDate,
                x.Quantity,
                x.UnitCost,
                x.Reference,
                x.SourceDocumentId
            })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("document-sequences")]
    public async Task<IActionResult> DocumentSequences(int companyId, CancellationToken cancellationToken)
    {
        var rows = await _db.DocumentSequences
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Key)
            .Select(x => new { x.Id, x.Key, x.NextValue })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }
}
