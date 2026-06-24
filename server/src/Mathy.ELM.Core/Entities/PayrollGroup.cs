namespace Mathy.ELM.Core.Entities;

public class PayrollGroup : BaseEntity
{
    public int CompanyCode { get; set; }
    public int GroupCode { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Viewpoint Sync
    public DateTime? ViewpointSyncDate { get; set; }
}