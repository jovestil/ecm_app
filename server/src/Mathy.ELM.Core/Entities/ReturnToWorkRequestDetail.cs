namespace Mathy.ELM.Core.Entities;

public class ReturnToWorkRequestDetail : BaseEntity
{
    public int RequestDetailId { get; set; }
    
    // Additional fields specific to return to work can be added here
    // Currently using base fields from HRRequestDetails
    
    public virtual HRRequestDetail HRRequestDetail { get; set; } = null!;
}