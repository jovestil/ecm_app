namespace Mathy.ELM.Core.Entities;

public class ITComputerRequirement : BaseEntity
{
    public int NewHireRequestId { get; set; }
    
    // Computer Requirements Information
    public int ComputerRequirementsId { get; set; }
    public string? ComputerRequirementsDescription { get; set; }
    public bool? IsChild { get; set; } = false;
    public int? ParentId { get; set; }
    
    public virtual NewHireRequestDetail NewHireRequest { get; set; } = null!;
    public virtual ComputerRequirement ComputerRequirement { get; set; } = null!;
}