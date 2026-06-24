using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Services;

namespace Mathy.ELM.Api.Controllers;

/// <summary>
/// Email notification testing endpoints for Azure Service Bus
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class EmailTestController : ControllerBase
{
    private readonly IAzureServiceBusEmailService _azureServiceBusEmailService;
    private readonly ILogger<EmailTestController> _logger;

    public EmailTestController(
        IAzureServiceBusEmailService azureServiceBusEmailService,
        ILogger<EmailTestController> logger)
    {
        _azureServiceBusEmailService = azureServiceBusEmailService;
        _logger = logger;
    }

    /// <summary>
    /// Send a single test email notification to Azure Service Bus queue
    /// </summary>
    /// <param name="emailNotification">Email notification data</param>
    /// <returns>Success status</returns>
    /// <response code="200">Email notification queued successfully</response>
    /// <response code="400">If email data is invalid</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="500">If queueing fails</response>
    [HttpPost("send-single")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> SendSingleEmail([FromBody] EmailNotificationDto emailNotification)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            });
        }

        _logger.LogInformation("[EMAIL TEST] Sending single test email to {ToEmail}", emailNotification.ToEmail);

        try
        {
            var result = await _azureServiceBusEmailService.SendEmailNotificationAsync(emailNotification);

            if (result.Success)
            {
                _logger.LogInformation("[EMAIL TEST] Successfully queued email to {ToEmail}", emailNotification.ToEmail);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("[EMAIL TEST] Failed to queue email to {ToEmail}: {Message}",
                    emailNotification.ToEmail, result.Message);
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL TEST] Exception occurred while queueing email to {ToEmail}",
                emailNotification.ToEmail);

            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"An error occurred while queueing email: {ex.Message}",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Send multiple test email notifications to Azure Service Bus queue
    /// </summary>
    /// <param name="emailNotifications">List of email notifications</param>
    /// <returns>Success status with count of queued messages</returns>
    /// <response code="200">Email notifications queued (partial or full success)</response>
    /// <response code="400">If email data is invalid</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="500">If queueing fails completely</response>
    [HttpPost("send-bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<int>>> SendBulkEmails([FromBody] List<EmailNotificationDto> emailNotifications)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new ApiResponse<int>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            });
        }

        if (emailNotifications == null || !emailNotifications.Any())
        {
            return BadRequest(new ApiResponse<int>
            {
                Success = false,
                Data = 0,
                Message = "At least one email notification is required"
            });
        }

        _logger.LogInformation("[EMAIL TEST] Sending {Count} bulk test emails", emailNotifications.Count);

        try
        {
            var result = await _azureServiceBusEmailService.SendBulkEmailNotificationsAsync(emailNotifications);

            if (result.Success)
            {
                _logger.LogInformation("[EMAIL TEST] Successfully queued {Count}/{Total} bulk emails",
                    result.Data, emailNotifications.Count);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("[EMAIL TEST] Bulk email queueing completed with errors. Queued: {Count}/{Total}",
                    result.Data, emailNotifications.Count);
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL TEST] Exception occurred while queueing bulk emails");

            return StatusCode(500, new ApiResponse<int>
            {
                Success = false,
                Data = 0,
                Message = $"An error occurred while queueing bulk emails: {ex.Message}",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Check Azure Service Bus queue status
    /// </summary>
    /// <returns>Queue existence status</returns>
    /// <response code="200">Queue status retrieved</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="500">If status check fails</response>
    [HttpGet("queue-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> GetQueueStatus()
    {
        _logger.LogInformation("[EMAIL TEST] Checking Azure Service Bus queue status");

        try
        {
            var queueExists = await _azureServiceBusEmailService.EnsureQueueExistsAsync();

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = queueExists,
                Message = queueExists
                    ? "Azure Service Bus queue is ready and configured"
                    : "Azure Service Bus queue could not be verified or created"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL TEST] Exception occurred while checking queue status");

            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"An error occurred while checking queue status: {ex.Message}",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
