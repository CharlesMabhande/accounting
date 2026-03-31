namespace Accounting.Application.DTOs;

public sealed class CreateFixedAssetRequest
{
    public int CompanyId { get; init; }
    public string AssetNumber { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateOnly AcquisitionDate { get; init; }
    public decimal Cost { get; init; }
    public int AssetAccountId { get; init; }
    public int DepreciationExpenseAccountId { get; init; }
    public int AccumulatedDepreciationAccountId { get; init; }
}

public sealed class PostDepreciationRequest
{
    public int CompanyId { get; init; }
    public int FixedAssetId { get; init; }
    public DateOnly EntryDate { get; init; }
    public decimal Amount { get; init; }
    public string Reference { get; init; } = string.Empty;
}
