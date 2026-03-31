using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class SalesLead : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string OrganizationName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public LeadStage Stage { get; set; } = LeadStage.New;
    public bool IsClosed { get; set; }
    public decimal EstimatedValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}
