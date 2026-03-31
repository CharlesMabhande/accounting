using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;

namespace Accounting.Infrastructure.Services;

public sealed class ProjectJobService : IProjectJobService
{
    private readonly AccountingDbContext _db;

    public ProjectJobService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<OperationResult<int>> CreateAsync(CreateProjectJobRequest request, CancellationToken cancellationToken = default)
    {
        var job = new ProjectJob
        {
            CompanyId = request.CompanyId,
            Code = request.Code,
            Name = request.Name,
            CostCentreId = request.CostCentreId,
            IsClosed = false
        };
        _db.ProjectJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Ok(job.Id);
    }
}
