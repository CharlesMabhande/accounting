namespace Accounting.Domain.Entities;

public class BankAccount : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int LedgerAccountId { get; set; }
    public LedgerAccount LedgerAccount { get; set; } = null!;
    public string CurrencyCode { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
}
