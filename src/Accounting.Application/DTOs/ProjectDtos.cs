namespace Accounting.Application.DTOs;

public sealed class CreateCostCentreRequest
{
    public int CompanyId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class CreateProjectJobRequest
{
    public int CompanyId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int? CostCentreId { get; init; }
}
