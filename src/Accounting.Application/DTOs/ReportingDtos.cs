namespace Accounting.Application.DTOs;

public sealed class TrialBalanceLineDto
{
    public int LedgerAccountId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string AccountType { get; init; } = string.Empty;
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal Balance { get; init; }
}

public sealed class LedgerLineDto
{
    public DateOnly EntryDate { get; init; }
    public string EntryNumber { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal RunningBalance { get; init; }
}
