using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class CompanyErpModule : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public ModuleCode ModuleCode { get; set; }
    public bool IsEnabled { get; set; } = true;
}
