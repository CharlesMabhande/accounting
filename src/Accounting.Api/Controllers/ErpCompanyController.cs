using Accounting.Application.Abstractions;
using Accounting.Application.DTOs;
using Accounting.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>Extended ERP: branches, HR org, CRM, procurement, manufacturing, budgets, service, module toggles.</summary>
[ApiController]
[Route("api/companies/{companyId:int}/erp")]
public sealed class ErpCompanyController : ControllerBase
{
    private readonly IErpPlatformService _erp;
    private readonly IWorkOrderManufacturingService _workOrderMfg;

    public ErpCompanyController(IErpPlatformService erp, IWorkOrderManufacturingService workOrderMfg)
    {
        _erp = erp;
        _workOrderMfg = workOrderMfg;
    }

    [HttpGet("branches")]
    public Task<IReadOnlyList<BranchDto>> Branches(int companyId, CancellationToken cancellationToken) =>
        _erp.ListBranchesAsync(companyId, cancellationToken);

    [HttpPost("branches")]
    public async Task<IActionResult> CreateBranch(int companyId, [FromBody] CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var r = await _erp.CreateBranchAsync(companyId, request, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpGet("exchange-rates")]
    public Task<IReadOnlyList<ExchangeRateDto>> ExchangeRates(int companyId, CancellationToken cancellationToken) =>
        _erp.ListExchangeRatesAsync(companyId, cancellationToken);

    [HttpPost("exchange-rates")]
    public async Task<IActionResult> UpsertExchangeRate(int companyId, [FromBody] UpsertExchangeRateRequest request, CancellationToken cancellationToken)
    {
        var r = await _erp.UpsertExchangeRateAsync(companyId, request, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpDelete("exchange-rates/{exchangeRateId:int}")]
    public async Task<IActionResult> DeleteExchangeRate(int companyId, int exchangeRateId, CancellationToken cancellationToken)
    {
        var r = await _erp.DeleteExchangeRateAsync(companyId, exchangeRateId, cancellationToken);
        return r.Success ? NoContent() : BadRequest(new { errors = r.Errors });
    }

    [HttpGet("departments")]
    public Task<IReadOnlyList<DepartmentDto>> Departments(int companyId, CancellationToken cancellationToken) =>
        _erp.ListDepartmentsAsync(companyId, cancellationToken);

    [HttpPost("departments")]
    public async Task<IActionResult> CreateDepartment(int companyId, [FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var r = await _erp.CreateDepartmentAsync(companyId, request, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpGet("employees")]
    public Task<IReadOnlyList<EmployeeDto>> Employees(int companyId, CancellationToken cancellationToken) =>
        _erp.ListEmployeesAsync(companyId, cancellationToken);

    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee(int companyId, [FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var r = await _erp.CreateEmployeeAsync(companyId, request, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpGet("crm/leads")]
    public Task<IReadOnlyList<SalesLeadDto>> Leads(int companyId, CancellationToken cancellationToken) =>
        _erp.ListSalesLeadsAsync(companyId, cancellationToken);

    [HttpPost("crm/leads")]
    public async Task<IActionResult> CreateLead(int companyId, [FromBody] CreateSalesLeadRequest request, CancellationToken cancellationToken)
    {
        var r = await _erp.CreateSalesLeadAsync(companyId, request, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("crm/leads/{leadId:int}/stage")]
    public async Task<IActionResult> SetLeadStage(int companyId, int leadId, [FromQuery] LeadStage stage, CancellationToken cancellationToken)
    {
        var r = await _erp.SetLeadStageAsync(companyId, leadId, stage, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpGet("crm/contacts")]
    public Task<IReadOnlyList<CrmContactDto>> Contacts(int companyId, CancellationToken cancellationToken) =>
        _erp.ListCrmContactsAsync(companyId, cancellationToken);

    [HttpPost("crm/contacts")]
    public async Task<IActionResult> CreateContact(int companyId, [FromBody] CreateCrmContactRequest request, CancellationToken cancellationToken)
    {
        var r = await _erp.CreateCrmContactAsync(companyId, request, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpGet("purchase-requisitions")]
    public Task<IReadOnlyList<PurchaseRequisitionDto>> PurchaseRequisitions(int companyId, CancellationToken cancellationToken) =>
        _erp.ListPurchaseRequisitionsAsync(companyId, cancellationToken);

    [HttpPost("purchase-requisitions")]
    public async Task<IActionResult> CreatePurchaseRequisition(int companyId, [FromBody] CreatePurchaseRequisitionRequest request, CancellationToken cancellationToken)
    {
        var r = await _erp.CreatePurchaseRequisitionAsync(companyId, request, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("purchase-requisitions/{id:int}/submit")]
    public async Task<IActionResult> SubmitPurchaseRequisition(int companyId, int id, CancellationToken cancellationToken)
    {
        var r = await _erp.SubmitPurchaseRequisitionAsync(companyId, id, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("purchase-requisitions/{id:int}/approve")]
    public async Task<IActionResult> ApprovePurchaseRequisition(int companyId, int id, [FromQuery] int? approvedByUserId, CancellationToken cancellationToken)
    {
        var r = await _erp.ApprovePurchaseRequisitionAsync(companyId, id, approvedByUserId, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("purchase-requisitions/{id:int}/reject")]
    public async Task<IActionResult> RejectPurchaseRequisition(int companyId, int id, [FromQuery] string? reason, CancellationToken cancellationToken)
    {
        var r = await _erp.RejectPurchaseRequisitionAsync(companyId, id, reason, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpGet("bom")]
    public Task<IReadOnlyList<BomHeaderDto>> Bom(int companyId, CancellationToken cancellationToken) =>
        _erp.ListBomsAsync(companyId, cancellationToken);

    [HttpPost("bom")]
    public async Task<IActionResult> CreateBom(int companyId, [FromBody] CreateBomRequest request, CancellationToken cancellationToken)
    {
        var r = await _erp.CreateBomAsync(companyId, request, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpGet("work-orders")]
    public Task<IReadOnlyList<WorkOrderDto>> WorkOrders(int companyId, CancellationToken cancellationToken) =>
        _erp.ListWorkOrdersAsync(companyId, cancellationToken);

    [HttpPost("work-orders")]
    public async Task<IActionResult> CreateWorkOrder(int companyId, [FromBody] CreateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var r = await _erp.CreateWorkOrderAsync(companyId, request, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("work-orders/{id:int}/status")]
    public async Task<IActionResult> SetWorkOrderStatus(int companyId, int id, [FromQuery] WorkOrderStatus status, CancellationToken cancellationToken)
    {
        var r = await _erp.SetWorkOrderStatusAsync(companyId, id, status, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("work-orders/{id:int}/issue-materials")]
    public async Task<IActionResult> IssueWorkOrderMaterials(int companyId, int id, CancellationToken cancellationToken)
    {
        var r = await _workOrderMfg.IssueMaterialsAsync(companyId, id, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpGet("budget-lines")]
    public Task<IReadOnlyList<BudgetLineDto>> BudgetLines(int companyId, [FromQuery] int? fiscalYearId, CancellationToken cancellationToken) =>
        _erp.ListBudgetLinesAsync(companyId, fiscalYearId, cancellationToken);

    [HttpPost("budget-lines")]
    public async Task<IActionResult> UpsertBudgetLine(int companyId, [FromBody] UpsertBudgetLineRequest request, CancellationToken cancellationToken)
    {
        var r = await _erp.UpsertBudgetLineAsync(companyId, request, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpGet("service-tickets")]
    public Task<IReadOnlyList<ServiceTicketDto>> ServiceTickets(int companyId, CancellationToken cancellationToken) =>
        _erp.ListServiceTicketsAsync(companyId, cancellationToken);

    [HttpPost("service-tickets")]
    public async Task<IActionResult> CreateServiceTicket(int companyId, [FromBody] CreateServiceTicketRequest request, CancellationToken cancellationToken)
    {
        var r = await _erp.CreateServiceTicketAsync(companyId, request, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("service-tickets/{id:int}/close")]
    public async Task<IActionResult> CloseServiceTicket(int companyId, int id, CancellationToken cancellationToken)
    {
        var r = await _erp.CloseServiceTicketAsync(companyId, id, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpGet("modules")]
    public Task<IReadOnlyList<CompanyErpModuleDto>> Modules(int companyId, CancellationToken cancellationToken) =>
        _erp.ListCompanyErpModulesAsync(companyId, cancellationToken);

    [HttpPost("modules")]
    public async Task<IActionResult> SetModule(int companyId, [FromBody] SetErpModuleRequest request, CancellationToken cancellationToken)
    {
        var r = await _erp.SetCompanyErpModuleAsync(companyId, request, cancellationToken);
        return r.Success ? Ok(r) : BadRequest(r);
    }
}
