namespace Accounting.Domain.Entities;

public class BudgetLine : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int FiscalYearId { get; set; }
    public FiscalYear FiscalYear { get; set; } = null!;
    public int LedgerAccountId { get; set; }
    public LedgerAccount LedgerAccount { get; set; } = null!;
    public int PeriodNumber { get; set; }
    public decimal Amount { get; set; }
}
