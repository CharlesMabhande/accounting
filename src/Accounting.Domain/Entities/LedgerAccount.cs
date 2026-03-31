using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class LedgerAccount : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public bool IsPostable { get; set; } = true;
    public bool IsControlAccount { get; set; }
    public int? ParentAccountId { get; set; }
    public LedgerAccount? ParentAccount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}
