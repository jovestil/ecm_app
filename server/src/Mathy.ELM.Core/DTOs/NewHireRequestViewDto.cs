namespace Mathy.ELM.Core.DTOs;

public class NewHireRequestViewDto
{
    // HR Request Information
    public int ParentRequestId { get; set; }
    public string RequestTitle { get; set; } = string.Empty;
    public string RequestDescription { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public string RequestStatusName { get; set; } = string.Empty;
    public string SubmittedByName { get; set; } = string.Empty;

    // HR Request Detail Information
    public int RequestDetailId { get; set; }
    public int? EmployeeId { get; set; }
    public string EmployeeNetworkId { get; set; } = string.Empty;
    public string EmployeePositionCode { get; set; } = string.Empty;

    // Personal Information
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleInitial { get; set; }
    public string? Suffix { get; set; }
    public string? PreferredFirstName { get; set; }
    public string? UserId { get; set; }
    public DateTime FirstDayEmployment { get; set; }
    public string? ReferredBy { get; set; }
    public bool Rehire { get; set; }

    // Position Information with Display Names
    public int CompanyCode { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int LocationCode { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string EmploymentStatus { get; set; } = string.Empty;
    public bool? IsUnion { get; set; }
    public int? UnionCraftId { get; set; }
    public string? UnionCraftDescription { get; set; }
    public bool? IsApprentice { get; set; }
    public bool? IsUnionWage { get; set; }
    public int? SalaryCode { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public string? PositionName { get; set; }
    public int PayrollDeptCode { get; set; }
    public string? PayrollDeptName { get; set; }
    public int SupervisorId { get; set; }
    public string? SupervisorName { get; set; }
    public string AppPercentage { get; set; } = string.Empty;

    // Credit Card Information
    public CreditCardDetailViewDto? CreditCardInfo { get; set; }

    // Vehicle Information
    public VehicleDetailViewDto? VehicleInfo { get; set; }

    // IT Information
    public ITDetailViewDto? ITInfo { get; set; }

    // Phone Requirements
    public ITPhoneRequirementViewDto? PhoneInfo { get; set; }

    // Application Requests
    public List<ApplicationRequestViewDto> Applications { get; set; } = new();

    // Folder Requests
    public List<FolderRequestViewDto> Folders { get; set; } = new();

    // Tablet Profiles
    public List<ITTabletProfileViewDto> TabletProfiles { get; set; } = new();

    // Computer Requirements
    public List<ITComputerRequirementViewDto> ComputerRequirements { get; set; } = new();

    // Building Access
    public List<NewHireBuildingAccessViewDto> BuildingAccess { get; set; } = new();
    public bool? UseExistingKeyFob { get; set; }
}

public class CreditCardDetailViewDto
{
    public bool KwikTripCard { get; set; }
    public bool CompanyExpenseCard { get; set; }
    public string? CreditExpenseType { get; set; }
    public decimal? WeeklyLimit { get; set; }
    public bool FuelCardlockAccess { get; set; }
    public string? FuelCardlockAddress { get; set; }
}

public class VehicleDetailViewDto
{
    public bool IsApprovedToOperate { get; set; }
    public string? DriverClassification { get; set; }
    public string? DrugAndAlcoholProfile { get; set; }
    public bool NeedCompanyCar { get; set; }
    public bool IsApplicationPart2Complete { get; set; }
}

public class ITDetailViewDto
{
    public bool EmailRequired { get; set; }
    public string? AlternateDeliveryLocation { get; set; }
    public bool MSOfficeLicenseE5 { get; set; }
    public bool MSOfficeLicenseF3 { get; set; }
    public string? EmailAddress { get; set; } // Work email from AD creation
}

public class ITPhoneRequirementViewDto
{
    public bool DeskPhone { get; set; }
    public bool CompanyCellphone { get; set; }
    public bool BYODCellphone { get; set; }
    public bool ReusingExistingPhone { get; set; }
    public string? WorkPhoneNumber { get; set; }
    public string? WorkExtension { get; set; }
}

public class ApplicationRequestViewDto
{
    public int ApplicationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string? AccessNotes { get; set; }
}

public class FolderRequestViewDto
{
    public string FolderType { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
}

public class ITTabletProfileViewDto
{
    public int TabletProfileId { get; set; }
    public string TabletProfileName { get; set; } = string.Empty;
    public string? RolesRequiredForNewHire { get; set; }
}

public class ITComputerRequirementViewDto
{
    public int ComputerRequirementsId { get; set; }
    public string ComputerRequirementsDescription { get; set; } = string.Empty;
    public bool? IsChild { get; set; }
    public int? ParentId { get; set; }
}

public class NewHireBuildingAccessViewDto
{
    public int AccessId { get; set; }
    public string AccessDescription { get; set; } = string.Empty;
}