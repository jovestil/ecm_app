namespace Mathy.ELM.Core.Entities;

public class PTCreditCardDetail : BaseEntity
{
    public int PTRequestDetailId { get; set; }

    // Credit Card Types
    public bool? KwikTripCard { get; set; }

    // New Hire Form Specific Fields (Option A expansion)
    public bool? CompanyExpenseCard { get; set; }    // Parent: "Does employee need Company Expense Card?"
    public string? CreditExpenseType { get; set; }   // "Fuel Only" or "Open for all business purchases"
    public decimal? WeeklyLimit { get; set; }        // "Company Credit Card Weekly Limit"

    // Shared Fields
    public bool? FuelCardlockAccess { get; set; }            // "Fuel Cardlock Access"
    public string? FuelCardlockAddress { get; set; }         // "Cardlock - ship address"

    public virtual PromotionRequestDetail PromotionRequestDetail { get; set; } = null!;
}
