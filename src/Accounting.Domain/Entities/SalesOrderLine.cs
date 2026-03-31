namespace Accounting.Domain.Entities;

public class SalesOrderLine : BaseEntity
{
    public int SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;
    public int LineNumber { get; set; }
    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;
    public decimal QuantityOrdered { get; set; }
    public decimal UnitPrice { get; set; }
}
