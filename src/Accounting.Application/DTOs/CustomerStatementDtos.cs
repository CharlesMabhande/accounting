namespace Accounting.Application.DTOs;

/// <summary>AR customer statement (invoices and receipts for the period).</summary>
public sealed class CustomerStatementDto
{
    public string CompanyName { get; init; } = "";
    public string CompanyCode { get; init; } = "";
    public string CustomerCode { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public string? CustomerAddress { get; init; }
    public DateOnly PeriodFrom { get; init; }
    public DateOnly PeriodTo { get; init; }
    public DateOnly StatementDate { get; init; }
    public decimal OpeningBalance { get; init; }
    public decimal ClosingBalance { get; init; }
    public IReadOnlyList<CustomerStatementLineDto> Lines { get; init; } = Array.Empty<CustomerStatementLineDto>();
}

public sealed class CustomerStatementLineDto
{
    public DateOnly Date { get; init; }
    public string DocumentType { get; init; } = "";
    public string Reference { get; init; } = "";
    public string Description { get; init; } = "";
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal Balance { get; init; }
}
