namespace Mathy.ELM.Core.Entities;

public class PromotionRequestDetail : BaseEntity
{
    public int RequestDetailId { get; set; }

    // Current Position
    public int? CurrentPayrollCompanyCode { get; set; }
    public int? CurrentPayrollGroupCode { get; set; }
    public int? CurrentPayrollDeptCode { get; set; }
    public string? CurrentPositionCode { get; set; }
    public int? CurrentSupervisorId { get; set; }
    public int? CurrentPhysicalLocationCode { get; set; }
    public string? CurrentStatus { get; set; }
    public int? CurrentSalaryCode { get; set; }

    // New Position
    public int NewPayrollCompanyCode { get; set; }
    public int NewPayrollGroupCode { get; set; }
    public int NewPayrollDeptCode { get; set; }
    public string NewPositionCode { get; set; } = string.Empty;
    public int? NewSupervisorId { get; set; }
    public int NewPhysicalLocationCode { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public int? NewSalaryCode { get; set; }

    // Work Email
    public string? CurrentWorkEmail { get; set; }
    public string? NewWorkEmail { get; set; }

    // Building Access
    public bool? UseExistingKeyFob { get; set; }

    // Navigation Properties
    public virtual HRRequestDetail HRRequestDetail { get; set; } = null!;

    // PT* Child Records (1:1 Relationships) - Foreign keys on child tables
    public virtual PTCreditCardDetail? PTCreditCardDetail { get; set; }
    public virtual PTVehicleDetail? PTVehicleDetail { get; set; }
    public virtual PTITDetail? PTITDetail { get; set; }
    public virtual PTITPhoneRequirement? PTITPhoneRequirement { get; set; }
    public virtual PTServiceDeskSyncData? PTServiceDeskSyncData { get; set; }

    // PT* Child Collections (1:Many Relationships)
    public virtual ICollection<PTApplicationRequest> PTApplicationRequests { get; set; } = new List<PTApplicationRequest>();
    public virtual ICollection<PTFolderRequest> PTFolderRequests { get; set; } = new List<PTFolderRequest>();
    public virtual ICollection<PTITTabletProfile> PTITTabletProfiles { get; set; } = new List<PTITTabletProfile>();
    public virtual ICollection<PTITComputerRequirement> PTITComputerRequirements { get; set; } = new List<PTITComputerRequirement>();
    public virtual ICollection<PTBuildingAccessRequirement> PTBuildingAccessRequirements { get; set; } = new List<PTBuildingAccessRequirement>();
}
