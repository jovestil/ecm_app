namespace Mathy.ELM.Core.Entities;

public class PhysicalLocation : BaseEntity
{
    public int LocationCode { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Viewpoint Sync
    public DateTime? ViewpointSyncDate { get; set; }
}