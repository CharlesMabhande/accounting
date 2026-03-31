namespace Accounting.Domain.Entities;

public class BomHeader : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int ParentStockItemId { get; set; }
    public StockItem ParentStockItem { get; set; } = null!;
    public string Version { get; set; } = "1";
    public bool IsActive { get; set; } = true;
    public ICollection<BomLine> Lines { get; set; } = new List<BomLine>();
}
