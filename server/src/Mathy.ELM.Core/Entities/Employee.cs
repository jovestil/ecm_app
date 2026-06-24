namespace Mathy.ELM.Core.Entities;

public class Employee : BaseEntity
{
    public int CompanyCode { get; set; }
    public int EmployeeNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? PersonalEmail { get; set; }
    public string? WorkEmail { get; set; }
    public string? NetworkId { get; set; } // AD Network ID
    public int? PayrollCompanyCode { get; set; }
    public int? PayrollGroupCode { get; set; }
    public int? PayrollDeptCode { get; set; }
    public string? PositionCode { get; set; }
    public int? SupervisorId { get; set; }
    public int? FunctionalDeptCode { get; set; }
    public int? PhysicalLocationCode { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? TerminationReasonCode { get; set; }
    public DateTime? ReturnToWorkDate { get; set; }
    public string? EmploymentStatus { get; set; }
    public int? SalaryCode { get; set; }
    public string? WorkPhoneNumber { get; set; } // Work phone number
    public string? WorkExtension { get; set; } // Work phone extension
    public string? WorkCell { get; set; } // Work cell phone number

    // Viewpoint Sync
    public DateTime? ViewpointSyncDate { get; set; }
}