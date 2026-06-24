using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Core.Entities;
using Mathy.ELM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mathy.ELM.Infrastructure.Services;

public class AzureEmailService : IEmailService
{
    private readonly EmailClient _emailClient;
    private readonly MathyELMContext _context;
    private readonly ILogger<AzureEmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _fromAddress;

    public AzureEmailService(
        IConfiguration configuration,
        MathyELMContext context,
        ILogger<AzureEmailService> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
        
        var connectionString = configuration["AzureEmail:ConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Azure Email connection string is not configured");
        }
        
        _fromAddress = configuration["AzureEmail:FromAddress"] ?? "DoNotReply@yourdomain.com";
        _emailClient = new EmailClient(connectionString);
    }

    public async Task<ApiResponse<bool>> SendEmailAsync(string toEmail, string subject, string body, string? ccEmail = null)
    {
        try
        {
            var emailMessage = new EmailMessage(
                senderAddress: _fromAddress,
                content: new EmailContent(subject)
                {
                    PlainText = body,
                    Html = body
                },
                recipients: new EmailRecipients(new List<EmailAddress> { new EmailAddress(toEmail) }));

            if (!string.IsNullOrEmpty(ccEmail))
            {
                emailMessage.Recipients.CC.Add(new EmailAddress(ccEmail));
            }

            _logger.LogInformation("Sending email to {ToEmail} with subject: {Subject}", toEmail, subject);
            
            var response = await _emailClient.SendAsync(Azure.WaitUntil.Completed, emailMessage);
            
            if (response.HasCompleted && !response.HasValue)
            {
                _logger.LogError("Email sending failed for {ToEmail}", toEmail);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to send email",
                    Errors = ["Email sending operation failed"]
                };
            }

            _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Email sent successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {ToEmail}", toEmail);
            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = "Error sending email",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<bool>> SendEmailFromTemplateAsync(int templateId, string toEmail, Dictionary<string, string> templateData, string? ccEmail = null)
    {
        try
        {
            var template = await _context.EmailTemplates
                .Where(t => t.Id == templateId && t.IsActive && !t.IsDeleted)
                .FirstOrDefaultAsync();

            if (template == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Email template with ID {templateId} not found",
                    Errors = ["Template not found"]
                };
            }

            // Replace template placeholders with actual data
            var subject = template.Subject;
            var body = template.Body;

            foreach (var kvp in templateData)
            {
                subject = subject.Replace($"{{{kvp.Key}}}", kvp.Value);
                body = body.Replace($"{{{kvp.Key}}}", kvp.Value);
            }

            return await SendEmailAsync(toEmail, subject, body, ccEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email from template {TemplateId} to {ToEmail}", templateId, toEmail);
            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = "Error sending email from template",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<bool>> ProcessNotificationQueueAsync()
    {
        try
        {
            var pendingNotifications = await _context.NotificationQueue
                .Where(n => n.Status == "Pending" && n.AttemptCount < 3)
                .Include(n => n.EmailTemplate)
                .Include(n => n.HRRequest)
                .OrderBy(n => n.CreatedDate)
                .Take(10) // Process 10 at a time
                .ToListAsync();

            var successCount = 0;
            var failureCount = 0;

            foreach (var notification in pendingNotifications)
            {
                try
                {
                    notification.AttemptCount++;
                    notification.LastAttempt = DateTime.UtcNow;
                    
                    var result = await SendEmailAsync(notification.ToEmail, notification.Subject, notification.Body, notification.CcEmail);
                    
                    if (result.Success)
                    {
                        notification.Status = "Sent";
                        notification.SentDate = DateTime.UtcNow;
                        successCount++;
                    }
                    else
                    {
                        notification.Status = notification.AttemptCount >= 3 ? "Failed" : "Pending";
                        notification.ErrorMessage = string.Join("; ", result.Errors);
                        failureCount++;
                    }
                }
                catch (Exception ex)
                {
                    notification.Status = notification.AttemptCount >= 3 ? "Failed" : "Pending";
                    notification.ErrorMessage = ex.Message;
                    failureCount++;
                    _logger.LogError(ex, "Error processing notification {NotificationId}", notification.Id);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed {Total} notifications: {Success} sent, {Failed} failed", 
                pendingNotifications.Count, successCount, failureCount);

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = $"Processed {pendingNotifications.Count} notifications: {successCount} sent, {failureCount} failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification queue");
            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = "Error processing notification queue",
                Errors = [ex.Message]
            };
        }
    }
}