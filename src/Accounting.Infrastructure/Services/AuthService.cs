using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private static readonly TimeSpan SessionDuration = TimeSpan.FromHours(8);
    private readonly AccountingDbContext _db;

    public AuthService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<OperationResult<LoginResponseDto>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.UserAccounts
            .Include(u => u.Roles).ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.UserName == request.UserName, cancellationToken);

        if (user is null || !user.IsActive)
            return OperationResult<LoginResponseDto>.Fail("Invalid credentials.");

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
            return OperationResult<LoginResponseDto>.Fail("Invalid credentials.");

        var permissions = user.Roles
            .SelectMany(r => r.Permissions)
            .Select(p => p.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var token = Guid.NewGuid();
        var expires = DateTime.UtcNow.Add(SessionDuration);
        _db.UserSessions.Add(new UserSession
        {
            Token = token,
            UserId = user.Id,
            ExpiresAtUtc = expires
        });
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<LoginResponseDto>.Ok(new LoginResponseDto
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Roles = user.Roles.Select(r => r.Name).ToList(),
            Permissions = permissions,
            SessionToken = token.ToString("N"),
            ExpiresAtUtc = expires,
            AccountKind = user.AccountKind == Domain.Enums.UserAccountKind.Agent ? "Agent" : "Staff"
        });
    }

    public async Task<OperationResult<SessionInfoDto>> GetSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionToken, out var guid))
            return OperationResult<SessionInfoDto>.Fail("Invalid session.");

        var session = await _db.UserSessions
            .Include(s => s.User).ThenInclude(u => u.Roles).ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(s => s.Token == guid, cancellationToken);
        if (session is null || session.ExpiresAtUtc < DateTime.UtcNow)
            return OperationResult<SessionInfoDto>.Fail("Session expired.");

        var user = session.User;
        if (!user.IsActive)
            return OperationResult<SessionInfoDto>.Fail("Account inactive.");

        var permissions = user.Roles
            .SelectMany(r => r.Permissions)
            .Select(p => p.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        return OperationResult<SessionInfoDto>.Ok(new SessionInfoDto
        {
            UserId = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Roles = user.Roles.Select(r => r.Name).ToList(),
            Permissions = permissions,
            AccountKind = user.AccountKind == Domain.Enums.UserAccountKind.Agent ? "Agent" : "Staff"
        });
    }

    public async Task LogoutAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionToken, out var guid))
            return;
        await _db.UserSessions.Where(s => s.Token == guid).ExecuteDeleteAsync(cancellationToken);
    }
}
