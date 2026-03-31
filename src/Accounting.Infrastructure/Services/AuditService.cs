using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class AuditService : IAuditService
{
    private readonly AccountingDbContext _db;

    public AuditService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        int? userId,
        string action,
        string entityName,
        string? entityKey,
        string details,
        CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityKey = entityKey,
            Details = details,
            OccurredAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogQueryDto>> ListAsync(int? limit, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit ?? 100, 1, 1000);
        return await _db.AuditLogs.AsNoTracking()
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(take)
            .Select(a => new AuditLogQueryDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Action = a.Action,
                EntityName = a.EntityName,
                EntityKey = a.EntityKey,
                Details = a.Details,
                OccurredAtUtc = a.OccurredAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
