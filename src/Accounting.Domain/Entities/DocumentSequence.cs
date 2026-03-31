namespace Accounting.Domain.Entities;

public class DocumentSequence : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Key { get; set; } = string.Empty;
    public int NextValue { get; set; } = 1;
}
