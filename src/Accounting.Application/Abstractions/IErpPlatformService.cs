using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Enums;

namespace Accounting.Application.Abstractions;

/// <summary>Cross-module ERP operations (organization, CRM, manufacturing, procurement, service, budgeting).</summary>
public interface IErpPlatformService
{
    Task<IReadOnlyList<BranchDto>> ListBranchesAsync(int companyId, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> CreateBranchAsync(int companyId, CreateBranchRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CurrencyDto>> ListCurrenciesAsync(CancellationToken cancellationToken = default);
    Task<OperationResult<int>> UpsertCurrencyAsync(UpsertCurrencyRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteCurrencyAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExchangeRateDto>> ListExchangeRatesAsync(int companyId, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> UpsertExchangeRateAsync(int companyId, UpsertExchangeRateRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteExchangeRateAsync(int companyId, int exchangeRateId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentDto>> ListDepartmentsAsync(int companyId, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> CreateDepartmentAsync(int companyId, CreateDepartmentRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeDto>> ListEmployeesAsync(int companyId, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> CreateEmployeeAsync(int companyId, CreateEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesLeadDto>> ListSalesLeadsAsync(int companyId, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> CreateSalesLeadAsync(int companyId, CreateSalesLeadRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> SetLeadStageAsync(int companyId, int leadId, LeadStage stage, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrmContactDto>> ListCrmContactsAsync(int companyId, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> CreateCrmContactAsync(int companyId, CreateCrmContactRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseRequisitionDto>> ListPurchaseRequisitionsAsync(int companyId, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> CreatePurchaseRequisitionAsync(int companyId, CreatePurchaseRequisitionRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> SubmitPurchaseRequisitionAsync(int companyId, int id, CancellationToken cancellationToken = default);
    Task<OperationResult> ApprovePurchaseRequisitionAsync(int companyId, int id, int? approvedByUserId, CancellationToken cancellationToken = default);
    Task<OperationResult> RejectPurchaseRequisitionAsync(int companyId, int id, string? reason, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BomHeaderDto>> ListBomsAsync(int companyId, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> CreateBomAsync(int companyId, CreateBomRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkOrderDto>> ListWorkOrdersAsync(int companyId, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> CreateWorkOrderAsync(int companyId, CreateWorkOrderRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> SetWorkOrderStatusAsync(int companyId, int id, WorkOrderStatus status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BudgetLineDto>> ListBudgetLinesAsync(int companyId, int? fiscalYearId, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> UpsertBudgetLineAsync(int companyId, UpsertBudgetLineRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceTicketDto>> ListServiceTicketsAsync(int companyId, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> CreateServiceTicketAsync(int companyId, CreateServiceTicketRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> CloseServiceTicketAsync(int companyId, int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyErpModuleDto>> ListCompanyErpModulesAsync(int companyId, CancellationToken cancellationToken = default);
    Task<OperationResult> SetCompanyErpModuleAsync(int companyId, SetErpModuleRequest request, CancellationToken cancellationToken = default);

    IReadOnlyList<ErpCapabilityDto> GetErpCapabilities();
}
