namespace Mathy.ELM.Core.Entities;

public class ServiceDeskSyncData : BaseEntity
{
    public int NewHireRequestId { get; set; }
    public string ServiceDeskID { get; set; } = string.Empty; // 179024000000991083
    public bool? HasBuildingAccess { get; set; } = false;
    public bool? HasPhoneRequirements { get; set; } = false;
    public bool? HasComputerRequirements { get; set; } = false;
    public bool? HasTabletProfiles { get; set; } = false;
    public bool? HasITApplications { get; set; } = false;
    public bool? HasSoftwareAccessReq { get; set; } = false;

    // Navigation
    public virtual NewHireRequestDetail? NewHireRequestDetail { get; set; }
}
