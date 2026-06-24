namespace Mathy.ELM.Core.Entities;

public class Company : BaseEntity
{
    public int CompanyCode { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Viewpoint Sync
    public DateTime? ViewpointSyncDate { get; set; }
    
    public virtual ICollection<UserCompanyAccess> UserAccess { get; set; } = new List<UserCompanyAccess>();
}