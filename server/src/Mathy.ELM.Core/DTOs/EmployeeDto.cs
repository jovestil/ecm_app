namespace Mathy.ELM.Core.DTOs;

public class EmployeeDto
{
    public int EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string? EmployeeNetworkId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? CompanyCode { get; set; }
    public string? CompanyName { get; set; }
    public string? DivisionCode { get; set; }
    public string? DivisionName { get; set; }
    public string? Position { get; set; }
    public string? Department { get; set; }
    public int? PayrollDeptCode { get; set; }
    public int? PayrollCompanyCode { get; set; }
    public int? PayrollGroupCode { get; set; }
    public int? PhysicalLocationCode { get; set; }
    public string? Email { get; set; }
    public string? WorkEmail { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }

    // Additional fields for comprehensive employee data
    public string? Supervisor { get; set; }
    public int? SupervisorId { get; set; }
    public DateTime? HireDate { get; set; }
    public string? Status { get; set; }
    public int? SalaryCode { get; set; }

    // Indicates if the employee already has an existing HR request
    public bool HasExistingHRRequest { get; set; }
}