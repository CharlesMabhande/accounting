namespace Accounting.Domain.Entities;

public class SupplierInvoiceLine : BaseEntity
{
    public int SupplierInvoiceId { get; set; }
    public SupplierInvoice SupplierInvoice { get; set; } = null!;
    public int LineNumber { get; set; }
    public int? StockItemId { get; set; }
    public StockItem? StockItem { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public int? ExpenseAccountId { get; set; }
    public LedgerAccount? ExpenseAccount { get; set; }
}
