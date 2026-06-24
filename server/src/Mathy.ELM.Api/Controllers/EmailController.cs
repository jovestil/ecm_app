using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Core.DTOs;

namespace Mathy.ELM.Api.Controllers;

/// <summary>
/// Email testing and management endpoints
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(IEmailService emailService, IUserContextService userContextService, ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _userContextService = userContextService;
        _logger = logger;
    }

    /// <summary>
    /// Send a test email to specified recipient
    /// </summary>
    /// <param name="request">Email send request containing recipient and optional message details</param>
    /// <returns>Result of email send operation</returns>
    /// <response code="200">Email sent successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="500">Email sending failed</response>
    [HttpPost("send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> SendEmail([FromBody] SendEmailRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.Recipient))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Invalid request: Recipient is required",
                    Errors = ["Recipient email address is required"]
                });
            }

            // Validate email format
            if (!IsValidEmail(request.Recipient))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Invalid email format",
                    Errors = ["Recipient email address format is invalid"]
                });
            }

            // Set default values if not provided
            var subject = string.IsNullOrEmpty(request.Subject) 
                ? "Test Email from HR System" 
                : request.Subject;
            
            var body = string.IsNullOrEmpty(request.Body)
                ? $"This is a test email from the HR Change Management System.\n\nSent at: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC\n\nBest regards,\nHR System"
                : request.Body;

            _logger.LogInformation("Sending test email to {Recipient} with subject: {Subject}", request.Recipient, subject);

            var result = await _emailService.SendEmailAsync(request.Recipient, subject, body, request.CcEmail);

            if (result.Success)
            {
                _logger.LogInformation("Test email sent successfully to {Recipient}", request.Recipient);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Failed to send test email to {Recipient}. Errors: {Errors}", 
                    request.Recipient, string.Join("; ", result.Errors));
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test email to {Recipient}", request?.Recipient);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = "Error sending email",
                Errors = [ex.Message]
            });
        }
    }

    /// <summary>
    /// Send an email to the currently logged-in user
    /// </summary>
    /// <param name="request">Email content for the logged-in user</param>
    /// <returns>Result of email send operation</returns>
    /// <response code="200">Email sent successfully</response>
    /// <response code="400">Invalid request data or user email not found</response>
    /// <response code="500">Email sending failed</response>
    [HttpPost("send-to-me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> SendEmailToMe([FromBody] SendEmailToMeRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Invalid request: Request body is required",
                    Errors = ["Request body cannot be null"]
                });
            }

            // Get the logged-in user's email from JWT token
            var userEmail = _userContextService.GetUserEmail();
            if (string.IsNullOrEmpty(userEmail))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "User email not found in access token",
                    Errors = ["Cannot retrieve email address from JWT token"]
                });
            }

            // Set default values if not provided
            var subject = string.IsNullOrEmpty(request.Subject) 
                ? "Test Email to Your Account" 
                : request.Subject;
            
            var body = string.IsNullOrEmpty(request.Body)
                ? $"Hello {_userContextService.GetUserName() ?? "User"},\n\n" +
                  $"This is a test email sent to your account from the HR Change Management System.\n\n" +
                  $"Your Details:\n" +
                  $"- Email: {userEmail}\n" +
                  $"- User ID: {_userContextService.GetUserId()}\n" +
                  $"- Companies: {string.Join(", ", _userContextService.GetUserCompanies())}\n" +
                  $"- Roles: {string.Join(", ", _userContextService.GetUserRoles())}\n\n" +
                  $"Sent at: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC\n\n" +
                  $"Best regards,\nHR System"
                : request.Body;

            _logger.LogInformation("Sending email to logged-in user {UserEmail} with subject: {Subject}", userEmail, subject);

            var result = await _emailService.SendEmailAsync(userEmail, subject, body);

            if (result.Success)
            {
                _logger.LogInformation("Email sent successfully to logged-in user {UserEmail}", userEmail);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Failed to send email to logged-in user {UserEmail}. Errors: {Errors}", 
                    userEmail, string.Join("; ", result.Errors));
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to logged-in user");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = "Error sending email to logged-in user",
                Errors = [ex.Message]
            });
        }
    }

    /// <summary>
    /// Process pending email notifications in the queue
    /// </summary>
    /// <returns>Result of processing notification queue</returns>
    /// <response code="200">Queue processed successfully</response>
    /// <response code="500">Error processing queue</response>
    [HttpPost("process-queue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> ProcessNotificationQueue()
    {
        try
        {
            _logger.LogInformation("Processing email notification queue");
            
            var result = await _emailService.ProcessNotificationQueueAsync();
            
            if (result.Success)
            {
                _logger.LogInformation("Email notification queue processed successfully");
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Failed to process email notification queue. Errors: {Errors}", 
                    string.Join("; ", result.Errors));
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email notification queue");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = "Error processing notification queue",
                Errors = [ex.Message]
            });
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Request model for sending emails
/// </summary>
public class SendEmailRequest
{
    /// <summary>
    /// Email address of the recipient (required)
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Email subject line (optional, default provided)
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Email body content (optional, default provided)
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// CC email address (optional)
    /// </summary>
    public string? CcEmail { get; set; }
}

/// <summary>
/// Request model for sending emails to logged-in user
/// </summary>
public class SendEmailToMeRequest
{
    /// <summary>
    /// Email subject line (optional, default provided)
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Email body content (optional, default with user details provided)
    /// </summary>
    public string? Body { get; set; }
}