namespace Mathy.ELM.Core.Entities;

public class UserCompanyAccess : BaseEntity
{
    public string UserId { get; set; } = string.Empty; // Entra ID user identifier
    public string UserName { get; set; } = string.Empty;
    public int CompanyCode { get; set; }
    
    // Access Control
    public bool CanSubmitRequests { get; set; } = true;
    
    // Sync Information
    public string Source { get; set; } = string.Empty; // 'Viewpoint', 'EntraGroups'
    public DateTime? LastSyncDate { get; set; }
}