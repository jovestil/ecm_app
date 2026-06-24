namespace Mathy.ELM.Core.Entities;

public class PTBuildingAccessRequirement : BaseEntity
{
    public int PTRequestDetailId { get; set; }
    public int AccessId { get; set; }
    public string AccessDescription { get; set; } = string.Empty;

    public virtual PromotionRequestDetail PromotionRequestDetail { get; set; } = null!;
    public virtual BuildingAccessRequirement BuildingAccessRequirement { get; set; } = null!;
}
