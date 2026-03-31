using Accounting.Application.Abstractions;
using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Accounting.Infrastructure.Persistence;

public sealed class AccountingDbContext : DbContext
{
    private readonly ICurrentSessionContext? _session;
    private bool _suppressDataAudit;

    public AccountingDbContext(DbContextOptions<AccountingDbContext> options, ICurrentSessionContext? session = null)
        : base(options)
    {
        _session = session;
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<TaxCode> TaxCodes => Set<TaxCode>();
    public DbSet<LedgerAccount> LedgerAccounts => Set<LedgerAccount>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<CashbookTransaction> CashbookTransactions => Set<CashbookTransaction>();
    public DbSet<CustomerInvoice> CustomerInvoices => Set<CustomerInvoice>();
    public DbSet<CustomerInvoiceLine> CustomerInvoiceLines => Set<CustomerInvoiceLine>();
    public DbSet<SupplierInvoice> SupplierInvoices => Set<SupplierInvoice>();
    public DbSet<SupplierInvoiceLine> SupplierInvoiceLines => Set<SupplierInvoiceLine>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<GoodsReceivedNote> GoodsReceivedNotes => Set<GoodsReceivedNote>();
    public DbSet<GoodsReceivedNoteLine> GoodsReceivedNoteLines => Set<GoodsReceivedNoteLine>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<CostCentre> CostCentres => Set<CostCentre>();
    public DbSet<ProjectJob> ProjectJobs => Set<ProjectJob>();
    public DbSet<FixedAsset> FixedAssets => Set<FixedAsset>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AuditTableSetting> AuditTableSettings => Set<AuditTableSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<SalesLead> SalesLeads => Set<SalesLead>();
    public DbSet<CrmContact> CrmContacts => Set<CrmContact>();
    public DbSet<PurchaseRequisition> PurchaseRequisitions => Set<PurchaseRequisition>();
    public DbSet<PurchaseRequisitionLine> PurchaseRequisitionLines => Set<PurchaseRequisitionLine>();
    public DbSet<BomHeader> BomHeaders => Set<BomHeader>();
    public DbSet<BomLine> BomLines => Set<BomLine>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<ServiceTicket> ServiceTickets => Set<ServiceTicket>();
    public DbSet<CompanyErpModule> CompanyErpModules => Set<CompanyErpModule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<DocumentSequence>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Key }).IsUnique();
        });

        modelBuilder.Entity<LedgerAccount>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            e.HasOne(x => x.ParentAccount).WithMany().HasForeignKey(x => x.ParentAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockItem>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            e.HasOne(x => x.InventoryAccount).WithMany().HasForeignKey(x => x.InventoryAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CostOfSalesAccount).WithMany().HasForeignKey(x => x.CostOfSalesAccountId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.TargetGpPercent).HasPrecision(9, 4);
            e.Property(x => x.BuyLength).HasPrecision(18, 4);
            e.Property(x => x.BuyWidth).HasPrecision(18, 4);
            e.Property(x => x.BuyHeight).HasPrecision(18, 4);
            e.Property(x => x.SellLength).HasPrecision(18, 4);
            e.Property(x => x.SellWidth).HasPrecision(18, 4);
            e.Property(x => x.SellHeight).HasPrecision(18, 4);
            e.Property(x => x.Weight).HasPrecision(18, 4);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            e.HasOne(x => x.AccountsReceivableAccount).WithMany().HasForeignKey(x => x.AccountsReceivableAccountId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CreditLimit).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Supplier>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            e.HasOne(x => x.AccountsPayableAccount).WithMany().HasForeignKey(x => x.AccountsPayableAccountId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CreditLimit).HasPrecision(18, 2);
        });

        modelBuilder.Entity<BankAccount>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            e.HasOne(x => x.LedgerAccount).WithMany().HasForeignKey(x => x.LedgerAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CashbookTransaction>(e =>
        {
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.BankAccount).WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.JournalEntry).WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JournalEntry>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.EntryNumber }).IsUnique();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.FiscalPeriod).WithMany().HasForeignKey(x => x.FiscalPeriodId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines).WithOne(x => x.JournalEntry).HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JournalLine>(e =>
        {
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerInvoice>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.DocumentNumber }).IsUnique();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TaxCode).WithMany().HasForeignKey(x => x.TaxCodeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.JournalEntry).WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines).WithOne(x => x.CustomerInvoice).HasForeignKey(x => x.CustomerInvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerInvoiceLine>(e =>
        {
            e.HasOne(x => x.RevenueAccount).WithMany().HasForeignKey(x => x.RevenueAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StockItem).WithMany().HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierInvoice>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.DocumentNumber }).IsUnique();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TaxCode).WithMany().HasForeignKey(x => x.TaxCodeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.JournalEntry).WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines).WithOne(x => x.SupplierInvoice).HasForeignKey(x => x.SupplierInvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SupplierInvoiceLine>(e =>
        {
            e.HasOne(x => x.ExpenseAccount).WithMany().HasForeignKey(x => x.ExpenseAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StockItem).WithMany().HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FixedAsset>(e =>
        {
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssetAccount).WithMany().HasForeignKey(x => x.AssetAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DepreciationExpenseAccount).WithMany().HasForeignKey(x => x.DepreciationExpenseAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AccumulatedDepreciationAccount).WithMany().HasForeignKey(x => x.AccumulatedDepreciationAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GoodsReceivedNote>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.GrnNumber }).IsUnique();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines).WithOne(x => x.GoodsReceivedNote).HasForeignKey(x => x.GoodsReceivedNoteId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GoodsReceivedNoteLine>(e =>
        {
            e.HasOne(x => x.StockItem).WithMany().HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectJob>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            e.HasOne(x => x.CostCentre).WithMany().HasForeignKey(x => x.CostCentreId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<WarehouseStock>(e =>
        {
            e.HasIndex(x => new { x.WarehouseId, x.StockItemId }).IsUnique();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StockItem).WithMany().HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockMovement>(e =>
        {
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StockItem).WithMany().HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayrollRun>(e =>
        {
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.JournalEntry).WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesOrder>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.OrderNumber }).IsUnique();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines).WithOne(x => x.SalesOrder).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseOrder>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.OrderNumber }).IsUnique();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines).WithOne(x => x.PurchaseOrder).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CostCentre>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<UserAccount>(e =>
        {
            e.HasIndex(x => x.UserName).IsUnique();
            e.Property(x => x.AccountKind).HasConversion<byte>();
        });

        modelBuilder.Entity<UserAccount>()
            .HasMany(u => u.Roles)
            .WithMany(r => r.Users)
            .UsingEntity(j => j.ToTable("UserRoles"));

        modelBuilder.Entity<Permission>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Role>()
            .HasMany(r => r.Permissions)
            .WithMany(p => p.Roles)
            .UsingEntity(j => j.ToTable("RolePermissions"));

        modelBuilder.Entity<UserSession>(e =>
        {
            e.HasIndex(x => x.Token).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditTableSetting>(e =>
        {
            e.HasIndex(x => x.EntityTypeName).IsUnique();
        });

        modelBuilder.Entity<TaxCode>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            e.HasOne(x => x.OutputTaxLedgerAccount).WithMany().HasForeignKey(x => x.OutputTaxLedgerAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.InputTaxLedgerAccount).WithMany().HasForeignKey(x => x.InputTaxLedgerAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Currency>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(3);
        });

        modelBuilder.Entity<Branch>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<Department>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<Employee>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<ExchangeRate>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.FromCurrencyCode, x.ToCurrencyCode, x.EffectiveDate }).IsUnique();
            e.Property(x => x.FromCurrencyCode).HasMaxLength(3);
            e.Property(x => x.ToCurrencyCode).HasMaxLength(3);
        });

        modelBuilder.Entity<PurchaseRequisition>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.DocumentNumber }).IsUnique();
            e.HasMany(x => x.Lines).WithOne(x => x.PurchaseRequisition).HasForeignKey(x => x.PurchaseRequisitionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ApprovedByUser).WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BomHeader>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.ParentStockItemId, x.Version }).IsUnique();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ParentStockItem).WithMany().HasForeignKey(x => x.ParentStockItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines).WithOne(x => x.BomHeader).HasForeignKey(x => x.BomHeaderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkOrder>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.DocumentNumber }).IsUnique();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StockItem).WithMany().HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.BomHeader).WithMany().HasForeignKey(x => x.BomHeaderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BudgetLine>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.FiscalYearId, x.LedgerAccountId, x.PeriodNumber }).IsUnique();
            // SQL Server: multiple CASCADE paths from Company (via FiscalYear / LedgerAccount) are not allowed.
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.FiscalYear).WithMany().HasForeignKey(x => x.FiscalYearId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.LedgerAccount).WithMany().HasForeignKey(x => x.LedgerAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CompanyErpModule>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.ModuleCode }).IsUnique();
        });

        modelBuilder.Entity<CrmContact>(e =>
        {
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SalesLead).WithMany().HasForeignKey(x => x.SalesLeadId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServiceTicket>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.TicketNumber }).IsUnique();
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssignedToUser).WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_suppressDataAudit)
            return await base.SaveChangesAsync(cancellationToken);

        var settings = await AuditTableSettings.AsNoTracking()
            .Where(s => s.IsEnabled)
            .ToDictionaryAsync(s => s.EntityTypeName, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var pending = new List<DataAuditPending>();
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
                continue;
            var clrName = entry.Metadata.ClrType.Name;
            if (clrName is nameof(AuditLog) or nameof(UserSession))
                continue;
            if (!settings.TryGetValue(clrName, out var cfg))
                continue;

            var action = entry.State switch
            {
                EntityState.Added when cfg.AuditInserts => "Insert",
                EntityState.Modified when cfg.AuditUpdates => "Update",
                EntityState.Deleted when cfg.AuditDeletes => "Delete",
                _ => null
            };
            if (action is null)
                continue;

            var details = BuildAuditDetails(entry);
            pending.Add(new DataAuditPending
            {
                EntityName = clrName,
                Action = action,
                Key = entry.State == EntityState.Added ? "" : GetPrimaryKeyPreview(entry),
                Details = details,
                Entry = entry
            });
        }

        var n = await base.SaveChangesAsync(cancellationToken);

        if (pending.Count == 0)
            return n;

        foreach (var p in pending.Where(x => x.Action == "Insert"))
            p.Key = GetPrimaryKeyPreview(p.Entry);

        _suppressDataAudit = true;
        try
        {
            var userId = _session?.UserId;
            foreach (var p in pending)
            {
                AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Action = p.Action,
                    EntityName = p.EntityName,
                    EntityKey = p.Key,
                    Details = p.Details,
                    OccurredAtUtc = DateTime.UtcNow
                });
            }

            await base.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _suppressDataAudit = false;
        }

        return n;
    }

    private static string GetPrimaryKeyPreview(EntityEntry entry)
    {
        return string.Join(",", entry.Properties.Where(x => x.Metadata.IsPrimaryKey()).Select(x =>
            entry.State == EntityState.Deleted ? x.OriginalValue?.ToString() ?? "" : x.CurrentValue?.ToString() ?? ""));
    }

    private static string BuildAuditDetails(EntityEntry entry)
    {
        return entry.State switch
        {
            EntityState.Added => "Inserted",
            EntityState.Deleted => "Deleted",
            EntityState.Modified => string.Join(", ",
                entry.Properties.Where(p => p.IsModified)
                    .Select(p => $"{p.Metadata.Name}={p.CurrentValue}")),
            _ => ""
        };
    }

    private sealed class DataAuditPending
    {
        public required string EntityName { get; init; }
        public required string Action { get; init; }
        public required string Key { get; set; }
        public required string Details { get; init; }
        public required EntityEntry Entry { get; init; }
    }
}
