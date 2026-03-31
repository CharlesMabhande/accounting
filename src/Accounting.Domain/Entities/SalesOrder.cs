using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class SalesOrder : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string OrderNumber { get; set; } = string.Empty;
    public DateOnly OrderDate { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
}
