using Accounting.Application.Common;
using Accounting.Application.DTOs;

namespace Accounting.Application.Abstractions;

public interface ICustomerInvoiceService
{
    Task<OperationResult<CreatedEntityInfo>> CreateAsync(CreateCustomerInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<PostJournalInfo>> PostAsync(int invoiceId, int? userId, CancellationToken cancellationToken = default);
}

public interface ISupplierInvoiceService
{
    Task<OperationResult<CreatedEntityInfo>> CreateAsync(CreateSupplierInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<PostJournalInfo>> PostAsync(int invoiceId, int? userId, CancellationToken cancellationToken = default);
}

public interface ICashbookService
{
    Task<OperationResult<CreatedEntityInfo>> CreateAsync(CreateCashbookRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<PostJournalInfo>> PostAsync(int transactionId, int? userId, CancellationToken cancellationToken = default);
}

public interface IGoodsReceivedNoteService
{
    Task<OperationResult<CreatedEntityInfo>> CreateAsync(CreateGoodsReceivedNoteRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> PostAsync(int grnId, int? userId, CancellationToken cancellationToken = default);
}

public interface IStockService
{
    Task<OperationResult> PostIssueAsync(PostStockIssueRequest request, CancellationToken cancellationToken = default);
}

public interface ISalesOrderService
{
    Task<OperationResult<CreatedEntityInfo>> CreateAsync(CreateSalesOrderRequest request, CancellationToken cancellationToken = default);
}

public interface IPurchaseOrderService
{
    Task<OperationResult<CreatedEntityInfo>> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default);
}

public interface IFixedAssetService
{
    Task<OperationResult<int>> CreateAsync(CreateFixedAssetRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<PostJournalInfo>> PostDepreciationAsync(PostDepreciationRequest request, int? userId, CancellationToken cancellationToken = default);
}

public interface IPayrollService
{
    Task<OperationResult<CreatedEntityInfo>> CreateRunAsync(CreatePayrollRunRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<PostJournalInfo>> PostRunAsync(int payrollRunId, int? userId, CancellationToken cancellationToken = default);
}

public interface IReportingService
{
    Task<IReadOnlyList<TrialBalanceLineDto>> GetTrialBalanceAsync(int companyId, DateOnly asOfDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LedgerLineDto>> GetLedgerAsync(int companyId, int ledgerAccountId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<byte[]> ExportTrialBalanceExcelAsync(int companyId, DateOnly asOfDate, CancellationToken cancellationToken = default);
    Task<CustomerStatementDto?> GetCustomerStatementAsync(int companyId, int customerId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}

public interface IFiscalPeriodControlService
{
    Task<IReadOnlyList<FiscalPeriodDto>> ListAsync(int companyId, CancellationToken cancellationToken = default);
    Task<OperationResult> ClosePeriodAsync(int companyId, int periodId, CancellationToken cancellationToken = default);
    Task<OperationResult> ReopenPeriodAsync(int companyId, int periodId, CancellationToken cancellationToken = default);
}

public interface IAuditService
{
    Task LogAsync(int? userId, string action, string entityName, string? entityKey, string details, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogQueryDto>> ListAsync(int? limit, CancellationToken cancellationToken = default);
}

public interface IAuthService
{
    Task<OperationResult<LoginResponseDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<SessionInfoDto>> GetSessionAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string sessionToken, CancellationToken cancellationToken = default);
}

public interface ICostCentreService
{
    Task<OperationResult<int>> CreateAsync(CreateCostCentreRequest request, CancellationToken cancellationToken = default);
}

public interface IProjectJobService
{
    Task<OperationResult<int>> CreateAsync(CreateProjectJobRequest request, CancellationToken cancellationToken = default);
}

public interface ITaxCodeQueryService
{
    Task<IReadOnlyList<TaxCodeQueryDto>> ListAsync(int companyId, CancellationToken cancellationToken = default);
}

public interface ICompanyQueryService
{
    Task<IReadOnlyList<CompanyQueryDto>> ListAsync(CancellationToken cancellationToken = default);
}

public interface IWarehouseQueryService
{
    Task<IReadOnlyList<WarehouseQueryDto>> ListAsync(int companyId, CancellationToken cancellationToken = default);
}

public interface IUserAccountAdminService
{
    Task<IReadOnlyList<UserAccountListDto>> ListAsync(int? limit, CancellationToken cancellationToken = default);
    Task<UserAccountDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<CreatedEntityInfo>> CreateAsync(CreateUserAccountRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateAsync(int id, UpdateUserAccountRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> DeactivateAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IRoleAdminService
{
    Task<IReadOnlyList<RoleListDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<RoleDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<CreatedEntityInfo>> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IAuditTableSettingsService
{
    Task<IReadOnlyList<AuditTableSettingDto>> ListAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(SaveAuditTableSettingsRequest request, CancellationToken cancellationToken = default);
}
