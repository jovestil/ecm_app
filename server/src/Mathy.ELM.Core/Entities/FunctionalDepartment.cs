namespace Mathy.ELM.Core.Entities;

public class FunctionalDepartment : BaseEntity
{
    public int FunctionalDeptCode { get; set; }
    public string FunctionalDeptName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Viewpoint Sync
    public DateTime? ViewpointSyncDate { get; set; }
}