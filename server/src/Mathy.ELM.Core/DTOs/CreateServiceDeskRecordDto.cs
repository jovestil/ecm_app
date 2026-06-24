namespace Mathy.ELM.Core.DTOs;

/// <summary>
/// DTO for creating a ServiceDesk record from a New Hire Request
/// Maps to ManageEngine Service Desk Plus API
/// </summary>
public class CreateServiceDeskRecordDto
{
    // PRIMARY IDENTIFIERS
    public int NewHireRequestDetailId { get; set; }
    public int ParentHRRequestId { get; set; }

    // PERSONAL INFORMATION
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "First Name is required")]
    public string FirstName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Last Name is required")]
    public string LastName { get; set; } = string.Empty;

    public string? PreferredFirstName { get; set; }
    public bool Rehire { get; set; }
    public string? EmailAddress { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Network Username is required for ServiceDesk record")]
    public string NetworkUserName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "First Day of Employment is required")]
    public DateTime FirstDayOfEmployment { get; set; }

    // For ServiceDesk API (needs milliseconds format)
    public long? FirstDayOfEmploymentMilliseconds { get; set; }

    // ORGANIZATION INFORMATION
    public int? CompanyCode { get; set; }
    public int? PayrollDeptCode { get; set; }
    public int? LocationCode { get; set; }
    public string? PositionCode { get; set; }
    public int? SupervisorId { get; set; }
    public string? FunctionalDept { get; set; }
    public string? EmploymentStatus { get; set; }

    // REQUESTOR INFORMATION (who submitted the request)
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Requestor name is required")]
    public string RequestorName { get; set; } = string.Empty;

    public string? RequestorUserName { get; set; }
    public int? RequestorId { get; set; }
    public string? RequestorFirstName { get; set; }
    public string? RequestorLastName { get; set; }

    // NETWORK & EMAIL REQUIREMENTS
    public string? RequireNetworkUser { get; set; } // "True"/"False"
    public string? RequireEmailAddress { get; set; } // "True"/"False"

    // PHONE REQUIREMENTS (from ITPhoneRequirement)
    public string? DeskPhoneRequired { get; set; } // "True"/"False"
    public string? ReuseExistingPhone { get; set; } // "True"/"False"
    public string? DeskPhoneNumber { get; set; }
    public string? CompanyCellPhoneRequired { get; set; } // "True"/"False"
    public string? CompanyCellPhoneNumber { get; set; }
    public string? CompanyCellPlan { get; set; }
    public string? BYODCellPhone { get; set; } // "True"/"False"
    public string? BYODCellPhoneNumber { get; set; }

    // IT REQUIREMENTS (from ITDetail)
    public string? MicrosoftLicenses { get; set; }
    public bool? MSOfficeLicenseE5 { get; set; }
    public bool? MSOfficeLicenseF3 { get; set; }
    public string? AlternateEmailDeliveryLocation { get; set; }

    // ADDITIONAL NOTES
    public string? AdditionalNotes { get; set; }

    // TYPE-SPECIFIC DETAILS
    public CreditCardDetailDto? CreditCardDetails { get; set; }
    public VehicleDetailDto? VehicleDetails { get; set; }
    public ITDetailDto? ITDetails { get; set; }

    // RELATED DATA COLLECTIONS
    public List<NewHireBuildingAccessDto>? BuildingAccess { get; set; }
    public List<NewHireComputerRequirementDto>? ComputerRequirements { get; set; }
    public List<NewHireTabletProfileDto>? TabletProfiles { get; set; }
    public List<NewHireApplicationRequestDto>? Applications { get; set; }
    public List<NewHireFolderRequestDto>? SharepointAndFolderAccess { get; set; }

    // SERVICEDESK SYNC TRACKING FLAGS
    public ServiceDeskRequirementsDto Requirements { get; set; } = new();
}

/// <summary>
/// Flags indicating which ServiceDesk requirements exist
/// </summary>
public class ServiceDeskRequirementsDto
{
    public bool HasPhoneRequirements { get; set; }
    public bool HasComputerRequirements { get; set; }
    public bool HasTabletProfiles { get; set; }
    public bool HasBuildingAccess { get; set; }
    public bool HasITApplications { get; set; }
    public bool HasSoftwareAccessReq { get; set; }
}

/// <summary>
/// Response from ServiceDesk creation
/// </summary>
public class ServiceDeskRecordResponseDto
{
    public bool Success { get; set; }
    public string? ServiceDeskTicketId { get; set; }
    public string? Message { get; set; }
}
