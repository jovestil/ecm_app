using Mathy.ELM.Core.DTOs;

namespace Mathy.ELM.Core.Services;

public interface IEmailService
{
    Task<ApiResponse<bool>> SendEmailAsync(string toEmail, string subject, string body, string? ccEmail = null);
    Task<ApiResponse<bool>> SendEmailFromTemplateAsync(int templateId, string toEmail, Dictionary<string, string> templateData, string? ccEmail = null);
    Task<ApiResponse<bool>> ProcessNotificationQueueAsync();
}