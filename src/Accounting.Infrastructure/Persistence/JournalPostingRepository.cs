using Accounting.Application.Abstractions;
using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Persistence;

public sealed class JournalPostingRepository : IJournalPostingRepository
{
    private readonly AccountingDbContext _db;

    public JournalPostingRepository(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<FiscalPeriod?> GetOpenPeriodForDateAsync(int companyId, DateOnly date, CancellationToken cancellationToken)
    {
        return await _db.FiscalPeriods
            .AsNoTracking()
            .Include(p => p.FiscalYear)
            .Where(p => p.FiscalYear.CompanyId == companyId && !p.IsClosed && !p.FiscalYear.IsClosed)
            .Where(p => date >= p.StartDate && date <= p.EndDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, LedgerAccount>> GetAccountsAsync(
        int companyId,
        IReadOnlyCollection<int> accountIds,
        CancellationToken cancellationToken)
    {
        if (accountIds.Count == 0)
            return new Dictionary<int, LedgerAccount>();

        var list = await _db.LedgerAccounts
            .Where(a => a.CompanyId == companyId && accountIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        return list.ToDictionary(a => a.Id);
    }

    public async Task<string> GetNextJournalEntryNumberAsync(int companyId, CancellationToken cancellationToken)
    {
        var count = await _db.JournalEntries.CountAsync(e => e.CompanyId == companyId, cancellationToken);
        return $"JE-{count + 1:D8}";
    }

    public Task<string> GetNextAuditTrailNumberAsync(int companyId, CancellationToken cancellationToken) =>
        DocumentSequenceHelper.NextAsync(_db, companyId, "AUDIT", "AUD", cancellationToken);

    public Task AddJournalAsync(JournalEntry entry, CancellationToken cancellationToken)
    {
        _db.JournalEntries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);
}
