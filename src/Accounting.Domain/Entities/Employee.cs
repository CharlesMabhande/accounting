namespace Accounting.Domain.Entities;

public class Employee : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Position { get; set; }
    public DateOnly? HireDate { get; set; }
    public bool IsActive { get; set; } = true;
}
