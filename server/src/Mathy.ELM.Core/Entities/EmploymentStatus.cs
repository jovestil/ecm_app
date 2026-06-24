namespace Mathy.ELM.Core.Entities;

public class EmploymentStatus : BaseEntity
{
    public int CompanyCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string? CodeType { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Viewpoint Sync
    public DateTime? ViewpointSyncDate { get; set; }
}