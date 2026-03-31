using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class FixedAssetService : IFixedAssetService
{
    private readonly AccountingDbContext _db;
    private readonly IJournalPostingService _journal;

    public FixedAssetService(AccountingDbContext db, IJournalPostingService journal)
    {
        _db = db;
        _journal = journal;
    }

    public async Task<OperationResult<int>> CreateAsync(CreateFixedAssetRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Cost <= 0)
            return OperationResult<int>.Fail("Cost must be positive.");

        var asset = new FixedAsset
        {
            CompanyId = request.CompanyId,
            AssetNumber = request.AssetNumber,
            Description = request.Description,
            AcquisitionDate = request.AcquisitionDate,
            Cost = request.Cost,
            AssetAccountId = request.AssetAccountId,
            DepreciationExpenseAccountId = request.DepreciationExpenseAccountId,
            AccumulatedDepreciationAccountId = request.AccumulatedDepreciationAccountId,
            IsDisposed = false
        };

        _db.FixedAssets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Ok(asset.Id);
    }

    public async Task<OperationResult<PostJournalInfo>> PostDepreciationAsync(
        PostDepreciationRequest request,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
            return OperationResult<PostJournalInfo>.Fail("Depreciation amount must be positive.");

        var asset = await _db.FixedAssets.FirstOrDefaultAsync(
            a => a.Id == request.FixedAssetId && a.CompanyId == request.CompanyId,
            cancellationToken);

        if (asset is null)
            return OperationResult<PostJournalInfo>.Fail("Fixed asset not found.");
        if (asset.IsDisposed)
            return OperationResult<PostJournalInfo>.Fail("Asset is disposed.");

        var postReq = new PostJournalRequest
        {
            CompanyId = request.CompanyId,
            EntryDate = request.EntryDate,
            Reference = request.Reference,
            Description = $"Depreciation {asset.AssetNumber}",
            SourceModule = ModuleCode.FixedAssets,
            SourceDocumentId = asset.Id,
            Lines = new[]
            {
                new PostJournalLineDto
                {
                    LedgerAccountId = asset.DepreciationExpenseAccountId,
                    Debit = request.Amount,
                    Credit = 0,
                    Narration = "Depreciation expense"
                },
                new PostJournalLineDto
                {
                    LedgerAccountId = asset.AccumulatedDepreciationAccountId,
                    Debit = 0,
                    Credit = request.Amount,
                    Narration = "Accumulated depreciation"
                }
            }
        };

        var jr = await _journal.PostJournalAsync(postReq, cancellationToken);
        if (!jr.Success)
            return OperationResult<PostJournalInfo>.Fail(jr.Errors.ToArray());

        return OperationResult<PostJournalInfo>.Ok(new PostJournalInfo
        {
            JournalEntryId = jr.JournalEntryId!.Value,
            EntryNumber = jr.EntryNumber!
        });
    }
}
