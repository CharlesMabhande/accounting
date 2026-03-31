namespace Accounting.Domain.Entities;

public class Company : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
}
