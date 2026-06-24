namespace Mathy.ELM.Core.Entities;

public class ITPhoneRequirement : BaseEntity
{
    public int NewHireRequestId { get; set; }

    // Phone Requirements
    public bool? DeskPhone { get; set; }
    public bool? CompanyCellphone { get; set; }
    public bool? BYODCellphone { get; set; }
    public bool? ReusingExistingPhone { get; set; }
    public string? WorkPhoneNumber { get; set; }
    public string? WorkExtension { get; set; }

    public virtual NewHireRequestDetail NewHireRequest { get; set; } = null!;
}