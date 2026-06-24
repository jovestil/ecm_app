using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Constants;

namespace Mathy.ELM.Api.Controllers;

/// <summary>
/// Controller for Viewpoint integration operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ViewpointController : ControllerBase
{
    private readonly IViewpointService _viewpointService;
    private readonly ILogger<ViewpointController> _logger;

    public ViewpointController(
        IViewpointService viewpointService,
        ILogger<ViewpointController> logger)
    {
        _viewpointService = viewpointService;
        _logger = logger;
    }

    /// <summary>
    /// Update employee status for promotion requests
    /// </summary>
    /// <param name="employees">List of employees to activate</param>
    /// <returns>Update result</returns>
    [HttpPost("employees/activate")]
    public async Task<IActionResult> ActivateEmployees([FromBody] List<ViewpointEmployeeDto> employees)
    {
        try
        {
            var result = await _viewpointService.UpdateEmployeeStatusInViewpointAsync(employees, ViewpointEmployeeStatus.Active);
            
            return Ok(new ApiResponse<bool>
            {
                Success = result.Success,
                Data = result.Success,
                Message = result.Success ? $"Employees successfully activated to status '{result.ActualStatusUsed}'" : $"Failed to activate employees: {result.ErrorMessage}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating employees in Viewpoint");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Failed to activate employees"
            });
        }
    }

    /// <summary>
    /// Update employee status for layoff requests
    /// </summary>
    /// <param name="employees">List of employees to layoff</param>
    /// <returns>Update result</returns>
    [HttpPost("employees/layoff")]
    public async Task<IActionResult> LayoffEmployees([FromBody] List<ViewpointEmployeeDto> employees)
    {
        try
        {
            var result = await _viewpointService.UpdateEmployeeStatusInViewpointAsync(employees, ViewpointEmployeeStatus.Layoff);
            
            return Ok(new ApiResponse<bool>
            {
                Success = result.Success,
                Data = result.Success,
                Message = result.Success ? $"Employees successfully laid off to status '{result.ActualStatusUsed}'" : $"Failed to layoff employees: {result.ErrorMessage}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error laying off employees in Viewpoint");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Failed to layoff employees"
            });
        }
    }

    /// <summary>
    /// Update employee status for termination requests
    /// </summary>
    /// <param name="employees">List of employees to terminate</param>
    /// <returns>Update result</returns>
    [HttpPost("employees/terminate")]
    public async Task<IActionResult> TerminateEmployees([FromBody] List<ViewpointEmployeeDto> employees)
    {
        try
        {
            var result = await _viewpointService.UpdateEmployeeStatusInViewpointAsync(employees, ViewpointEmployeeStatus.Terminated);
            
            return Ok(new ApiResponse<bool>
            {
                Success = result.Success,
                Data = result.Success,
                Message = result.Success ? $"Employees successfully terminated to status '{result.ActualStatusUsed}'" : $"Failed to terminate employees: {result.ErrorMessage}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating employees in Viewpoint");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Failed to terminate employees"
            });
        }
    }

    /// <summary>
    /// Update employee status for leave requests
    /// </summary>
    /// <param name="employees">List of employees to put on leave</param>
    /// <returns>Update result</returns>
    [HttpPost("employees/leave")]
    public async Task<IActionResult> PutEmployeesOnLeave([FromBody] List<ViewpointEmployeeDto> employees)
    {
        try
        {
            var result = await _viewpointService.UpdateEmployeeStatusInViewpointAsync(employees, ViewpointEmployeeStatus.OnLeave);
            
            return Ok(new ApiResponse<bool>
            {
                Success = result.Success,
                Data = result.Success,
                Message = result.Success ? $"Employees successfully put on leave to status '{result.ActualStatusUsed}'" : $"Failed to put employees on leave: {result.ErrorMessage}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error putting employees on leave in Viewpoint");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Failed to put employees on leave"
            });
        }
    }

    /// <summary>
    /// Generic endpoint to update employee status with custom status value
    /// </summary>
    /// <param name="request">Update request with employees and status</param>
    /// <returns>Update result</returns>
    [HttpPost("employees/update-status")]
    public async Task<IActionResult> UpdateEmployeeStatus([FromBody] UpdateEmployeeStatusRequest request)
    {
        try
        {
            if (request?.Employees == null || !request.Employees.Any())
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "No employees provided"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Status is required"
                });
            }

            var result = await _viewpointService.UpdateEmployeeStatusInViewpointAsync(request.Employees, request.Status);
            
            return Ok(new ApiResponse<bool>
            {
                Success = result.Success,
                Data = result.Success,
                Message = result.Success ? $"Employees successfully updated to '{result.ActualStatusUsed}'" : $"Failed to update employees: {result.ErrorMessage}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee status to {Status} in Viewpoint", request?.Status);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Failed to update employee status"
            });
        }
    }

    /// <summary>
    /// Get all companies from Viewpoint API
    /// </summary>
    /// <returns>List of companies</returns>
    /// <response code="200">Returns all companies</response>
    /// <response code="500">If there's an error fetching from Viewpoint API</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("companies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ViewpointCompanyDto>>>> GetAllCompaniesFromViewpoint()
    {
        try
        {
            var result = await _viewpointService.GetAllCompaniesAsync();
            
            return Ok(new ApiResponse<List<ViewpointCompanyDto>>
            {
                Success = true,
                Data = result,
                Message = $"Successfully retrieved {result.Count} companies"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching companies from Viewpoint");
            return StatusCode(500, new ApiResponse<List<ViewpointCompanyDto>>
            {
                Success = false,
                Message = $"Error fetching companies: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get all payroll groups from Viewpoint API
    /// </summary>
    /// <returns>List of payroll groups</returns>
    /// <response code="200">Returns all payroll groups</response>
    /// <response code="500">If there's an error fetching from Viewpoint API</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("payroll-groups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ViewpointPayrollGroupDto>>>> GetAllPayrollGroupsFromViewpoint()
    {
        try
        {
            var result = await _viewpointService.GetAllPayrollGroupsAsync();
            
            return Ok(new ApiResponse<List<ViewpointPayrollGroupDto>>
            {
                Success = true,
                Data = result,
                Message = $"Successfully retrieved {result.Count} payroll groups"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payroll groups from Viewpoint");
            return StatusCode(500, new ApiResponse<List<ViewpointPayrollGroupDto>>
            {
                Success = false,
                Message = $"Error fetching payroll groups: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get all departments from Viewpoint API
    /// </summary>
    /// <returns>List of departments</returns>
    /// <response code="200">Returns all departments</response>
    /// <response code="500">If there's an error fetching from Viewpoint API</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("departments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ViewpointDepartmentDto>>>> GetAllDepartmentsFromViewpoint()
    {
        try
        {
            var result = await _viewpointService.GetAllDepartmentsAsync();
            
            return Ok(new ApiResponse<List<ViewpointDepartmentDto>>
            {
                Success = true,
                Data = result,
                Message = $"Successfully retrieved {result.Count} departments"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching departments from Viewpoint");
            return StatusCode(500, new ApiResponse<List<ViewpointDepartmentDto>>
            {
                Success = false,
                Message = $"Error fetching departments: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get all positions from Viewpoint API
    /// </summary>
    /// <returns>List of positions</returns>
    /// <response code="200">Returns all positions</response>
    /// <response code="500">If there's an error fetching from Viewpoint API</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("positions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ViewpointPositionDto>>>> GetAllPositionsFromViewpoint()
    {
        try
        {
            var result = await _viewpointService.GetAllPositionsAsync();
            
            return Ok(new ApiResponse<List<ViewpointPositionDto>>
            {
                Success = true,
                Data = result,
                Message = $"Successfully retrieved {result.Count} positions"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching positions from Viewpoint");
            return StatusCode(500, new ApiResponse<List<ViewpointPositionDto>>
            {
                Success = false,
                Message = $"Error fetching positions: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get all PREH employees from Viewpoint API
    /// </summary>
    /// <returns>List of PREH employees</returns>
    /// <response code="200">Returns all PREH employees</response>
    /// <response code="500">If there's an error fetching from Viewpoint API</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("preh-employees")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ViewpointPREHEmployeeDto>>>> GetPREHEmployees()
    {
        try
        {
            var result = await _viewpointService.GetPREHEmployeesAsync();
            
            return Ok(new ApiResponse<List<ViewpointPREHEmployeeDto>>
            {
                Success = true,
                Data = result,
                Message = $"Successfully retrieved {result.Count} PREH employees"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching PREH employees from Viewpoint");
            return StatusCode(500, new ApiResponse<List<ViewpointPREHEmployeeDto>>
            {
                Success = false,
                Message = $"Error fetching PREH employees: {ex.Message}"
            });
        }
    }
}

/// <summary>
/// Request model for updating employee status
/// </summary>
public class UpdateEmployeeStatusRequest
{
    /// <summary>
    /// List of employees to update
    /// </summary>
    public List<ViewpointEmployeeDto> Employees { get; set; } = new();

    /// <summary>
    /// Status to set for the employees
    /// </summary>
    public string Status { get; set; } = string.Empty;
}