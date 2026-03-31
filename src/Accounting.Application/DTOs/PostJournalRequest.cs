using Accounting.Domain.Enums;

namespace Accounting.Application.DTOs;

public sealed class PostJournalRequest
{
    public int CompanyId { get; init; }
    public DateOnly EntryDate { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ModuleCode SourceModule { get; init; } = ModuleCode.GeneralLedger;
    public int? SourceDocumentId { get; init; }
    public IReadOnlyList<PostJournalLineDto> Lines { get; init; } = Array.Empty<PostJournalLineDto>();
}

public sealed class PostJournalLineDto
{
    public int LedgerAccountId { get; init; }
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public string Narration { get; init; } = string.Empty;
    public int? CustomerId { get; init; }
    public int? SupplierId { get; init; }
}
