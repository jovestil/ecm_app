using Microsoft.Extensions.Logging;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Services;

namespace Mathy.ELM.Infrastructure.Services;

public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> SendEmailAsync(string toEmail, string subject, string body, string? ccEmail = null)
    {
        _logger.LogInformation("MockEmailService: Would send email to {ToEmail}, Subject: {Subject}, CC: {CcEmail}",
            toEmail, subject, ccEmail ?? "None");

        // Simulate async operation
        await Task.Delay(1);

        return new ApiResponse<bool>
        {
            Success = true,
            Data = true,
            Message = "Email sent successfully (mocked)"
        };
    }

    public async Task<ApiResponse<bool>> SendEmailFromTemplateAsync(int templateId, string toEmail, Dictionary<string, string> templateData, string? ccEmail = null)
    {
        _logger.LogInformation("MockEmailService: Would send template email (ID: {TemplateId}) to {ToEmail}, CC: {CcEmail}, Template Data: {TemplateDataCount} items",
            templateId, toEmail, ccEmail ?? "None", templateData?.Count ?? 0);

        // Simulate async operation
        await Task.Delay(1);

        return new ApiResponse<bool>
        {
            Success = true,
            Data = true,
            Message = "Template email sent successfully (mocked)"
        };
    }

    public async Task<ApiResponse<bool>> ProcessNotificationQueueAsync()
    {
        _logger.LogInformation("MockEmailService: Would process notification queue");

        // Simulate async operation
        await Task.Delay(1);

        return new ApiResponse<bool>
        {
            Success = true,
            Data = true,
            Message = "Notification queue processed successfully (mocked)"
        };
    }
}