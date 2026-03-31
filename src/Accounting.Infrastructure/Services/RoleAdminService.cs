using Accounting.Application.Abstractions;
using Accounting.Application.Common;
using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public sealed class RoleAdminService : IRoleAdminService
{
    private readonly AccountingDbContext _db;

    public RoleAdminService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RoleListDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleListDto
            {
                Id = r.Id,
                Name = r.Name,
                UserCount = r.Users.Count()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var r = await _db.Roles.AsNoTracking()
            .Include(x => x.Permissions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (r is null)
            return null;
        return new RoleDetailDto
        {
            Id = r.Id,
            Name = r.Name,
            Permissions = r.Permissions.Select(p => p.Name).OrderBy(x => x).ToList()
        };
    }

    public async Task<OperationResult<CreatedEntityInfo>> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return OperationResult<CreatedEntityInfo>.Fail("Role name is required.");
        if (await _db.Roles.AnyAsync(r => r.Name == request.Name, cancellationToken))
            return OperationResult<CreatedEntityInfo>.Fail("Role name already exists.");

        var perms = await _db.Permissions.Where(p => request.PermissionNames.Contains(p.Name)).ToListAsync(cancellationToken);
        var role = new Role { Name = request.Name.Trim() };
        foreach (var p in perms)
            role.Permissions.Add(p);
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<CreatedEntityInfo>.Ok(new CreatedEntityInfo { Id = role.Id });
    }

    public async Task<OperationResult> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null)
            return OperationResult.Fail("Role not found.");
        if (!string.IsNullOrWhiteSpace(request.Name))
            role.Name = request.Name.Trim();
        var perms = await _db.Permissions.Where(p => request.PermissionNames.Contains(p.Name)).ToListAsync(cancellationToken);
        role.Permissions.Clear();
        foreach (var p in perms)
            role.Permissions.Add(p);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null)
            return OperationResult.Fail("Role not found.");
        if (role.Users.Count > 0)
            return OperationResult.Fail("Remove users from this role before deleting it.");
        _db.Roles.Remove(role);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok();
    }
}
