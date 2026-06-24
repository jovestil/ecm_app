using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Services;

namespace Mathy.ELM.Api.Controllers;

/// <summary>
/// HR Request management endpoints
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class HRRequestsController : ControllerBase
{
    private readonly IHRRequestService _hrRequestService;
    private readonly IUserContextService _userContextService;

    public HRRequestsController(
        IHRRequestService hrRequestService,
        IUserContextService userContextService)
    {
        _hrRequestService = hrRequestService;
        _userContextService = userContextService;
    }

    /// <summary>
    /// Get all HR requests with optional filtering and pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 25, max: 100)</param>
    /// <param name="requestTypeId">Optional filter by request type ID</param>
    /// <param name="statusId">Optional filter by status ID</param>
    /// <param name="submittedBy">Optional filter by submitter user ID</param>
    /// <returns>Paginated list of HR requests</returns>
    /// <response code="200">Returns paginated HR requests</response>
    /// <response code="400">If parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<List<HRRequestDto>>>> GetHRRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] int? requestTypeId = null,
        [FromQuery] int? statusId = null,
        [FromQuery] int? submittedBy = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 25;

        var result = await _hrRequestService.GetHRRequestsAsync(page, pageSize, requestTypeId, statusId, submittedBy);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get HR request by ID
    /// </summary>
    /// <param name="id">HR request ID</param>
    /// <returns>HR request details</returns>
    /// <response code="200">Returns HR request details</response>
    /// <response code="404">If HR request is not found</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<HRRequestDto>>> GetHRRequest(int id)
    {
        var result = await _hrRequestService.GetHRRequestByIdAsync(id);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return NotFound(result);
    }

    /// <summary>
    /// Create a new HR request
    /// </summary>
    /// <param name="createDto">HR request creation data</param>
    /// <returns>Created HR request details</returns>
    /// <response code="201">Returns created HR request details</response>
    /// <response code="400">If creation data is invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<HRRequestDetailDto>>>> CreateHRRequest(CreateHRRequestDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "Invalid data provided",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        var result = await _hrRequestService.CreateHRRequestAsync(createDto);
        
        if (result.Success)
        {
            return CreatedAtAction(nameof(GetHRRequestDetails), new { requestId = result.Data?.FirstOrDefault()?.ParentRequestId }, result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Create a new HR request for multiple employees with the same request type
    /// </summary>
    /// <param name="createDto">Multi-employee HR request creation data</param>
    /// <returns>Created HR request details</returns>
    /// <response code="201">Returns created HR request details</response>
    /// <response code="400">If creation data is invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPost("multi-employee")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<HRRequestDetailDto>>>> CreateMultiEmployeeHRRequest(CreateMultiEmployeeHRRequestDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "Invalid data provided",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        if (createDto.EmployeeIds == null || !createDto.EmployeeIds.Any())
        {
            return BadRequest(new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "At least one employee ID must be provided"
            });
        }

        var result = await _hrRequestService.CreateMultiEmployeeHRRequestAsync(createDto);
        
        if (result.Success)
        {
            return CreatedAtAction(nameof(GetHRRequestDetails), new { requestId = result.Data?.FirstOrDefault()?.ParentRequestId }, result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Update an existing HR request
    /// </summary>
    /// <param name="id">HR request ID</param>
    /// <param name="updateDto">HR request update data</param>
    /// <returns>Updated HR request</returns>
    /// <response code="200">Returns updated HR request</response>
    /// <response code="400">If update data is invalid</response>
    /// <response code="404">If HR request is not found</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<HRRequestDto>>> UpdateHRRequest(int id, UpdateHRRequestDto updateDto)
    {
        if (id != updateDto.Id)
        {
            return BadRequest(new ApiResponse<HRRequestDto>
            {
                Success = false,
                Message = "ID mismatch between route and body"
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<HRRequestDto>
            {
                Success = false,
                Message = "Invalid data provided",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        var result = await _hrRequestService.UpdateHRRequestAsync(updateDto);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return result.Message?.Contains("not found") == true ? NotFound(result) : BadRequest(result);
    }

    /// <summary>
    /// Delete an HR request (soft delete)
    /// </summary>
    /// <param name="id">HR request ID</param>
    /// <returns>Success indicator</returns>
    /// <response code="200">Returns success indicator</response>
    /// <response code="404">If HR request is not found</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteHRRequest(int id)
    {
        var result = await _hrRequestService.DeleteHRRequestAsync(id);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return result.Message?.Contains("not found") == true ? NotFound(result) : BadRequest(result);
    }

    /// <summary>
    /// Get HR requests by status
    /// </summary>
    /// <param name="statusId">Status ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 25, max: 100)</param>
    /// <returns>Paginated list of HR requests</returns>
    /// <response code="200">Returns paginated HR requests</response>
    /// <response code="400">If parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("status/{statusId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<List<HRRequestDto>>>> GetHRRequestsByStatus(
        int statusId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 25;

        var result = await _hrRequestService.GetHRRequestsByStatusAsync(statusId, page, pageSize);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get HR requests by submitter
    /// </summary>
    /// <param name="submittedBy">Submitter user ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 25, max: 100)</param>
    /// <returns>Paginated list of HR requests</returns>
    /// <response code="200">Returns paginated HR requests</response>
    /// <response code="400">If parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("submitter/{submittedBy}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<List<HRRequestDto>>>> GetHRRequestsBySubmitter(
        int submittedBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 25;

        var result = await _hrRequestService.GetHRRequestsBySubmitterAsync(submittedBy, page, pageSize);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get HR requests by type
    /// </summary>
    /// <param name="requestTypeId">Request type ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 25, max: 100)</param>
    /// <returns>Paginated list of HR requests</returns>
    /// <response code="200">Returns paginated HR requests</response>
    /// <response code="400">If parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("type/{requestTypeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<List<HRRequestDto>>>> GetHRRequestsByType(
        int requestTypeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 25;

        var result = await _hrRequestService.GetHRRequestsByTypeAsync(requestTypeId, page, pageSize);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get HR request details for a specific request
    /// </summary>
    /// <param name="requestId">HR request ID</param>
    /// <returns>List of HR request details</returns>
    /// <response code="200">Returns HR request details</response>
    /// <response code="404">If HR request is not found</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("{requestId}/details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<HRRequestDetailDto>>>> GetHRRequestDetails(int requestId)
    {
        var result = await _hrRequestService.GetHRRequestDetailsAsync(requestId);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return NotFound(result);
    }

    /// <summary>
    /// Update HR request detail status and processing information
    /// </summary>
    /// <param name="detailId">HR request detail ID</param>
    /// <param name="updateDto">Update data</param>
    /// <returns>Updated HR request detail</returns>
    /// <response code="200">Returns updated HR request detail</response>
    /// <response code="400">If update data is invalid</response>
    /// <response code="404">If HR request detail is not found</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPut("details/{detailId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<HRRequestDetailDto>>> UpdateHRRequestDetail(
        int detailId, 
        UpdateHRRequestDetailDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<HRRequestDetailDto>
            {
                Success = false,
                Message = "Invalid data provided",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        var result = await _hrRequestService.UpdateHRRequestDetailAsync(detailId, updateDto);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return result.Message?.Contains("not found") == true ? NotFound(result) : BadRequest(result);
    }

    /// <summary>
    /// Update effective date for all details under a parent HR request
    /// </summary>
    [HttpPut("{parentId}/effective-date")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateEffectiveDate(
        int parentId,
        [FromBody] UpdateEffectiveDateDto updateDto)
    {
        if (updateDto == null || string.IsNullOrEmpty(updateDto.EffectiveDate))
        {
            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "Effective date is required"
            });
        }

        var result = await _hrRequestService.UpdateEffectiveDateByParentIdAsync(parentId, updateDto.EffectiveDate);

        if (result.Success)
        {
            return Ok(result);
        }

        return result.Message?.Contains("not found") == true ? NotFound(result) : BadRequest(result);
    }

    /// <summary>
    /// Process HR request detail with Viewpoint integration
    /// </summary>
    /// <param name="detailId">HR request detail ID</param>
    /// <returns>Processing result</returns>
    /// <response code="200">Returns processing result</response>
    /// <response code="404">If HR request detail is not found</response>
    /// <response code="500">If processing fails</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPost("details/{detailId}/process")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> ProcessHRRequestDetail(int detailId)
    {
        var result = await _hrRequestService.ProcessHRRequestDetailAsync(detailId);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        if (result.Message?.Contains("not found") == true)
        {
            return NotFound(result);
        }
        
        return StatusCode(500, result);
    }

    /// <summary>
    /// Get all HR request details across all request types with optional filtering, pagination, and sorting
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 25, max: 100)</param>
    /// <param name="requestTypeId">Optional filter by request type ID</param>
    /// <param name="statusId">Optional filter by status ID</param>
    /// <param name="employeeId">Optional filter by employee ID</param>
    /// <param name="submittedBy">Optional filter by submitter user ID</param>
    /// <param name="searchTerm">Optional search term to filter results</param>
    /// <param name="sortField">Optional field to sort by (requestTypeName, employeeName, effectiveDate, requestStatusName, submittedByName)</param>
    /// <param name="sortDirection">Optional sort direction (asc or desc, default: asc)</param>
    /// <returns>Paginated list of HR request details</returns>
    /// <response code="200">Returns paginated HR request details</response>
    /// <response code="400">If parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<List<HRRequestDetailDto>>>> GetAllHRRequestDetails(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] int? requestTypeId = null,
        [FromQuery] int? statusId = null,
        [FromQuery] int? employeeId = null,
        [FromQuery] int? submittedBy = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortField = null,
        [FromQuery] string? sortDirection = "asc")
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 25;

        // Validate sort direction
        if (!string.IsNullOrEmpty(sortDirection) && 
            !sortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase) && 
            !sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase))
        {
            sortDirection = "asc";
        }

        var result = await _hrRequestService.GetAllHRRequestDetailsAsync(page, pageSize, requestTypeId, statusId, employeeId, submittedBy, searchTerm, sortField, sortDirection);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Get current user's HR requests
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 25, max: 100)</param>
    /// <returns>Paginated list of current user's HR requests</returns>
    /// <response code="200">Returns paginated HR requests</response>
    /// <response code="400">If parameters are invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("my-requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<List<HRRequestDto>>>> GetMyHRRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 25;

        var currentUserIdString = _userContextService.GetUserId();
        var currentUserId = 1; // TODO: Map string user ID to int user ID
        var result = await _hrRequestService.GetHRRequestsBySubmitterAsync(currentUserId, page, pageSize);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Cancel an HR request detail
    /// </summary>
    /// <param name="detailId">HR request detail ID</param>
    /// <returns>Cancel result</returns>
    /// <response code="200">Request detail cancelled successfully</response>
    /// <response code="400">If request detail cannot be cancelled or is invalid</response>
    /// <response code="404">If request detail is not found</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPost("details/{detailId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<HRRequestDetailDto>>> CancelHRRequestDetail(int detailId)
    {
        var result = await _hrRequestService.CancelHRRequestDetailAsync(detailId);

        if (result.Success)
        {
            return Ok(result);
        }

        if (result.Message?.Contains("not found") == true)
        {
            return NotFound(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Retry a failed HR request detail
    /// </summary>
    /// <param name="detailId">HR request detail ID</param>
    /// <returns>Retry result</returns>
    /// <response code="200">Request detail retry initiated successfully</response>
    /// <response code="400">If request detail cannot be retried or is invalid</response>
    /// <response code="404">If request detail is not found</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPost("details/{detailId}/retry")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> RetryHRRequestDetail(int detailId)
    {
        var result = await _hrRequestService.RetryHRRequestDetailAsync(detailId);

        if (result.Success)
        {
            return Ok(result);
        }

        if (result.Message?.Contains("not found") == true)
        {
            return NotFound(result);
        }

        return BadRequest(result);
    }
}