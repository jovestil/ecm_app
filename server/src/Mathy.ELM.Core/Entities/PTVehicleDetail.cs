namespace Mathy.ELM.Core.Entities;

public class PTVehicleDetail : BaseEntity
{
    public int PTRequestDetailId { get; set; }

    public bool? IsApprovedToOperate { get; set; }
    public string? LicenseClass { get; set; }
    public string? DrugAndAlcoholProfile { get; set; } // 'DOT Drug Pool', 'WI Prevailing Wage Pool', 'No Testing'
    public bool? NeedCompanyCar { get; set; }
    public bool? IsApplicationPart2Complete { get; set; }

    public virtual PromotionRequestDetail PromotionRequestDetail { get; set; } = null!;
}
