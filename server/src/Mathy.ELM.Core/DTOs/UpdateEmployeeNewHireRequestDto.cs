namespace Mathy.ELM.Core.DTOs;

/// <summary>
/// Request DTO for updating a new hire employee in Viewpoint
/// </summary>
public class UpdateEmployeeNewHireRequestDto
{
    /// <summary>
    /// Company code (HRCo)
    /// </summary>
    public int HRCo { get; set; }

    /// <summary>
    /// Employee reference number (HRRef)
    /// </summary>
    public int HRRef { get; set; }

    /// <summary>
    /// Payroll department code
    /// </summary>
    public string? PRDept { get; set; }

    /// <summary>
    /// Employee last name (used for verification)
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Hire date (used for verification)
    /// </summary>
    public string? HireDate { get; set; }

    /// <summary>
    /// Custom fields to update
    /// </summary>
    public ViewpointCustomFieldsUpdateDto? CustomFields { get; set; }
}

/// <summary>
/// Custom fields for Viewpoint employee update
/// </summary>
public class ViewpointCustomFieldsUpdateDto
{
    /// <summary>
    /// Supervisor employee number
    /// </summary>
    public string? udSupervisor { get; set; }

    /// <summary>
    /// Network user ID (Active Directory username)
    /// </summary>
    public string? udNetworkUserID { get; set; }

    /// <summary>
    /// Work email address
    /// </summary>
    public string? udWorkEmail { get; set; }

    /// <summary>
    /// Physical location code
    /// </summary>
    public string? udPhysicalLocation { get; set; }

    /// <summary>
    /// Employee nickname
    /// </summary>
    public string? udNickname { get; set; }

    /// <summary>
    /// Position code
    /// </summary>
    public string? PositionCode { get; set; }

    /// <summary>
    /// Employment status
    /// </summary>
    public string? Status { get; set; }
}
