using Accounting.Domain.Entities;

namespace Accounting.Application.Abstractions;

public interface IJournalPostingRepository
{
    Task<FiscalPeriod?> GetOpenPeriodForDateAsync(int companyId, DateOnly date, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<int, LedgerAccount>> GetAccountsAsync(int companyId, IReadOnlyCollection<int> accountIds, CancellationToken cancellationToken);
    Task<string> GetNextJournalEntryNumberAsync(int companyId, CancellationToken cancellationToken);
    Task<string> GetNextAuditTrailNumberAsync(int companyId, CancellationToken cancellationToken);
    Task AddJournalAsync(JournalEntry entry, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
