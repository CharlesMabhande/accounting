namespace Accounting.Domain.Entities;

public class TaxCode : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal RatePercent { get; set; }
    public bool IsActive { get; set; } = true;
    public int? OutputTaxLedgerAccountId { get; set; }
    public LedgerAccount? OutputTaxLedgerAccount { get; set; }
    public int? InputTaxLedgerAccountId { get; set; }
    public LedgerAccount? InputTaxLedgerAccount { get; set; }
}
