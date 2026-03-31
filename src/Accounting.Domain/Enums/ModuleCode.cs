namespace Accounting.Domain.Enums;

/// <summary>ERP subsystems (financial + operational).</summary>
public enum ModuleCode
{
    GeneralLedger = 1,
    AccountsReceivable = 2,
    AccountsPayable = 3,
    CashBook = 4,
    Inventory = 5,
    SalesOrders = 6,
    PurchaseOrders = 7,
    Tax = 8,
    FixedAssets = 9,
    Payroll = 10,
    Projects = 11,
    HumanResources = 12,
    Crm = 13,
    Manufacturing = 14,
    Procurement = 15,
    Budgeting = 16,
    ServiceManagement = 17,
    MultiCurrency = 18,
    MultiSite = 19
}
