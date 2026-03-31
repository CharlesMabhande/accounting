namespace Accounting.Domain.Entities;

public class PurchaseRequisitionLine : BaseEntity
{
    public int PurchaseRequisitionId { get; set; }
    public PurchaseRequisition PurchaseRequisition { get; set; } = null!;
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? StockItemId { get; set; }
    public StockItem? StockItem { get; set; }
    public decimal Quantity { get; set; }
    public decimal EstimatedUnitCost { get; set; }
}
