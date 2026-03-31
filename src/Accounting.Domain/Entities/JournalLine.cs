namespace Accounting.Domain.Entities;

public class JournalLine : BaseEntity
{
    public int JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;
    public int LineNumber { get; set; }
    public int LedgerAccountId { get; set; }
    public LedgerAccount LedgerAccount { get; set; } = null!;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Narration { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
}
