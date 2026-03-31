using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class StockMovement : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;
    public StockMovementType MovementType { get; set; }
    public DateOnly MovementDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int? SourceDocumentId { get; set; }
}
