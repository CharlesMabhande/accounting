namespace Accounting.Application.Abstractions;

/// <summary>Per-request authenticated user (set by API middleware from session token).</summary>
public interface ICurrentSessionContext
{
    int? UserId { get; set; }
}
