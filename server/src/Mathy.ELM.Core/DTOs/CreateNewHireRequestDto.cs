namespace Mathy.ELM.Core.DTOs;

public class CreateNewHireRequestDto
{
    public string? Notes { get; set; }
    public NewHirePersonalInfoDto PersonalInfo { get; set; } = new();
    public NewHirePositionInfoDto PositionInfo { get; set; } = new();
    public NewHireCreditCardInfoDto? CreditCardInfo { get; set; }
    public NewHireVehicleInfoDto? VehicleInfo { get; set; }
    public NewHireITInfoDto? ITInfo { get; set; }
    public NewHirePhoneInfoDto? PhoneInfo { get; set; }
    public List<NewHireApplicationRequestDto> Applications { get; set; } = new();
    public List<NewHireFolderRequestDto> Folders { get; set; } = new();
    public List<NewHireTabletProfileDto> TabletProfiles { get; set; } = new();
    public List<NewHireComputerRequirementDto> ComputerRequirements { get; set; } = new();
    public List<NewHireBuildingAccessDto> BuildingAccess { get; set; } = new();
    public bool? UseExistingKeyFob { get; set; }
    public string? ErrorMessage { get; set; }
}

public class NewHirePersonalInfoDto
{
    public int? EmployeeId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleInitial { get; set; }
    public string? Suffix { get; set; }
    public string? PreferredFirstName { get; set; }
    public string? UserId { get; set; }
    public DateTime? FirstDayEmployment { get; set; }
    public DateTime? PreviousFirstDayEmployment { get; set; }
    public string? ReferredBy { get; set; }
    public bool? Rehire { get; set; }
}

public class NewHirePositionInfoDto
{
    public int? CompanyCode { get; set; }
    public int? LocationCode { get; set; }
    public string? EmploymentStatus { get; set; }
    public bool? IsUnion { get; set; }
    public int? UnionCraftId { get; set; }
    public bool? IsApprentice { get; set; }
    public bool? IsUnionWage { get; set; }
    public int? SalaryCode { get; set; }
    public string? PositionCode { get; set; }
    public int? PayrollDeptCode { get; set; }
    public int? SupervisorId { get; set; }
    public string? AppPercentage { get; set; }
}

public class NewHireCreditCardInfoDto
{
    public bool? KwikTripCard { get; set; }
    public bool? CompanyExpenseCard { get; set; }
    public string? CreditExpenseType { get; set; }
    public decimal? WeeklyLimit { get; set; }
    public bool? FuelCardlockAccess { get; set; }
    public string? FuelCardlockAddress { get; set; }
}

public class NewHireVehicleInfoDto
{
    public bool? IsApprovedToOperate { get; set; }
    public string? DriverClassification { get; set; }
    public string? DrugAndAlcoholProfile { get; set; }
    public bool? NeedCompanyCar { get; set; }
    public bool? IsApplicationPart2Complete { get; set; }
}

public class NewHireITInfoDto
{
    public bool? EmailRequired { get; set; }
    public string? AlternateDeliveryLocation { get; set; }
    public bool? MSOfficeLicenseE5 { get; set; }
    public bool? MSOfficeLicenseF3 { get; set; }
    public string? EmailAddress { get; set; } // Maps to WorkEmail in NewHireRequestDetail
}

public class NewHirePhoneInfoDto
{
    public bool? DeskPhone { get; set; }
    public bool? CompanyCellphone { get; set; }
    public bool? BYODCellphone { get; set; }
    public bool? ReusingExistingPhone { get; set; }
    public string? WorkPhoneNumber { get; set; }
    public string? WorkExtension { get; set; }
}

public class NewHireApplicationRequestDto
{
    public int ApplicationId { get; set; }
    public string? ApplicationName { get; set; }
    public string? AccessNotes { get; set; }
}

public class NewHireFolderRequestDto
{
    public string FolderType { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
}

public class NewHireTabletProfileDto
{
    public int TabletProfileId { get; set; }
    public string? TabletProfileName { get; set; }
    public string RolesRequiredForNewHire { get; set; } = string.Empty;
}

public class NewHireComputerRequirementDto
{
    public int ComputerRequirementsId { get; set; }
    public string? ComputerRequirementsDescription { get; set; }
    public bool? IsChild { get; set; }
    public int? ParentId { get; set; }
}

public class NewHireBuildingAccessDto
{
    public int AccessId { get; set; }
    public string AccessDescription { get; set; } = string.Empty;
}