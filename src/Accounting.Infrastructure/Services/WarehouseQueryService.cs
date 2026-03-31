using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class WarehouseQueryService : IWarehouseQueryService
{
    private readonly AccountingDbContext _db;

    public WarehouseQueryService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<WarehouseQueryDto>> ListAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _db.Warehouses.AsNoTracking()
            .Where(w => w.CompanyId == companyId)
            .OrderBy(w => w.Code)
            .Select(w => new WarehouseQueryDto
            {
                Id = w.Id,
                Code = w.Code,
                Name = w.Name,
                IsActive = w.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}
