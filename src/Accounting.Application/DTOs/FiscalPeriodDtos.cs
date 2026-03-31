namespace Accounting.Application.DTOs;

public sealed class FiscalPeriodDto
{
    public int Id { get; init; }
    public int FiscalYearId { get; init; }
    public int Year { get; init; }
    public int PeriodNumber { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public bool IsClosed { get; init; }
    public bool FiscalYearClosed { get; init; }
}
