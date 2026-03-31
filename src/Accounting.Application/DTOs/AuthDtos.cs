namespace Accounting.Application.DTOs;

public sealed class LoginRequest
{
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class LoginResponseDto
{
    public int UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
    public string SessionToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    public string AccountKind { get; init; } = "Staff";
}

public sealed class SessionInfoDto
{
    public int UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
    public string AccountKind { get; init; } = "Staff";
}
