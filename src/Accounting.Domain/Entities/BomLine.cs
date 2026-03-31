namespace Accounting.Domain.Entities;

public class BomLine : BaseEntity
{
    public int BomHeaderId { get; set; }
    public BomHeader BomHeader { get; set; } = null!;
    public int LineNumber { get; set; }
    public int ComponentStockItemId { get; set; }
    public StockItem ComponentStockItem { get; set; } = null!;
    public decimal QuantityPer { get; set; } = 1;
    public decimal ScrapPercent { get; set; }
}
