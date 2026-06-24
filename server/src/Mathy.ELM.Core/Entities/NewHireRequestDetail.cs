namespace Mathy.ELM.Core.Entities;

public class NewHireRequestDetail : BaseEntity
{
    public int RequestDetailId { get; set; }
    
    // Personal Information
    public int? EmployeeId { get; set; }
    public string? FirstName { get; set; } // Changed to nullable to match Arnie's schema
    public string? LastName { get; set; } // Changed to nullable to match Arnie's schema
    public string? MiddleInitial { get; set; }
    public string? Suffix { get; set; }
    public string? PreferredFirstName { get; set; }
    public DateTime? FirstDayEmployment { get; set; } // Changed to nullable to match Arnie's schema
    public string? ReferredBy { get; set; }
    public bool? Rehire { get; set; } // Changed to nullable to match Arnie's schema

    // Position Information
    public int? CompanyCode { get; set; } // Changed to nullable to match Arnie's schema
    public int? LocationCode { get; set; } // Changed to nullable to match Arnie's schema
    public string? EmploymentStatus { get; set; } // Changed to nullable to match Arnie's schema
    public bool? IsUnion { get; set; }
    public int? UnionCraftId { get; set; }
    public bool? IsApprentice { get; set; }
    public bool? IsUnionWage { get; set; }
    public string? AppPercentage { get; set; } // Changed to nullable for consistency
    public int? SalaryCode { get; set; }
    public string? PositionCode { get; set; } // Changed to nullable to match Arnie's schema
    public int? PayrollDeptCode { get; set; } // Changed to nullable to match Arnie's schema
    public int? SupervisorId { get; set; } // Changed to nullable to match Arnie's schema
    public string? NetworkId { get; set; } // AD network username
    public string? WorkEmail { get; set; } // AD work email address
    public string? AdPassword { get; set; } // Temporary AD password for new hire
    public string? Notes { get; set; }
    public bool? UseExistingKeyFob { get; set; }

    // Navigation Properties
    public virtual HRRequestDetail HRRequestDetail { get; set; } = null!;
    
    // New hire specific details (one-to-one and one-to-many relationships)
    public virtual CreditCardDetail? CreditCardDetail { get; set; }
    public virtual VehicleDetail? VehicleDetail { get; set; }
    public virtual ITDetail? ITDetail { get; set; }
    public virtual ITPhoneRequirement? ITPhoneRequirement { get; set; }
    
    // One-to-many relationships
    public virtual ICollection<ApplicationRequest> ApplicationRequests { get; set; } = new List<ApplicationRequest>();
    public virtual ICollection<FolderRequest> FolderRequests { get; set; } = new List<FolderRequest>();
    public virtual ICollection<ITTabletProfile> ITTabletProfiles { get; set; } = new List<ITTabletProfile>();
    public virtual ICollection<ITComputerRequirement> ITComputerRequirements { get; set; } = new List<ITComputerRequirement>();
    public virtual ICollection<NewHireBuildingAccessRequirement> BuildingAccessRequirements { get; set; } = new List<NewHireBuildingAccessRequirement>();
    public virtual ServiceDeskSyncData? ServiceDeskSyncData { get; set; }
}