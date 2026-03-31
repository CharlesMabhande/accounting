using System.Data;
using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Persistence;

public static class DocumentSequenceHelper
{
    public static async Task<string> NextAsync(
        AccountingDbContext db,
        int companyId,
        string key,
        string prefix,
        CancellationToken cancellationToken)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var seq = await db.DocumentSequences
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Key == key, cancellationToken);
        if (seq is null)
        {
            seq = new DocumentSequence { CompanyId = companyId, Key = key, NextValue = 1 };
            db.DocumentSequences.Add(seq);
            await db.SaveChangesAsync(cancellationToken);
        }

        var n = seq.NextValue;
        seq.NextValue++;
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return $"{prefix}-{n:D6}";
    }
}
