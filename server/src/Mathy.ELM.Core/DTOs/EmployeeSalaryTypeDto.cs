namespace Mathy.ELM.Core.DTOs;

public class EmployeeSalaryTypeDto
{
    public int Id { get; set; }
    public int CompanyCode { get; set; }
    public int? SalaryCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? ViewpointSyncDate { get; set; }
}