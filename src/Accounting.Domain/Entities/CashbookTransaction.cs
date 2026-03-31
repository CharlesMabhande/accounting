using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class CashbookTransaction : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;
    public DateOnly TransactionDate { get; set; }
    public string AuditTrailNumber { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsReceipt { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public int? JournalEntryId { get; set; }
    public JournalEntry? JournalEntry { get; set; }
}
