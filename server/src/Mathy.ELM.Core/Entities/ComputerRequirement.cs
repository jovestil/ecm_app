namespace Mathy.ELM.Core.Entities;

public class ComputerRequirement : BaseEntity
{
    public string Description { get; set; } = string.Empty;
    public bool? IsChild { get; set; } = false; // 0 Main, 1 Child
    public int? ParentId { get; set; } // What is the Id of the parent
    public bool IsActive { get; set; } = true;
    
    public virtual ICollection<ITComputerRequirement> ITComputerRequirements { get; set; } = new List<ITComputerRequirement>();
}