namespace Accounting.Domain.Entities;

public class FixedAsset : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string AssetNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly AcquisitionDate { get; set; }
    public decimal Cost { get; set; }
    public int AssetAccountId { get; set; }
    public LedgerAccount AssetAccount { get; set; } = null!;
    /// <summary>P&amp;L depreciation expense account.</summary>
    public int DepreciationExpenseAccountId { get; set; }
    public LedgerAccount DepreciationExpenseAccount { get; set; } = null!;
    public int AccumulatedDepreciationAccountId { get; set; }
    public LedgerAccount AccumulatedDepreciationAccount { get; set; } = null!;
    public bool IsDisposed { get; set; }
}
