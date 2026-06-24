namespace Mathy.ELM.Core.DTOs;

public class CreatePromotionRequestDto
{
    public string? Notes { get; set; }
    public int EmployeeId { get; set; }

    // Current position (optional - can be filled from Viewpoint)
    public int? CurrentPayrollCompanyCode { get; set; }
    public int? CurrentPayrollGroupCode { get; set; }
    public int? CurrentPayrollDeptCode { get; set; }
    public string? CurrentPositionCode { get; set; }
    public int? CurrentSupervisorId { get; set; }
    public int? CurrentPhysicalLocationCode { get; set; }
    public string? CurrentStatus { get; set; }
    public int? CurrentSalaryCode { get; set; }
    public string? CurrentWorkEmail { get; set; }

    // New position (required for submit)
    public int NewPayrollCompanyCode { get; set; }
    public int NewPayrollGroupCode { get; set; }
    public int NewPayrollDeptCode { get; set; }
    public string NewPositionCode { get; set; } = string.Empty;
    public int? NewSupervisorId { get; set; }
    public int NewPhysicalLocationCode { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public int? NewSalaryCode { get; set; }
    public string? NewWorkEmail { get; set; }

    // Promotion details
    public DateTime EffectiveDate { get; set; }
    public DateTime? PreviousEffectiveDate { get; set; }

    // Access features (PT tables)
    public PromotionCreditCardInfoDto? CreditCardInfo { get; set; }
    public PromotionVehicleInfoDto? VehicleInfo { get; set; }
    public PromotionITInfoDto? ITInfo { get; set; }
    public PromotionPhoneRequirementDto? PhoneInfo { get; set; }
    public List<PromotionApplicationRequestDto> Applications { get; set; } = new();
    public List<PromotionFolderRequestDto> Folders { get; set; } = new();
    public List<PromotionTabletProfileDto> TabletProfiles { get; set; } = new();
    public List<PromotionComputerRequirementDto> ComputerRequirements { get; set; } = new();
    public List<PromotionBuildingAccessDto> BuildingAccess { get; set; } = new();
    public bool? UseExistingKeyFob { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PromotionCreditCardInfoDto
{
    public bool? KwikTripCard { get; set; }
    public bool? CompanyExpenseCard { get; set; }
    public string? CreditExpenseType { get; set; }
    public decimal? WeeklyLimit { get; set; }
    public bool? FuelCardlockAccess { get; set; }
    public string? FuelCardlockAddress { get; set; }
}

public class PromotionVehicleInfoDto
{
    public bool? IsApprovedToOperate { get; set; }
    public string? LicenseClass { get; set; }
    public string? DrugAndAlcoholProfile { get; set; }
    public bool? NeedCompanyCar { get; set; }
    public bool? IsApplicationPart2Complete { get; set; }
}

public class PromotionITInfoDto
{
    public bool? EmailRequired { get; set; }
    public string? AlternateDeliveryLocation { get; set; }
    public bool? MSOfficeLicenseE5 { get; set; }
    public bool? MSOfficeLicenseF3 { get; set; }
}

public class PromotionPhoneRequirementDto
{
    public bool? DeskPhone { get; set; }
    public bool? CompanyCellphone { get; set; }
    public bool? BYODCellphone { get; set; }
    public string? WorkPhoneNumber { get; set; }
    public string? WorkExtension { get; set; }
    public string? WorkCell { get; set; }
    public bool? ReusingExistingPhone { get; set; }
}

public class PromotionApplicationRequestDto
{
    public int ApplicationId { get; set; }
    public string? ApplicationName { get; set; }
    public string? AccessNotes { get; set; }
}

public class PromotionFolderRequestDto
{
    public string FolderType { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
}

public class PromotionTabletProfileDto
{
    public int TabletProfileId { get; set; }
    public string? TabletProfileName { get; set; }
    public string RolesRequiredForNewHire { get; set; } = string.Empty;
}

public class PromotionComputerRequirementDto
{
    public int ComputerRequirementsId { get; set; }
    public string? ComputerRequirementsDescription { get; set; }
    public bool? IsChild { get; set; }
    public int? ParentId { get; set; }
}

public class PromotionBuildingAccessDto
{
    public int AccessId { get; set; }
    public string AccessDescription { get; set; } = string.Empty;
}
