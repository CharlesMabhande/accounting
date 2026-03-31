using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class WorkOrder : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string DocumentNumber { get; set; } = string.Empty;
    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;
    public int? BomHeaderId { get; set; }
    public BomHeader? BomHeader { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public decimal QuantityPlanned { get; set; }
    public decimal QuantityCompleted { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Planned;
    public DateOnly? PlannedStart { get; set; }
    public DateOnly? PlannedEnd { get; set; }
    public DateTime? MaterialsIssuedAtUtc { get; set; }
}
