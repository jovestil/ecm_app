namespace Mathy.ELM.Core.DTOs;

public class SupervisorDto
{
    public int Id { get; set; }
    public int? SupervisorId { get; set; }
    public int EmployeeNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int CompanyCode { get; set; }
    public int? PayrollDeptCode { get; set; }
    public string? EmploymentStatus { get; set; }
}