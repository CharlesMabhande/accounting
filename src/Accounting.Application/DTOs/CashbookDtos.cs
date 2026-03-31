namespace Accounting.Application.DTOs;

public sealed class CreateCashbookRequest
{
    public int CompanyId { get; init; }
    public int BankAccountId { get; init; }
    public DateOnly TransactionDate { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public bool IsReceipt { get; init; }
    public int? CustomerId { get; init; }
    public int? SupplierId { get; init; }
}
