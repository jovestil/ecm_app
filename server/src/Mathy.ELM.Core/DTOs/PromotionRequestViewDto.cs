namespace Mathy.ELM.Core.DTOs;

public class PromotionRequestViewDto
{
    // HR Request Information
    public int ParentRequestId { get; set; }
    public string RequestTitle { get; set; } = string.Empty;
    public string RequestDescription { get; set; } = string.Empty;
    public DateTime? EffectiveDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public int RequestStatusId { get; set; }
    public string RequestStatusName { get; set; } = string.Empty;
    public string SubmittedByName { get; set; } = string.Empty;

    // HR Request Detail Information
    public int RequestDetailId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeNetworkId { get; set; } = string.Empty;
    public string EmployeePositionCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;

    // Current Position Information with Display Names
    public int? CurrentPayrollCompanyCode { get; set; }
    public string? CurrentCompanyName { get; set; }
    public int? CurrentPayrollGroupCode { get; set; }
    public string? CurrentPayrollGroupName { get; set; }
    public int? CurrentPayrollDeptCode { get; set; }
    public string? CurrentPayrollDeptName { get; set; }
    public string? CurrentPositionCode { get; set; }
    public string? CurrentPositionName { get; set; }
    public int? CurrentSupervisorId { get; set; }
    public string? CurrentSupervisorName { get; set; }
    public int? CurrentPhysicalLocationCode { get; set; }
    public string? CurrentPhysicalLocationName { get; set; }
    public string? CurrentStatus { get; set; }
    public int? CurrentSalaryCode { get; set; }
    public string? CurrentSalaryDescription { get; set; }
    public string? CurrentWorkEmail { get; set; }

    // New Position Information with Display Names
    public int? NewPayrollCompanyCode { get; set; }
    public string? NewCompanyName { get; set; }
    public int? NewPayrollGroupCode { get; set; }
    public string? NewPayrollGroupName { get; set; }
    public int? NewPayrollDeptCode { get; set; }
    public string? NewPayrollDeptName { get; set; }
    public string? NewPositionCode { get; set; }
    public string? NewPositionName { get; set; }
    public int? NewSupervisorId { get; set; }
    public string? NewSupervisorName { get; set; }
    public int? NewPhysicalLocationCode { get; set; }
    public string? NewPhysicalLocationName { get; set; }
    public string? NewStatus { get; set; }
    public int? NewSalaryCode { get; set; }
    public string? NewSalaryDescription { get; set; }
    public string? NewWorkEmail { get; set; }

    // Credit Card Information
    public PTCreditCardDetailViewDto? CreditCardInfo { get; set; }

    // Vehicle Information
    public PTVehicleDetailViewDto? VehicleInfo { get; set; }

    // IT Information
    public PTITDetailViewDto? ITInfo { get; set; }

    // Phone Requirements
    public PTITPhoneRequirementViewDto? PhoneInfo { get; set; }

    // Application Requests
    public List<PTApplicationRequestViewDto> Applications { get; set; } = new();

    // Folder Requests
    public List<PTFolderRequestViewDto> Folders { get; set; } = new();

    // Tablet Profiles
    public List<PTITTabletProfileViewDto> TabletProfiles { get; set; } = new();

    // Computer Requirements
    public List<PTITComputerRequirementViewDto> ComputerRequirements { get; set; } = new();

    // Building Access
    public List<PTBuildingAccessViewDto> BuildingAccess { get; set; } = new();
    public bool? UseExistingKeyFob { get; set; }
}

public class PTCreditCardDetailViewDto
{
    public bool KwikTripCard { get; set; }
    public bool CompanyExpenseCard { get; set; }
    public string? CreditExpenseType { get; set; }
    public decimal? WeeklyLimit { get; set; }
    public bool FuelCardlockAccess { get; set; }
    public string? FuelCardlockAddress { get; set; }
}

public class PTVehicleDetailViewDto
{
    public bool IsApprovedToOperate { get; set; }
    public string? LicenseClass { get; set; }
    public string? DrugAndAlcoholProfile { get; set; }
    public bool NeedCompanyCar { get; set; }
    public bool IsApplicationPart2Complete { get; set; }
}

public class PTITDetailViewDto
{
    public bool EmailRequired { get; set; }
    public string? AlternateDeliveryLocation { get; set; }
    public bool MSOfficeLicenseE5 { get; set; }
    public bool MSOfficeLicenseF3 { get; set; }
}

public class PTITPhoneRequirementViewDto
{
    public bool DeskPhone { get; set; }
    public bool CompanyCellphone { get; set; }
    public bool BYODCellphone { get; set; }
    public string? WorkPhoneNumber { get; set; }
    public string? WorkExtension { get; set; }
    public string? WorkCell { get; set; }
    public bool ReusingExistingPhone { get; set; }
}

public class PTApplicationRequestViewDto
{
    public int ApplicationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string? AccessNotes { get; set; }
}

public class PTFolderRequestViewDto
{
    public string FolderType { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
}

public class PTITTabletProfileViewDto
{
    public int TabletProfileId { get; set; }
    public string TabletProfileName { get; set; } = string.Empty;
    public string? RolesRequiredForNewHire { get; set; }
}

public class PTITComputerRequirementViewDto
{
    public int ComputerRequirementsId { get; set; }
    public string ComputerRequirementsDescription { get; set; } = string.Empty;
    public bool? IsChild { get; set; }
    public int? ParentId { get; set; }
}

public class PTBuildingAccessViewDto
{
    public int AccessId { get; set; }
    public string AccessDescription { get; set; } = string.Empty;
}
