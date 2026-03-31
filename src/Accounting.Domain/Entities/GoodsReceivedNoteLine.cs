namespace Accounting.Domain.Entities;

public class GoodsReceivedNoteLine : BaseEntity
{
    public int GoodsReceivedNoteId { get; set; }
    public GoodsReceivedNote GoodsReceivedNote { get; set; } = null!;
    public int LineNumber { get; set; }
    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;
    public decimal QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
}
