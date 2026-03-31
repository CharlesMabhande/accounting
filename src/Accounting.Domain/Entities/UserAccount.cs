using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class UserAccount : BaseEntity
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    /// <summary>Staff vs agent classification (RBAC still applies via roles).</summary>
    public UserAccountKind AccountKind { get; set; } = UserAccountKind.Staff;
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}
