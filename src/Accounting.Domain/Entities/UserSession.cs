namespace Accounting.Domain.Entities;

public class UserSession
{
    public int Id { get; set; }
    public Guid Token { get; set; }
    public int UserId { get; set; }
    public UserAccount User { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
