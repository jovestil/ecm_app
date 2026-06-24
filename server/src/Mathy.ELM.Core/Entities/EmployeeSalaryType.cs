namespace Mathy.ELM.Core.Entities;

public class EmployeeSalaryType : BaseEntity
{
    public int CompanyCode { get; set; }
    public int? SalaryCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Viewpoint Sync
    public DateTime? ViewpointSyncDate { get; set; }
}