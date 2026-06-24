namespace Mathy.ELM.Core.Entities;

public class VehicleDetail : BaseEntity
{
    public int NewHireRequestId { get; set; }
    
    public bool? IsApprovedToOperate { get; set; }
    public string? DriverClassification { get; set; }
    public string? DrugAndAlcoholProfile { get; set; } // 'DOT Drug Pool', 'WI Prevailing Wage Pool', 'No Testing'
    public bool? NeedCompanyCar { get; set; }
    public bool? IsApplicationPart2Complete { get; set; }
    
    public virtual NewHireRequestDetail NewHireRequest { get; set; } = null!;
}