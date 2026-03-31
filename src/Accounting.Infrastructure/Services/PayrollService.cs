using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class PayrollService : IPayrollService
{
    private readonly AccountingDbContext _db;
    private readonly IJournalPostingService _journal;

    public PayrollService(AccountingDbContext db, IJournalPostingService journal)
    {
        _db = db;
        _journal = journal;
    }

    public async Task<OperationResult<CreatedEntityInfo>> CreateRunAsync(
        CreatePayrollRunRequest request,
        CancellationToken cancellationToken = default)
    {
        var sum = request.TaxWithheld + request.NetPay;
        if (Math.Abs(request.GrossWages - sum) > 0.02m)
            return OperationResult<CreatedEntityInfo>.Fail("Gross wages must equal tax withheld plus net pay (within 0.02).");

        var docNo = await DocumentSequenceHelper.NextAsync(_db, request.CompanyId, "PR", "PR", cancellationToken);
        var run = new PayrollRun
        {
            CompanyId = request.CompanyId,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            RunNumber = docNo,
            GrossWages = request.GrossWages,
            TaxWithheld = request.TaxWithheld,
            NetPay = request.NetPay,
            IsPosted = false
        };

        _db.PayrollRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<CreatedEntityInfo>.Ok(new CreatedEntityInfo { Id = run.Id, DocumentNumber = docNo });
    }

    public async Task<OperationResult<PostJournalInfo>> PostRunAsync(
        int payrollRunId,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var run = await _db.PayrollRuns.FirstOrDefaultAsync(r => r.Id == payrollRunId, cancellationToken);
        if (run is null)
            return OperationResult<PostJournalInfo>.Fail("Payroll run not found.");
        if (run.IsPosted)
            return OperationResult<PostJournalInfo>.Fail("Payroll run already posted.");

        var wages = await LedgerLookup.ByCodeAsync(_db, run.CompanyId, "6000", cancellationToken);
        var tax = await LedgerLookup.ByCodeAsync(_db, run.CompanyId, "2400", cancellationToken);
        var netLiab = await LedgerLookup.ByCodeAsync(_db, run.CompanyId, "2300", cancellationToken);

        if (wages is null || tax is null || netLiab is null)
            return OperationResult<PostJournalInfo>.Fail("Payroll accounts (6000, 2400, 2300) must exist.");

        var postReq = new PostJournalRequest
        {
            CompanyId = run.CompanyId,
            EntryDate = run.PeriodEnd,
            Reference = run.RunNumber,
            Description = "Payroll accrual",
            SourceModule = ModuleCode.Payroll,
            SourceDocumentId = run.Id,
            Lines = new[]
            {
                new PostJournalLineDto { LedgerAccountId = wages.Id, Debit = run.GrossWages, Credit = 0, Narration = "Wages" },
                new PostJournalLineDto { LedgerAccountId = tax.Id, Debit = 0, Credit = run.TaxWithheld, Narration = "Tax withheld" },
                new PostJournalLineDto { LedgerAccountId = netLiab.Id, Debit = 0, Credit = run.NetPay, Narration = "Net pay payable" }
            }
        };

        var jr = await _journal.PostJournalAsync(postReq, cancellationToken);
        if (!jr.Success)
            return OperationResult<PostJournalInfo>.Fail(jr.Errors.ToArray());

        run.JournalEntryId = jr.JournalEntryId;
        run.IsPosted = true;
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<PostJournalInfo>.Ok(new PostJournalInfo
        {
            JournalEntryId = jr.JournalEntryId!.Value,
            EntryNumber = jr.EntryNumber!
        });
    }
}
