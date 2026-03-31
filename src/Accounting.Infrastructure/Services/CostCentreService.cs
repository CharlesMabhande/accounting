using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;

namespace Accounting.Infrastructure.Services;

public sealed class CostCentreService : ICostCentreService
{
    private readonly AccountingDbContext _db;

    public CostCentreService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<OperationResult<int>> CreateAsync(CreateCostCentreRequest request, CancellationToken cancellationToken = default)
    {
        var cc = new CostCentre
        {
            CompanyId = request.CompanyId,
            Code = request.Code,
            Name = request.Name,
            IsActive = true
        };
        _db.CostCentres.Add(cc);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Ok(cc.Id);
    }
}
