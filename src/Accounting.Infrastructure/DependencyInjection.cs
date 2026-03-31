using Accounting.Application.Abstractions;
using Accounting.Application.Services;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Security;
using Accounting.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ICurrentSessionContext, CurrentSessionContext>();
        services.AddDbContext<AccountingDbContext>(options => ConfigureDatabase(options, configuration));

        services.AddScoped<IJournalPostingRepository, JournalPostingRepository>();
        services.AddScoped<IJournalPostingService, JournalPostingService>();

        services.AddScoped<ICustomerInvoiceService, CustomerInvoiceService>();
        services.AddScoped<ISupplierInvoiceService, SupplierInvoiceService>();
        services.AddScoped<ICashbookService, CashbookService>();
        services.AddScoped<IGoodsReceivedNoteService, GoodsReceivedNoteService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<ISalesOrderService, SalesOrderService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IFixedAssetService, FixedAssetService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IFiscalPeriodControlService, FiscalPeriodControlService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAccountAdminService, UserAccountAdminService>();
        services.AddScoped<IRoleAdminService, RoleAdminService>();
        services.AddScoped<IAuditTableSettingsService, AuditTableSettingsService>();
        services.AddScoped<ICostCentreService, CostCentreService>();
        services.AddScoped<IProjectJobService, ProjectJobService>();
        services.AddScoped<ITaxCodeQueryService, TaxCodeQueryService>();
        services.AddScoped<ICompanyQueryService, CompanyQueryService>();
        services.AddScoped<IWarehouseQueryService, WarehouseQueryService>();
        services.AddScoped<IStockItemQueryService, StockItemQueryService>();
        services.AddScoped<BeltsImportService>();
        services.AddScoped<DatabaseArchiveService>();
        services.AddScoped<IErpPlatformService, ErpPlatformService>();
        services.AddScoped<IWorkOrderManufacturingService, WorkOrderManufacturingService>();

        return services;
    }

    private static void ConfigureDatabase(DbContextOptionsBuilder options, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"];
        if (string.IsNullOrWhiteSpace(provider))
        {
            var cs = configuration.GetConnectionString("DefaultConnection");
            provider = !string.IsNullOrWhiteSpace(cs) && cs.Contains("Server=", StringComparison.OrdinalIgnoreCase)
                ? DatabaseProvider.SqlServer
                : DatabaseProvider.Sqlite;
        }

        if (string.Equals(provider, DatabaseProvider.SqlServer, StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=(localdb)\\mssqllocaldb;Database=AccountingDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });
            return;
        }

        var relativePath = configuration["Database:SqlitePath"] ?? Path.Combine("Data", "Accounting.db");
        var combined = Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var fullPath = Path.GetFullPath(combined);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        options.UseSqlite($"Data Source={fullPath}");
    }
}
