using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class TaxCodeQueryService : ITaxCodeQueryService
{
    private readonly AccountingDbContext _db;

    public TaxCodeQueryService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TaxCodeQueryDto>> ListAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _db.TaxCodes.AsNoTracking()
            .Where(t => t.CompanyId == companyId)
            .OrderBy(t => t.Code)
            .Select(t => new TaxCodeQueryDto
            {
                Id = t.Id,
                Code = t.Code,
                Description = t.Description,
                RatePercent = t.RatePercent,
                IsActive = t.IsActive,
                OutputTaxLedgerAccountId = t.OutputTaxLedgerAccountId,
                InputTaxLedgerAccountId = t.InputTaxLedgerAccountId
            })
            .ToListAsync(cancellationToken);
    }
}
