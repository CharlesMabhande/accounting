namespace Accounting.Domain.Entities;

public class PayrollRun : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string RunNumber { get; set; } = string.Empty;
    public bool IsPosted { get; set; }
    public int? JournalEntryId { get; set; }
    public JournalEntry? JournalEntry { get; set; }
    public decimal GrossWages { get; set; }
    public decimal TaxWithheld { get; set; }
    public decimal NetPay { get; set; }
}
