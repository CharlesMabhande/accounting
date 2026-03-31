using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class AuditTableSettingsService : IAuditTableSettingsService
{
    private static readonly HashSet<string> Excluded = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(AuditLog),
        nameof(UserSession)
    };

    private readonly AccountingDbContext _db;

    public AuditTableSettingsService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AuditTableSettingDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var names = _db.Model.GetEntityTypes()
            .Where(e => e.ClrType?.Namespace != null &&
                        e.ClrType.Namespace.StartsWith("Accounting.Domain.Entities", StringComparison.Ordinal))
            .Select(e => e.ClrType!.Name)
            .Where(n => !Excluded.Contains(n))
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var stored = await _db.AuditTableSettings.AsNoTracking()
            .ToDictionaryAsync(x => x.EntityTypeName, StringComparer.OrdinalIgnoreCase, cancellationToken);

        return names.Select(n =>
        {
            if (stored.TryGetValue(n, out var s))
            {
                return new AuditTableSettingDto
                {
                    EntityTypeName = n,
                    IsEnabled = s.IsEnabled,
                    AuditInserts = s.AuditInserts,
                    AuditUpdates = s.AuditUpdates,
                    AuditDeletes = s.AuditDeletes
                };
            }

            return new AuditTableSettingDto
            {
                EntityTypeName = n,
                IsEnabled = false,
                AuditInserts = false,
                AuditUpdates = true,
                AuditDeletes = true
            };
        }).ToList();
    }

    public async Task SaveAsync(SaveAuditTableSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _db.AuditTableSettings.ToDictionaryAsync(x => x.EntityTypeName, StringComparer.OrdinalIgnoreCase, cancellationToken);
        foreach (var row in request.Settings)
        {
            if (existing.TryGetValue(row.EntityTypeName, out var e))
            {
                e.IsEnabled = row.IsEnabled;
                e.AuditInserts = row.AuditInserts;
                e.AuditUpdates = row.AuditUpdates;
                e.AuditDeletes = row.AuditDeletes;
            }
            else
            {
                _db.AuditTableSettings.Add(new AuditTableSetting
                {
                    EntityTypeName = row.EntityTypeName,
                    IsEnabled = row.IsEnabled,
                    AuditInserts = row.AuditInserts,
                    AuditUpdates = row.AuditUpdates,
                    AuditDeletes = row.AuditDeletes
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
