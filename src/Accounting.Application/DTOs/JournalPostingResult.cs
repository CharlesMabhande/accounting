namespace Accounting.Application.DTOs;

public sealed class JournalPostingResult
{
    public bool Success { get; init; }
    public int? JournalEntryId { get; init; }
    public string? EntryNumber { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static JournalPostingResult Ok(int journalEntryId, string entryNumber) =>
        new() { Success = true, JournalEntryId = journalEntryId, EntryNumber = entryNumber };

    public static JournalPostingResult Fail(params string[] errors) =>
        new() { Success = false, Errors = errors };
}
