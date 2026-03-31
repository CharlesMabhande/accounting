namespace Accounting.Domain.Entities;

/// <summary>Project / job costing header.</summary>
public class ProjectJob : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? CostCentreId { get; set; }
    public CostCentre? CostCentre { get; set; }
    public bool IsClosed { get; set; }
}
