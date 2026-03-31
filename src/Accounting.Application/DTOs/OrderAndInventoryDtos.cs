namespace Accounting.Application.DTOs;

public sealed class CreateSalesOrderLineDto
{
    public int StockItemId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public sealed class CreateSalesOrderRequest
{
    public int CompanyId { get; init; }
    public int CustomerId { get; init; }
    public DateOnly OrderDate { get; init; }
    public IReadOnlyList<CreateSalesOrderLineDto> Lines { get; init; } = Array.Empty<CreateSalesOrderLineDto>();
}

public sealed class CreatePurchaseOrderLineDto
{
    public int StockItemId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
}

public sealed class CreatePurchaseOrderRequest
{
    public int CompanyId { get; init; }
    public int SupplierId { get; init; }
    public DateOnly OrderDate { get; init; }
    public IReadOnlyList<CreatePurchaseOrderLineDto> Lines { get; init; } = Array.Empty<CreatePurchaseOrderLineDto>();
}

public sealed class CreateGrnLineDto
{
    public int StockItemId { get; init; }
    public decimal QuantityReceived { get; init; }
    public decimal UnitCost { get; init; }
}

public sealed class CreateGoodsReceivedNoteRequest
{
    public int CompanyId { get; init; }
    public int SupplierId { get; init; }
    public int WarehouseId { get; init; }
    public int? PurchaseOrderId { get; init; }
    public DateOnly ReceivedDate { get; init; }
    public IReadOnlyList<CreateGrnLineDto> Lines { get; init; } = Array.Empty<CreateGrnLineDto>();
}

public sealed class PostStockIssueRequest
{
    public int CompanyId { get; init; }
    public int WarehouseId { get; init; }
    public int StockItemId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitCostOverride { get; init; }
    public string Reference { get; init; } = string.Empty;
    public DateOnly MovementDate { get; init; }
}
