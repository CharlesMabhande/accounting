namespace Accounting.Domain.Entities;

/// <summary>ISO currency master (e.g. USD, ZAR).</summary>
public class Currency : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DecimalPlaces { get; set; } = 2;
}
