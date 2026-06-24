namespace Mathy.ELM.Core.Entities;

public class PTApplicationRequest : BaseEntity
{
    public int PTRequestDetailId { get; set; }
    public int ApplicationId { get; set; }
    public string? AccessNotes { get; set; }

    public virtual PromotionRequestDetail PromotionRequestDetail { get; set; } = null!;
    public virtual Application Application { get; set; } = null!;
}
