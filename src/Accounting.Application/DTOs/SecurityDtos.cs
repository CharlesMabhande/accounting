using Accounting.Domain.Enums;

namespace Accounting.Application.DTOs;

public sealed class UserAccountListDto
{
    public int Id { get; init; }
    public string UserName { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public bool IsActive { get; init; }
    public string AccountKind { get; init; } = "Staff";
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}

public sealed class UserAccountDetailDto
{
    public int Id { get; init; }
    public string UserName { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public bool IsActive { get; init; }
    public string AccountKind { get; init; } = "Staff";
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<int> RoleIds { get; init; } = Array.Empty<int>();
}

public sealed class CreateUserAccountRequest
{
    public string UserName { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Password { get; init; } = "";
    public bool IsActive { get; init; } = true;
    public UserAccountKind AccountKind { get; init; } = UserAccountKind.Staff;
    public IReadOnlyList<int> RoleIds { get; init; } = Array.Empty<int>();
}

public sealed class UpdateUserAccountRequest
{
    public string DisplayName { get; init; } = "";
    public string? Password { get; init; }
    public bool IsActive { get; init; } = true;
    public UserAccountKind AccountKind { get; init; } = UserAccountKind.Staff;
    public IReadOnlyList<int> RoleIds { get; init; } = Array.Empty<int>();
}

public sealed class RoleListDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int UserCount { get; init; }
}

public sealed class RoleDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}

public sealed class CreateRoleRequest
{
    public string Name { get; init; } = "";
    public IReadOnlyList<string> PermissionNames { get; init; } = Array.Empty<string>();
}

public sealed class UpdateRoleRequest
{
    public string Name { get; init; } = "";
    public IReadOnlyList<string> PermissionNames { get; init; } = Array.Empty<string>();
}

public sealed class PermissionDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
}

public sealed class AuditTableSettingDto
{
    public string EntityTypeName { get; init; } = "";
    public bool IsEnabled { get; init; }
    public bool AuditInserts { get; init; }
    public bool AuditUpdates { get; init; }
    public bool AuditDeletes { get; init; }
}

public sealed class SaveAuditTableSettingsRequest
{
    public IReadOnlyList<AuditTableSettingDto> Settings { get; init; } = Array.Empty<AuditTableSettingDto>();
}
