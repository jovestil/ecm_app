namespace Mathy.ELM.Core.Entities;

public class EmployeeLicenseClass : BaseEntity
{
    public string LicenseClass { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsUnion { get; set; } = false;
    
    // Viewpoint Sync
    public DateTime? ViewpointSyncDate { get; set; }
}