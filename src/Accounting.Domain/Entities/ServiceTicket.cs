using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class ServiceTicket : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string TicketNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ServiceTicketPriority Priority { get; set; } = ServiceTicketPriority.Normal;
    public ServiceTicketStatus Status { get; set; } = ServiceTicketStatus.Open;
    public int? AssignedToUserId { get; set; }
    public UserAccount? AssignedToUser { get; set; }
    public DateTime OpenedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }
}
