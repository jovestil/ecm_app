namespace Mathy.ELM.Core.Entities;

public class PTITTabletProfile : BaseEntity
{
    public int PTRequestDetailId { get; set; }

    // Tablet Profile Information
    public int TabletProfileId { get; set; }
    public string? TabletProfileName { get; set; }
    public string RolesRequiredForNewHire { get; set; } = string.Empty;

    public virtual PromotionRequestDetail PromotionRequestDetail { get; set; } = null!;
    public virtual TabletProfile TabletProfile { get; set; } = null!;
}
