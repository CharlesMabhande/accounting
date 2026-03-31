namespace Accounting.Domain.Entities;

/// <summary>Customer master — extended with <c>Client</c>-style contact and address fields (legacy import mapping).</summary>
public class Customer : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? AccountsReceivableAccountId { get; set; }
    public LedgerAccount? AccountsReceivableAccount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhysicalAddress1 { get; set; }
    public string? PhysicalAddress2 { get; set; }
    public string? PhysicalAddress3 { get; set; }
    public string? PhysicalCity { get; set; }
    public string? PostalAddress1 { get; set; }
    public string? PostalAddress2 { get; set; }
    public string? PostalAddress3 { get; set; }
    public string? PostalCode { get; set; }
    public string? TaxNumber { get; set; }
    public string? RegistrationNumber { get; set; }
    public decimal? CreditLimit { get; set; }
    public bool OnHold { get; set; }
}
