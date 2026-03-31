namespace Accounting.Application.Common;

public sealed class OperationResult
{
    public bool Success { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static OperationResult Ok() => new() { Success = true };
    public static OperationResult Fail(params string[] errors) => new() { Success = false, Errors = errors };
}

public sealed class OperationResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static OperationResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static OperationResult<T> Fail(params string[] errors) => new() { Success = false, Errors = errors };
}

public sealed class PostJournalInfo
{
    public int JournalEntryId { get; init; }
    public string EntryNumber { get; init; } = string.Empty;
}

public sealed class CreatedEntityInfo
{
    public int Id { get; init; }
    public string DocumentNumber { get; init; } = string.Empty;
}
