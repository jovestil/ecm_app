using System.ComponentModel.DataAnnotations;

namespace Mathy.ELM.Core.DTOs;

public class CreateADUserRequestDto
{
    [Required(ErrorMessage = "Company code is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Company code must be a positive number")]
    public int CompanyCode { get; set; }

    [Required(ErrorMessage = "Payroll department code is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Payroll department code must be a positive number")]
    public int PayrollDeptCode { get; set; }

    [StringLength(100, ErrorMessage = "Preferred first name cannot exceed 100 characters")]
    public string? PreferredFirstName { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(5, ErrorMessage = "Middle initial cannot exceed 5 characters")]
    public string? MiddleInitial { get; set; }

    [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public string? Title { get; set; }

    [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
    public string? Department { get; set; }
}