namespace Accounting.Application.DTOs;

public sealed class StockItemQueryDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? LongDescription { get; init; }
    public string? AlternateCode { get; init; }
    public string UnitOfMeasure { get; init; } = "EA";
    public int InventoryAccountId { get; init; }
    public int CostOfSalesAccountId { get; init; }
    public bool TrackSerialNumbers { get; init; }
    public bool TrackLots { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsServiceItem { get; init; }
    public decimal? TargetGpPercent { get; init; }
    public decimal? BuyLength { get; init; }
    public decimal? BuyWidth { get; init; }
    public decimal? BuyHeight { get; init; }
    public decimal? SellLength { get; init; }
    public decimal? SellWidth { get; init; }
    public decimal? SellHeight { get; init; }
    public decimal? Weight { get; init; }
    public string? WeightUnit { get; init; }
    public string? MeasurementNotes { get; init; }
}

public sealed class UpsertStockItemRequest
{
    public string Code { get; init; } = "";
    public string Description { get; init; } = "";
    public string? LongDescription { get; init; }
    public string? AlternateCode { get; init; }
    public string UnitOfMeasure { get; init; } = "EA";
    public int InventoryAccountId { get; init; }
    public int CostOfSalesAccountId { get; init; }
    public bool TrackSerialNumbers { get; init; }
    public bool TrackLots { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsServiceItem { get; init; }
    public decimal? TargetGpPercent { get; init; }
    public decimal? BuyLength { get; init; }
    public decimal? BuyWidth { get; init; }
    public decimal? BuyHeight { get; init; }
    public decimal? SellLength { get; init; }
    public decimal? SellWidth { get; init; }
    public decimal? SellHeight { get; init; }
    public decimal? Weight { get; init; }
    public string? WeightUnit { get; init; }
    public string? MeasurementNotes { get; init; }
}

public sealed class BeltsImportRequest
{
    public bool ImportStockItems { get; init; } = true;
    public bool ImportCustomers { get; init; } = true;
    public bool ImportSuppliers { get; init; } = true;
    public bool OverwriteExisting { get; init; }
}

public sealed class BeltsImportResultDto
{
    public int StockItemsInserted { get; init; }
    public int StockItemsUpdated { get; init; }
    public int StockItemsSkipped { get; init; }
    public int CustomersInserted { get; init; }
    public int CustomersUpdated { get; init; }
    public int CustomersSkipped { get; init; }
    public int SuppliersInserted { get; init; }
    public int SuppliersUpdated { get; init; }
    public int SuppliersSkipped { get; init; }
    public int Errors { get; init; }
    public string? Message { get; init; }
}

public sealed class WarehouseStockQueryDto
{
    public int WarehouseId { get; init; }
    public string WarehouseCode { get; init; } = string.Empty;
    public int StockItemId { get; init; }
    public string StockCode { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal LastUnitCost { get; init; }
}
