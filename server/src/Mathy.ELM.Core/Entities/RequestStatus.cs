namespace Mathy.ELM.Core.Entities;

public class RequestStatus : BaseEntity
{
    public string RequestStatusName { get; set; } = string.Empty;
    public string? RequestStatusDescription { get; set; }
    public string? RequestDisplayStatusName { get; set; } // Added to match Arnie's schema - for user-friendly status display
    public bool IsActive { get; set; } = true;

    public virtual ICollection<HRRequestDetail> HRRequestDetails { get; set; } = new List<HRRequestDetail>();
}