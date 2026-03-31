namespace Accounting.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<UserAccount> Users { get; set; } = new List<UserAccount>();
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
