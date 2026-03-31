using Accounting.Domain.Enums;

namespace Accounting.Application.DTOs;

public sealed class BranchDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class CreateBranchRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class CurrencyDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int DecimalPlaces { get; init; }
}

public sealed class UpsertCurrencyRequest
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int DecimalPlaces { get; init; } = 2;
}

public sealed class ExchangeRateDto
{
    public int Id { get; init; }
    public string FromCurrencyCode { get; init; } = string.Empty;
    public string ToCurrencyCode { get; init; } = string.Empty;
    public decimal Rate { get; init; }
    public DateOnly EffectiveDate { get; init; }
}

public sealed class UpsertExchangeRateRequest
{
    public string FromCurrencyCode { get; init; } = string.Empty;
    public string ToCurrencyCode { get; init; } = string.Empty;
    public decimal Rate { get; init; }
    public DateOnly EffectiveDate { get; init; }
}

public sealed class DepartmentDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class CreateDepartmentRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class EmployeeDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Position { get; init; }
    public int? DepartmentId { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CreateEmployeeRequest
{
    public string Code { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Position { get; init; }
    public int? DepartmentId { get; init; }
}

public sealed class SalesLeadDto
{
    public int Id { get; init; }
    public string OrganizationName { get; init; } = string.Empty;
    public string? ContactName { get; init; }
    public string? Email { get; init; }
    public LeadStage Stage { get; init; }
    public bool IsClosed { get; init; }
    public decimal EstimatedValue { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
}

public sealed class CreateSalesLeadRequest
{
    public string OrganizationName { get; init; } = string.Empty;
    public string? ContactName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public decimal EstimatedValue { get; init; }
    public string CurrencyCode { get; init; } = "USD";
}

public sealed class CrmContactDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public int? CustomerId { get; init; }
    public int? SalesLeadId { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed class CreateCrmContactRequest
{
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public int? CustomerId { get; init; }
    public int? SalesLeadId { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed class PurchaseRequisitionDto
{
    public int Id { get; init; }
    public string DocumentNumber { get; init; } = string.Empty;
    public DateOnly RequestDate { get; init; }
    public PurchaseRequisitionStatus Status { get; init; }
    public int? ApprovedByUserId { get; init; }
    public DateTime? ApprovedAtUtc { get; init; }
    public string? RejectedReason { get; init; }
    public IReadOnlyList<PurchaseRequisitionLineDto> Lines { get; init; } = Array.Empty<PurchaseRequisitionLineDto>();
}

public sealed class PurchaseRequisitionLineDto
{
    public int Id { get; init; }
    public int LineNumber { get; init; }
    public string Description { get; init; } = string.Empty;
    public int? StockItemId { get; init; }
    public decimal Quantity { get; init; }
    public decimal EstimatedUnitCost { get; init; }
}

public sealed class CreatePurchaseRequisitionRequest
{
    public DateOnly RequestDate { get; init; }
    public int? BranchId { get; init; }
    public int? DepartmentId { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<CreatePurchaseRequisitionLineRequest> Lines { get; init; } = Array.Empty<CreatePurchaseRequisitionLineRequest>();
}

public sealed class CreatePurchaseRequisitionLineRequest
{
    public string Description { get; init; } = string.Empty;
    public int? StockItemId { get; init; }
    public decimal Quantity { get; init; }
    public decimal EstimatedUnitCost { get; init; }
}

public sealed class BomHeaderDto
{
    public int Id { get; init; }
    public int ParentStockItemId { get; init; }
    public string Version { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public IReadOnlyList<BomLineDto> Lines { get; init; } = Array.Empty<BomLineDto>();
}

public sealed class BomLineDto
{
    public int Id { get; init; }
    public int LineNumber { get; init; }
    public int ComponentStockItemId { get; init; }
    public decimal QuantityPer { get; init; }
    public decimal ScrapPercent { get; init; }
}

public sealed class CreateBomRequest
{
    public int ParentStockItemId { get; init; }
    public string Version { get; init; } = "1";
    public IReadOnlyList<CreateBomLineRequest> Lines { get; init; } = Array.Empty<CreateBomLineRequest>();
}

public sealed class CreateBomLineRequest
{
    public int ComponentStockItemId { get; init; }
    public decimal QuantityPer { get; init; }
    public decimal ScrapPercent { get; init; }
}

public sealed class WorkOrderDto
{
    public int Id { get; init; }
    public string DocumentNumber { get; init; } = string.Empty;
    public int StockItemId { get; init; }
    public int? BomHeaderId { get; init; }
    public int WarehouseId { get; init; }
    public decimal QuantityPlanned { get; init; }
    public decimal QuantityCompleted { get; init; }
    public WorkOrderStatus Status { get; init; }
    public DateTime? MaterialsIssuedAtUtc { get; init; }
}

public sealed class CreateWorkOrderRequest
{
    public int StockItemId { get; init; }
    public int? BomHeaderId { get; init; }
    public int WarehouseId { get; init; }
    public decimal QuantityPlanned { get; init; }
    public DateOnly? PlannedStart { get; init; }
    public DateOnly? PlannedEnd { get; init; }
}

public sealed class BudgetLineDto
{
    public int Id { get; init; }
    public int FiscalYearId { get; init; }
    public int LedgerAccountId { get; init; }
    public string? AccountCode { get; init; }
    public int PeriodNumber { get; init; }
    public decimal Amount { get; init; }
}

public sealed class UpsertBudgetLineRequest
{
    public int FiscalYearId { get; init; }
    public int LedgerAccountId { get; init; }
    public int PeriodNumber { get; init; }
    public decimal Amount { get; init; }
}

public sealed class ServiceTicketDto
{
    public int Id { get; init; }
    public string TicketNumber { get; init; } = string.Empty;
    public int CustomerId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public ServiceTicketPriority Priority { get; init; }
    public ServiceTicketStatus Status { get; init; }
    public int? AssignedToUserId { get; init; }
    public DateTime OpenedAtUtc { get; init; }
    public DateTime? ClosedAtUtc { get; init; }
}

public sealed class CreateServiceTicketRequest
{
    public int CustomerId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public ServiceTicketPriority Priority { get; init; } = ServiceTicketPriority.Normal;
}

public sealed class CompanyErpModuleDto
{
    public int ModuleCode { get; init; }
    public string ModuleName { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
}

public sealed class SetErpModuleRequest
{
    public ModuleCode ModuleCode { get; init; }
    public bool IsEnabled { get; init; }
}

public sealed class ErpCapabilityDto
{
    public string Area { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
