using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class UserAccountAdminService : IUserAccountAdminService
{
    private readonly AccountingDbContext _db;

    public UserAccountAdminService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserAccountListDto>> ListAsync(int? limit, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit ?? 500, 1, 2000);
        var rows = await _db.UserAccounts.AsNoTracking()
            .Include(u => u.Roles)
            .OrderBy(u => u.UserName)
            .Take(take)
            .ToListAsync(cancellationToken);
        return rows.Select(u => new UserAccountListDto
        {
            Id = u.Id,
            UserName = u.UserName,
            DisplayName = u.DisplayName,
            IsActive = u.IsActive,
            AccountKind = u.AccountKind == Domain.Enums.UserAccountKind.Agent ? "Agent" : "Staff",
            Roles = u.Roles.Select(r => r.Name).ToList()
        }).ToList();
    }

    public async Task<UserAccountDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var u = await _db.UserAccounts.AsNoTracking()
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (u is null)
            return null;
        return new UserAccountDetailDto
        {
            Id = u.Id,
            UserName = u.UserName,
            DisplayName = u.DisplayName,
            IsActive = u.IsActive,
            AccountKind = u.AccountKind == Domain.Enums.UserAccountKind.Agent ? "Agent" : "Staff",
            Roles = u.Roles.Select(r => r.Name).ToList(),
            RoleIds = u.Roles.Select(r => r.Id).ToList()
        };
    }

    public async Task<OperationResult<CreatedEntityInfo>> CreateAsync(CreateUserAccountRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            return OperationResult<CreatedEntityInfo>.Fail("User name and password are required.");
        if (await _db.UserAccounts.AnyAsync(u => u.UserName == request.UserName, cancellationToken))
            return OperationResult<CreatedEntityInfo>.Fail("User name already exists.");

        var roles = await _db.Roles.Where(r => request.RoleIds.Contains(r.Id)).ToListAsync(cancellationToken);
        var user = new UserAccount
        {
            UserName = request.UserName.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.UserName.Trim() : request.DisplayName.Trim(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            IsActive = request.IsActive,
            AccountKind = request.AccountKind
        };
        foreach (var r in roles)
            user.Roles.Add(r);
        _db.UserAccounts.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<CreatedEntityInfo>.Ok(new CreatedEntityInfo { Id = user.Id });
    }

    public async Task<OperationResult> UpdateAsync(int id, UpdateUserAccountRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.UserAccounts.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return OperationResult.Fail("User not found.");
        user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? user.DisplayName : request.DisplayName.Trim();
        user.IsActive = request.IsActive;
        user.AccountKind = request.AccountKind;
        if (!string.IsNullOrEmpty(request.Password))
            user.PasswordHash = PasswordHasher.Hash(request.Password);

        var roles = await _db.Roles.Where(r => request.RoleIds.Contains(r.Id)).ToListAsync(cancellationToken);
        user.Roles.Clear();
        foreach (var r in roles)
            user.Roles.Add(r);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return OperationResult.Fail("User not found.");
        user.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return OperationResult.Fail("User not found.");
        _db.UserSessions.RemoveRange(_db.UserSessions.Where(s => s.UserId == id));
        _db.UserAccounts.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }
}
