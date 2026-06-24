namespace Mathy.ELM.Core.DTOs;

public class HRRequestDto
{
    public int Id { get; set; }
    public int SubmittedBy { get; set; }
    public string? SubmittedByName { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
    
    public List<HRRequestDetailDto> Details { get; set; } = new List<HRRequestDetailDto>();
}

public class HRRequestDetailDto
{
    public int Id { get; set; }
    public int ParentRequestId { get; set; }
    public int RequestTypeId { get; set; }
    public string? RequestTypeName { get; set; }
    public int RequestStatusId { get; set; }
    public string? RequestStatusName { get; set; }
    public string? RequestDisplayStatusName { get; set; }
    
    // Employee Information
    public int EmployeeId { get; set; }
    public string? EmployeeNetworkId { get; set; }
    public string? EmployeePositionCode { get; set; }
    public int? EmployeeCompanyCode { get; set; }
    public int? EmployeeDepartmentCode { get; set; }
    
    // Employee Name and Details (from lookup)
    public string? EmployeeName { get; set; }
    public string? CompanyName { get; set; }
    public string? DepartmentName { get; set; }
    
    // Request Specific Details
    public DateTime? EffectiveDate { get; set; }
    public string? ProcessingNotes { get; set; }
    
    // Submitter Information (from parent request)
    public int SubmittedBy { get; set; }
    public string? SubmittedByName { get; set; }
    public DateTime? SubmittedDate { get; set; }
    
    // Viewpoint Integration
    public bool ViewpointProcessed { get; set; }
    public DateTime? ViewpointProcessedDate { get; set; }
    public string? ViewpointErrorMessage { get; set; }
    
    // Background Job Integration
    public string? HangfireJobId { get; set; }

    // Phone info flag for dashboard
    public bool HasDeskPhone { get; set; }

    // Type-specific details
    public PromotionRequestDetailDto? PromotionDetails { get; set; }
    public LayoffRequestDetailDto? LayoffDetails { get; set; }
    public TerminationRequestDetailDto? TerminationDetails { get; set; }
    public ReturnToWorkRequestDetailDto? ReturnToWorkDetails { get; set; }
    public CreditCardDetailDto? CreditCardDetails { get; set; }
    public VehicleDetailDto? VehicleDetails { get; set; }
    public ITDetailDto? ITDetails { get; set; }
}

public class CreateHRRequestDto
{
    public string? Notes { get; set; }
    public List<CreateHRRequestDetailDto> Details { get; set; } = new List<CreateHRRequestDetailDto>();
}

public class CreateMultiEmployeeHRRequestDto
{
    public int RequestTypeId { get; set; }
    public List<int> EmployeeIds { get; set; } = new List<int>();
    public DateTime? EffectiveDate { get; set; }
    public string ProcessingNotes { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string RequestTitle { get; set; } = string.Empty;
    public string RequestDescription { get; set; } = string.Empty;
    public int RequestedBy { get; set; }
    public int? CompanyId { get; set; }
    public int? PayrollGroupId { get; set; }
}

public class CreateSingleEmployeeHRRequestDto
{
    public int RequestTypeId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string ProcessingNotes { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string RequestTitle { get; set; } = string.Empty;
    public string RequestDescription { get; set; } = string.Empty;
    public int RequestedBy { get; set; }
    public int? CompanyId { get; set; }
    public int? PayrollGroupId { get; set; }
    public CreateTerminationRequestDto? TerminationDetails { get; set; }
}

public class CreateTerminationRequestDto
{
    public string ReasonCode { get; set; } = string.Empty;
    public string? ForwardEmail { get; set; }
    public string? ForwardDeskPhone { get; set; }
    public string? ForwardCellPhone { get; set; }
    public string? AutoReply { get; set; }
    public string? GiveOneDriveAccessTo { get; set; }
    public bool WithKwikTripCard { get; set; }
    public string? KwikCard4DigitNo { get; set; }
}

public class CreateHRRequestDetailDto
{
    public int RequestTypeId { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeNetworkId { get; set; }
    public string? EmployeePositionCode { get; set; }
    public int? EmployeeCompanyCode { get; set; }
    public int? EmployeeDepartmentCode { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? ProcessingNotes { get; set; }
    
    // Type-specific details will be handled separately
}

public class UpdateHRRequestDto
{
    public int Id { get; set; }
    public string? Notes { get; set; }
}

public class UpdateHRRequestDetailDto
{
    public int Id { get; set; }
    public int RequestStatusId { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? ProcessingNotes { get; set; }
    public bool ViewpointProcessed { get; set; }
    public string? ViewpointErrorMessage { get; set; }
    public string? HangfireJobId { get; set; }
}

public class UpdateEffectiveDateDto
{
    public string EffectiveDate { get; set; } = string.Empty;
}

// Placeholder DTOs for type-specific details (these would need to be implemented based on actual entities)
public class PromotionRequestDetailDto
{
    public int Id { get; set; }

    // Current Position
    public int? CurrentPayrollCompanyCode { get; set; }
    public int? CurrentPayrollGroupCode { get; set; }
    public int? CurrentPayrollDeptCode { get; set; }
    public string? CurrentPositionCode { get; set; }
    public int? CurrentSupervisorId { get; set; }
    public int? CurrentPhysicalLocationCode { get; set; }
    public string? CurrentStatus { get; set; }
    public int? CurrentSalaryCode { get; set; }
    public string? CurrentWorkEmail { get; set; }

    // New Position
    public int NewPayrollCompanyCode { get; set; }
    public int NewPayrollGroupCode { get; set; }
    public int NewPayrollDeptCode { get; set; }
    public string NewPositionCode { get; set; } = string.Empty;
    public int? NewSupervisorId { get; set; }
    public int NewPhysicalLocationCode { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public int? NewSalaryCode { get; set; }
    public string? NewWorkEmail { get; set; }

    // Building Access
    public bool? UseExistingKeyFob { get; set; }
}

// PromotionRequestViewDto moved to PromotionRequestViewDto.cs for better organization

public class LayoffRequestDetailDto
{
    public int Id { get; set; }
    public int RequestDetailId { get; set; }
    public DateTime LastDayWorked { get; set; }
    
    // Audit Fields
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
}

public class TerminationRequestDetailDto
{
    public int Id { get; set; }
    public int RequestDetailId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;

    // Communication Forwarding
    public string? ForwardEmail { get; set; }
    public string? ForwardDeskPhone { get; set; }
    public string? ForwardCellPhone { get; set; }
    public string? AutoReply { get; set; }
    public string? GiveOneDriveAccessTo { get; set; }

    // Kwik Trip Card
    public bool WithKwikTripCard { get; set; }
    public string? KwikCard4DigitNo { get; set; }

    // Audit Fields
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
}

public class ReturnToWorkRequestDetailDto
{
    public int Id { get; set; }
    public int RequestDetailId { get; set; }
    
    // Audit Fields
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
}

public class CreditCardDetailDto
{
    // Add credit card-specific fields here
}

public class VehicleDetailDto
{
    // Add vehicle-specific fields here
}

public class ITDetailDto
{
    // Add IT-specific fields here
}

public class CompleteReturnToWorkRequestDto
{
    public List<ViewpointEmployeeDto> Employees { get; set; } = new();
    public CreateMultiEmployeeHRRequestDto HRRequest { get; set; } = null!;
}