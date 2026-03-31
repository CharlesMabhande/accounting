using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class SupplierInvoiceService : ISupplierInvoiceService
{
    private readonly AccountingDbContext _db;
    private readonly IJournalPostingService _journal;

    public SupplierInvoiceService(AccountingDbContext db, IJournalPostingService journal)
    {
        _db = db;
        _journal = journal;
    }

    public async Task<OperationResult<CreatedEntityInfo>> CreateAsync(
        CreateSupplierInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0)
            return OperationResult<CreatedEntityInfo>.Fail("At least one line is required.");

        var supplier = await _db.Suppliers.FirstOrDefaultAsync(
            s => s.Id == request.SupplierId && s.CompanyId == request.CompanyId,
            cancellationToken);
        if (supplier is null)
            return OperationResult<CreatedEntityInfo>.Fail("Supplier not found.");

        TaxCode? taxCode = null;
        if (request.TaxCodeId.HasValue)
        {
            taxCode = await _db.TaxCodes.FirstOrDefaultAsync(
                t => t.Id == request.TaxCodeId.Value && t.CompanyId == request.CompanyId,
                cancellationToken);
            if (taxCode is null)
                return OperationResult<CreatedEntityInfo>.Fail("Tax code not found.");
        }

        var lines = new List<SupplierInvoiceLine>();
        var lineNo = 1;
        decimal subTotal = 0;
        foreach (var l in request.Lines)
        {
            var lineTotal = decimal.Round(l.Quantity * l.UnitCost, 2, MidpointRounding.AwayFromZero);
            subTotal += lineTotal;
            lines.Add(new SupplierInvoiceLine
            {
                LineNumber = lineNo++,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitCost = l.UnitCost,
                LineTotal = lineTotal,
                StockItemId = l.StockItemId,
                ExpenseAccountId = l.ExpenseAccountId
            });
        }

        var taxAmount = 0m;
        if (taxCode is not null)
            taxAmount = decimal.Round(subTotal * taxCode.RatePercent / 100m, 2, MidpointRounding.AwayFromZero);

        var total = subTotal + taxAmount;
        var docNo = await DocumentSequenceHelper.NextAsync(_db, request.CompanyId, "PI", "PI", cancellationToken);
        var auditNo = await DocumentSequenceHelper.NextAsync(_db, request.CompanyId, "AUDIT", "AUD", cancellationToken);

        var inv = new SupplierInvoice
        {
            CompanyId = request.CompanyId,
            SupplierId = request.SupplierId,
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

        _db.SupplierInvoices.Add(inv);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<CreatedEntityInfo>.Ok(new CreatedEntityInfo { Id = inv.Id, DocumentNumber = docNo });
    }

    public async Task<OperationResult<PostJournalInfo>> PostAsync(
        int invoiceId,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var inv = await _db.SupplierInvoices
            .Include(i => i.Supplier)
            .Include(i => i.Lines)
            .Include(i => i.TaxCode)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (inv is null)
            return OperationResult<PostJournalInfo>.Fail("Invoice not found.");
        if (inv.Status == DocumentStatus.Posted)
            return OperationResult<PostJournalInfo>.Fail("Invoice already posted.");

        var apAccountId = inv.Supplier.AccountsPayableAccountId
            ?? (await LedgerLookup.ByCodeAsync(_db, inv.CompanyId, "2000", cancellationToken))?.Id;
        if (apAccountId is null)
            return OperationResult<PostJournalInfo>.Fail("Accounts payable account is not configured.");

        var defaultExpense = await LedgerLookup.ByCodeAsync(_db, inv.CompanyId, "5100", cancellationToken);
        if (defaultExpense is null)
            return OperationResult<PostJournalInfo>.Fail("Default expense account (5100) not found.");

        var debitByAccount = new Dictionary<int, decimal>();
        foreach (var line in inv.Lines.OrderBy(l => l.LineNumber))
        {
            int debitAccountId;
            if (line.StockItemId.HasValue)
            {
                var item = await _db.StockItems.FirstAsync(s => s.Id == line.StockItemId!.Value, cancellationToken);
                debitAccountId = item.InventoryAccountId;
            }
            else
            {
                debitAccountId = line.ExpenseAccountId ?? defaultExpense.Id;
            }

            if (!debitByAccount.ContainsKey(debitAccountId))
                debitByAccount[debitAccountId] = 0;
            debitByAccount[debitAccountId] += line.LineTotal;
        }

        var journalLines = new List<PostJournalLineDto>();
        foreach (var kv in debitByAccount)
        {
            journalLines.Add(new PostJournalLineDto
            {
                LedgerAccountId = kv.Key,
                Debit = kv.Value,
                Credit = 0,
                Narration = "Purchase"
            });
        }

        if (inv.TaxAmount > 0)
        {
            var vatAccountId = inv.TaxCode?.InputTaxLedgerAccountId
                ?? (await LedgerLookup.ByCodeAsync(_db, inv.CompanyId, "2150", cancellationToken))?.Id;
            if (vatAccountId is null)
                return OperationResult<PostJournalInfo>.Fail("VAT input account not configured.");

            journalLines.Add(new PostJournalLineDto
            {
                LedgerAccountId = vatAccountId.Value,
                Debit = inv.TaxAmount,
                Credit = 0,
                Narration = "VAT input"
            });
        }

        journalLines.Add(new PostJournalLineDto
        {
            LedgerAccountId = apAccountId.Value,
            Debit = 0,
            Credit = inv.Total,
            Narration = $"AP {inv.DocumentNumber}",
            SupplierId = inv.SupplierId
        });

        var postReq = new PostJournalRequest
        {
            CompanyId = inv.CompanyId,
            EntryDate = inv.DocumentDate,
            Reference = inv.DocumentNumber,
            Description = "Supplier invoice",
            SourceModule = ModuleCode.AccountsPayable,
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
