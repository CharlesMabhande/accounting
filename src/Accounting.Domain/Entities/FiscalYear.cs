namespace Accounting.Domain.Entities;

public class FiscalYear : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int Year { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsClosed { get; set; }
    public ICollection<FiscalPeriod> Periods { get; set; } = new List<FiscalPeriod>();
}
