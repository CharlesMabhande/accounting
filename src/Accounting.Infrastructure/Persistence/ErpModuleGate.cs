using Accounting.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Persistence;

/// <summary>Respects <see cref="Domain.Entities.CompanyErpModule"/> toggles (absent row = enabled).</summary>
public static class ErpModuleGate
{
    public static async Task<string?> GetDenialReasonAsync(
        AccountingDbContext db,
        int companyId,
        ModuleCode module,
        CancellationToken cancellationToken = default)
    {
        var row = await db.CompanyErpModules.AsNoTracking()
            .FirstOrDefaultAsync(m => m.CompanyId == companyId && m.ModuleCode == module, cancellationToken);
        if (row is not null && !row.IsEnabled)
            return $"The {module} module is disabled for this company.";
        return null;
    }
}
