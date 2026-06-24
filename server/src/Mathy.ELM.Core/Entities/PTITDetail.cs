namespace Mathy.ELM.Core.Entities;

public class PTITDetail : BaseEntity
{
    public int PTRequestDetailId { get; set; }

    // Email and Delivery
    public bool? EmailRequired { get; set; }                // "Does New Hire Require an Email Address"
    public string? AlternateDeliveryLocation { get; set; }  // Alternative delivery location override

    // Microsoft Office License Requirements (New Hire Form expansion)
    public bool? MSOfficeLicenseE5 { get; set; }      // "Will need an E5 M$ license" checkbox
    public bool? MSOfficeLicenseF3 { get; set; }      // "Will need an F3 M$ license" checkbox

    public virtual PromotionRequestDetail PromotionRequestDetail { get; set; } = null!;
}
