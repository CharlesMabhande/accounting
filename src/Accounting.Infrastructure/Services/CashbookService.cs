using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class CashbookService : ICashbookService
{
    private readonly AccountingDbContext _db;
    private readonly IJournalPostingService _journal;

    public CashbookService(AccountingDbContext db, IJournalPostingService journal)
    {
        _db = db;
        _journal = journal;
    }

    public async Task<OperationResult<CreatedEntityInfo>> CreateAsync(
        CreateCashbookRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
            return OperationResult<CreatedEntityInfo>.Fail("Amount must be positive.");

        var bank = await _db.BankAccounts
            .Include(b => b.LedgerAccount)
            .FirstOrDefaultAsync(b => b.Id == request.BankAccountId && b.CompanyId == request.CompanyId, cancellationToken);
        if (bank is null)
            return OperationResult<CreatedEntityInfo>.Fail("Bank account not found.");

        var docNo = await DocumentSequenceHelper.NextAsync(_db, request.CompanyId, "CB", "CB", cancellationToken);
        var auditNo = await DocumentSequenceHelper.NextAsync(_db, request.CompanyId, "AUDIT", "AUD", cancellationToken);
        var tx = new CashbookTransaction
        {
            CompanyId = request.CompanyId,
            BankAccountId = request.BankAccountId,
            TransactionDate = request.TransactionDate,
            AuditTrailNumber = auditNo,
            Reference = string.IsNullOrWhiteSpace(request.Reference) ? docNo : request.Reference,
            Description = request.Description,
            Amount = request.Amount,
            IsReceipt = request.IsReceipt,
            CustomerId = request.CustomerId,
            SupplierId = request.SupplierId,
            Status = DocumentStatus.Draft
        };

        _db.CashbookTransactions.Add(tx);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<CreatedEntityInfo>.Ok(new CreatedEntityInfo { Id = tx.Id, DocumentNumber = docNo });
    }

    public async Task<OperationResult<PostJournalInfo>> PostAsync(
        int transactionId,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CashbookTransactions
            .Include(t => t.BankAccount).ThenInclude(b => b.LedgerAccount)
            .Include(t => t.Customer)
            .Include(t => t.Supplier)
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

        if (row is null)
            return OperationResult<PostJournalInfo>.Fail("Cashbook transaction not found.");
        if (row.Status == DocumentStatus.Posted)
            return OperationResult<PostJournalInfo>.Fail("Already posted.");

        var bankLedgerId = row.BankAccount.LedgerAccountId;
        var lines = new List<PostJournalLineDto>();

        if (row.IsReceipt)
        {
            lines.Add(new PostJournalLineDto
            {
                LedgerAccountId = bankLedgerId,
                Debit = row.Amount,
                Credit = 0,
                Narration = row.Description
            });

            if (row.CustomerId.HasValue)
            {
                var arId = row.Customer!.AccountsReceivableAccountId
                    ?? (await LedgerLookup.ByCodeAsync(_db, row.CompanyId, "1200", cancellationToken))?.Id;
                if (arId is null)
                    return OperationResult<PostJournalInfo>.Fail("AR account not configured.");

                lines.Add(new PostJournalLineDto
                {
                    LedgerAccountId = arId.Value,
                    Debit = 0,
                    Credit = row.Amount,
                    Narration = "Customer receipt",
                    CustomerId = row.CustomerId
                });
            }
            else
            {
                var misc = await LedgerLookup.ByCodeAsync(_db, row.CompanyId, "4000", cancellationToken);
                if (misc is null)
                    return OperationResult<PostJournalInfo>.Fail("Miscellaneous income account not found.");

                lines.Add(new PostJournalLineDto
                {
                    LedgerAccountId = misc.Id,
                    Debit = 0,
                    Credit = row.Amount,
                    Narration = "Receipt"
                });
            }
        }
        else
        {
            lines.Add(new PostJournalLineDto
            {
                LedgerAccountId = bankLedgerId,
                Debit = 0,
                Credit = row.Amount,
                Narration = row.Description
            });

            if (row.SupplierId.HasValue)
            {
                var apId = row.Supplier!.AccountsPayableAccountId
                    ?? (await LedgerLookup.ByCodeAsync(_db, row.CompanyId, "2000", cancellationToken))?.Id;
                if (apId is null)
                    return OperationResult<PostJournalInfo>.Fail("AP account not configured.");

                lines.Add(new PostJournalLineDto
                {
                    LedgerAccountId = apId.Value,
                    Debit = row.Amount,
                    Credit = 0,
                    Narration = "Supplier payment",
                    SupplierId = row.SupplierId
                });
            }
            else
            {
                var opex = await LedgerLookup.ByCodeAsync(_db, row.CompanyId, "5100", cancellationToken);
                if (opex is null)
                    return OperationResult<PostJournalInfo>.Fail("Operating expense account not found.");

                lines.Add(new PostJournalLineDto
                {
                    LedgerAccountId = opex.Id,
                    Debit = row.Amount,
                    Credit = 0,
                    Narration = "Payment"
                });
            }
        }

        var postReq = new PostJournalRequest
        {
            CompanyId = row.CompanyId,
            EntryDate = row.TransactionDate,
            Reference = row.Reference,
            Description = "Cashbook",
            SourceModule = ModuleCode.CashBook,
            SourceDocumentId = row.Id,
            Lines = lines
        };

        var jr = await _journal.PostJournalAsync(postReq, cancellationToken);
        if (!jr.Success)
            return OperationResult<PostJournalInfo>.Fail(jr.Errors.ToArray());

        row.JournalEntryId = jr.JournalEntryId;
        row.Status = DocumentStatus.Posted;
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<PostJournalInfo>.Ok(new PostJournalInfo
        {
            JournalEntryId = jr.JournalEntryId!.Value,
            EntryNumber = jr.EntryNumber!
        });
    }
}
