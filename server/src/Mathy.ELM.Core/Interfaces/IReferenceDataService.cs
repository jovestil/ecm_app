using Mathy.ELM.Core.DTOs;

namespace Mathy.ELM.Core.Interfaces;

public interface IReferenceDataService
{
    Task<ApiResponse<List<RequestTypeDto>>> GetRequestTypesAsync(int? requestTypeId = null, string? requestTypeName = null);
    Task<ApiResponse<List<RequestStatusDto>>> GetRequestStatusesAsync(int? requestStatusId = null, string? requestStatusName = null);
    Task<ApiResponse<List<TerminationReasonDto>>> GetTerminationReasonsAsync(int? reasonId = null, string? reasonCode = null, int? companyCode = null);
    Task<ApiResponse<List<PayrollDepartmentDto>>> GetPayrollDepartmentsAsync(int? companyCode = null, int? deptCode = null);
    Task<ApiResponse<List<PayrollDepartmentShortNameDto>>> GetPayrollDepartmentShortNamesAsync(int? companyCode = null, int? deptCode = null, string? deptShortName = null);
    Task<ApiResponse<List<PayrollGroupDto>>> GetPayrollGroupsAsync(int? companyCode = null, int? groupCode = null);
    Task<ApiResponse<List<CompanyDto>>> GetCompaniesAsync(int? companyId = null, int? companyCode = null, string? companyName = null);
    Task<ApiResponse<List<PhysicalLocationDto>>> GetPhysicalLocationsAsync(int? locationId = null, int? locationCode = null, string? locationName = null);
    Task<ApiResponse<List<CompanyTypeLocationDto>>> GetCompanyTypeLocationsAsync(int? id = null, int? companyCode = null, string? locationType = null);
    Task<ApiResponse<List<EmploymentStatusDto>>> GetEmploymentStatusesAsync(int? id = null, int? companyCode = null, string? status = null);
    Task<ApiResponse<List<EmployeeSalaryTypeDto>>> GetEmployeeSalaryTypesAsync(int? companyCode = null);
    Task<ApiResponse<List<UnionCraftDto>>> GetUnionCraftsAsync(int? companyCode = null);
    Task<ApiResponse<List<ApprenticePercentageDto>>> GetApprenticePercentagesAsync(int? id = null, string? appPercentage = null);
    Task<ApiResponse<List<PositionDto>>> GetPositionsAsync(int? companyCode = null);
    Task<ApiResponse<List<SupervisorDto>>> GetSupervisorsAsync(int? companyCode = null, int? payrollDeptCode = null);
    Task<ApiResponse<List<BuildingAccessRequirementDto>>> GetBuildingAccessRequirementsAsync(int? companyCode = null, string? description = null, string? locationType = null);
    Task<ApiResponse<List<TabletProfileDto>>> GetTabletProfilesAsync(int? companyCode = null, string? locationType = null, string? profileName = null);
    Task<ApiResponse<List<ApplicationDto>>> GetApplicationsAsync(int? companyCode = null, string? name = null, string? locationType = null);
    Task<ApiResponse<List<EmployeeLicenseClassDto>>> GetEmployeeLicenseClassesAsync(int? id = null, string? licenseClass = null, bool? isUnion = null);
    Task<ApiResponse<List<ComputerRequirementDto>>> GetComputerRequirementsAsync(int? id = null, bool? isChild = null, int? parentId = null, string? description = null);

    /// <summary>
    /// Generates a unique username for AD creation based on preferred first name or first name
    /// Format: [name]001, [name]002, etc.
    /// </summary>
    /// <param name="firstName">The employee's first name</param>
    /// <param name="preferredFirstName">The employee's preferred first name (optional)</param>
    /// <returns>Generated unique username</returns>
    Task<ApiResponse<string>> GenerateUsernameAsync(string firstName, string? preferredFirstName = null);

    /// <summary>
    /// Generates an email address based on employee name and payroll department's email domain.
    /// If emailRequired is true: firstname.lastname@domain (or preferred.lastname@domain)
    /// If emailRequired is false: userId@domain
    /// </summary>
    /// <param name="firstName">Employee's first name</param>
    /// <param name="lastName">Employee's last name</param>
    /// <param name="companyCode">The company code</param>
    /// <param name="payrollDeptCode">The payroll department code</param>
    /// <param name="emailRequired">Whether the new hire requires an email address</param>
    /// <param name="preferredFirstName">Optional preferred first name</param>
    /// <param name="userId">The generated User ID (used when emailRequired is false)</param>
    /// <returns>Generated email address</returns>
    Task<ApiResponse<string>> GenerateEmailAddressAsync(string firstName, string lastName, int companyCode, int payrollDeptCode, bool emailRequired, string? preferredFirstName = null, string? userId = null);

    /// <summary>
    /// Syncs companies from Viewpoint API to local database
    /// </summary>
    /// <returns>Sync result with statistics</returns>
    Task<ApiResponse<CompanySyncResultDto>> SyncCompaniesFromViewpointAsync();
    
    /// <summary>
    /// Syncs departments from Viewpoint API to local database
    /// </summary>
    /// <returns>Sync result with statistics</returns>
    Task<ApiResponse<DepartmentSyncResultDto>> SyncDepartmentsFromViewpointAsync();
    
    /// <summary>
    /// Syncs positions from Viewpoint API to local database
    /// </summary>
    /// <returns>Sync result with statistics</returns>
    Task<ApiResponse<PositionSyncResultDto>> SyncPositionsFromViewpointAsync();
    
    /// <summary>
    /// Syncs payroll groups from Viewpoint API to local database
    /// </summary>
    /// <returns>Sync result with statistics</returns>
    Task<ApiResponse<PayrollGroupSyncResultDto>> SyncPayrollGroupsFromViewpointAsync();

    /// <summary>
    /// Syncs union crafts from Viewpoint API to local database
    /// </summary>
    /// <returns>Sync result with statistics</returns>
    Task<ApiResponse<UnionCraftSyncResultDto>> SyncUnionCraftsFromViewpointAsync();

    /// <summary>
    /// Syncs employment statuses from Viewpoint API to local database
    /// </summary>
    /// <returns>Sync result with statistics</returns>
    Task<ApiResponse<EmploymentStatusSyncResultDto>> SyncEmploymentStatusesFromViewpointAsync();

    /// <summary>
    /// Syncs employee salary types from Viewpoint API to local database
    /// </summary>
    /// <returns>Sync result with statistics</returns>
    Task<ApiResponse<EmployeeSalaryTypeSyncResultDto>> SyncEmployeeSalaryTypesFromViewpointAsync();

    /// <summary>
    /// Gets Viewpoint sync status with last sync dates for all data types
    /// </summary>
    /// <returns>Sync status information</returns>
    Task<ApiResponse<ViewpointSyncStatusDto>> GetViewpointSyncStatusAsync();
    
    /// <summary>
    /// Gets the current sync schedule configuration
    /// </summary>
    /// <returns>Current schedule settings</returns>
    Task<ApiResponse<SyncScheduleConfigDto>> GetSyncScheduleConfigAsync();
    
    /// <summary>
    /// Updates the sync schedule configuration and sets up Hangfire recurring jobs
    /// </summary>
    /// <param name="config">Schedule configuration</param>
    /// <returns>Result with scheduled job information</returns>
    Task<ApiResponse<SyncScheduleResultDto>> UpdateSyncScheduleConfigAsync(SyncScheduleConfigDto config);
}