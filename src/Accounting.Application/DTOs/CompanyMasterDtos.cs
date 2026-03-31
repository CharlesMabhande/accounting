namespace Accounting.Application.DTOs;

public sealed class BankAccountDto
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public int LedgerAccountId { get; init; }
    public string LedgerAccountCode { get; init; } = "";
    public string LedgerAccountName { get; init; } = "";
    public string CurrencyCode { get; init; } = "USD";
    public bool IsActive { get; init; } = true;
}

public sealed class UpsertBankAccountRequest
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public int LedgerAccountId { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public bool IsActive { get; init; } = true;
}

public sealed class CustomerDto
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public int? AccountsReceivableAccountId { get; init; }
    public string? AccountsReceivableAccountCode { get; init; }
    public string? AccountsReceivableAccountName { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public bool IsActive { get; init; } = true;
    public string? ContactName { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? PhysicalAddress1 { get; init; }
    public string? PhysicalAddress2 { get; init; }
    public string? PhysicalAddress3 { get; init; }
    public string? PhysicalCity { get; init; }
    public string? PostalAddress1 { get; init; }
    public string? PostalAddress2 { get; init; }
    public string? PostalAddress3 { get; init; }
    public string? PostalCode { get; init; }
    public string? TaxNumber { get; init; }
    public string? RegistrationNumber { get; init; }
    public decimal? CreditLimit { get; init; }
    public bool OnHold { get; init; }
}

public sealed class UpsertCustomerRequest
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public int? AccountsReceivableAccountId { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public bool IsActive { get; init; } = true;
    public string? ContactName { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? PhysicalAddress1 { get; init; }
    public string? PhysicalAddress2 { get; init; }
    public string? PhysicalAddress3 { get; init; }
    public string? PhysicalCity { get; init; }
    public string? PostalAddress1 { get; init; }
    public string? PostalAddress2 { get; init; }
    public string? PostalAddress3 { get; init; }
    public string? PostalCode { get; init; }
    public string? TaxNumber { get; init; }
    public string? RegistrationNumber { get; init; }
    public decimal? CreditLimit { get; init; }
    public bool OnHold { get; init; }
}

public sealed class SupplierDto
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public int? AccountsPayableAccountId { get; init; }
    public string? AccountsPayableAccountCode { get; init; }
    public string? AccountsPayableAccountName { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public bool IsActive { get; init; } = true;
    public string? ContactName { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? PhysicalAddress1 { get; init; }
    public string? PhysicalAddress2 { get; init; }
    public string? PhysicalAddress3 { get; init; }
    public string? PhysicalCity { get; init; }
    public string? PostalAddress1 { get; init; }
    public string? PostalAddress2 { get; init; }
    public string? PostalAddress3 { get; init; }
    public string? PostalCode { get; init; }
    public string? TaxNumber { get; init; }
    public string? RegistrationNumber { get; init; }
    public decimal? CreditLimit { get; init; }
    public bool OnHold { get; init; }
}

public sealed class UpsertSupplierRequest
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public int? AccountsPayableAccountId { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public bool IsActive { get; init; } = true;
    public string? ContactName { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? PhysicalAddress1 { get; init; }
    public string? PhysicalAddress2 { get; init; }
    public string? PhysicalAddress3 { get; init; }
    public string? PhysicalCity { get; init; }
    public string? PostalAddress1 { get; init; }
    public string? PostalAddress2 { get; init; }
    public string? PostalAddress3 { get; init; }
    public string? PostalCode { get; init; }
    public string? TaxNumber { get; init; }
    public string? RegistrationNumber { get; init; }
    public decimal? CreditLimit { get; init; }
    public bool OnHold { get; init; }
}

public sealed class LedgerAccountOptionDto
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
}
