using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class WorkOrderManufacturingService : IWorkOrderManufacturingService
{
    private readonly AccountingDbContext _db;
    private readonly IJournalPostingService _journal;

    public WorkOrderManufacturingService(AccountingDbContext db, IJournalPostingService journal)
    {
        _db = db;
        _journal = journal;
    }

    public async Task<OperationResult> IssueMaterialsAsync(int companyId, int workOrderId, CancellationToken cancellationToken = default)
    {
        var denied = await ErpModuleGate.GetDenialReasonAsync(_db, companyId, ModuleCode.Manufacturing, cancellationToken);
        if (denied is not null)
            return OperationResult.Fail(denied);

        var wo = await _db.WorkOrders
            .Include(w => w.StockItem)
            .FirstOrDefaultAsync(w => w.Id == workOrderId && w.CompanyId == companyId, cancellationToken);
        if (wo is null)
            return OperationResult.Fail("Work order not found.");
        if (wo.MaterialsIssuedAtUtc is not null)
            return OperationResult.Fail("Materials have already been issued for this work order.");

        BomHeader? bom;
        if (wo.BomHeaderId is not null)
        {
            bom = await _db.BomHeaders
                .Include(b => b.Lines)
                .FirstOrDefaultAsync(b => b.Id == wo.BomHeaderId.Value && b.CompanyId == companyId, cancellationToken);
        }
        else
        {
            bom = await _db.BomHeaders
                .Include(b => b.Lines)
                .FirstOrDefaultAsync(
                    b => b.CompanyId == companyId && b.ParentStockItemId == wo.StockItemId && b.IsActive,
                    cancellationToken);
        }

        if (bom is null || bom.Lines.Count == 0)
            return OperationResult.Fail("No active BOM with lines was found for this work order.");

        var wip = await LedgerLookup.ByCodeAsync(_db, companyId, "1500", cancellationToken);
        if (wip is null)
            return OperationResult.Fail("Ledger account 1500 (Work in Progress) is required for manufacturing.");

        var creditLines = new List<PostJournalLineDto>();
        decimal totalCost = 0;
        var moveDate = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var line in bom.Lines.OrderBy(l => l.LineNumber))
        {
            var qty = wo.QuantityPlanned * line.QuantityPer * (1 + line.ScrapPercent / 100m);
            if (qty <= 0)
                continue;

            var comp = await _db.StockItems.FirstAsync(s => s.Id == line.ComponentStockItemId && s.CompanyId == companyId, cancellationToken);
            var ws = await _db.WarehouseStocks.FirstOrDefaultAsync(
                t => t.WarehouseId == wo.WarehouseId && t.StockItemId == comp.Id,
                cancellationToken);
            if (ws is null || ws.Quantity < qty)
                return OperationResult.Fail($"Insufficient stock for component {comp.Code}.");

            var unitCost = ws.LastUnitCost;
            if (unitCost <= 0)
                return OperationResult.Fail($"Unit cost is not set for component {comp.Code}.");

            var ext = decimal.Round(qty * unitCost, 2, MidpointRounding.AwayFromZero);
            totalCost += ext;
            ws.Quantity -= qty;

            _db.StockMovements.Add(new StockMovement
            {
                CompanyId = companyId,
                WarehouseId = wo.WarehouseId,
                StockItemId = comp.Id,
                MovementType = StockMovementType.Issue,
                MovementDate = moveDate,
                Quantity = qty,
                UnitCost = unitCost,
                Reference = wo.DocumentNumber,
                SourceDocumentId = wo.Id
            });

            creditLines.Add(new PostJournalLineDto
            {
                LedgerAccountId = comp.InventoryAccountId,
                Debit = 0,
                Credit = ext,
                Narration = $"WO {wo.DocumentNumber} {comp.Code}"
            });
        }

        if (creditLines.Count == 0)
            return OperationResult.Fail("Nothing to issue.");

        var lines = new List<PostJournalLineDto>
        {
            new()
            {
                LedgerAccountId = wip.Id,
                Debit = totalCost,
                Credit = 0,
                Narration = $"WO {wo.DocumentNumber} WIP"
            }
        };
        lines.AddRange(creditLines);

        var postReq = new PostJournalRequest
        {
            CompanyId = companyId,
            EntryDate = moveDate,
            Reference = wo.DocumentNumber,
            Description = "Work order material issue to WIP",
            SourceModule = ModuleCode.Manufacturing,
            SourceDocumentId = wo.Id,
            Lines = lines
        };

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var jr = await _journal.PostJournalAsync(postReq, cancellationToken);
            if (!jr.Success)
            {
                await tx.RollbackAsync(cancellationToken);
                return OperationResult.Fail(jr.Errors.ToArray());
            }

            wo.MaterialsIssuedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
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
