namespace Accounting.Domain.Entities;

public class PurchaseOrderLine : BaseEntity
{
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public int LineNumber { get; set; }
    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;
    public decimal QuantityOrdered { get; set; }
    public decimal UnitCost { get; set; }
}
