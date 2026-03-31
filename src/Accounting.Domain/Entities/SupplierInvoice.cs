using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class SupplierInvoice : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public string DocumentNumber { get; set; } = string.Empty;
    public string AuditTrailNumber { get; set; } = string.Empty;
    public DateOnly DocumentDate { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public int? TaxCodeId { get; set; }
    public TaxCode? TaxCode { get; set; }
    public int? JournalEntryId { get; set; }
    public JournalEntry? JournalEntry { get; set; }
    public ICollection<SupplierInvoiceLine> Lines { get; set; } = new List<SupplierInvoiceLine>();
}
