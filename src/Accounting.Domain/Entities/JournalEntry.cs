using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class JournalEntry : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int FiscalPeriodId { get; set; }
    public FiscalPeriod FiscalPeriod { get; set; } = null!;
    public string EntryNumber { get; set; } = string.Empty;
    /// <summary>Company-wide audit trail reference (shared sequence with other posted documents).</summary>
    public string AuditTrailNumber { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public ModuleCode SourceModule { get; set; }
    public int? SourceDocumentId { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}
