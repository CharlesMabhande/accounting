using System.Collections.ObjectModel;
using System.Windows.Controls;
using Accounting.Application.Security;
using Accounting.Desktop.Services;
using Accounting.Desktop.Views;

namespace Accounting.Desktop.Shell;

/// <summary>
/// Explorer-style module tree for enterprise accounting areas.
/// Everywhere an HTTP GET exists, the desktop uses <see cref="DataBrowserView"/> (with CSV/PDF export) or a dedicated view.
/// Remaining items stay as <see cref="PlaceholderModuleView"/> until APIs exist.
/// </summary>
public static class EvolutionNavigationCatalog
{
    public static ObservableCollection<ShellNavNode> Build(AccountingApiClient api)
    {
        ShellNavNode L(string key, string title, string caption, Func<UserControl> view) =>
            new()
            {
                ModuleKey = key,
                Title = title,
                Caption = caption,
                RequiredPermission = BuiltInPermissions.Nav(key),
                CreateView = view,
                IconGlyph = NavIcons.ForModuleKey(key)
            };

        ShellNavNode F(string title, params ShellNavNode[] children) =>
            new()
            {
                Title = title,
                Caption = "",
                IconGlyph = NavIcons.ForSectionTitle(title),
                Children = new ObservableCollection<ShellNavNode>(children)
            };

        ShellNavNode P(string key, string title, string caption, string blurb) =>
            L(key, title, caption, () => new PlaceholderModuleView(title, blurb));

        return new ObservableCollection<ShellNavNode>
        {
            L("dashboard", "Dashboard", "home workspace", () => new DashboardView(api)),

            F("Administration",
                L("companies", "Companies", "organisations", () => new CompaniesView(api)),
                L("import", "Belts import", "master data import", () => new BeltsImportView(api)),
                L("adm_backup", "Backup & restore", "export / import database",
                    () => new BackupRestoreView(api)),
                L("adm_users", "Users & agents", "accounts & roles",
                    () => new UsersAndAgentsView(api)),
                L("adm_roles", "Roles & permissions", "RBAC",
                    () => new RolesPermissionsView(api)),
                L("adm_audit_settings", "Audit table settings", "data change auditing",
                    () => new AuditTableSettingsView(api)),
                L("adm_periods", "Period maintenance", "open / lock periods",
                    () => new FiscalPeriodsView(api)),
                L("adm_sequences", "Document numbering", "sequences",
                    () => new DataBrowserView(api, "Document sequences", "Next numbers per document type / key.",
                        "api/companies/{companyId}/inquiry/document-sequences")),
                L("adm_currency", "Currencies", "global catalog",
                    () => new CurrenciesExchangeRatesView(api)),
                L("adm_fx", "Exchange rates", "FX tables",
                    () => new CurrenciesExchangeRatesView(api)),
                L("adm_modules", "ERP modules", "licensed features",
                    () => new DataBrowserView(api, "Company ERP modules", "Licensed ERP modules for this company.",
                        "api/companies/{companyId}/erp/modules")),
                L("adm_audit", "Audit trail", "system log",
                    () => new DataBrowserView(api, "Audit log", "Recent system and user activity (global).", "api/Audit?limit=500"))),

            F("General ledger",
                L("ledger", "General ledger (workspace)", "chart · journals · trial balance", () => new GeneralLedgerView(api)),
                L("gl_coa", "Chart of accounts", "ledger accounts",
                    () => new DataBrowserView(api, "Chart of accounts", "Ledger accounts for the selected company.",
                        "api/companies/{companyId}/ChartOfAccounts")),
                L("gl_journals", "Journal entries", "GL batches",
                    () => new DataBrowserView(api, "Journal entries", "Recent journal batches.",
                        "api/companies/{companyId}/inquiry/journal-entries")),
                L("gl_trial", "Trial balance", "balances by period",
                    () => new TrialBalanceView(api)),
                L("gl_budget", "Budgets", "budget lines",
                    () => new DataBrowserView(api, "Budget lines", "Budget amounts by account and period.",
                        "api/companies/{companyId}/erp/budget-lines")),
                P("gl_recur", "Recurring journals", "templates",
                    "Templates that generate periodic journal batches—implement recurring journal engine and API."),
                P("gl_consol", "Consolidation", "group reporting",
                    "Intercompany elimination and group consolidation.")),

            F("Receivables",
                L("ar_customers", "Customers", "customer master", () => new CustomersView(api)),
                L("ar_invoices", "Customer invoices", "AR documents",
                    () => new DataBrowserView(api, "Customer invoices", "Open and posted AR invoices.",
                        "api/companies/{companyId}/inquiry/customer-invoices")),
                P("ar_quotes", "Quotes", "sales quotes",
                    "Sales quotations—extend SalesOrder / new Quote entity when ready."),
                L("ar_orders", "Sales orders", "order lines",
                    () => new DataBrowserView(api, "Sales orders", "Sales order lines.",
                        "api/companies/{companyId}/inquiry/sales-orders")),
                P("ar_credit", "Credit notes", "AR credit",
                    "Customer credit notes—extend CustomerInvoice types or dedicated credit note API."),
                L("ar_receipts", "Customer receipts", "cash allocation",
                    () => new DataBrowserView(api, "Customer receipts", "Receipts and payments in cashbook linked to customers where applicable.",
                        "api/companies/{companyId}/inquiry/cashbook-transactions")),
                L("ar_statements", "Customer statements", "print & aged listing",
                    () => new CustomerStatementsView(api)),
                P("ar_price", "Selling prices", "price lists",
                    "Customer price lists and discounts—extend pricing tables.")),

            F("Payables",
                L("ap_suppliers", "Suppliers", "supplier master", () => new SuppliersView(api)),
                L("ap_invoices", "Supplier invoices", "AP documents",
                    () => new DataBrowserView(api, "Supplier invoices", "AP invoices.",
                        "api/companies/{companyId}/inquiry/supplier-invoices")),
                L("ap_orders", "Purchase orders", "PO lines",
                    () => new DataBrowserView(api, "Purchase orders", "Open and closed POs.",
                        "api/companies/{companyId}/inquiry/purchase-orders")),
                L("ap_grn", "Goods received", "GRN",
                    () => new DataBrowserView(api, "Goods received", "Goods received notes.",
                        "api/companies/{companyId}/inquiry/goods-received-notes")),
                P("ap_payments", "Supplier payments", "remittance",
                    "Supplier payment batches—add payment run posting API."),
                P("ap_debit", "Debit notes", "AP debit",
                    "Supplier debit notes—extend supplier invoice types."),
                L("ap_req", "Purchase requisitions", "internal reqs",
                    () => new DataBrowserView(api, "Purchase requisitions", "Internal purchase requisitions.",
                        "api/companies/{companyId}/erp/purchase-requisitions"))),

            F("Cash & bank",
                L("cb_cashbook", "Cashbook", "receipts & payments",
                    () => new DataBrowserView(api, "Cashbook transactions", "Receipts and payments.",
                        "api/companies/{companyId}/inquiry/cashbook-transactions")),
                L("cb_banks", "Bank accounts", "cash & bank", () => new BankAccountsView(api)),
                P("cb_recon", "Bank reconciliation", "statement import",
                    "Import bank statements and reconcile to cashbook—add reconciliation engine."),
                P("cb_petty", "Petty cash", "imprest",
                    "Petty cash floats—optional separate bank account / cashbook type.")),

            F("Inventory",
                L("inv_items", "Stock items", "items & dimensions", () => new StockItemsView(api)),
                L("inv_wh", "Warehouses", "locations",
                    () => new DataBrowserView(api, "Warehouses", "Storage locations.",
                        "api/companies/{companyId}/Warehouses")),
                L("inv_balance", "Warehouse stock", "on hand",
                    () => new DataBrowserView(api, "Warehouse stock", "On-hand by warehouse.",
                        "api/companies/{companyId}/StockItems/warehouse-stock")),
                L("inv_adjust", "Stock movements", "issues & receipts",
                    () => new DataBrowserView(api, "Stock movements", "Inventory movements and adjustments.",
                        "api/companies/{companyId}/inquiry/stock-movements")),
                P("inv_take", "Stock takes", "physical count",
                    "Stock take documents and variance posting—add stock take API."),
                P("inv_transfer", "Inter-warehouse transfers", "movements",
                    "Dedicated transfer documents—filter or extend stock movement types."),
                L("inv_bom", "Bill of materials", "manufacturing BOM",
                    () => new DataBrowserView(api, "Bill of materials", "BOM headers for manufacturing.",
                        "api/companies/{companyId}/erp/bom"))),

            F("Fixed assets",
                L("fixedassets", "Fixed assets register", "capital assets",
                    () => new DataBrowserView(api, "Fixed assets", "Capitalised assets for the selected company.",
                        "api/companies/{companyId}/inquiry/fixed-assets")),
                P("fa_depreciation", "Depreciation runs", "period depreciation",
                    "Automated depreciation journals—add depreciation run service."),
                P("fa_disposal", "Asset disposals", "retirements",
                    "Disposal postings and gain/loss—extend fixed asset disposal workflow.")),

            F("Payroll",
                L("payroll", "Payroll runs", "pay periods",
                    () => new DataBrowserView(api, "Payroll runs", "Posted and draft payroll periods.",
                        "api/companies/{companyId}/inquiry/payroll-runs")),
                L("pr_employees", "Payroll employees", "HR directory",
                    () => new DataBrowserView(api, "Employees", "Employee directory for the selected company.",
                        "api/companies/{companyId}/erp/employees")),
                P("pr_leave", "Leave", "absence",
                    "Leave accrual and pays—add leave tables and API.")),

            F("Tax",
                L("tax", "Tax codes", "VAT / sales tax",
                    () => new DataBrowserView(api, "Tax codes", "VAT / sales tax configuration.",
                        "api/companies/{companyId}/TaxCodes")),
                P("tax_return", "Tax returns", "returns",
                    "VAT return preparation—align with local legislation and reporting exports.")),

            F("Project tracking",
                L("proj_cc", "Cost centres", "dimensions",
                    () => new DataBrowserView(api, "Cost centres", "Analytical dimensions.",
                        "api/companies/{companyId}/inquiry/cost-centres")),
                L("proj_jobs", "Project jobs", "jobs",
                    () => new DataBrowserView(api, "Project jobs", "Jobs and projects.",
                        "api/companies/{companyId}/inquiry/project-jobs")),
                P("proj_time", "Time & billing", "WIP",
                    "Time sheets and billing to jobs—add time entry API.")),

            F("Manufacturing",
                L("erp_wo", "Work orders", "shop floor",
                    () => new DataBrowserView(api, "Work orders", "Manufacturing work orders.",
                        "api/companies/{companyId}/erp/work-orders")),
                P("mfg_mrp", "MRP", "planning",
                    "Material requirements planning—future scheduling service."),
                P("mfg_sched", "Scheduling", "capacity",
                    "Production scheduling—future capacity planning.")),

            F("CRM",
                L("erp_leads", "Sales leads", "pipeline",
                    () => new DataBrowserView(api, "Sales leads", "Pipeline leads.",
                        "api/companies/{companyId}/erp/crm/leads")),
                L("crm_contacts", "CRM contacts", "address book",
                    () => new DataBrowserView(api, "CRM contacts", "Contacts linked to customers and leads.",
                        "api/companies/{companyId}/erp/crm/contacts")),
                L("crm_opp", "Opportunities", "pipeline stages",
                    () => new DataBrowserView(api, "Sales opportunities", "Leads with stages (use lead stage updates in API).",
                        "api/companies/{companyId}/erp/crm/leads")),
                L("crm_cases", "Service cases", "support",
                    () => new DataBrowserView(api, "Service tickets", "Support and service cases.",
                        "api/companies/{companyId}/erp/service-tickets"))),

            F("ERP workspace",
                L("erp_br", "Branches", "branches",
                    () => new DataBrowserView(api, "Branches", "Organisational branches.",
                        "api/companies/{companyId}/erp/branches")),
                L("erp_dept", "Departments", "HR",
                    () => new DataBrowserView(api, "Departments", "HR departments.",
                        "api/companies/{companyId}/erp/departments")),
                L("erp_emp", "Employees", "directory",
                    () => new DataBrowserView(api, "Employees", "Employee directory.",
                        "api/companies/{companyId}/erp/employees"))),

            F("Retail & POS",
                P("pos_till", "Point of sale", "till",
                    "Retail POS—integrate till transactions when retail module is modelled."),
                P("pos_cashup", "Cash-up", "end of day",
                    "Till cash-up—add POS cash-up API.")),

            F("Enquiries & reports",
                L("rep_financial", "Trial balance report", "financial position",
                    () => new TrialBalanceView(api)),
                P("rep_aged", "Aged analysis", "debtors / creditors",
                    "Aged debtors and creditors—add aged listing endpoints to reporting service."),
                L("rep_stock", "Stock valuation", "inventory movements",
                    () => new DataBrowserView(api, "Stock movements", "Movement history for valuation support.",
                        "api/companies/{companyId}/inquiry/stock-movements")),
                P("rep_custom", "Custom reports", "report designer",
                    "Custom layouts—integrate external reporting or export engine."))
        };
    }
}
