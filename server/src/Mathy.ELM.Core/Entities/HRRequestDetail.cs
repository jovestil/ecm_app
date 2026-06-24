namespace Mathy.ELM.Core.Entities;

public class HRRequestDetail : BaseEntity
{
    public int ParentRequestId { get; set; }
    
    // Request Type and Status (foreign keys to lookup tables)
    public int RequestTypeId { get; set; }
    public int RequestStatusId { get; set; } = 1; // Default to 'Pending'
    
    // Employee Information (from Viewpoint)
    public int EmployeeId { get; set; } // Viewpoint Employee ID
    public string? EmployeeNetworkId { get; set; } // AD Network ID
    public string? EmployeePositionCode { get; set; }
    public int? EmployeeCompanyCode { get; set; } // Company Code
    public int? EmployeeDepartmentCode { get; set; } // Department Code
    
    // Request Specific Details
    public DateTime? EffectiveDate { get; set; }
    public string? ProcessingNotes { get; set; }
    
    // Viewpoint Integration
    public bool ViewpointProcessed { get; set; } = false;
    public DateTime? ViewpointProcessedDate { get; set; }
    public string? ViewpointErrorMessage { get; set; }
    
    // Background Job Integration
    public string? HangfireJobId { get; set; }
    
    // Navigation Properties
    public virtual HRRequest ParentRequest { get; set; } = null!;
    public virtual RequestType RequestType { get; set; } = null!;
    public virtual RequestStatus RequestStatus { get; set; } = null!;
    
    // Type-specific details (one-to-one relationships)
    public virtual PromotionRequestDetail? PromotionDetails { get; set; }
    public virtual LayoffRequestDetail? LayoffDetails { get; set; }
    public virtual TerminationRequestDetail? TerminationDetails { get; set; }
    public virtual ReturnToWorkRequestDetail? ReturnToWorkDetails { get; set; }
    public virtual NewHireRequestDetail? NewHireDetails { get; set; }
}