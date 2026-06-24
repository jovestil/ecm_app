using Microsoft.AspNetCore.Mvc;
using Mathy.ELM.Core.Services;
using Microsoft.AspNetCore.Authorization;

namespace Mathy.ELM.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;
    private readonly INotificationService _notificationService;

    public TestController(
        ILogger<TestController> logger,
        INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Simple test endpoint to verify controller is working
    /// </summary>
    /// <returns>Success response with timestamp</returns>
    /// <response code="200">Controller is working properly</response>
    [HttpGet("ping")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult Ping()
    {
        return Ok(new
        {
            success = true,
            message = "TestController is working!",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test SignalR status update notification
    /// </summary>
    /// <param name="request">The status update test request containing user ID, HR request detail ID, status, employee name, and message</param>
    /// <returns>Success response with notification details</returns>
    /// <response code="200">SignalR status update notification sent successfully</response>
    /// <response code="400">Invalid request or error sending notification</response>
    [HttpPost("signalr/status-update")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<IActionResult> TestSignalRStatusUpdate([FromBody] TestSignalRStatusUpdateRequest request)
    {
        try
        {
            _logger.LogInformation("Testing SignalR status update notification for User: {UserId}, HRRequestDetailId: {HRRequestDetailId}", 
                request.UserId, request.HRRequestDetailId);

            await _notificationService.SendHRRequestStatusUpdateAsync(
                "system",
                request.HRRequestDetailId,
                request.Status,
                request.EmployeeName,
                request.Message
            );

            return Ok(new
            {
                success = true,
                message = "SignalR status update notification sent successfully",
                data = new
                {
                    userId = request.UserId,
                    hrRequestDetailId = request.HRRequestDetailId,
                    status = request.Status,
                    employeeName = request.EmployeeName,
                    notificationMessage = request.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SignalR status update notification");
            return BadRequest(new
            {
                success = false,
                message = "Failed to send SignalR status update notification",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Test SignalR completion notification
    /// </summary>
    /// <param name="request">The completion test request containing user ID, HR request detail ID, employee name, success status, and message</param>
    /// <returns>Success response with notification details</returns>
    /// <response code="200">SignalR completion notification sent successfully</response>
    /// <response code="400">Invalid request or error sending notification</response>
    [HttpPost("signalr/completion")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<IActionResult> TestSignalRCompletion([FromBody] TestSignalRCompletionRequest request)
    {
        try
        {
            _logger.LogInformation("Testing SignalR completion notification for User: {UserId}, HRRequestDetailId: {HRRequestDetailId}, Success: {IsSuccess}", 
                request.UserId, request.HRRequestDetailId, request.IsSuccess);

            await _notificationService.SendHRRequestCompletionNotificationAsync(
                request.UserId,
                request.HRRequestDetailId,
                request.EmployeeName,
                request.IsSuccess,
                request.Message
            );

            return Ok(new
            {
                success = true,
                message = "SignalR completion notification sent successfully",
                data = new
                {
                    userId = request.UserId,
                    hrRequestDetailId = request.HRRequestDetailId,
                    employeeName = request.EmployeeName,
                    isSuccess = request.IsSuccess,
                    notificationMessage = request.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SignalR completion notification");
            return BadRequest(new
            {
                success = false,
                message = "Failed to send SignalR completion notification",
                error = ex.Message
            });
        }
    }
}

/// <summary>
/// Request model for testing SignalR status update notifications
/// </summary>
public class TestSignalRStatusUpdateRequest
{
    /// <summary>
    /// The user ID to send the notification to
    /// </summary>
    /// <example>user-123</example>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// The HR request detail ID for the notification
    /// </summary>
    /// <example>12345</example>
    public int HRRequestDetailId { get; set; }
    
    /// <summary>
    /// The status to update to (e.g., "Pending", "InProgress")
    /// </summary>
    /// <example>InProgress</example>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// The name of the employee being processed
    /// </summary>
    /// <example>John Doe</example>
    public string EmployeeName { get; set; } = string.Empty;
    
    /// <summary>
    /// The message to include in the notification
    /// </summary>
    /// <example>Employee status update is now in progress</example>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request model for testing SignalR completion notifications
/// </summary>
public class TestSignalRCompletionRequest
{
    /// <summary>
    /// The user ID to send the notification to
    /// </summary>
    /// <example>user-123</example>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// The HR request detail ID for the notification
    /// </summary>
    /// <example>12345</example>
    public int HRRequestDetailId { get; set; }
    
    /// <summary>
    /// The name of the employee being processed
    /// </summary>
    /// <example>John Doe</example>
    public string EmployeeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    /// <example>true</example>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// The completion message to include in the notification
    /// </summary>
    /// <example>Employee status update completed successfully</example>
    public string Message { get; set; } = string.Empty;
}