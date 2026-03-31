using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class CustomerInvoiceService : ICustomerInvoiceService
{
    private readonly AccountingDbContext _db;
    private readonly IJournalPostingService _journal;

    public CustomerInvoiceService(AccountingDbContext db, IJournalPostingService journal)
    {
        _db = db;
        _journal = journal;
    }

    public async Task<OperationResult<CreatedEntityInfo>> CreateAsync(
        CreateCustomerInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0)
            return OperationResult<CreatedEntityInfo>.Fail("At least one line is required.");

        var customer = await _db.Customers.FirstOrDefaultAsync(
            c => c.Id == request.CustomerId && c.CompanyId == request.CompanyId,
            cancellationToken);
        if (customer is null)
            return OperationResult<CreatedEntityInfo>.Fail("Customer not found.");

        TaxCode? taxCode = null;
        if (request.TaxCodeId.HasValue)
        {
            taxCode = await _db.TaxCodes.FirstOrDefaultAsync(
                t => t.Id == request.TaxCodeId.Value && t.CompanyId == request.CompanyId,
                cancellationToken);
            if (taxCode is null)
                return OperationResult<CreatedEntityInfo>.Fail("Tax code not found.");
        }

        var lines = new List<CustomerInvoiceLine>();
        var lineNo = 1;
        decimal subTotal = 0;
        foreach (var l in request.Lines)
        {
            var lineTotal = decimal.Round(l.Quantity * l.UnitPrice, 2, MidpointRounding.AwayFromZero);
            subTotal += lineTotal;
            lines.Add(new CustomerInvoiceLine
            {
                LineNumber = lineNo++,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = lineTotal,
                StockItemId = l.StockItemId,
                RevenueAccountId = l.RevenueAccountId
            });
        }

        var taxAmount = 0m;
        if (taxCode is not null)
            taxAmount = decimal.Round(subTotal * taxCode.RatePercent / 100m, 2, MidpointRounding.AwayFromZero);

        var total = subTotal + taxAmount;
        var docNo = await DocumentSequenceHelper.NextAsync(_db, request.CompanyId, "SI", "SI", cancellationToken);
        var auditNo = await DocumentSequenceHelper.NextAsync(_db, request.CompanyId, "AUDIT", "AUD", cancellationToken);

        var inv = new CustomerInvoice
        {
            CompanyId = request.CompanyId,
            CustomerId = request.CustomerId,
            DocumentNumber = docNo,
            AuditTrailNumber = auditNo,
            DocumentDate = request.DocumentDate,
            Status = DocumentStatus.Draft,
            SubTotal = subTotal,
            TaxAmount = taxAmount,
            Total = total,
            TaxCodeId = request.TaxCodeId,
            Lines = lines
        };

        _db.CustomerInvoices.Add(inv);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<CreatedEntityInfo>.Ok(new CreatedEntityInfo { Id = inv.Id, DocumentNumber = docNo });
    }

    public async Task<OperationResult<PostJournalInfo>> PostAsync(
        int invoiceId,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var inv = await _db.CustomerInvoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.TaxCode)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (inv is null)
            return OperationResult<PostJournalInfo>.Fail("Invoice not found.");
        if (inv.Status == DocumentStatus.Posted)
            return OperationResult<PostJournalInfo>.Fail("Invoice already posted.");

        var arAccountId = inv.Customer.AccountsReceivableAccountId
            ?? (await LedgerLookup.ByCodeAsync(_db, inv.CompanyId, "1200", cancellationToken))?.Id;

        if (arAccountId is null)
            return OperationResult<PostJournalInfo>.Fail("Accounts receivable account is not configured.");

        var defaultRevenue = await LedgerLookup.ByCodeAsync(_db, inv.CompanyId, "4000", cancellationToken);
        if (defaultRevenue is null)
            return OperationResult<PostJournalInfo>.Fail("Default sales account (4000) not found.");

        var revenueByAccount = new Dictionary<int, decimal>();
        foreach (var line in inv.Lines.OrderBy(l => l.LineNumber))
        {
            var revId = line.RevenueAccountId ?? defaultRevenue.Id;
            if (!revenueByAccount.ContainsKey(revId))
                revenueByAccount[revId] = 0;
            revenueByAccount[revId] += line.LineTotal;
        }

        var journalLines = new List<PostJournalLineDto>
        {
            new()
            {
                LedgerAccountId = arAccountId.Value,
                Debit = inv.Total,
                Credit = 0,
                Narration = $"AR {inv.DocumentNumber}",
                CustomerId = inv.CustomerId
            }
        };

        foreach (var kv in revenueByAccount)
        {
            journalLines.Add(new PostJournalLineDto
            {
                LedgerAccountId = kv.Key,
                Debit = 0,
                Credit = kv.Value,
                Narration = "Sales"
            });
        }

        if (inv.TaxAmount > 0)
        {
            var vatAccountId = inv.TaxCode?.OutputTaxLedgerAccountId
                ?? (await LedgerLookup.ByCodeAsync(_db, inv.CompanyId, "2200", cancellationToken))?.Id;
            if (vatAccountId is null)
                return OperationResult<PostJournalInfo>.Fail("VAT output account not configured.");

            journalLines.Add(new PostJournalLineDto
            {
                LedgerAccountId = vatAccountId.Value,
                Debit = 0,
                Credit = inv.TaxAmount,
                Narration = "VAT output"
            });
        }

        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.CompanyId == inv.CompanyId, cancellationToken);
        if (warehouse is not null)
        {
            foreach (var line in inv.Lines.Where(l => l.StockItemId.HasValue))
            {
                var ws = await _db.WarehouseStocks.FirstOrDefaultAsync(
                    x => x.WarehouseId == warehouse.Id && x.StockItemId == line.StockItemId!.Value,
                    cancellationToken);
                var unitCost = ws?.LastUnitCost ?? 0m;
                if (unitCost <= 0) continue;

                var cogs = decimal.Round(line.Quantity * unitCost, 2, MidpointRounding.AwayFromZero);
                if (cogs <= 0) continue;

                var stockItem = await _db.StockItems.FirstAsync(s => s.Id == line.StockItemId!.Value, cancellationToken);
                var cos = await LedgerLookup.ByCodeAsync(_db, inv.CompanyId, "5000", cancellationToken);
                if (cos is null) continue;

                journalLines.Add(new PostJournalLineDto
                {
                    LedgerAccountId = cos.Id,
                    Debit = cogs,
                    Credit = 0,
                    Narration = "COGS"
                });
                journalLines.Add(new PostJournalLineDto
                {
                    LedgerAccountId = stockItem.InventoryAccountId,
                    Debit = 0,
                    Credit = cogs,
                    Narration = "Inventory"
                });
            }
        }

        var postReq = new PostJournalRequest
        {
            CompanyId = inv.CompanyId,
            EntryDate = inv.DocumentDate,
            Reference = inv.DocumentNumber,
            Description = "Customer invoice",
            SourceModule = ModuleCode.AccountsReceivable,
            SourceDocumentId = inv.Id,
            Lines = journalLines
        };

        var jr = await _journal.PostJournalAsync(postReq, cancellationToken);
        if (!jr.Success)
            return OperationResult<PostJournalInfo>.Fail(jr.Errors.ToArray());

        inv.JournalEntryId = jr.JournalEntryId;
        inv.Status = DocumentStatus.Posted;
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<PostJournalInfo>.Ok(new PostJournalInfo
        {
            JournalEntryId = jr.JournalEntryId!.Value,
            EntryNumber = jr.EntryNumber!
        });
    }
}
