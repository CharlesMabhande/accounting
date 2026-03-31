namespace Accounting.Desktop.Shell;

/// <summary>Segoe MDL2 Assets glyphs for navigation (see Font.Icon in theme).</summary>
public static class NavIcons
{
    /// <summary>Folder / section headers in the module tree.</summary>
    public static string ForSectionTitle(string title) => title switch
    {
        "Administration" => "\uE713", // Settings
        "General ledger" => "\uE8FA", // Financial
        "Receivables" => "\uE716", // Contact2
        "Payables" => "\uE8CC", // Money
        "Cash & bank" => "\uE8B0", // Bank
        "Inventory" => "\uE74C", // Package
        "Fixed assets" => "\uE7C1", // Cityscape
        "Payroll" => "\uE77B", // ContactInfo
        "Tax" => "\uE8F1", // Tag
        "Project tracking" => "\uE7F5", // Work
        "Manufacturing" => "\uE7C6", // Factory
        "CRM" => "\uE8BD", // Contact
        "ERP workspace" => "\uE9D5", // Org
        "Retail & POS" => "\uE81B", // Shop
        "Enquiries & reports" => "\uE9F9", // LineChart
        _ => "\uE8B7" // OpenFolderHorizontal
    };

    /// <summary>Leaf modules keyed by navigation module id (e.g. nav.ar_invoices).</summary>
    public static string ForModuleKey(string key) =>
        Module.TryGetValue(key, out var g) ? g : "\uE8A5"; // Page

    private static readonly Dictionary<string, string> Module = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dashboard"] = "\uE80F", // Home
        ["companies"] = "\uE9D5", // Org
        ["import"] = "\uE896", // Download
        ["adm_backup"] = "\uE8C8", // Save
        ["adm_users"] = "\uE716", // Contact2
        ["adm_roles"] = "\uE72E", // Permissions
        ["adm_audit_settings"] = "\uE8E0", // Shield
        ["adm_periods"] = "\uE787", // Calendar
        ["adm_sequences"] = "\uE8F1", // Tag
        ["adm_currency"] = "\uE8CF", // Currency
        ["adm_fx"] = "\uE8E1", // World
        ["adm_modules"] = "\uE8F1", // Tag (features)
        ["adm_audit"] = "\uE9D9", // History
        ["ledger"] = "\uE8FA", // Financial
        ["gl_coa"] = "\uE8E7", // Library
        ["gl_journals"] = "\uE8A5", // Page / document
        ["gl_trial"] = "\uE91F", // Calculator
        ["gl_budget"] = "\uE8FD", // Money
        ["gl_recur"] = "\uE8C8", // Sync
        ["gl_consol"] = "\uE8E7", // Library
        ["ar_customers"] = "\uE716",
        ["ar_invoices"] = "\uE8E8", // Mail
        ["ar_quotes"] = "\uE8A5", // Page
        ["ar_orders"] = "\uE8B8", // Shop
        ["ar_credit"] = "\uE8E8",
        ["ar_receipts"] = "\uE8CC",
        ["ar_statements"] = "\uE749", // Print
        ["ar_price"] = "\uE8EB", // Tag
        ["ap_suppliers"] = "\uE7C1",
        ["ap_invoices"] = "\uE8E8",
        ["ap_orders"] = "\uE8B8",
        ["ap_grn"] = "\uE74C",
        ["ap_payments"] = "\uE8CC",
        ["ap_debit"] = "\uE8E8",
        ["ap_req"] = "\uE8E8",
        ["cb_cashbook"] = "\uE8E8", // Book
        ["cb_banks"] = "\uE8B0",
        ["cb_recon"] = "\uE8C8",
        ["cb_petty"] = "\uE8CC",
        ["inv_items"] = "\uE74C",
        ["inv_wh"] = "\uE81E", // MapPin
        ["inv_balance"] = "\uE74C",
        ["inv_adjust"] = "\uE8F8", // Switch
        ["inv_take"] = "\uE8E1",
        ["inv_transfer"] = "\uE8F0", // Share
        ["inv_bom"] = "\uE7C5", // Engineering
        ["fixedassets"] = "\uE7C1",
        ["fa_depreciation"] = "\uE9D9",
        ["fa_disposal"] = "\uE8E8",
        ["payroll"] = "\uE8CC",
        ["pr_employees"] = "\uE77B",
        ["pr_leave"] = "\uE787",
        ["tax"] = "\uE8F1",
        ["tax_return"] = "\uE8E1",
        ["proj_cc"] = "\uE8E7",
        ["proj_jobs"] = "\uE7F5",
        ["proj_time"] = "\uE787",
        ["erp_wo"] = "\uE7C6",
        ["mfg_mrp"] = "\uE8E7",
        ["mfg_sched"] = "\uE787",
        ["erp_leads"] = "\uE8E0", // Flash / lead
        ["crm_contacts"] = "\uE8BD",
        ["crm_opp"] = "\uE8E7",
        ["crm_cases"] = "\uE8E8",
        ["erp_br"] = "\uE81E",
        ["erp_dept"] = "\uE716",
        ["erp_emp"] = "\uE77B",
        ["pos_till"] = "\uE81B",
        ["pos_cashup"] = "\uE8CC",
        ["rep_financial"] = "\uE9F9",
        ["rep_aged"] = "\uE9F9",
        ["rep_stock"] = "\uE74C",
        ["rep_custom"] = "\uE9F9"
    };
}
