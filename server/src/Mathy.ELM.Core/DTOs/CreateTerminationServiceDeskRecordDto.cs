namespace Mathy.ELM.Core.DTOs;

/// <summary>
/// DTO for creating a ServiceDesk record from a Termination Request.
/// Mirrors the legacy ServiceDeskNotifications_Terminations payload (Off-Boarding template).
/// </summary>
public class CreateTerminationServiceDeskRecordDto
{
    // PRIMARY IDENTIFIERS
    public int TerminationRequestDetailId { get; set; }
    public int ParentHRRequestId { get; set; }

    // EMPLOYEE INFORMATION
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "First Name is required")]
    public string FirstName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Last Name is required")]
    public string LastName { get; set; } = string.Empty;

    // OFF-BOARD DATE
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Off-Board Date is required")]
    public DateTime OffBoardDate { get; set; }

    public long? OffBoardDateMilliseconds { get; set; }

    // REQUESTOR (who submitted the termination)
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Requestor name is required")]
    public string RequestorName { get; set; } = string.Empty;

    // COMMUNICATION FORWARDING
    public string? ForwardEmail { get; set; }
    public string? EmailAutoReply { get; set; }
    public string? ForwardDeskPhone { get; set; }
    public string? ForwardCellPhone { get; set; }
    public string? OneDriveAccessTo { get; set; }

    /// <summary>
    /// Reclaim equipment codes. Legacy values: "no", "computer", "deskPhone", "cellPhone".
    /// When null/empty, the service emits the legacy default ("No IT Equipment to reclaim").
    /// </summary>
    public List<string>? ReclaimEquipment { get; set; }
}
