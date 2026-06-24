namespace Mathy.ELM.Core.DTOs;

/// <summary>
/// DTO containing data needed for ReturnToWork email template field mapping
/// </summary>
public class ReturnToWorkEmailDataDto
{
    /// <summary>
    /// Employee ID for lookups
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Employee name (First + Last)
    /// </summary>
    public string? EmployeeName { get; set; }

    /// <summary>
    /// Company code for lookups
    /// </summary>
    public int? CompanyCode { get; set; }

    /// <summary>
    /// Company name
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// Department/Division code for lookups
    /// </summary>
    public int? DeptCode { get; set; }

    /// <summary>
    /// Department/Division name
    /// </summary>
    public string? DivisionName { get; set; }

    /// <summary>
    /// Employment status
    /// </summary>
    public string? EmploymentStatus { get; set; }

    /// <summary>
    /// Effective date of return to work
    /// </summary>
    public DateTime? EffectiveDate { get; set; }

    /// <summary>
    /// Previous effective date (before date change)
    /// </summary>
    public DateTime? PreviousEffectiveDate { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Submitter name/email for the request
    /// </summary>
    public string? Submitter { get; set; }

    /// <summary>
    /// Manager/Supervisor email for notifications
    /// </summary>
    public string? ManagerEmail { get; set; }

    /// <summary>
    /// Error message for failed request notifications
    /// </summary>
    public string? ErrorMessage { get; set; }
}
