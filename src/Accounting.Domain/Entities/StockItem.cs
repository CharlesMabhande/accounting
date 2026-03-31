namespace Accounting.Domain.Entities;

/// <summary>Inventory item — extended with StkItem-style dimensions (e.g. belts, cut lengths).</summary>
public class StockItem : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>Maps from legacy <c>cExtDescription</c> / long text.</summary>
    public string? LongDescription { get; set; }
    /// <summary>Maps from legacy <c>cSimpleCode</c> / alternate lookup.</summary>
    public string? AlternateCode { get; set; }
    public string UnitOfMeasure { get; set; } = "EA";
    public int InventoryAccountId { get; set; }
    public LedgerAccount InventoryAccount { get; set; } = null!;
    public int CostOfSalesAccountId { get; set; }
    public LedgerAccount CostOfSalesAccount { get; set; } = null!;
    public bool TrackSerialNumbers { get; set; }
    public bool TrackLots { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Legacy <c>ServiceItem</c> — non-stock service line.</summary>
    public bool IsServiceItem { get; set; }
    /// <summary>Optional target gross-profit % (legacy <c>fStockGPPercent</c>).</summary>
    public decimal? TargetGpPercent { get; set; }
    /// <summary>Buy/sell dimensions for configurable products (legacy import).</summary>
    public decimal? BuyLength { get; set; }
    public decimal? BuyWidth { get; set; }
    public decimal? BuyHeight { get; set; }
    public decimal? SellLength { get; set; }
    public decimal? SellWidth { get; set; }
    public decimal? SellHeight { get; set; }
    public decimal? Weight { get; set; }
    public string? WeightUnit { get; set; }
    /// <summary>Legacy <c>cMeasurement</c> notes.</summary>
    public string? MeasurementNotes { get; set; }
}
