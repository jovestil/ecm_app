namespace Mathy.ELM.Core.Entities;

public class ITTabletProfile : BaseEntity
{
    public int NewHireRequestId { get; set; }
    
    // Tablet Profile Information
    public int TabletProfileId { get; set; }
    public string? TabletProfileName { get; set; }
    public string RolesRequiredForNewHire { get; set; } = string.Empty;
    
    public virtual NewHireRequestDetail NewHireRequest { get; set; } = null!;
    public virtual TabletProfile TabletProfile { get; set; } = null!;
}