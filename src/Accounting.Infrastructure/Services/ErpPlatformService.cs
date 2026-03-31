using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class ErpPlatformService : IErpPlatformService
{
    private readonly AccountingDbContext _db;

    public ErpPlatformService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BranchDto>> ListBranchesAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _db.Branches.AsNoTracking()
            .Where(b => b.CompanyId == companyId)
            .OrderBy(b => b.Code)
            .Select(b => new BranchDto { Id = b.Id, Code = b.Code, Name = b.Name, IsActive = b.IsActive })
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> CreateBranchAsync(int companyId, CreateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.MultiSite, cancellationToken);
        if (denied is not null)
            return OperationResult<int>.Fail(denied);
        if (string.IsNullOrWhiteSpace(request.Code))
            return OperationResult<int>.Fail("Code is required.");
        var b = new Branch { CompanyId = companyId, Code = request.Code.Trim(), Name = request.Name.Trim(), IsActive = true };
        _db.Branches.Add(b);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Ok(b.Id);
    }

    public async Task<IReadOnlyList<CurrencyDto>> ListCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Currencies.AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new CurrencyDto { Id = c.Id, Code = c.Code, Name = c.Name, DecimalPlaces = c.DecimalPlaces })
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> UpsertCurrencyAsync(UpsertCurrencyRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 10)
            return OperationResult<int>.Fail("Currency code is required (max 10 characters).");
        var dp = Math.Clamp(request.DecimalPlaces, 0, 6);
        var name = string.IsNullOrWhiteSpace(request.Name) ? code : request.Name.Trim();

        if (request.Id > 0)
        {
            var c = await _db.Currencies.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (c is null)
                return OperationResult<int>.Fail("Currency not found.");
            if (!string.Equals(c.Code, code, StringComparison.Ordinal))
                return OperationResult<int>.Fail("Currency code cannot be changed. Delete and add a new code if needed.");
            c.Name = name;
            c.DecimalPlaces = dp;
            await _db.SaveChangesAsync(cancellationToken);
            return OperationResult<int>.Ok(c.Id);
        }

        if (await _db.Currencies.AnyAsync(x => x.Code == code, cancellationToken))
            return OperationResult<int>.Fail("Currency code already exists.");
        var n = new Currency { Code = code, Name = name, DecimalPlaces = dp };
        _db.Currencies.Add(n);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Ok(n.Id);
    }

    public async Task<OperationResult> DeleteCurrencyAsync(int id, CancellationToken cancellationToken = default)
    {
        var c = await _db.Currencies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (c is null)
            return OperationResult.Fail("Currency not found.");
        var code = c.Code;
        if (await _db.Companies.AnyAsync(co => co.BaseCurrency == code, cancellationToken))
            return OperationResult.Fail("A company uses this currency as base currency.");
        if (await _db.ExchangeRates.AnyAsync(e => e.FromCurrencyCode == code || e.ToCurrencyCode == code, cancellationToken))
            return OperationResult.Fail("Remove or re-point exchange rates that use this currency first.");
        if (await _db.Customers.AnyAsync(x => x.CurrencyCode == code, cancellationToken))
            return OperationResult.Fail("Customers reference this currency.");
        if (await _db.Suppliers.AnyAsync(x => x.CurrencyCode == code, cancellationToken))
            return OperationResult.Fail("Suppliers reference this currency.");
        if (await _db.BankAccounts.AnyAsync(x => x.CurrencyCode == code, cancellationToken))
            return OperationResult.Fail("Bank accounts reference this currency.");
        _db.Currencies.Remove(c);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<IReadOnlyList<ExchangeRateDto>> ListExchangeRatesAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _db.ExchangeRates.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.EffectiveDate)
            .Select(x => new ExchangeRateDto
            {
                Id = x.Id,
                FromCurrencyCode = x.FromCurrencyCode,
                ToCurrencyCode = x.ToCurrencyCode,
                Rate = x.Rate,
                EffectiveDate = x.EffectiveDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> UpsertExchangeRateAsync(int companyId, UpsertExchangeRateRequest request, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.MultiCurrency, cancellationToken);
        if (denied is not null)
            return OperationResult<int>.Fail(denied);
        var from = request.FromCurrencyCode.Trim().ToUpperInvariant();
        var to = request.ToCurrencyCode.Trim().ToUpperInvariant();
        if (!await _db.Currencies.AnyAsync(c => c.Code == from, cancellationToken)
            || !await _db.Currencies.AnyAsync(c => c.Code == to, cancellationToken))
            return OperationResult<int>.Fail("Unknown currency code.");

        var existing = await _db.ExchangeRates
            .FirstOrDefaultAsync(
                x => x.CompanyId == companyId && x.FromCurrencyCode == from && x.ToCurrencyCode == to && x.EffectiveDate == request.EffectiveDate,
                cancellationToken);
        if (existing is not null)
        {
            existing.Rate = request.Rate;
            await _db.SaveChangesAsync(cancellationToken);
            return OperationResult<int>.Ok(existing.Id);
        }

        var row = new ExchangeRate
        {
            CompanyId = companyId,
            FromCurrencyCode = from,
            ToCurrencyCode = to,
            Rate = request.Rate,
            EffectiveDate = request.EffectiveDate
        };
        _db.ExchangeRates.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Ok(row.Id);
    }

    public async Task<OperationResult> DeleteExchangeRateAsync(int companyId, int exchangeRateId, CancellationToken cancellationToken = default)
    {
        var row = await _db.ExchangeRates.FirstOrDefaultAsync(
            x => x.Id == exchangeRateId && x.CompanyId == companyId, cancellationToken);
        if (row is null)
            return OperationResult.Fail("Exchange rate not found.");
        _db.ExchangeRates.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<IReadOnlyList<DepartmentDto>> ListDepartmentsAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _db.Departments.AsNoTracking()
            .Where(d => d.CompanyId == companyId)
            .OrderBy(d => d.Code)
            .Select(d => new DepartmentDto { Id = d.Id, Code = d.Code, Name = d.Name, IsActive = d.IsActive })
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> CreateDepartmentAsync(int companyId, CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.HumanResources, cancellationToken);
        if (denied is not null)
            return OperationResult<int>.Fail(denied);
        if (string.IsNullOrWhiteSpace(request.Code))
            return OperationResult<int>.Fail("Code is required.");
        var d = new Department { CompanyId = companyId, Code = request.Code.Trim(), Name = request.Name.Trim(), IsActive = true };
        _db.Departments.Add(d);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Ok(d.Id);
    }

    public async Task<IReadOnlyList<EmployeeDto>> ListEmployeesAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _db.Employees.AsNoTracking()
            .Where(e => e.CompanyId == companyId)
            .OrderBy(e => e.Code)
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                Code = e.Code,
                FullName = e.FullName,
                Email = e.Email,
                Position = e.Position,
                DepartmentId = e.DepartmentId,
                IsActive = e.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> CreateEmployeeAsync(int companyId, CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.HumanResources, cancellationToken);
        if (denied is not null)
            return OperationResult<int>.Fail(denied);
        if (string.IsNullOrWhiteSpace(request.Code))
            return OperationResult<int>.Fail("Code is required.");
        var e = new Employee
        {
            CompanyId = companyId,
            Code = request.Code.Trim(),
            FullName = request.FullName.Trim(),
            Email = request.Email,
            Position = request.Position,
            DepartmentId = request.DepartmentId,
            IsActive = true
        };
        _db.Employees.Add(e);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Ok(e.Id);
    }

    public async Task<IReadOnlyList<SalesLeadDto>> ListSalesLeadsAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _db.SalesLeads.AsNoTracking()
            .Where(l => l.CompanyId == companyId)
            .OrderByDescending(l => l.Id)
            .Select(l => new SalesLeadDto
            {
                Id = l.Id,
                OrganizationName = l.OrganizationName,
                ContactName = l.ContactName,
                Email = l.Email,
                Stage = l.Stage,
                IsClosed = l.IsClosed,
                EstimatedValue = l.EstimatedValue,
                CurrencyCode = l.CurrencyCode
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> CreateSalesLeadAsync(int companyId, CreateSalesLeadRequest request, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.Crm, cancellationToken);
        if (denied is not null)
            return OperationResult<int>.Fail(denied);
        var l = new SalesLead
        {
            CompanyId = companyId,
            OrganizationName = request.OrganizationName.Trim(),
            ContactName = request.ContactName,
            Email = request.Email,
            Phone = request.Phone,
            EstimatedValue = request.EstimatedValue,
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "USD" : request.CurrencyCode.Trim().ToUpperInvariant(),
            Stage = LeadStage.New
        };
        _db.SalesLeads.Add(l);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Ok(l.Id);
    }

    public async Task<OperationResult> SetLeadStageAsync(int companyId, int leadId, LeadStage stage, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.Crm, cancellationToken);
        if (denied is not null)
            return OperationResult.Fail(denied);
        var l = await _db.SalesLeads.FirstOrDefaultAsync(x => x.Id == leadId && x.CompanyId == companyId, cancellationToken);
        if (l is null)
            return OperationResult.Fail("Lead not found.");
        l.Stage = stage;
        l.IsClosed = stage is LeadStage.Won or LeadStage.Lost;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<IReadOnlyList<CrmContactDto>> ListCrmContactsAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _db.CrmContacts.AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.FullName)
            .Select(c => new CrmContactDto
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                Phone = c.Phone,
                CustomerId = c.CustomerId,
                SalesLeadId = c.SalesLeadId,
                IsPrimary = c.IsPrimary
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> CreateCrmContactAsync(int companyId, CreateCrmContactRequest request, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.Crm, cancellationToken);
        if (denied is not null)
            return OperationResult<int>.Fail(denied);
        var c = new CrmContact
        {
            CompanyId = companyId,
            FullName = request.FullName.Trim(),
            Email = request.Email,
            Phone = request.Phone,
            CustomerId = request.CustomerId,
            SalesLeadId = request.SalesLeadId,
            IsPrimary = request.IsPrimary
        };
        _db.CrmContacts.Add(c);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Ok(c.Id);
    }

    public async Task<IReadOnlyList<PurchaseRequisitionDto>> ListPurchaseRequisitionsAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var list = await _db.PurchaseRequisitions.AsNoTracking()
            .Include(p => p.Lines)
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.Id)
            .ToListAsync(cancellationToken);

        return list.Select(p => new PurchaseRequisitionDto
        {
            Id = p.Id,
            DocumentNumber = p.DocumentNumber,
            RequestDate = p.RequestDate,
            Status = p.Status,
            ApprovedByUserId = p.ApprovedByUserId,
            ApprovedAtUtc = p.ApprovedAtUtc,
            RejectedReason = p.RejectedReason,
            Lines = p.Lines.OrderBy(l => l.LineNumber).Select(l => new PurchaseRequisitionLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                Description = l.Description,
                StockItemId = l.StockItemId,
                Quantity = l.Quantity,
                EstimatedUnitCost = l.EstimatedUnitCost
            }).ToList()
        }).ToList();
    }

    public async Task<OperationResult<int>> CreatePurchaseRequisitionAsync(int companyId, CreatePurchaseRequisitionRequest request, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.Procurement, cancellationToken);
        if (denied is not null)
            return OperationResult<int>.Fail(denied);
        var docNo = await DocumentSequenceHelper.NextAsync(_db, companyId, "PREQ", "PREQ", cancellationToken);
        var pr = new PurchaseRequisition
        {
            CompanyId = companyId,
            DocumentNumber = docNo,
            RequestDate = request.RequestDate,
            Status = PurchaseRequisitionStatus.Draft,
            BranchId = request.BranchId,
            DepartmentId = request.DepartmentId,
            Notes = request.Notes
        };
        var n = 1;
        foreach (var line in request.Lines)
        {
            pr.Lines.Add(new PurchaseRequisitionLine
            {
                LineNumber = n++,
                Description = line.Description,
                StockItemId = line.StockItemId,
                Quantity = line.Quantity,
                EstimatedUnitCost = line.EstimatedUnitCost
            });
        }

        _db.PurchaseRequisitions.Add(pr);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Ok(pr.Id);
    }

    public async Task<OperationResult> SubmitPurchaseRequisitionAsync(int companyId, int id, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.Procurement, cancellationToken);
        if (denied is not null)
            return OperationResult.Fail(denied);
        var pr = await _db.PurchaseRequisitions.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken);
        if (pr is null)
            return OperationResult.Fail("Requisition not found.");
        if (pr.Status != PurchaseRequisitionStatus.Draft)
            return OperationResult.Fail("Only draft requisitions can be submitted.");
        pr.Status = PurchaseRequisitionStatus.Submitted;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> ApprovePurchaseRequisitionAsync(int companyId, int id, int? approvedByUserId, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.Procurement, cancellationToken);
        if (denied is not null)
            return OperationResult.Fail(denied);
        var pr = await _db.PurchaseRequisitions.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken);
        if (pr is null)
            return OperationResult.Fail("Requisition not found.");
        if (pr.Status != PurchaseRequisitionStatus.Submitted)
            return OperationResult.Fail("Only submitted requisitions can be approved.");
        pr.Status = PurchaseRequisitionStatus.Approved;
        pr.ApprovedByUserId = approvedByUserId;
        pr.ApprovedAtUtc = DateTime.UtcNow;
        pr.RejectedReason = null;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> RejectPurchaseRequisitionAsync(int companyId, int id, string? reason, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.Procurement, cancellationToken);
        if (denied is not null)
            return OperationResult.Fail(denied);
        var pr = await _db.PurchaseRequisitions.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken);
        if (pr is null)
            return OperationResult.Fail("Requisition not found.");
        if (pr.Status != PurchaseRequisitionStatus.Submitted)
            return OperationResult.Fail("Only submitted requisitions can be rejected.");
        pr.Status = PurchaseRequisitionStatus.Rejected;
        pr.RejectedReason = string.IsNullOrWhiteSpace(reason) ? "Rejected" : reason.Trim();
        pr.ApprovedByUserId = null;
        pr.ApprovedAtUtc = null;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<IReadOnlyList<BomHeaderDto>> ListBomsAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var list = await _db.BomHeaders.AsNoTracking()
            .Include(b => b.Lines)
            .Where(b => b.CompanyId == companyId)
            .OrderBy(b => b.ParentStockItemId)
            .ToListAsync(cancellationToken);

        return list.Select(b => new BomHeaderDto
        {
            Id = b.Id,
            ParentStockItemId = b.ParentStockItemId,
            Version = b.Version,
            IsActive = b.IsActive,
            Lines = b.Lines.OrderBy(l => l.LineNumber).Select(l => new BomLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ComponentStockItemId = l.ComponentStockItemId,
                QuantityPer = l.QuantityPer,
                ScrapPercent = l.ScrapPercent
            }).ToList()
        }).ToList();
    }

    public async Task<OperationResult<int>> CreateBomAsync(int companyId, CreateBomRequest request, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.Manufacturing, cancellationToken);
        if (denied is not null)
            return OperationResult<int>.Fail(denied);
        if (request.Lines.Count == 0)
            return OperationResult<int>.Fail("BOM requires at least one component line.");

        var parent = await _db.StockItems.FirstOrDefaultAsync(s => s.Id == request.ParentStockItemId && s.CompanyId == companyId, cancellationToken);
        if (parent is null)
            return OperationResult<int>.Fail("Parent stock item not found.");

        var bom = new BomHeader
        {
            CompanyId = companyId,
            ParentStockItemId = request.ParentStockItemId,
            Version = string.IsNullOrWhiteSpace(request.Version) ? "1" : request.Version.Trim(),
            IsActive = true
        };
        var n = 1;
        foreach (var line in request.Lines)
        {
            var comp = await _db.StockItems.FirstOrDefaultAsync(s => s.Id == line.ComponentStockItemId && s.CompanyId == companyId, cancellationToken);
            if (comp is null)
                return OperationResult<int>.Fail($"Component stock item {line.ComponentStockItemId} not found.");
            bom.Lines.Add(new BomLine
            {
                LineNumber = n++,
                ComponentStockItemId = line.ComponentStockItemId,
                QuantityPer = line.QuantityPer <= 0 ? 1 : line.QuantityPer,
                ScrapPercent = line.ScrapPercent
            });
        }

        _db.BomHeaders.Add(bom);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Ok(bom.Id);
    }

    public async Task<IReadOnlyList<WorkOrderDto>> ListWorkOrdersAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _db.WorkOrders.AsNoTracking()
            .Where(w => w.CompanyId == companyId)
            .OrderByDescending(w => w.Id)
            .Select(w => new WorkOrderDto
            {
                Id = w.Id,
                DocumentNumber = w.DocumentNumber,
                StockItemId = w.StockItemId,
                BomHeaderId = w.BomHeaderId,
                WarehouseId = w.WarehouseId,
                QuantityPlanned = w.QuantityPlanned,
                QuantityCompleted = w.QuantityCompleted,
                Status = w.Status,
                MaterialsIssuedAtUtc = w.MaterialsIssuedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> CreateWorkOrderAsync(int companyId, CreateWorkOrderRequest request, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.Manufacturing, cancellationToken);
        if (denied is not null)
            return OperationResult<int>.Fail(denied);
        var item = await _db.StockItems.FirstOrDefaultAsync(s => s.Id == request.StockItemId && s.CompanyId == companyId, cancellationToken);
        if (item is null)
            return OperationResult<int>.Fail("Stock item not found.");
        var wh = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == request.WarehouseId && w.CompanyId == companyId, cancellationToken);
        if (wh is null)
            return OperationResult<int>.Fail("Warehouse not found.");

        if (request.BomHeaderId is not null)
        {
            var bom = await _db.BomHeaders.FirstOrDefaultAsync(
                b => b.Id == request.BomHeaderId && b.CompanyId == companyId && b.ParentStockItemId == request.StockItemId,
                cancellationToken);
            if (bom is null)
                return OperationResult<int>.Fail("BOM not found for this item.");
        }

        var docNo = await DocumentSequenceHelper.NextAsync(_db, companyId, "WO", "WO", cancellationToken);
        var wo = new WorkOrder
        {
            CompanyId = companyId,
            DocumentNumber = docNo,
            StockItemId = request.StockItemId,
            BomHeaderId = request.BomHeaderId,
            WarehouseId = request.WarehouseId,
            QuantityPlanned = request.QuantityPlanned,
            QuantityCompleted = 0,
            Status = WorkOrderStatus.Planned,
            PlannedStart = request.PlannedStart,
            PlannedEnd = request.PlannedEnd
        };
        _db.WorkOrders.Add(wo);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Ok(wo.Id);
    }

    public async Task<OperationResult> SetWorkOrderStatusAsync(int companyId, int id, WorkOrderStatus status, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.Manufacturing, cancellationToken);
        if (denied is not null)
            return OperationResult.Fail(denied);
        var wo = await _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == companyId, cancellationToken);
        if (wo is null)
            return OperationResult.Fail("Work order not found.");
        wo.Status = status;
        if (status == WorkOrderStatus.Completed)
            wo.QuantityCompleted = wo.QuantityPlanned;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<IReadOnlyList<BudgetLineDto>> ListBudgetLinesAsync(int companyId, int? fiscalYearId, CancellationToken cancellationToken = default)
    {
        var q =
            from b in _db.BudgetLines.AsNoTracking()
            join a in _db.LedgerAccounts.AsNoTracking() on b.LedgerAccountId equals a.Id
            where b.CompanyId == companyId && a.CompanyId == companyId
            select new { b, a.Code };

        if (fiscalYearId is not null)
            q = q.Where(x => x.b.FiscalYearId == fiscalYearId);

        var rows = await q
            .OrderBy(x => x.b.FiscalYearId)
            .ThenBy(x => x.b.PeriodNumber)
            .ThenBy(x => x.b.LedgerAccountId)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new BudgetLineDto
        {
            Id = x.b.Id,
            FiscalYearId = x.b.FiscalYearId,
            LedgerAccountId = x.b.LedgerAccountId,
            AccountCode = x.Code,
            PeriodNumber = x.b.PeriodNumber,
            Amount = x.b.Amount
        }).ToList();
    }

    public async Task<OperationResult<int>> UpsertBudgetLineAsync(int companyId, UpsertBudgetLineRequest request, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.Budgeting, cancellationToken);
        if (denied is not null)
            return OperationResult<int>.Fail(denied);
        var fy = await _db.FiscalYears.FirstOrDefaultAsync(f => f.Id == request.FiscalYearId && f.CompanyId == companyId, cancellationToken);
        if (fy is null)
            return OperationResult<int>.Fail("Fiscal year not found.");
        var acc = await _db.LedgerAccounts.FirstOrDefaultAsync(
            a => a.Id == request.LedgerAccountId && a.CompanyId == companyId,
            cancellationToken);
        if (acc is null)
            return OperationResult<int>.Fail("Ledger account not found.");

        var existing = await _db.BudgetLines.FirstOrDefaultAsync(
            b => b.CompanyId == companyId
                 && b.FiscalYearId == request.FiscalYearId
                 && b.LedgerAccountId == request.LedgerAccountId
                 && b.PeriodNumber == request.PeriodNumber,
            cancellationToken);
        if (existing is not null)
        {
            existing.Amount = request.Amount;
            await _db.SaveChangesAsync(cancellationToken);
            return OperationResult<int>.Ok(existing.Id);
        }

        var row = new BudgetLine
        {
            CompanyId = companyId,
            FiscalYearId = request.FiscalYearId,
            LedgerAccountId = request.LedgerAccountId,
            PeriodNumber = request.PeriodNumber,
            Amount = request.Amount
        };
        _db.BudgetLines.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Ok(row.Id);
    }

    public async Task<IReadOnlyList<ServiceTicketDto>> ListServiceTicketsAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _db.ServiceTickets.AsNoTracking()
            .Where(t => t.CompanyId == companyId)
            .OrderByDescending(t => t.Id)
            .Select(t => new ServiceTicketDto
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                CustomerId = t.CustomerId,
                Title = t.Title,
                Description = t.Description,
                Priority = t.Priority,
                Status = t.Status,
                AssignedToUserId = t.AssignedToUserId,
                OpenedAtUtc = t.OpenedAtUtc,
                ClosedAtUtc = t.ClosedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> CreateServiceTicketAsync(int companyId, CreateServiceTicketRequest request, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.ServiceManagement, cancellationToken);
        if (denied is not null)
            return OperationResult<int>.Fail(denied);
        if (!await _db.Customers.AnyAsync(c => c.Id == request.CustomerId && c.CompanyId == companyId, cancellationToken))
            return OperationResult<int>.Fail("Customer not found.");

        var no = await DocumentSequenceHelper.NextAsync(_db, companyId, "ST", "ST", cancellationToken);
        var t = new ServiceTicket
        {
            CompanyId = companyId,
            TicketNumber = no,
            CustomerId = request.CustomerId,
            Title = request.Title.Trim(),
            Description = request.Description,
            Priority = request.Priority,
            Status = ServiceTicketStatus.Open
        };
        _db.ServiceTickets.Add(t);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Ok(t.Id);
    }

    public async Task<OperationResult> CloseServiceTicketAsync(int companyId, int id, CancellationToken cancellationToken = default)
    {
        var denied = await DeniedWriteAsync(companyId, ModuleCode.ServiceManagement, cancellationToken);
        if (denied is not null)
            return OperationResult.Fail(denied);
        var t = await _db.ServiceTickets.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
        if (t is null)
            return OperationResult.Fail("Ticket not found.");
        t.Status = ServiceTicketStatus.Closed;
        t.ClosedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<IReadOnlyList<CompanyErpModuleDto>> ListCompanyErpModulesAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.CompanyErpModules.AsNoTracking()
            .Where(m => m.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        var map = rows.ToDictionary(m => m.ModuleCode, m => m.IsEnabled);

        return Enum.GetValues<ModuleCode>()
            .Select(mc => new CompanyErpModuleDto
            {
                ModuleCode = (int)mc,
                ModuleName = mc.ToString(),
                IsEnabled = map.TryGetValue(mc, out var en) ? en : true
            })
            .ToList();
    }

    public async Task<OperationResult> SetCompanyErpModuleAsync(int companyId, SetErpModuleRequest request, CancellationToken cancellationToken = default)
    {
        var row = await _db.CompanyErpModules
            .FirstOrDefaultAsync(m => m.CompanyId == companyId && m.ModuleCode == request.ModuleCode, cancellationToken);
        if (row is null)
        {
            row = new CompanyErpModule { CompanyId = companyId, ModuleCode = request.ModuleCode, IsEnabled = request.IsEnabled };
            _db.CompanyErpModules.Add(row);
        }
        else
        {
            row.IsEnabled = request.IsEnabled;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }

    private async Task<string?> DeniedWriteAsync(int companyId, ModuleCode module, CancellationToken cancellationToken) =>
        await ErpModuleGate.GetDenialReasonAsync(_db, companyId, module, cancellationToken);

    public IReadOnlyList<ErpCapabilityDto> GetErpCapabilities() =>
    [
        new() { Area = "Financial", Description = "General ledger, journals, multi-currency, tax, budgets, period control." },
        new() { Area = "Sales & CRM", Description = "Customers, invoices, sales orders, leads, contacts." },
        new() { Area = "Procurement", Description = "Suppliers, purchase orders, purchase requisitions, GRN." },
        new() { Area = "Inventory", Description = "Stock items, warehouses, movements, BOM, work orders." },
        new() { Area = "Operations", Description = "Cashbook, banking, fixed assets, payroll, projects, cost centres." },
        new() { Area = "Service", Description = "Service tickets and assignments." },
        new() { Area = "Organization", Description = "Companies, branches, departments, employees." },
        new() { Area = "Governance", Description = "Per-company module enable/disable; write APIs respect disabled modules." }
    ];
}
