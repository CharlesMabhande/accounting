using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class StockService : IStockService
{
    private readonly AccountingDbContext _db;
    private readonly IJournalPostingService _journal;

    public StockService(AccountingDbContext db, IJournalPostingService journal)
    {
        _db = db;
        _journal = journal;
    }

    public async Task<OperationResult> PostIssueAsync(PostStockIssueRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            return OperationResult.Fail("Quantity must be positive.");

        var ws = await _db.WarehouseStocks.FirstOrDefaultAsync(
            t => t.WarehouseId == request.WarehouseId && t.StockItemId == request.StockItemId,
            cancellationToken);

        if (ws is null || ws.Quantity < request.Quantity)
            return OperationResult.Fail("Insufficient stock on hand.");

        var unitCost = request.UnitCostOverride > 0 ? request.UnitCostOverride : ws.LastUnitCost;
        if (unitCost <= 0)
            return OperationResult.Fail("Unit cost is not available.");

        var cogs = decimal.Round(request.Quantity * unitCost, 2, MidpointRounding.AwayFromZero);
        var stockItem = await _db.StockItems.FirstAsync(s => s.Id == request.StockItemId, cancellationToken);
        var cos = await LedgerLookup.ByCodeAsync(_db, request.CompanyId, "5000", cancellationToken);
        if (cos is null)
            return OperationResult.Fail("Cost of sales account (5000) not found.");

        var postReq = new PostJournalRequest
        {
            CompanyId = request.CompanyId,
            EntryDate = request.MovementDate,
            Reference = request.Reference,
            Description = "Stock issue",
            SourceModule = ModuleCode.Inventory,
            SourceDocumentId = null,
            Lines = new[]
            {
                new PostJournalLineDto { LedgerAccountId = cos.Id, Debit = cogs, Credit = 0, Narration = "COGS" },
                new PostJournalLineDto { LedgerAccountId = stockItem.InventoryAccountId, Debit = 0, Credit = cogs, Narration = "Inventory" }
            }
        };

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            ws.Quantity -= request.Quantity;
            if (ws.Quantity < 0) ws.Quantity = 0;

            _db.StockMovements.Add(new StockMovement
            {
                CompanyId = request.CompanyId,
                WarehouseId = request.WarehouseId,
                StockItemId = request.StockItemId,
                MovementType = StockMovementType.Issue,
                MovementDate = request.MovementDate,
                Quantity = request.Quantity,
                UnitCost = unitCost,
                Reference = request.Reference,
                SourceDocumentId = null
            });

            var jr = await _journal.PostJournalAsync(postReq, cancellationToken);
            if (!jr.Success)
            {
                await tx.RollbackAsync(cancellationToken);
                return OperationResult.Fail(jr.Errors.ToArray());
            }

            await tx.CommitAsync(cancellationToken);
            return OperationResult.Ok();
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
