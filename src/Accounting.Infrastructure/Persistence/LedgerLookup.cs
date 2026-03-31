using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Persistence;

public static class LedgerLookup
{
    public static Task<LedgerAccount?> ByCodeAsync(
        AccountingDbContext db,
        int companyId,
        string code,
        CancellationToken cancellationToken) =>
        db.LedgerAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == code, cancellationToken);
}
