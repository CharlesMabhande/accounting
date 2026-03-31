using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class GoodsReceivedNoteService : IGoodsReceivedNoteService
{
    private readonly AccountingDbContext _db;

    public GoodsReceivedNoteService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<OperationResult<CreatedEntityInfo>> CreateAsync(
        CreateGoodsReceivedNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0)
            return OperationResult<CreatedEntityInfo>.Fail("At least one line is required.");

        var supplier = await _db.Suppliers.FirstOrDefaultAsync(
            s => s.Id == request.SupplierId && s.CompanyId == request.CompanyId,
            cancellationToken);
        if (supplier is null)
            return OperationResult<CreatedEntityInfo>.Fail("Supplier not found.");

        var wh = await _db.Warehouses.FirstOrDefaultAsync(
            w => w.Id == request.WarehouseId && w.CompanyId == request.CompanyId,
            cancellationToken);
        if (wh is null)
            return OperationResult<CreatedEntityInfo>.Fail("Warehouse not found.");

        if (request.PurchaseOrderId.HasValue)
        {
            var po = await _db.PurchaseOrders.FirstOrDefaultAsync(
                p => p.Id == request.PurchaseOrderId.Value && p.CompanyId == request.CompanyId,
                cancellationToken);
            if (po is null)
                return OperationResult<CreatedEntityInfo>.Fail("Purchase order not found.");
        }

        var docNo = await DocumentSequenceHelper.NextAsync(_db, request.CompanyId, "GRN", "GRN", cancellationToken);
        var lines = new List<GoodsReceivedNoteLine>();
        var lineNo = 1;
        foreach (var l in request.Lines)
        {
            var item = await _db.StockItems.FirstOrDefaultAsync(
                s => s.Id == l.StockItemId && s.CompanyId == request.CompanyId,
                cancellationToken);
            if (item is null)
                return OperationResult<CreatedEntityInfo>.Fail($"Stock item {l.StockItemId} not found.");

            lines.Add(new GoodsReceivedNoteLine
            {
                LineNumber = lineNo++,
                StockItemId = l.StockItemId,
                QuantityReceived = l.QuantityReceived,
                UnitCost = l.UnitCost
            });
        }

        var grn = new GoodsReceivedNote
        {
            CompanyId = request.CompanyId,
            SupplierId = request.SupplierId,
            WarehouseId = request.WarehouseId,
            PurchaseOrderId = request.PurchaseOrderId,
            GrnNumber = docNo,
            ReceivedDate = request.ReceivedDate,
            Status = DocumentStatus.Draft,
            Lines = lines
        };

        _db.GoodsReceivedNotes.Add(grn);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<CreatedEntityInfo>.Ok(new CreatedEntityInfo { Id = grn.Id, DocumentNumber = docNo });
    }

    public async Task<OperationResult> PostAsync(int grnId, int? userId, CancellationToken cancellationToken = default)
    {
        var grn = await _db.GoodsReceivedNotes
            .Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.Id == grnId, cancellationToken);

        if (grn is null)
            return OperationResult.Fail("GRN not found.");
        if (grn.Status == DocumentStatus.Posted)
            return OperationResult.Fail("GRN already posted.");

        foreach (var line in grn.Lines.OrderBy(l => l.LineNumber))
        {
            var ws = await _db.WarehouseStocks.FirstOrDefaultAsync(
                t => t.WarehouseId == grn.WarehouseId && t.StockItemId == line.StockItemId,
                cancellationToken);

            var qtyIn = line.QuantityReceived;
            var cost = line.UnitCost;

            if (ws is null)
            {
                ws = new WarehouseStock
                {
                    CompanyId = grn.CompanyId,
                    WarehouseId = grn.WarehouseId,
                    StockItemId = line.StockItemId,
                    Quantity = qtyIn,
                    LastUnitCost = cost
                };
                _db.WarehouseStocks.Add(ws);
            }
            else
            {
                var oldQty = ws.Quantity;
                var newQty = oldQty + qtyIn;
                if (newQty <= 0)
                {
                    ws.Quantity = 0;
                    ws.LastUnitCost = cost;
                }
                else
                {
                    var weighted = (oldQty * ws.LastUnitCost + qtyIn * cost) / newQty;
                    ws.Quantity = newQty;
                    ws.LastUnitCost = decimal.Round(weighted, 4, MidpointRounding.AwayFromZero);
                }
            }

            _db.StockMovements.Add(new StockMovement
            {
                CompanyId = grn.CompanyId,
                WarehouseId = grn.WarehouseId,
                StockItemId = line.StockItemId,
                MovementType = StockMovementType.Receipt,
                MovementDate = grn.ReceivedDate,
                Quantity = qtyIn,
                UnitCost = cost,
                Reference = grn.GrnNumber,
                SourceDocumentId = grn.Id
            });
        }

        grn.Status = DocumentStatus.Posted;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }
}
