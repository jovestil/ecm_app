namespace Mathy.ELM.Core.Entities;

public class TerminationReason : BaseEntity
{
    public string ReasonCode { get; set; } = string.Empty;
    public string ReasonDescription { get; set; } = string.Empty;
    public int CompanyCode { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Viewpoint Sync
    public DateTime? ViewpointSyncDate { get; set; }
}