namespace Mathy.ELM.Core.Entities;

public class PTITComputerRequirement : BaseEntity
{
    public int PTRequestDetailId { get; set; }

    // Computer Requirements Information
    public int ComputerRequirementsId { get; set; }
    public string? ComputerRequirementsDescription { get; set; }
    public bool? IsChild { get; set; } = false;
    public int? ParentId { get; set; }

    public virtual PromotionRequestDetail PromotionRequestDetail { get; set; } = null!;
    public virtual ComputerRequirement ComputerRequirement { get; set; } = null!;
}
