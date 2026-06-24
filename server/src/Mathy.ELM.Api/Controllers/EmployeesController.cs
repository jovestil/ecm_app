using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Core.Authorization;

namespace Mathy.ELM.Api.Controllers;

/// <summary>
/// Employee management and search endpoints
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IViewpointService _viewpointService;
    private readonly IUserContextService _userContextService;

    public EmployeesController(
        IEmployeeService employeeService, 
        IViewpointService viewpointService,
        IUserContextService userContextService)
    {
        _employeeService = employeeService;
        _viewpointService = viewpointService;
        _userContextService = userContextService;
    }

    /// <summary>
    /// Search for employees by name or employee number
    /// </summary>
    /// <param name="searchTerm">Search term to match against employee name or number</param>
    /// <param name="companyCode">Optional company code to filter results</param>
    /// <returns>List of employees matching the search criteria</returns>
    /// <response code="200">Returns matching employees</response>
    /// <response code="400">If search term is missing or invalid</response>
    /// <response code="403">If user doesn't have access to the specified company</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<EmployeeDto>>>> SearchEmployees(
        [FromQuery] string searchTerm, 
        [FromQuery] string? companyCode = null)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequest(new ApiResponse<List<EmployeeDto>>
            {
                Success = false,
                Message = "Search term is required"
            });
        }

        // Check company access authorization
        var userCompanies = _userContextService.GetUserCompanies();
        
        // If user specifies a company code, ensure they have access to it
        if (!string.IsNullOrEmpty(companyCode))
        {
            if (!userCompanies.Contains(companyCode) && !_userContextService.IsInRole("SystemAdmin"))
            {
                return Forbid("You don't have access to this company");
            }
        }
        
        // If no company code specified, restrict to user's companies (unless system admin)
        if (string.IsNullOrEmpty(companyCode) && !_userContextService.IsInRole("SystemAdmin"))
        {
            // For non-system admins, we'll need to modify the service to filter by user's companies
            // For now, we'll use the first company the user has access to
            companyCode = userCompanies.FirstOrDefault();
        }

        var result = await _employeeService.SearchEmployeesAsync(searchTerm, companyCode);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get employee details by employee number
    /// </summary>
    /// <param name="employeeNumber">The employee number to look up</param>
    /// <returns>Employee details</returns>
    /// <response code="200">Returns employee details</response>
    /// <response code="400">If employee number is missing</response>
    /// <response code="404">If employee is not found</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("{employeeNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetEmployee(string employeeNumber)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            return BadRequest(new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = "Employee number is required"
            });
        }

        var result = await _employeeService.GetEmployeeByNumberAsync(employeeNumber);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return NotFound(result);
    }

    /// <summary>
    /// Get employees by company with pagination
    /// </summary>
    /// <param name="companyCode">Company code to filter employees</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 25, max: 100)</param>
    /// <returns>Paginated list of employees for the specified company</returns>
    /// <response code="200">Returns paginated employee list</response>
    /// <response code="400">If company code is missing</response>
    /// <response code="403">If user doesn't have access to the specified company</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("company/{companyCode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<List<EmployeeDto>>>> GetEmployeesByCompany(
        string companyCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        if (string.IsNullOrWhiteSpace(companyCode))
        {
            return BadRequest(new PagedResponse<List<EmployeeDto>>
            {
                Success = false,
                Message = "Company code is required"
            });
        }

        // Check company access authorization
        var userCompanies = _userContextService.GetUserCompanies();
        if (!userCompanies.Contains(companyCode) && !_userContextService.IsInRole("SystemAdmin"))
        {
            return Forbid("You don't have access to this company");
        }

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 25;

        var result = await _employeeService.GetEmployeesByCompanyAsync(companyCode, page, pageSize);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get all employees from Viewpoint API with optional filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 25, max: 100)</param>
    /// <param name="filter">Optional filter parameter. If 'RTW', filters for employees with Status 'U-LAYOFF'</param>
    /// <returns>Paginated list of employees</returns>
    /// <response code="200">Returns all employees</response>
    /// <response code="500">If there's an error fetching from Viewpoint API</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("viewpoint/all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ViewpointEmployeesResponse>>> GetAllEmployeesFromViewpoint(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? filter = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 25;

        try
        {
            var result = await _viewpointService.GetAllEmployeesAsync(page, pageSize, filter);
            
            if (result == null)
            {
                return StatusCode(500, new ApiResponse<ViewpointEmployeesResponse>
                {
                    Success = false,
                    Message = "Failed to fetch employees from Viewpoint API"
                });
            }

            return Ok(new ApiResponse<ViewpointEmployeesResponse>
            {
                Success = true,
                Data = result,
                Message = $"Successfully retrieved {result.Data?.Count ?? 0} employees"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<ViewpointEmployeesResponse>
            {
                Success = false,
                Message = $"Error fetching employees: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Search employees in Viewpoint API by HRRef (Employee Number)
    /// </summary>
    /// <param name="HRRef_EmployeeNumber">Employee number (HRRef) to search for</param>
    /// <returns>Employee matching the HRRef criteria</returns>
    /// <response code="200">Returns matching employee</response>
    /// <response code="400">If employee number is missing or invalid</response>
    /// <response code="500">If there's an error fetching from Viewpoint API</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPost("viewpoint/search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ViewpointEmployeeDto>>>> SearchEmployeesInViewpoint([FromBody] int HRRef_EmployeeNumber)
    {
        if (HRRef_EmployeeNumber <= 0)
        {
            return BadRequest(new ApiResponse<List<ViewpointEmployeeDto>>
            {
                Success = false,
                Message = "Employee number (HRRef) must be a positive integer"
            });
        }

        try
        {
            var result = await _viewpointService.SearchEmployeesAsync(HRRef_EmployeeNumber);
            
            return Ok(new ApiResponse<List<ViewpointEmployeeDto>>
            {
                Success = true,
                Data = result,
                Message = $"Found {result.Count} employees matching HRRef '{HRRef_EmployeeNumber}'"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<ViewpointEmployeeDto>>
            {
                Success = false,
                Message = $"Error searching employees: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Search employees in Viewpoint API for New Hire functionality using multiple filters
    /// </summary>
    /// <param name="request">Search criteria containing HRCo, PRDept, LastName, and HireDate</param>
    /// <returns>List of employees matching the search criteria</returns>
    /// <response code="200">Returns matching employees</response>
    /// <response code="400">If request parameters are missing or invalid</response>
    /// <response code="500">If there's an error fetching from Viewpoint API</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPost("viewpoint/search-new-hire")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ViewpointEmployeeDto>>>> SearchEmployeeInViewpointForNewHire([FromBody] NewHireSearchRequest request)
    {
        if (request == null)
        {
            return BadRequest(new ApiResponse<List<ViewpointEmployeeDto>>
            {
                Success = false,
                Message = "Search request is required"
            });
        }

        if (request.HRCo <= 0)
        {
            return BadRequest(new ApiResponse<List<ViewpointEmployeeDto>>
            {
                Success = false,
                Message = "HRCo must be a positive integer"
            });
        }

        if (string.IsNullOrWhiteSpace(request.PRDept))
        {
            return BadRequest(new ApiResponse<List<ViewpointEmployeeDto>>
            {
                Success = false,
                Message = "PRDept is required"
            });
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new ApiResponse<List<ViewpointEmployeeDto>>
            {
                Success = false,
                Message = "LastName is required"
            });
        }

        if (string.IsNullOrWhiteSpace(request.HireDate))
        {
            return BadRequest(new ApiResponse<List<ViewpointEmployeeDto>>
            {
                Success = false,
                Message = "HireDate is required"
            });
        }

        try
        {
            var result = await _viewpointService.SearchEmployeeInNewHireWithAPIAsync(
                request.HRCo,
                request.PRDept,
                request.LastName,
                request.HireDate);

            return Ok(new ApiResponse<List<ViewpointEmployeeDto>>
            {
                Success = true,
                Data = result,
                Message = $"Found {result.Count} employees matching the new hire search criteria"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<ViewpointEmployeeDto>>
            {
                Success = false,
                Message = $"Error searching employees for new hire: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get current user's employee data from Viewpoint HR API using their email
    /// </summary>
    /// <returns>Current user's employee details from Viewpoint HR system</returns>
    /// <response code="200">Returns current user's employee details</response>
    /// <response code="404">If current user's email is not found in Viewpoint</response>
    /// <response code="500">If there's an error fetching from Viewpoint API</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("viewpoint/email")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ViewpointEmployeeDto>>> GetEmployeeByEmailFromViewpoint()
    {
        try
        {
            var userEmail = _userContextService.GetUserEmail();

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return BadRequest(new ApiResponse<ViewpointEmployeeDto>
                {
                    Success = false,
                    Message = "User email not found in authentication context"
                });
            }

            // var result = await _viewpointService.GetEmployeeByEmailAsync(userEmail);

            // Static dummy data for testing
            var result = new ViewpointEmployeeDto
            {
                HRCo = 4,
                HRRef = 640942,
                PRCo = 19,
                PREmp = 640942,
                FirstName = "Kimberly",
                LastName = "Oberweiser",
                MiddleName = "A",
                SortName = "OBERWKIMBERLY",
                Address = "123 Main Street",
                City = "Cebu City",
                State = "Cebu",
                Zip = "6000",
                HireDate = "2020-01-15",
                PRGroup = 1,
                PRDept = "ENG",
                Status = "Active",
                ActiveYN = "Y",
                Email = "kim.oberweiser@corpmts.com",
                CustomFields = new ViewpointCustomFields
                {
                    WorkEmail = "kim.oberweiser@corpmts.com",
                    I9OnFile = "Y",
                    CommsMethod = "Email",
                    PhotoRelease = "Y"
                }
            };

            if (result == null)
            {
                return NotFound(new ApiResponse<ViewpointEmployeeDto>
                {
                    Success = false,
                    Message = "No employee found in Viewpoint system"
                });
            }

            return Ok(new ApiResponse<ViewpointEmployeeDto>
            {
                Success = true,
                Data = result,
                Message = "Current user's employee data retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<ViewpointEmployeeDto>
            {
                Success = false,
                Message = $"Error fetching current user's employee data: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Sync employees from Viewpoint API to local database
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 100, max: 1000)</param>
    /// <param name="filter">Optional filter parameter. If 'RTW', filters for employees with Status 'U-LAYOFF'</param>
    /// <returns>Sync operation results</returns>
    /// <response code="200">Returns sync operation results</response>
    /// <response code="500">If there's an error during sync operation</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPost("sync/viewpoint")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<EmployeeSyncResultDto>>> SyncEmployeesFromViewpoint(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] string? filter = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 1000) pageSize = 100;

        try
        {
            var result = await _employeeService.SyncEmployeesFromViewpointAsync(page, pageSize, filter);

            if (result.Success)
            {
                return Ok(result);
            }

            return StatusCode(500, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<EmployeeSyncResultDto>
            {
                Success = false,
                Message = $"Sync operation failed: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Full-sweep sync of all employees from Viewpoint.
    /// Fetches every employee in one pass, upserts them into the local Employees table,
    /// and soft-deletes (IsDeleted=true) any local employee whose natural key is absent
    /// from the Viewpoint payload. Missing employees that later reappear in Viewpoint
    /// will be automatically restored on the next sync.
    /// </summary>
    /// <response code="200">Returns full sync operation results</response>
    /// <response code="500">If there's an error during sync operation</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPost("sync/viewpoint/full")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<EmployeeSyncResultDto>>> SyncAllEmployeesFromViewpoint()
    {
        try
        {
            var result = await _employeeService.SyncAllEmployeesFromViewpointAsync();

            if (result.Success)
            {
                return Ok(result);
            }

            return StatusCode(500, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<EmployeeSyncResultDto>
            {
                Success = false,
                Message = $"Error during employee sync: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get employees by HR request type with pagination and sorting
    /// </summary>
    /// <param name="requestType">HR request type (layoff-request, active, return-to-work)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 25, max: 100)</param>
    /// <param name="orderBy">Field to sort by (employeeName, employeeNumber, companyCode, position, division)</param>
    /// <param name="orderByDesc">Sort in descending order (default: false)</param>
    /// <param name="search">Search term to filter employees by name, employee number, company code, and department code. When isEditMode is true, this can be a comma-separated list of employee IDs</param>
    /// <param name="isEditMode">If true, don't filter by EmploymentStatus (for edit mode). When true and search contains employee IDs, filters by those specific employees</param>
    /// <param name="employeeIds">Optional array of employee IDs to filter by (alternative to using search parameter)</param>
    /// <returns>Paginated list of employees for the specified HR request type</returns>
    /// <response code="200">Returns paginated employee list</response>
    /// <response code="400">If request type is invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("hr-request/{requestType}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<List<EmployeeDto>>>> GetEmployeesByHRRequest(
        string requestType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? orderBy = null,
        [FromQuery] bool orderByDesc = false,
        [FromQuery] string? search = null,
        [FromQuery] bool isEditMode = false,
        [FromQuery] int[]? employeeIds = null)
    {
        if (page < 1) page = 1;

        var result = await _employeeService.GetEmployeesByHRRequestAsync(requestType, page, pageSize, orderBy, orderByDesc, search, isEditMode, employeeIds);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get employees with layoff status (U-LAYOFF) with pagination and sorting
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 25, max: 100)</param>
    /// <param name="orderBy">Field to sort by (employeeName, employeeNumber, companyCode, position, division)</param>
    /// <param name="orderByDesc">Sort in descending order (default: false)</param>
    /// <param name="search">Search term to filter employees by name, employee number, company code, and department code</param>
    /// <param name="isEditMode">If true, don't filter by EmploymentStatus (for edit mode)</param>
    /// <returns>Paginated list of laid-off employees</returns>
    /// <response code="200">Returns paginated laid-off employee list</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("layoff")]
    [Obsolete("Use GetEmployeesByHRRequest with requestType 'return-to-work' instead")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<List<EmployeeDto>>>> GetEmployeesByLayoff(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? orderBy = null,
        [FromQuery] bool orderByDesc = false,
        [FromQuery] string? search = null,
        [FromQuery] bool isEditMode = false)
    {
        // Forward to new generic method for backward compatibility
        return await GetEmployeesByHRRequest("return-to-work", page, pageSize, orderBy, orderByDesc, search, isEditMode);
    }

    /// <summary>
    /// Get employees with active status (U-ACTIVE) with pagination and sorting
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 25, max: 100)</param>
    /// <param name="orderBy">Field to sort by (employeeName, employeeNumber, companyCode, position, division)</param>
    /// <param name="orderByDesc">Sort in descending order (default: false)</param>
    /// <param name="search">Search term to filter employees by name, employee number, company code, and department code</param>
    /// <returns>Paginated list of active employees</returns>
    /// <response code="200">Returns paginated active employee list</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<List<EmployeeDto>>>> GetEmployeesByActive(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? orderBy = null,
        [FromQuery] bool orderByDesc = false,
        [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;

        var result = await _employeeService.GetEmployeesByActiveAsync(page, pageSize, orderBy, orderByDesc, search);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Update employee information in Viewpoint for new hire
    /// </summary>
    /// <param name="request">Update request containing employee search criteria and custom fields to update</param>
    /// <returns>Result of the update operation including employee verification and action status</returns>
    /// <response code="200">Returns the update result (check Success property for actual outcome)</response>
    /// <response code="400">If request parameters are missing or invalid</response>
    /// <response code="500">If there's an error during the update process</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPost("viewpoint/update-new-hire")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UpdateEmployeeNewHireResultDto>>> UpdateEmployeeForNewHire([FromBody] UpdateEmployeeNewHireRequestDto request)
    {
        if (request == null)
        {
            return BadRequest(new ApiResponse<UpdateEmployeeNewHireResultDto>
            {
                Success = false,
                Message = "Request body is required"
            });
        }

        if (request.HRCo <= 0)
        {
            return BadRequest(new ApiResponse<UpdateEmployeeNewHireResultDto>
            {
                Success = false,
                Message = "HRCo (Company Code) must be a positive integer"
            });
        }

        if (string.IsNullOrWhiteSpace(request.PRDept))
        {
            return BadRequest(new ApiResponse<UpdateEmployeeNewHireResultDto>
            {
                Success = false,
                Message = "PRDept (Department Code) is required"
            });
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new ApiResponse<UpdateEmployeeNewHireResultDto>
            {
                Success = false,
                Message = "LastName is required for employee verification"
            });
        }

        if (string.IsNullOrWhiteSpace(request.HireDate))
        {
            return BadRequest(new ApiResponse<UpdateEmployeeNewHireResultDto>
            {
                Success = false,
                Message = "HireDate is required for employee verification"
            });
        }

        if (request.CustomFields == null)
        {
            return BadRequest(new ApiResponse<UpdateEmployeeNewHireResultDto>
            {
                Success = false,
                Message = "CustomFields are required - specify at least one field to update"
            });
        }

        try
        {
            var result = await _viewpointService.UpdateEmployeeForNewHireInViewPointAsync(request);

            // Return 200 OK with the result - the Success property in the result indicates actual outcome
            return Ok(new ApiResponse<UpdateEmployeeNewHireResultDto>
            {
                Success = result.Success,
                Data = result,
                Message = result.Message ?? (result.Success
                    ? "Employee update completed successfully"
                    : "Employee update failed")
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<UpdateEmployeeNewHireResultDto>
            {
                Success = false,
                Message = $"Error updating employee for new hire: {ex.Message}"
            });
        }
    }
}