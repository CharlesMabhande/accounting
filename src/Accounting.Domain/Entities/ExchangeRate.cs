namespace Accounting.Domain.Entities;

/// <summary>Rate from FromCurrency to ToCurrency (multiply amount in From by Rate to get To).</summary>
public class ExchangeRate : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string FromCurrencyCode { get; set; } = string.Empty;
    public string ToCurrencyCode { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateOnly EffectiveDate { get; set; }
}
