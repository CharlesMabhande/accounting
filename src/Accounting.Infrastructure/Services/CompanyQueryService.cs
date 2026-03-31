using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class CompanyQueryService : ICompanyQueryService
{
    private readonly AccountingDbContext _db;

    public CompanyQueryService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CompanyQueryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Companies.AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new CompanyQueryDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                BaseCurrency = c.BaseCurrency,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}
