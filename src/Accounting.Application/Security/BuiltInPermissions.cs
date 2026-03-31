using System.Linq;

namespace Accounting.Application.Security;

/// <summary>Permission names: nav.&lt;moduleKey&gt; for UI modules; security.* for administration.</summary>
public static class BuiltInPermissions
{
    public const string SecurityUsersManage = "security.users.manage";
    public const string SecurityRolesManage = "security.roles.manage";
    public const string SecurityAuditSettings = "security.audit.settings";
    public const string SecurityBackupManage = "security.backup.manage";

    public static string Nav(string moduleKey) => $"nav.{moduleKey}";

    /// <summary>All module keys from the desktop navigation catalog (L and P entries).</summary>
    public static readonly string[] AllModuleKeys =
    [
        "dashboard", "companies", "import", "adm_backup", "adm_users", "adm_roles", "adm_audit_settings", "adm_periods", "adm_sequences",
        "adm_currency", "adm_fx", "adm_modules", "adm_audit", "ledger", "gl_coa", "gl_journals", "gl_trial", "gl_budget",
        "gl_recur", "gl_consol", "ar_customers", "ar_invoices", "ar_quotes", "ar_orders", "ar_credit", "ar_receipts",
        "ar_statements", "ar_price", "ap_suppliers", "ap_invoices", "ap_orders", "ap_grn", "ap_payments", "ap_debit",
        "ap_req", "cb_cashbook", "cb_banks", "cb_recon", "cb_petty", "inv_items", "inv_wh", "inv_balance", "inv_adjust",
        "inv_take", "inv_transfer", "inv_bom", "fixedassets", "fa_depreciation", "fa_disposal", "payroll", "pr_employees",
        "pr_leave", "tax", "tax_return", "proj_cc", "proj_jobs", "proj_time", "erp_wo", "mfg_mrp", "mfg_sched",
        "erp_leads", "crm_contacts", "crm_opp", "crm_cases", "erp_br", "erp_dept", "erp_emp", "pos_till", "pos_cashup",
        "rep_financial", "rep_aged", "rep_stock", "rep_custom"
    ];

    public static IEnumerable<string> AllNavigationPermissions() =>
        AllModuleKeys.Select(Nav);

    public static IEnumerable<string> AllPermissions() =>
        AllNavigationPermissions()
            .Append(SecurityUsersManage)
            .Append(SecurityRolesManage)
            .Append(SecurityAuditSettings)
            .Append(SecurityBackupManage);
}
