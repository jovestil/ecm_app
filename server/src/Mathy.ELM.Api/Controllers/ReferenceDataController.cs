using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Interfaces;

namespace Mathy.ELM.Api.Controllers;

/// <summary>
/// Reference data management endpoints
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class ReferenceDataController : ControllerBase
{
    private readonly IReferenceDataService _referenceDataService;

    public ReferenceDataController(IReferenceDataService referenceDataService)
    {
        _referenceDataService = referenceDataService;
    }

    /// <summary>
    /// Get all active request types with optional filtering
    /// </summary>
    /// <param name="requestTypeId">Optional filter by request type ID</param>
    /// <param name="requestTypeName">Optional filter by request type name (partial match)</param>
    /// <returns>List of active request types</returns>
    /// <response code="200">Returns list of active request types</response>
    /// <response code="400">If request parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("request-types")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<RequestTypeDto>>>> GetRequestTypes(
        [FromQuery] int? requestTypeId = null,
        [FromQuery] string? requestTypeName = null)
    {
        var result = await _referenceDataService.GetRequestTypesAsync(requestTypeId, requestTypeName);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get all active request statuses with optional filtering
    /// </summary>
    /// <param name="requestStatusId">Optional filter by request status ID</param>
    /// <param name="requestStatusName">Optional filter by request status name (partial match)</param>
    /// <returns>List of active request statuses</returns>
    /// <response code="200">Returns list of active request statuses</response>
    /// <response code="400">If request parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("request-statuses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<RequestStatusDto>>>> GetRequestStatuses(
        [FromQuery] int? requestStatusId = null,
        [FromQuery] string? requestStatusName = null)
    {
        var result = await _referenceDataService.GetRequestStatusesAsync(requestStatusId, requestStatusName);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get all active termination reasons with optional filtering
    /// </summary>
    /// <param name="reasonId">Optional filter by termination reason ID</param>
    /// <param name="reasonCode">Optional filter by reason code (partial match)</param>
    /// <param name="companyCode">Optional filter by company code</param>
    /// <returns>List of active termination reasons</returns>
    /// <response code="200">Returns list of active termination reasons</response>
    /// <response code="400">If request parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("termination-reasons")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<TerminationReasonDto>>>> GetTerminationReasons(
        [FromQuery] int? reasonId = null,
        [FromQuery] string? reasonCode = null,
        [FromQuery] int? companyCode = null)
    {
        var result = await _referenceDataService.GetTerminationReasonsAsync(reasonId, reasonCode, companyCode);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get all payroll departments with optional filtering
    /// </summary>
    /// <param name="companyCode">Optional filter by company code</param>
    /// <param name="deptCode">Optional filter by department code</param>
    /// <returns>List of payroll departments</returns>
    /// <response code="200">Returns list of payroll departments</response>
    /// <response code="400">If request parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("payroll-departments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<PayrollDepartmentDto>>>> GetPayrollDepartments(
        [FromQuery] int? companyCode = null,
        [FromQuery] int? deptCode = null)
    {
        var result = await _referenceDataService.GetPayrollDepartmentsAsync(companyCode, deptCode);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get all payroll department short names with optional filtering (for role dropdown)
    /// </summary>
    /// <param name="companyCode">Optional filter by company code</param>
    /// <param name="deptCode">Optional filter by department code</param>
    /// <param name="deptShortName">Optional filter by department short name (partial match)</param>
    /// <returns>List of payroll department short names</returns>
    /// <response code="200">Returns list of payroll department short names</response>
    /// <response code="400">If request parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("payroll-department-short-names")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<PayrollDepartmentShortNameDto>>>> GetPayrollDepartmentShortNames(
        [FromQuery] int? companyCode = null,
        [FromQuery] int? deptCode = null,
        [FromQuery] string? deptShortName = null)
    {
        var result = await _referenceDataService.GetPayrollDepartmentShortNamesAsync(companyCode, deptCode, deptShortName);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Get all active payroll groups with optional filtering
    /// </summary>
    /// <param name="companyCode">Optional filter by company code</param>
    /// <param name="groupCode">Optional filter by payroll group code</param>
    /// <returns>List of active payroll groups</returns>
    /// <response code="200">Returns list of active payroll groups</response>
    /// <response code="400">If request parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("payroll-groups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<PayrollGroupDto>>>> GetPayrollGroups(
        [FromQuery] int? companyCode = null,
        [FromQuery] int? groupCode = null)
    {
        var result = await _referenceDataService.GetPayrollGroupsAsync(companyCode, groupCode);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Get all active companies with optional filtering
    /// </summary>
    /// <param name="companyId">Optional filter by company ID</param>
    /// <param name="companyCode">Optional filter by company code</param>
    /// <param name="companyName">Optional filter by company name (partial match)</param>
    /// <returns>List of active companies</returns>
    /// <response code="200">Returns list of active companies</response>
    /// <response code="400">If request parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("companies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<CompanyDto>>>> GetCompanies(
        [FromQuery] int? companyId = null,
        [FromQuery] int? companyCode = null,
        [FromQuery] string? companyName = null)
    {
        var result = await _referenceDataService.GetCompaniesAsync(companyId, companyCode, companyName);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get all active company type locations with optional filtering
    /// </summary>
    /// <param name="id">Optional filter by company type location ID</param>
    /// <param name="companyCode">Optional filter by company code</param>
    /// <param name="locationType">Optional filter by location type (partial match)</param>
    /// <returns>List of active company type locations</returns>
    /// <response code="200">Returns list of active company type locations</response>
    /// <response code="400">If request parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("company-type-locations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<CompanyTypeLocationDto>>>> GetCompanyTypeLocations(
        [FromQuery] int? id = null,
        [FromQuery] int? companyCode = null,
        [FromQuery] string? locationType = null)
    {
        var result = await _referenceDataService.GetCompanyTypeLocationsAsync(id, companyCode, locationType);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get all active physical locations with optional filtering
    /// </summary>
    /// <param name="locationId">Optional filter by location ID</param>
    /// <param name="locationCode">Optional filter by location code</param>
    /// <param name="locationName">Optional filter by location name (partial match)</param>
    /// <returns>List of active physical locations</returns>
    /// <response code="200">Returns list of active physical locations</response>
    /// <response code="400">If request parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("physical-locations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<PhysicalLocationDto>>>> GetPhysicalLocations(
        [FromQuery] int? locationId = null,
        [FromQuery] int? locationCode = null,
        [FromQuery] string? locationName = null)
    {
        var result = await _referenceDataService.GetPhysicalLocationsAsync(locationId, locationCode, locationName);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }


    /// <summary>
    /// Get all active employment statuses with optional filtering
    /// </summary>
    /// <param name="id">Optional filter by employment status ID</param>
    /// <param name="companyCode">Optional filter by company code</param>
    /// <param name="status">Optional filter by status (partial match)</param>
    /// <returns>List of active employment statuses</returns>
    /// <response code="200">Returns list of active employment statuses</response>
    /// <response code="400">If request parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("employment-statuses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<EmploymentStatusDto>>>> GetEmploymentStatuses(
        [FromQuery] int? id = null,
        [FromQuery] int? companyCode = null,
        [FromQuery] string? status = null)
    {
        var result = await _referenceDataService.GetEmploymentStatusesAsync(id, companyCode, status);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get all building access requirements with optional filtering
    /// </summary>
    /// <param name="companyCode">Optional filter by company code</param>
    /// <param name="description">Optional filter by description (partial match)</param>
    /// <param name="locationType">Optional filter by location type (partial match)</param>
    /// <returns>List of building access requirements</returns>
    /// <response code="200">Returns list of building access requirements</response>
    /// <response code="400">If request fails</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("building-access-requirements")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<BuildingAccessRequirementDto>>>> GetBuildingAccessRequirements(
        [FromQuery] int? companyCode = null,
        [FromQuery] string? description = null,
        [FromQuery] string? locationType = null)
    {
        var result = await _referenceDataService.GetBuildingAccessRequirementsAsync(companyCode, description, locationType);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get tablet profiles with optional filtering by company code, location type, or profile name
    /// </summary>
    /// <param name="companyCode">Optional company code to filter by</param>
    /// <param name="locationType">Optional location type to filter by</param>
    /// <param name="profileName">Optional profile name to filter by</param>
    /// <returns>List of tablet profiles</returns>
    /// <response code="200">Returns list of tablet profiles</response>
    /// <response code="400">If request fails or encounters errors</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("tablet-profiles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<TabletProfileDto>>>> GetTabletProfiles(
        [FromQuery] int? companyCode = null,
        [FromQuery] string? locationType = null,
        [FromQuery] string? profileName = null)
    {
        var result = await _referenceDataService.GetTabletProfilesAsync(companyCode, locationType, profileName);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get applications with optional filtering by company code, name, or location type
    /// </summary>
    /// <param name="companyCode">Optional company code to filter by</param>
    /// <param name="name">Optional name to filter by (partial match)</param>
    /// <param name="locationType">Optional location type to filter by</param>
    /// <returns>List of applications</returns>
    /// <response code="200">Returns list of applications</response>
    /// <response code="400">If request fails or encounters errors</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("applications")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ApplicationDto>>>> GetApplications(
        [FromQuery] int? companyCode = null,
        [FromQuery] string? name = null,
        [FromQuery] string? locationType = null)
    {
        var result = await _referenceDataService.GetApplicationsAsync(companyCode, name, locationType);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Manually trigger company sync from Viewpoint API
    /// </summary>
    /// <returns>Company sync result with statistics</returns>
    /// <response code="200">Returns company sync result</response>
    /// <response code="400">If sync fails or encounters errors</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user is not authorized</response>
    [HttpPost("sync/companies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<CompanySyncResultDto>>> SyncCompaniesFromViewpoint()
    {
        // TODO: Add authorization check for admin users only
        // For now, any authenticated user can trigger sync
        
        var result = await _referenceDataService.SyncCompaniesFromViewpointAsync();
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Sync departments from Viewpoint API to local database
    /// </summary>
    /// <returns>Department sync result with statistics</returns>
    /// <response code="200">Returns department sync result</response>
    /// <response code="400">If sync operation fails</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user is not authorized</response>
    [HttpPost("sync/departments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<DepartmentSyncResultDto>>> SyncDepartmentsFromViewpoint()
    {
        // TODO: Add authorization check for admin users only
        // For now, any authenticated user can trigger sync
        
        var result = await _referenceDataService.SyncDepartmentsFromViewpointAsync();
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Sync positions from Viewpoint API to local database
    /// </summary>
    /// <returns>Position sync result with statistics</returns>
    /// <response code="200">Returns position sync result</response>
    /// <response code="400">If sync operation fails</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user is not authorized</response>
    [HttpPost("sync/positions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PositionSyncResultDto>>> SyncPositionsFromViewpoint()
    {
        // TODO: Add authorization check for admin users only
        // For now, any authenticated user can trigger sync
        
        var result = await _referenceDataService.SyncPositionsFromViewpointAsync();
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Sync payroll groups from Viewpoint API to local database
    /// </summary>
    /// <returns>Payroll group sync result with statistics</returns>
    /// <response code="200">Returns payroll group sync result</response>
    /// <response code="400">If sync operation fails</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user is not authorized</response>
    [HttpPost("sync/payroll-groups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PayrollGroupSyncResultDto>>> SyncPayrollGroupsFromViewpoint()
    {
        // TODO: Add authorization check for admin users only
        // For now, any authenticated user can trigger sync

        var result = await _referenceDataService.SyncPayrollGroupsFromViewpointAsync();

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Sync union crafts from Viewpoint API to local database
    /// </summary>
    /// <returns>Union craft sync result with statistics</returns>
    /// <response code="200">Returns union craft sync result</response>
    /// <response code="400">If sync operation fails</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user is not authorized</response>
    [HttpPost("sync/union-crafts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<UnionCraftSyncResultDto>>> SyncUnionCraftsFromViewpoint()
    {
        // TODO: Add authorization check for admin users only
        // For now, any authenticated user can trigger sync

        var result = await _referenceDataService.SyncUnionCraftsFromViewpointAsync();

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Sync employment statuses from Viewpoint API to local database
    /// </summary>
    /// <returns>Employment status sync result with statistics</returns>
    /// <response code="200">Returns employment status sync result</response>
    /// <response code="400">If sync operation fails</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user is not authorized</response>
    [HttpPost("sync/employment-statuses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<EmploymentStatusSyncResultDto>>> SyncEmploymentStatusesFromViewpoint()
    {
        // TODO: Add authorization check for admin users only
        // For now, any authenticated user can trigger sync

        var result = await _referenceDataService.SyncEmploymentStatusesFromViewpointAsync();

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Sync employee salary types from Viewpoint API to local database
    /// </summary>
    /// <returns>Employee salary type sync result with statistics</returns>
    /// <response code="200">Returns employee salary type sync result</response>
    /// <response code="400">If sync operation fails</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user is not authorized</response>
    [HttpPost("sync/employee-salary-types")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<EmployeeSalaryTypeSyncResultDto>>> SyncEmployeeSalaryTypesFromViewpoint()
    {
        // TODO: Add authorization check for admin users only
        // For now, any authenticated user can trigger sync

        var result = await _referenceDataService.SyncEmployeeSalaryTypesFromViewpointAsync();

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Get all active union crafts with optional company filtering
    /// </summary>
    /// <param name="companyCode">Optional filter by company code</param>
    /// <returns>List of active union crafts</returns>
    /// <response code="200">Returns list of active union crafts</response>
    /// <response code="400">If request fails</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("union-crafts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<UnionCraftDto>>>> GetUnionCrafts(
        [FromQuery] int? companyCode = null)
    {
        var result = await _referenceDataService.GetUnionCraftsAsync(companyCode);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get all active employee salary types with optional company filtering
    /// </summary>
    /// <param name="companyCode">Optional filter by company code</param>
    /// <returns>List of active employee salary types</returns>
    /// <response code="200">Returns list of active employee salary types</response>
    /// <response code="400">If request fails</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("employee-salary-types")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<EmployeeSalaryTypeDto>>>> GetEmployeeSalaryTypes(
        [FromQuery] int? companyCode = null)
    {
        var result = await _referenceDataService.GetEmployeeSalaryTypesAsync(companyCode);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get all active apprentice percentages with optional filtering
    /// </summary>
    /// <param name="id">Optional filter by apprentice percentage ID</param>
    /// <param name="appPercentage">Optional filter by percentage value (partial match)</param>
    /// <returns>List of active apprentice percentages</returns>
    /// <response code="200">Returns list of active apprentice percentages</response>
    /// <response code="400">If request fails</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("apprentice-percentages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ApprenticePercentageDto>>>> GetApprenticePercentages(
        [FromQuery] int? id = null,
        [FromQuery] string? appPercentage = null)
    {
        var result = await _referenceDataService.GetApprenticePercentagesAsync(id, appPercentage);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get all active positions with optional company filtering
    /// </summary>
    /// <param name="companyCode">Optional filter by company code</param>
    /// <returns>List of active positions</returns>
    /// <response code="200">Returns list of active positions</response>
    /// <response code="400">If request fails</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("positions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<PositionDto>>>> GetPositions(
        [FromQuery] int? companyCode = null)
    {
        var result = await _referenceDataService.GetPositionsAsync(companyCode);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get all supervisors with optional company and payroll department filtering
    /// </summary>
    /// <param name="companyCode">Optional filter by company code</param>
    /// <param name="payrollDeptCode">Optional filter by payroll department code</param>
    /// <returns>List of supervisors</returns>
    /// <response code="200">Returns list of supervisors</response>
    /// <response code="400">If request fails</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("supervisors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<SupervisorDto>>>> GetSupervisors(
        [FromQuery] int? companyCode = null,
        [FromQuery] int? payrollDeptCode = null)
    {
        var result = await _referenceDataService.GetSupervisorsAsync(companyCode, payrollDeptCode);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get all employee license classes with optional filtering
    /// </summary>
    /// <param name="id">Optional filter by employee license class ID</param>
    /// <param name="licenseClass">Optional filter by license class (partial match)</param>
    /// <param name="isUnion">Optional filter by union status</param>
    /// <returns>List of employee license classes</returns>
    /// <response code="200">Returns list of employee license classes</response>
    /// <response code="400">If request fails or encounters errors</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("employee-license-classes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<EmployeeLicenseClassDto>>>> GetEmployeeLicenseClasses(
        [FromQuery] int? id = null,
        [FromQuery] string? licenseClass = null,
        [FromQuery] bool? isUnion = null)
    {
        var result = await _referenceDataService.GetEmployeeLicenseClassesAsync(id, licenseClass, isUnion);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get computer requirements with optional filtering by ID, parent/child status, parent ID, or description
    /// </summary>
    /// <param name="id">Optional ID to filter by</param>
    /// <param name="isChild">Optional filter to get only parent (false) or child (true) requirements</param>
    /// <param name="parentId">Optional parent ID to get all children of a specific parent</param>
    /// <param name="description">Optional description to filter by (partial match)</param>
    /// <returns>List of computer requirements</returns>
    /// <response code="200">Returns list of computer requirements</response>
    /// <response code="400">If request parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("computer-requirements")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ComputerRequirementDto>>>> GetComputerRequirements(
        [FromQuery] int? id = null,
        [FromQuery] bool? isChild = null,
        [FromQuery] int? parentId = null,
        [FromQuery] string? description = null)
    {
        var result = await _referenceDataService.GetComputerRequirementsAsync(id, isChild, parentId, description);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Generate a unique username for AD creation based on preferred first name or first name
    /// </summary>
    /// <param name="firstName">The employee's first name (required)</param>
    /// <param name="preferredFirstName">The employee's preferred first name (optional)</param>
    /// <returns>Generated unique username in format [name]001, [name]002, etc.</returns>
    /// <response code="200">Returns the generated username</response>
    /// <response code="400">If first name is not provided or request fails</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("generate-username")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<string>>> GenerateUsername(
        [FromQuery] string firstName,
        [FromQuery] string? preferredFirstName = null)
    {
        var result = await _referenceDataService.GenerateUsernameAsync(firstName, preferredFirstName);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Generate an email address based on employee name and payroll department's email domain.
    /// If emailRequired is true: firstname.lastname@domain (or preferred.lastname@domain)
    /// If emailRequired is false: firstname001@domain (or preferred001@domain)
    /// </summary>
    /// <param name="firstName">Employee's first name</param>
    /// <param name="lastName">Employee's last name</param>
    /// <param name="companyCode">The company code</param>
    /// <param name="payrollDeptCode">The payroll department code</param>
    /// <param name="emailRequired">Whether the new hire requires an email address</param>
    /// <param name="preferredFirstName">Optional preferred first name</param>
    /// <param name="userId">The generated User ID (used when emailRequired is false)</param>
    /// <returns>Generated email address</returns>
    /// <response code="200">Returns the generated email address</response>
    /// <response code="400">If parameters are invalid or request fails</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("generate-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<string>>> GenerateEmailAddress(
        [FromQuery] string firstName,
        [FromQuery] string lastName,
        [FromQuery] int companyCode,
        [FromQuery] int payrollDeptCode,
        [FromQuery] bool emailRequired = false,
        [FromQuery] string? preferredFirstName = null,
        [FromQuery] string? userId = null)
    {
        var result = await _referenceDataService.GenerateEmailAddressAsync(firstName, lastName, companyCode, payrollDeptCode, emailRequired, preferredFirstName, userId);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Get Viewpoint sync status with last sync dates for all data types
    /// </summary>
    /// <returns>Viewpoint sync status information</returns>
    /// <response code="200">Returns sync status information</response>
    /// <response code="400">If request fails</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("viewpoint-sync-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ViewpointSyncStatusDto>>> GetViewpointSyncStatus()
    {
        var result = await _referenceDataService.GetViewpointSyncStatusAsync();
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get current sync schedule configuration
    /// </summary>
    /// <returns>Current schedule settings</returns>
    /// <response code="200">Returns current schedule configuration</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user is not authorized</response>
    [HttpGet("sync-schedule")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<SyncScheduleConfigDto>>> GetSyncSchedule()
    {
        var result = await _referenceDataService.GetSyncScheduleConfigAsync();
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Update sync schedule configuration and set up recurring jobs
    /// </summary>
    /// <param name="config">Schedule configuration</param>
    /// <returns>Result with scheduled job information</returns>
    /// <response code="200">Returns schedule update result</response>
    /// <response code="400">If configuration is invalid</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user is not authorized</response>
    [HttpPost("sync-schedule")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<SyncScheduleResultDto>>> UpdateSyncSchedule([FromBody] SyncScheduleConfigDto config)
    {
        if (config == null)
        {
            return BadRequest(new ApiResponse<SyncScheduleResultDto>
            {
                Success = false,
                Message = "Invalid configuration provided"
            });
        }

        var result = await _referenceDataService.UpdateSyncScheduleConfigAsync(config);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }
}