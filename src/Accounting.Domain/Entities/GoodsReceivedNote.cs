using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

/// <summary>Goods received voucher (GRV) — receipt against purchase orders / suppliers.</summary>
public class GoodsReceivedNote : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public string GrnNumber { get; set; } = string.Empty;
    public DateOnly ReceivedDate { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public ICollection<GoodsReceivedNoteLine> Lines { get; set; } = new List<GoodsReceivedNoteLine>();
}
