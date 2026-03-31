namespace Accounting.Domain.Entities;

public class AuditLog : BaseEntity
{
    public int? UserId { get; set; }
    public UserAccount? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityKey { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
