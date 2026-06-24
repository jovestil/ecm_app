namespace Mathy.ELM.Core.Entities;

public class BuildingAccessRequirement : BaseEntity
{
    public string LocationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<NewHireBuildingAccessRequirement> NewHireBuildingAccessRequirements { get; set; } = new List<NewHireBuildingAccessRequirement>();
}