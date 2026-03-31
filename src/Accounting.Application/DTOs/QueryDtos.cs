namespace Accounting.Application.DTOs;

public sealed class TaxCodeQueryDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal RatePercent { get; init; }
    public bool IsActive { get; init; }
    public int? OutputTaxLedgerAccountId { get; init; }
    public int? InputTaxLedgerAccountId { get; init; }
}

public sealed class CompanyQueryDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string BaseCurrency { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class WarehouseQueryDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class AuditLogQueryDto
{
    public int Id { get; init; }
    public int? UserId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string EntityName { get; init; } = string.Empty;
    public string? EntityKey { get; init; }
    public string Details { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; }
}
