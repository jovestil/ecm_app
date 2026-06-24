namespace Mathy.ELM.Core.Entities;

public class CompanyDL : BaseEntity
{
    public int CompanyCode { get; set; }
    public int DeptCode { get; set; }
    public string? SiteDL { get; set; } // 'DL-NewHireNote-Mathy-Corp-Lab-Shop@corpmts.com'
    public string? SecurityDL { get; set; }
    public string? CreditCardDL { get; set; } // 'DL-NewHireCashMgmt-Corp@corpmts.com'
    public string? FleetDL { get; set; } // 'DL-NewHireTransportationMgmt-Corp@corpmts.com'
    public string? ComplianceDL { get; set; } // 'DL-NewHireCompliance-Corp@corpmts.com'
    public string? SafetyDL { get; set; } // 'DL-Construction-Safety@corpmts.com;DL-Corporate-Safety@corpmts.com'
    public string? FuelFobDL { get; set; } // Fuel fob distribution list
    public string? HRDL { get; set; } // 'DL-NewHireNote-Milestone@corpmts.com'
    public string? ITDL { get; set; }
    public string? PAYROLLDL { get; set; } // Payroll distribution list
}
