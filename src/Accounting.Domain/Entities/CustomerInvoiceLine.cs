namespace Accounting.Domain.Entities;

public class CustomerInvoiceLine : BaseEntity
{
    public int CustomerInvoiceId { get; set; }
    public CustomerInvoice CustomerInvoice { get; set; } = null!;
    public int LineNumber { get; set; }
    public int? StockItemId { get; set; }
    public StockItem? StockItem { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int? RevenueAccountId { get; set; }
    public LedgerAccount? RevenueAccount { get; set; }
}
