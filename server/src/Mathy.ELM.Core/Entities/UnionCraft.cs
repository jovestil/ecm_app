namespace Mathy.ELM.Core.Entities;

public class UnionCraft : BaseEntity
{
    public int CompanyCode { get; set; }
    public string CraftCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Viewpoint Sync
    public DateTime? ViewpointSyncDate { get; set; }
}