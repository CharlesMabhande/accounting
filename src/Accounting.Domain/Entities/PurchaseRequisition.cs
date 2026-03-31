using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class PurchaseRequisition : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateOnly RequestDate { get; set; }
    public PurchaseRequisitionStatus Status { get; set; } = PurchaseRequisitionStatus.Draft;
    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string? Notes { get; set; }
    public int? ApprovedByUserId { get; set; }
    public UserAccount? ApprovedByUser { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? RejectedReason { get; set; }
    public ICollection<PurchaseRequisitionLine> Lines { get; set; } = new List<PurchaseRequisitionLine>();
}
