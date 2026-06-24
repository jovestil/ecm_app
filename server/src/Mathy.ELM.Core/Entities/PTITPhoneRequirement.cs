namespace Mathy.ELM.Core.Entities;

public class PTITPhoneRequirement : BaseEntity
{
    public int PTRequestDetailId { get; set; }

    // Phone Requirements
    public bool? DeskPhone { get; set; }
    public bool? CompanyCellphone { get; set; }
    public bool? BYODCellphone { get; set; }
    public string? WorkPhoneNumber { get; set; }
    public string? WorkExtension { get; set; }
    public string? WorkCell { get; set; }
    public bool? ReusingExistingPhone { get; set; }

    public virtual PromotionRequestDetail PromotionRequestDetail { get; set; } = null!;
}
