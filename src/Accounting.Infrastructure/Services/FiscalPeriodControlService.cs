using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class FiscalPeriodControlService : IFiscalPeriodControlService
{
    private readonly AccountingDbContext _db;

    public FiscalPeriodControlService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FiscalPeriodDto>> ListAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.FiscalPeriods
            .AsNoTracking()
            .Include(p => p.FiscalYear)
            .Where(p => p.FiscalYear.CompanyId == companyId)
            .OrderBy(p => p.FiscalYear.Year)
            .ThenBy(p => p.PeriodNumber)
            .Select(p => new FiscalPeriodDto
            {
                Id = p.Id,
                FiscalYearId = p.FiscalYearId,
                Year = p.FiscalYear.Year,
                PeriodNumber = p.PeriodNumber,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsClosed = p.IsClosed,
                FiscalYearClosed = p.FiscalYear.IsClosed
            })
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<OperationResult> ClosePeriodAsync(int companyId, int periodId, CancellationToken cancellationToken = default)
    {
        var period = await _db.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.Id == periodId && p.FiscalYear.CompanyId == companyId, cancellationToken);

        if (period is null)
            return OperationResult.Fail("Fiscal period not found for this company.");

        if (period.FiscalYear.IsClosed)
            return OperationResult.Fail("The fiscal year is closed; reopen the year before changing periods.");

        if (period.IsClosed)
            return OperationResult.Ok();

        period.IsClosed = true;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> ReopenPeriodAsync(int companyId, int periodId, CancellationToken cancellationToken = default)
    {
        var period = await _db.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.Id == periodId && p.FiscalYear.CompanyId == companyId, cancellationToken);

        if (period is null)
            return OperationResult.Fail("Fiscal period not found for this company.");

        if (period.FiscalYear.IsClosed)
            return OperationResult.Fail("The fiscal year is closed; reopen the year first.");

        if (!period.IsClosed)
            return OperationResult.Ok();

        period.IsClosed = false;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }
}
