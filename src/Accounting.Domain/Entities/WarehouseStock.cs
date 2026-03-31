namespace Accounting.Domain.Entities;

/// <summary>Per-warehouse quantity and last cost for weighted-average style valuation.</summary>
public class WarehouseStock : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal LastUnitCost { get; set; }
}
