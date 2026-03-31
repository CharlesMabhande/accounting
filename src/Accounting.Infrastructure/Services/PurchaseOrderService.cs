using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class PurchaseOrderService : IPurchaseOrderService
{
    private readonly AccountingDbContext _db;

    public PurchaseOrderService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<OperationResult<CreatedEntityInfo>> CreateAsync(
        CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0)
            return OperationResult<CreatedEntityInfo>.Fail("At least one line is required.");

        var supplier = await _db.Suppliers.FirstOrDefaultAsync(
            s => s.Id == request.SupplierId && s.CompanyId == request.CompanyId,
            cancellationToken);
        if (supplier is null)
            return OperationResult<CreatedEntityInfo>.Fail("Supplier not found.");

        var docNo = await DocumentSequenceHelper.NextAsync(_db, request.CompanyId, "PO", "PO", cancellationToken);
        var lines = new List<PurchaseOrderLine>();
        var lineNo = 1;
        foreach (var l in request.Lines)
        {
            var item = await _db.StockItems.FirstOrDefaultAsync(
                s => s.Id == l.StockItemId && s.CompanyId == request.CompanyId,
                cancellationToken);
            if (item is null)
                return OperationResult<CreatedEntityInfo>.Fail($"Stock item {l.StockItemId} not found.");

            lines.Add(new PurchaseOrderLine
            {
                LineNumber = lineNo++,
                StockItemId = l.StockItemId,
                QuantityOrdered = l.Quantity,
                UnitCost = l.UnitCost
            });
        }

        var order = new PurchaseOrder
        {
            CompanyId = request.CompanyId,
            SupplierId = request.SupplierId,
            OrderNumber = docNo,
            OrderDate = request.OrderDate,
            Status = DocumentStatus.Draft,
            Lines = lines
        };

        _db.PurchaseOrders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<CreatedEntityInfo>.Ok(new CreatedEntityInfo { Id = order.Id, DocumentNumber = docNo });
    }
}
