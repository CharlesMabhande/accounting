namespace Accounting.Application.DTOs;

public sealed class CreatePayrollRunRequest
{
    public int CompanyId { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public decimal GrossWages { get; init; }
    public decimal TaxWithheld { get; init; }
    public decimal NetPay { get; init; }
}
