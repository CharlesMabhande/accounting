using Accounting.Domain.Enums;

namespace Accounting.Domain.Evolution;

/// <summary>
/// Maps common third-party installer folder names to this application's <see cref="ModuleCode"/> where applicable.
/// Derived from public installer layout (folder names), not from proprietary binaries or schemas.
/// </summary>
public static class EvolutionInstallerCatalog
{
    public static IReadOnlyList<EvolutionInstallerModule> Modules { get; } =
    [
        new("AP", "Accounts Payable", "Supplier invoices, creditor ageings, payments", ModuleCode.AccountsPayable),
        new("AM", "Asset Management", "Fixed assets, depreciation registers", ModuleCode.FixedAssets),
        new("BM", "Bank Manager", "Bank reconciliation, cashbook feeds", ModuleCode.CashBook),
        new("CM", "Cash Book", "Cash payments and receipts", ModuleCode.CashBook),
        new("DM", "Document Management", "Document links and filing", null),
        new("EDM", "Electronic Document Management", "Scanned / electronic documents", null),
        new("EFT", "Electronic Funds Transfer", "Bank payment batches", ModuleCode.CashBook),
        new("II", "Inventory Integration", "Stock interfaces", ModuleCode.Inventory),
        new("IO", "Inventory Operations", "Stock processing", ModuleCode.Inventory),
        new("MS", "Multi-warehouse stock", "Warehouse / bin stock", ModuleCode.Inventory),
        new("VM", "Vehicle / fleet (regional)", "Fleet add-on where licensed", null),
        new("RetailPOS", "Retail Point of Sale", "POS tills and retail sales", ModuleCode.SalesOrders),
        new("VAT201", "VAT 201 (regional)", "Tax return layouts (e.g. ZA)", ModuleCode.Tax),
        new("MunicipalBilling", "Municipal billing", "Utilities billing add-on", null),
        new("PaymentGateway", "Payment gateway", "Card / hosted payments", null),
        new("BEE123", "B-BBEE (regional)", "Scorecard data", null),
        new("AdvancedProcurementWeb", "Procurement web", "Requisitions / approvals", ModuleCode.PurchaseOrders),
        new("ActiveDirectoryIntegration", "Active Directory", "Domain authentication", null),
        new("FiscalGateway", "Fiscal compliance", "Fiscal printer / compliance gateway", null),
        new("MobileService", "Mobile service", "Mobile sync API", null),
        new("ServiceManager", "Service Manager", "Windows services for the ERP host", null),
        new("SyncMonitor", "Sync Monitor", "Remote sync health", null),
        new("VAS", "Value-added services", "Licensing / telemetry", null),
        new("PSplit", "Parallel processing", "Data split utilities", null),
        new("DBScripts", "Database scripts", "SQL deployment (vendor)", null),
        new("DataDictionary", "Data dictionary", "Metadata (vendor)", null),
        new("Assembly", "Third-party assemblies", "DevExpress, Open XML, controls (vendor)", null),
        new("SQLExpress", "SQL Express media", "SQL Server Express installer (vendor)", null),
        new("Help", "Help", "Help content (vendor)", null)
    ];
}
