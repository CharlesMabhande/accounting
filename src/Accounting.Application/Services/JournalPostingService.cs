using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Services;

/// <summary>Double-entry validation and journal persistence (Debit = Credit).</summary>
public sealed class JournalPostingService : IJournalPostingService
{
    private readonly IJournalPostingRepository _repository;

    public JournalPostingService(IJournalPostingRepository repository)
    {
        _repository = repository;
    }

    public async Task<JournalPostingResult> PostJournalAsync(PostJournalRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count < 2)
            return JournalPostingResult.Fail("A journal must contain at least two lines.");

        var period = await _repository.GetOpenPeriodForDateAsync(request.CompanyId, request.EntryDate, cancellationToken);
        if (period is null)
            return JournalPostingResult.Fail("No open fiscal period exists for this date.");

        if (period.IsClosed)
            return JournalPostingResult.Fail("The fiscal period is closed.");

        var accountIds = request.Lines.Select(l => l.LedgerAccountId).Distinct().ToList();
        var accounts = await _repository.GetAccountsAsync(request.CompanyId, accountIds, cancellationToken);
        if (accounts.Count != accountIds.Count)
            return JournalPostingResult.Fail("One or more ledger accounts were not found for this company.");

        decimal totalDebit = 0, totalCredit = 0;
        var lineNumber = 1;
        var journalLines = new List<JournalLine>();

        foreach (var line in request.Lines)
        {
            if (line.Debit < 0 || line.Credit < 0)
                return JournalPostingResult.Fail("Debit and credit amounts cannot be negative.");

            if (line.Debit > 0 && line.Credit > 0)
                return JournalPostingResult.Fail("Each line must be either debit or credit, not both.");

            if (line.Debit == 0 && line.Credit == 0)
                return JournalPostingResult.Fail("Each line must have a debit or credit amount.");

            if (!accounts.TryGetValue(line.LedgerAccountId, out var account))
                return JournalPostingResult.Fail($"Unknown account id {line.LedgerAccountId}.");

            if (!account.IsPostable)
                return JournalPostingResult.Fail($"Account {account.Code} is not postable.");

            totalDebit += line.Debit;
            totalCredit += line.Credit;

            journalLines.Add(new JournalLine
            {
                LineNumber = lineNumber++,
                LedgerAccountId = line.LedgerAccountId,
                Debit = line.Debit,
                Credit = line.Credit,
                Narration = line.Narration,
                CustomerId = line.CustomerId,
                SupplierId = line.SupplierId
            });
        }

        if (totalDebit != totalCredit)
            return JournalPostingResult.Fail($"Journal is not balanced. Debits {totalDebit} != Credits {totalCredit}.");

        var entryNumber = await _repository.GetNextJournalEntryNumberAsync(request.CompanyId, cancellationToken);
        var auditNo = await _repository.GetNextAuditTrailNumberAsync(request.CompanyId, cancellationToken);
        var entry = new JournalEntry
        {
            CompanyId = request.CompanyId,
            FiscalPeriodId = period.Id,
            EntryNumber = entryNumber,
            AuditTrailNumber = auditNo,
            EntryDate = request.EntryDate,
            Reference = request.Reference,
            Description = request.Description,
            Status = DocumentStatus.Posted,
            SourceModule = request.SourceModule,
            SourceDocumentId = request.SourceDocumentId,
            PostedAtUtc = DateTime.UtcNow,
            Lines = journalLines
        };

        await _repository.AddJournalAsync(entry, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return JournalPostingResult.Ok(entry.Id, entry.EntryNumber);
    }
}
