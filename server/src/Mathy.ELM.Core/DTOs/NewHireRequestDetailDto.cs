namespace Mathy.ELM.Core.DTOs;

public class NewHireRequestDetailDto
{
    public int Id { get; set; }
    public int RequestDetailId { get; set; }

    // Personal Information
    public int? EmployeeId { get; set; }
    public string? FirstName { get; set; } // Changed to nullable to match entity
    public string? LastName { get; set; } // Changed to nullable to match entity
    public string? MiddleInitial { get; set; }
    public string? Suffix { get; set; }
    public string? PreferredFirstName { get; set; }
    public string? UserId { get; set; }
    public DateTime? FirstDayEmployment { get; set; } // Changed to nullable to match entity
    public string? ReferredBy { get; set; }
    public bool? Rehire { get; set; } // Changed to nullable to match entity

    // Position Information
    public int? CompanyCode { get; set; } // Changed to nullable to match entity
    public int? LocationCode { get; set; } // Changed to nullable to match entity
    public string? EmploymentStatus { get; set; } // Changed to nullable to match entity
    public bool? IsUnion { get; set; }
    public int? UnionCraftId { get; set; }
    public bool? IsApprentice { get; set; }
    public bool? IsUnionWage { get; set; }
    public int? SalaryCode { get; set; }
    public string? PositionCode { get; set; } // Changed to nullable to match entity
    public int? PayrollDeptCode { get; set; }
    public int? SupervisorId { get; set; }
    public string? Notes { get; set; }

    // Audit Fields
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
}