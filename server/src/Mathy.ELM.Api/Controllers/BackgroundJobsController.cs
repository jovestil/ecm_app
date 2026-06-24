using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Core.DTOs;
using Hangfire;

namespace Mathy.ELM.Api.Controllers;

/// <summary>
/// Controller for managing background jobs
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class BackgroundJobsController : ControllerBase
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<BackgroundJobsController> _logger;

    public BackgroundJobsController(
        IBackgroundJobService backgroundJobService,
        IUserContextService userContextService,
        ILogger<BackgroundJobsController> logger)
    {
        _backgroundJobService = backgroundJobService;
        _userContextService = userContextService;
        _logger = logger;
    }

    /// <summary>
    /// Enqueue a notification job for an HR request
    /// </summary>
    /// <param name="hrRequestId">The HR request ID</param>
    /// <returns>Job ID</returns>
    [HttpPost("notifications/{hrRequestId:int}")]
    public IActionResult EnqueueNotificationJob(int hrRequestId)
    {
        try
        {
            var jobId = _backgroundJobService.EnqueueNotificationJob(hrRequestId);
            
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Data = jobId,
                Message = "Notification job enqueued successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing notification job for HR request {HRRequestId}", hrRequestId);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Failed to enqueue notification job"
            });
        }
    }

    /// <summary>
    /// Schedule a follow-up job for an HR request
    /// </summary>
    /// <param name="hrRequestId">The HR request ID</param>
    /// <param name="scheduledDate">When to execute the job</param>
    /// <returns>Job ID</returns>
    [HttpPost("follow-up/{hrRequestId:int}")]
    public IActionResult ScheduleFollowUpJob(int hrRequestId, [FromBody] DateTime scheduledDate)
    {
        try
        {
            var jobId = _backgroundJobService.ScheduleFollowUpJob(hrRequestId, scheduledDate);
            
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Data = jobId,
                Message = "Follow-up job scheduled successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling follow-up job for HR request {HRRequestId}", hrRequestId);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Failed to schedule follow-up job"
            });
        }
    }

    /// <summary>
    /// Schedule a Viewpoint status update job for an HR request detail
    /// </summary>
    /// <param name="hrRequestDetailId">The HR request detail ID</param>
    /// <param name="request">The schedule request containing effective date</param>
    /// <returns>Job ID</returns>
    [HttpPost("viewpoint-status-update/{hrRequestDetailId:int}")]
    public async Task<IActionResult> ScheduleViewpointStatusUpdate(int hrRequestDetailId, [FromBody] ScheduleViewpointStatusUpdateRequest request)
    {
        try
        {
            var submitterEmail = _userContextService.GetUserEmail();
            var jobId = await _backgroundJobService.ScheduleViewpointStatusUpdateJob(hrRequestDetailId, request.EffectiveDate, null, submitterEmail);
            
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Data = jobId,
                Message = "Viewpoint status update job scheduled successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling Viewpoint status update job for HR request detail {HRRequestDetailId}", hrRequestDetailId);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Failed to schedule Viewpoint status update job"
            });
        }
    }

    /// <summary>
    /// Setup the recurring reference data sync job
    /// </summary>
    /// <returns>Success indicator</returns>
    [HttpPost("setup-reference-data-sync")]
    [Authorize(Roles = "SystemAdmin")]
    public IActionResult SetupReferenceDataSync()
    {
        try
        {
            _backgroundJobService.SetupReferenceDataSyncJob();
            
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Reference data sync job setup successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up reference data sync job");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Failed to setup reference data sync job"
            });
        }
    }

    /// <summary>
    /// Manually trigger reference data sync from Viewpoint (for testing)
    /// </summary>
    /// <returns>Job enqueue confirmation</returns>
    /// <response code="200">Job enqueued successfully</response>
    /// <response code="500">If job enqueueing fails</response>
    [HttpPost("reference-data-sync/trigger")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult TriggerReferenceDataSync()
    {
        try
        {
            // Enqueue the sync job to run immediately
            var jobId = BackgroundJob.Enqueue(() => _backgroundJobService.SyncReferenceDataAsync());
            
            _logger.LogInformation("Reference data sync job enqueued with ID: {JobId}", jobId);
            
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Data = jobId,
                Message = $"Reference data sync job enqueued successfully with ID: {jobId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueueing reference data sync job");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Failed to enqueue reference data sync job",
                Errors = [ex.Message]
            });
        }
    }
}