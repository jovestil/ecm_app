namespace Mathy.ELM.Core.Entities;

public class NewHireBuildingAccessRequirement : BaseEntity
{
    public int NewHireRequestId { get; set; }
    public int AccessId { get; set; }
    public string AccessDescription { get; set; } = string.Empty;
    
    public virtual NewHireRequestDetail NewHireRequest { get; set; } = null!;
    public virtual BuildingAccessRequirement BuildingAccessRequirement { get; set; } = null!;
}