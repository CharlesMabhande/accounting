using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class SalesOrderService : ISalesOrderService
{
    private readonly AccountingDbContext _db;

    public SalesOrderService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<OperationResult<CreatedEntityInfo>> CreateAsync(
        CreateSalesOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0)
            return OperationResult<CreatedEntityInfo>.Fail("At least one line is required.");

        var customer = await _db.Customers.FirstOrDefaultAsync(
            c => c.Id == request.CustomerId && c.CompanyId == request.CompanyId,
            cancellationToken);
        if (customer is null)
            return OperationResult<CreatedEntityInfo>.Fail("Customer not found.");

        var docNo = await DocumentSequenceHelper.NextAsync(_db, request.CompanyId, "SO", "SO", cancellationToken);
        var lines = new List<SalesOrderLine>();
        var lineNo = 1;
        foreach (var l in request.Lines)
        {
            var item = await _db.StockItems.FirstOrDefaultAsync(
                s => s.Id == l.StockItemId && s.CompanyId == request.CompanyId,
                cancellationToken);
            if (item is null)
                return OperationResult<CreatedEntityInfo>.Fail($"Stock item {l.StockItemId} not found.");

            lines.Add(new SalesOrderLine
            {
                LineNumber = lineNo++,
                StockItemId = l.StockItemId,
                QuantityOrdered = l.Quantity,
                UnitPrice = l.UnitPrice
            });
        }

        var order = new SalesOrder
        {
            CompanyId = request.CompanyId,
            CustomerId = request.CustomerId,
            OrderNumber = docNo,
            OrderDate = request.OrderDate,
            Status = DocumentStatus.Draft,
            Lines = lines
        };

        _db.SalesOrders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<CreatedEntityInfo>.Ok(new CreatedEntityInfo { Id = order.Id, DocumentNumber = docNo });
    }
}
