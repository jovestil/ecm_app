namespace Mathy.ELM.Core.DTOs;

/// <summary>
/// DTO for creating a ServiceDesk record from a Promotion/Transfer Request
/// Maps to ManageEngine Service Desk Plus API
/// </summary>
public class CreatePromotionServiceDeskRecordDto
{
    // PRIMARY IDENTIFIERS
    public int PromotionRequestDetailId { get; set; }
    public int ParentHRRequestId { get; set; }

    // PERSONAL INFORMATION
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "First Name is required")]
    public string FirstName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Last Name is required")]
    public string LastName { get; set; } = string.Empty;

    public string? PreferredFirstName { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Effective Date is required")]
    public DateTime EffectiveDate { get; set; }

    // For ServiceDesk API (needs milliseconds format)
    public long? EffectiveDateMilliseconds { get; set; }

    // CURRENT EMPLOYEE INFORMATION
    public int? CurrentCompanyCode { get; set; }
    public string? CurrentCompanyName { get; set; }
    public int? CurrentPayrollDeptCode { get; set; }
    public string? CurrentPayrollDeptName { get; set; }
    public int? CurrentPayrollGroupCode { get; set; }
    public string? CurrentPayrollGroupName { get; set; }
    public string? CurrentPositionCode { get; set; }
    public string? CurrentPositionName { get; set; }
    public int? CurrentLocationCode { get; set; }
    public string? CurrentLocationName { get; set; }
    public string? CurrentEmailAddress { get; set; }
    public string? CurrentNetworkUserName { get; set; }

    // NEW/PROMOTED POSITION INFORMATION
    public int? NewCompanyCode { get; set; }
    public string? NewCompanyName { get; set; }
    public int? NewPayrollDeptCode { get; set; }
    public string? NewPayrollDeptName { get; set; }
    public int? NewPayrollGroupCode { get; set; }
    public string? NewPayrollGroupName { get; set; }
    public string? NewPositionCode { get; set; }
    public string? NewPositionName { get; set; }
    public int? NewLocationCode { get; set; }
    public string? NewLocationName { get; set; }
    public string? NewEmailAddress { get; set; }

    // SUPERVISOR INFORMATION
    public int? NewSupervisorId { get; set; }
    public string? NewSupervisorName { get; set; }

    // REQUESTOR INFORMATION (who submitted the request)
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Requestor name is required")]
    public string RequestorName { get; set; } = string.Empty;

    public string? RequestorUserName { get; set; }
    public int? RequestorId { get; set; }
    public string? RequestorFirstName { get; set; }
    public string? RequestorLastName { get; set; }

    // IT REQUIREMENTS
    public bool RequiresITSupport { get; set; }
    public string? ITSupportNotes { get; set; }

    // PHONE REQUIREMENTS (from PTITPhoneRequirement)
    public string? DeskPhoneRequired { get; set; }
    public string? ReuseExistingPhone { get; set; }
    public string? DeskPhoneNumber { get; set; }
    public string? CompanyCellPhoneRequired { get; set; }
    public string? CompanyCellPhoneNumber { get; set; }
    public string? CompanyCellPlan { get; set; }
    public string? BYODCellPhone { get; set; }
    public string? BYODCellPhoneNumber { get; set; }

    // BUILDING ACCESS
    public bool? UseExistingKeyFob { get; set; }

    // TYPE-SPECIFIC DETAILS
    public PTCreditCardDetailDto? CreditCardDetails { get; set; }
    public PTVehicleDetailDto? VehicleDetails { get; set; }
    public PTITDetailDto? ITDetails { get; set; }

    // RELATED DATA COLLECTIONS
    public List<PTBuildingAccessDto>? BuildingAccess { get; set; }
    public List<PTComputerRequirementDto>? ComputerRequirements { get; set; }
    public List<PTTabletProfileDto>? TabletProfiles { get; set; }
    public List<PTApplicationRequestDto>? Applications { get; set; }
    public List<PTFolderRequestDto>? SharepointAndFolderAccess { get; set; }

    // SERVICEDESK SYNC TRACKING FLAGS
    public ServiceDeskRequirementsDto Requirements { get; set; } = new();
}

// Supporting DTOs for Promotion/Transfer child tables
public class PTCreditCardDetailDto
{
    public bool RequiresCreditCard { get; set; }
    public string? CreditCardNotes { get; set; }
}

public class PTVehicleDetailDto
{
    public bool RequiresVehicle { get; set; }
    public string? VehicleNotes { get; set; }
}

public class PTITDetailDto
{
    public string? MicrosoftLicenses { get; set; }
    public bool? MSOfficeLicenseE5 { get; set; }
    public bool? MSOfficeLicenseF3 { get; set; }
}

public class PTBuildingAccessDto
{
    public int BuildingAccessRequirementId { get; set; }
    public string AccessDescription { get; set; } = string.Empty;
}

public class PTComputerRequirementDto
{
    public int ComputerRequirementsId { get; set; }
    public string ComputerRequirementsDescription { get; set; } = string.Empty;
}

public class PTTabletProfileDto
{
    public int TabletProfileId { get; set; }
    public string TabletProfileName { get; set; } = string.Empty;
    public string? RolesRequired { get; set; }
}

public class PTApplicationRequestDto
{
    public int ApplicationId { get; set; }
    public string? ApplicationName { get; set; }
    public string? AccessNotes { get; set; }
}

public class PTFolderRequestDto
{
    public string FolderType { get; set; } = string.Empty; // "1" = Outlook, "2" = Shared Mailbox, etc.
    public string FolderName { get; set; } = string.Empty;
}
