namespace Mathy.ELM.Core.Entities;

public class HRRequest : BaseEntity
{
    public int SubmittedBy { get; set; }
    public string? SubmitterName { get; set; } // Display name of the user who submitted the request (captured at submission time)
    public DateTime? SubmittedDate { get; set; }
    public string? SubmitterEmail { get; set; } // Email of the user who submitted the request (captured at submission time)
    public string? Notes { get; set; }

    public virtual ICollection<HRRequestDetail> Details { get; set; } = new List<HRRequestDetail>();
    public virtual ICollection<NotificationQueue> Notifications { get; set; } = new List<NotificationQueue>();
}