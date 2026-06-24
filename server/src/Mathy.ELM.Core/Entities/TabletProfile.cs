namespace Mathy.ELM.Core.Entities;

public class TabletProfile : BaseEntity
{
    public string LocationType { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<ITTabletProfile> ITTabletProfiles { get; set; } = new List<ITTabletProfile>();
}