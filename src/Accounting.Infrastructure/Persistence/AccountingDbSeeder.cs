using Accounting.Application.Security;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Persistence;

public static class AccountingDbSeeder
{
    public static async Task SeedAsync(AccountingDbContext db, CancellationToken cancellationToken = default)
    {
        await SeedSecurityAsync(db, cancellationToken);

        if (await db.Companies.AnyAsync(cancellationToken))
            return;

        var company = new Company
        {
            Code = "MAIN",
            Name = "Main Company",
            BaseCurrency = "USD"
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync(cancellationToken);

        var fy = new FiscalYear
        {
            CompanyId = company.Id,
            Year = 2026,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            IsClosed = false
        };
        db.FiscalYears.Add(fy);
        await db.SaveChangesAsync(cancellationToken);

        for (var m = 1; m <= 12; m++)
        {
            var start = new DateOnly(2026, m, 1);
            var end = new DateOnly(2026, m, DateTime.DaysInMonth(2026, m));
            db.FiscalPeriods.Add(new FiscalPeriod
            {
                FiscalYearId = fy.Id,
                PeriodNumber = m,
                StartDate = start,
                EndDate = end,
                IsClosed = false
            });
        }

        var accountDefs = new (string Code, string Name, AccountType Type)[]
        {
            ("1000", "Cash", AccountType.Asset),
            ("1100", "Bank Current", AccountType.Asset),
            ("1200", "Accounts Receivable", AccountType.Asset),
            ("1300", "Inventory", AccountType.Asset),
            ("1500", "Work in Progress", AccountType.Asset),
            ("1400", "Accumulated Depreciation", AccountType.Asset),
            ("1999", "Suspense", AccountType.Asset),
            ("2000", "Accounts Payable", AccountType.Liability),
            ("2100", "Goods Received Not Invoiced", AccountType.Liability),
            ("2150", "VAT Input Recoverable", AccountType.Asset),
            ("2200", "VAT Output Payable", AccountType.Liability),
            ("2300", "Payroll Payable", AccountType.Liability),
            ("2400", "Tax Withholding Payable", AccountType.Liability),
            ("3000", "Retained Earnings", AccountType.Equity),
            ("4000", "Sales Revenue", AccountType.Income),
            ("5000", "Cost of Sales", AccountType.Expense),
            ("5100", "Operating Expenses", AccountType.Expense),
            ("5300", "Depreciation Expense", AccountType.Expense),
            ("6000", "Wages Expense", AccountType.Expense)
        };

        foreach (var (code, name, type) in accountDefs)
        {
            db.LedgerAccounts.Add(new LedgerAccount
            {
                CompanyId = company.Id,
                Code = code,
                Name = name,
                AccountType = type,
                IsPostable = true
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var acc = await db.LedgerAccounts.Where(a => a.CompanyId == company.Id).ToDictionaryAsync(a => a.Code, cancellationToken);

        var vatTax = new TaxCode
        {
            CompanyId = company.Id,
            Code = "VAT15",
            Description = "Standard VAT 15%",
            RatePercent = 15m,
            OutputTaxLedgerAccountId = acc["2200"].Id,
            InputTaxLedgerAccountId = acc["2150"].Id
        };
        db.TaxCodes.Add(vatTax);
        await db.SaveChangesAsync(cancellationToken);

        var ar = acc["1200"];
        var ap = acc["2000"];
        var bank = acc["1100"];

        db.Customers.Add(new Customer
        {
            CompanyId = company.Id,
            Code = "CUST01",
            Name = "Walk-in Customer",
            AccountsReceivableAccountId = ar.Id
        });

        db.Suppliers.Add(new Supplier
        {
            CompanyId = company.Id,
            Code = "SUPP01",
            Name = "Default Supplier",
            AccountsPayableAccountId = ap.Id
        });

        db.BankAccounts.Add(new BankAccount
        {
            CompanyId = company.Id,
            Code = "BANK01",
            Name = "Main Bank",
            LedgerAccountId = bank.Id
        });

        db.Warehouses.Add(new Warehouse
        {
            CompanyId = company.Id,
            Code = "WH01",
            Name = "Main Warehouse",
            IsActive = true
        });

        var inv = acc["1300"];
        var cos = acc["5000"];

        db.StockItems.Add(new StockItem
        {
            CompanyId = company.Id,
            Code = "ITEM01",
            Description = "Finished good (sample)",
            UnitOfMeasure = "EA",
            InventoryAccountId = inv.Id,
            CostOfSalesAccountId = cos.Id,
            IsActive = true
        });

        db.StockItems.Add(new StockItem
        {
            CompanyId = company.Id,
            Code = "ITEM02",
            Description = "Component part (sample)",
            UnitOfMeasure = "EA",
            InventoryAccountId = inv.Id,
            CostOfSalesAccountId = cos.Id,
            IsActive = true
        });

        db.Currencies.AddRange(
            new Currency { Code = "USD", Name = "US Dollar", DecimalPlaces = 2 },
            new Currency { Code = "EUR", Name = "Euro", DecimalPlaces = 2 },
            new Currency { Code = "ZAR", Name = "South African Rand", DecimalPlaces = 2 },
            new Currency { Code = "ZWG", Name = "Zimbabwe Gold", DecimalPlaces = 2 });

        db.Branches.Add(new Branch
        {
            CompanyId = company.Id,
            Code = "HEAD",
            Name = "Head Office",
            IsActive = true
        });

        db.Departments.Add(new Department
        {
            CompanyId = company.Id,
            Code = "GEN",
            Name = "General",
            IsActive = true
        });

        db.CostCentres.Add(new CostCentre
        {
            CompanyId = company.Id,
            Code = "CC01",
            Name = "General",
            IsActive = true
        });

        var dept = await db.Departments.FirstAsync(d => d.CompanyId == company.Id, cancellationToken);
        db.Employees.Add(new Employee
        {
            CompanyId = company.Id,
            DepartmentId = dept.Id,
            Code = "EMP01",
            FullName = "Sample Employee",
            Email = "employee@example.com",
            Position = "Clerk",
            HireDate = new DateOnly(2026, 1, 1),
            IsActive = true
        });

        db.SalesLeads.Add(new SalesLead
        {
            CompanyId = company.Id,
            OrganizationName = "Sample Prospect Ltd",
            ContactName = "Jane Doe",
            Email = "prospect@example.com",
            Phone = "+1-555-0100",
            Stage = LeadStage.Qualified,
            EstimatedValue = 10000m,
            CurrencyCode = "USD"
        });

        await db.SaveChangesAsync(cancellationToken);

        var wh = await db.Warehouses.FirstAsync(w => w.CompanyId == company.Id, cancellationToken);
        var items = await db.StockItems.Where(s => s.CompanyId == company.Id).ToDictionaryAsync(s => s.Code, cancellationToken);
        foreach (var si in items.Values)
        {
            db.WarehouseStocks.Add(new WarehouseStock
            {
                CompanyId = company.Id,
                WarehouseId = wh.Id,
                StockItemId = si.Id,
                Quantity = 10000,
                LastUnitCost = 10m
            });
        }

        db.ExchangeRates.Add(new ExchangeRate
        {
            CompanyId = company.Id,
            FromCurrencyCode = "USD",
            ToCurrencyCode = "USD",
            Rate = 1m,
            EffectiveDate = new DateOnly(2026, 1, 1)
        });

        var salesAcc = await db.LedgerAccounts.FirstAsync(a => a.CompanyId == company.Id && a.Code == "4000", cancellationToken);
        db.BudgetLines.Add(new BudgetLine
        {
            CompanyId = company.Id,
            FiscalYearId = fy.Id,
            LedgerAccountId = salesAcc.Id,
            PeriodNumber = 1,
            Amount = 50000m
        });

        var bom = new BomHeader
        {
            CompanyId = company.Id,
            ParentStockItemId = items["ITEM01"].Id,
            Version = "1",
            IsActive = true
        };
        bom.Lines.Add(new BomLine
        {
            LineNumber = 1,
            ComponentStockItemId = items["ITEM02"].Id,
            QuantityPer = 1m,
            ScrapPercent = 0
        });
        db.BomHeaders.Add(bom);
        await db.SaveChangesAsync(cancellationToken);

        db.WorkOrders.Add(new WorkOrder
        {
            CompanyId = company.Id,
            DocumentNumber = "WO-000001",
            StockItemId = items["ITEM01"].Id,
            BomHeaderId = bom.Id,
            WarehouseId = wh.Id,
            QuantityPlanned = 10,
            QuantityCompleted = 0,
            Status = WorkOrderStatus.Planned,
            PlannedStart = new DateOnly(2026, 3, 1),
            PlannedEnd = new DateOnly(2026, 3, 31)
        });

        var cust = await db.Customers.FirstAsync(c => c.CompanyId == company.Id, cancellationToken);
        db.ServiceTickets.Add(new ServiceTicket
        {
            CompanyId = company.Id,
            TicketNumber = "ST-000001",
            CustomerId = cust.Id,
            Title = "Sample support request",
            Description = "Demonstration service ticket",
            Priority = ServiceTicketPriority.Normal,
            Status = ServiceTicketStatus.Open
        });

        db.DocumentSequences.AddRange(
            new DocumentSequence { CompanyId = company.Id, Key = "WO", NextValue = 2 },
            new DocumentSequence { CompanyId = company.Id, Key = "ST", NextValue = 2 });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static readonly string[] AgentNavModuleKeys =
    [
        "dashboard", "ar_customers", "ar_invoices", "ar_statements", "erp_leads", "crm_contacts", "crm_opp", "crm_cases", "rep_financial"
    ];

    /// <summary>Nav entries not granted to the default &quot;User&quot; role (admin / destructive).</summary>
    private static readonly string[] UserRoleExcludedNavKeys = ["adm_backup"];

    private static async Task SeedSecurityAsync(AccountingDbContext db, CancellationToken cancellationToken)
    {
        var existingNames = await db.Permissions.Select(p => p.Name).ToListAsync(cancellationToken);
        var nameSet = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var name in BuiltInPermissions.AllPermissions())
        {
            if (!nameSet.Contains(name))
                db.Permissions.Add(new Permission { Name = name });
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(cancellationToken);

        var allPerms = await db.Permissions.ToListAsync(cancellationToken);
        var navPerms = allPerms.Where(p => p.Name.StartsWith("nav.", StringComparison.Ordinal)).ToList();
        var userNavExcludes = new HashSet<string>(UserRoleExcludedNavKeys.Select(BuiltInPermissions.Nav),
            StringComparer.OrdinalIgnoreCase);
        var navPermsForUser = navPerms.Where(p => !userNavExcludes.Contains(p.Name)).ToList();
        var agentNavNames = new HashSet<string>(AgentNavModuleKeys.Select(BuiltInPermissions.Nav), StringComparer.OrdinalIgnoreCase);
        var agentPermList = allPerms.Where(p => agentNavNames.Contains(p.Name)).ToList();

        await EnsureRoleWithPermissionsAsync(db, "Administrator", allPerms, cancellationToken);
        await EnsureRoleWithPermissionsAsync(db, "User", navPermsForUser, cancellationToken);
        await EnsureRoleWithPermissionsAsync(db, "Agent", agentPermList, cancellationToken);

        if (!await db.UserAccounts.AnyAsync(cancellationToken))
        {
            var adminRole = await db.Roles.FirstAsync(r => r.Name == "Administrator", cancellationToken);
            var adminUser = new UserAccount
            {
                UserName = "admin",
                DisplayName = "Administrator",
                PasswordHash = PasswordHasher.Hash("Admin123!"),
                IsActive = true,
                AccountKind = UserAccountKind.Staff
            };
            adminUser.Roles.Add(adminRole);
            db.UserAccounts.Add(adminUser);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.UserAccounts.AnyAsync(u => u.UserName == "agent", cancellationToken))
        {
            var agentRole = await db.Roles.FirstAsync(r => r.Name == "Agent", cancellationToken);
            var agentUser = new UserAccount
            {
                UserName = "agent",
                DisplayName = "Sample agent",
                PasswordHash = PasswordHasher.Hash("Agent123!"),
                IsActive = true,
                AccountKind = UserAccountKind.Agent
            };
            agentUser.Roles.Add(agentRole);
            db.UserAccounts.Add(agentUser);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task EnsureRoleWithPermissionsAsync(
        AccountingDbContext db,
        string roleName,
        IReadOnlyList<Permission> desired,
        CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
        if (role is null)
        {
            role = new Role { Name = roleName };
            db.Roles.Add(role);
            await db.SaveChangesAsync(cancellationToken);
            role = await db.Roles.Include(r => r.Permissions).FirstAsync(r => r.Name == roleName, cancellationToken);
        }

        var have = role.Permissions.Select(p => p.Id).ToHashSet();
        foreach (var p in desired)
        {
            if (!have.Contains(p.Id))
                role.Permissions.Add(p);
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(cancellationToken);
    }
}
