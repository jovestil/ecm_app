using Mathy.ELM.Core.DTOs;

namespace Mathy.ELM.Core.Services;

/// <summary>
/// Producer-only service for sending email notifications to Azure Service Bus queue
/// Messages are consumed and processed by Power Automate to send actual emails
/// </summary>
public interface IAzureServiceBusEmailService
{
    /// <summary>
    /// Send a single email notification to the Azure Service Bus queue
    /// </summary>
    /// <param name="emailNotification">Email notification data</param>
    /// <returns>Success status</returns>
    Task<ApiResponse<bool>> SendEmailNotificationAsync(EmailNotificationDto emailNotification);

    /// <summary>
    /// Send multiple email notifications to the Azure Service Bus queue
    /// </summary>
    /// <param name="emailNotifications">List of email notifications</param>
    /// <returns>Success status with count of successfully queued messages</returns>
    Task<ApiResponse<int>> SendBulkEmailNotificationsAsync(List<EmailNotificationDto> emailNotifications);

    /// <summary>
    /// Ensure the queue exists in Azure Service Bus, creating it if necessary
    /// </summary>
    Task<bool> EnsureQueueExistsAsync();

    /// <summary>
    /// Send an email using an EmailTemplate by template name
    /// Parses the template's Body field (comma-delimited field list), maps fields to request data,
    /// generates HTML email body, and sends via Azure Service Bus
    /// </summary>
    /// <param name="templateName">Name of the email template (e.g., "NEWHIRE-Confirmation")</param>
    /// <param name="requestData">The New Hire request data
    /// <param name="toEmail">Primary recipient email address</param>
    /// <param name="ccEmail">Optional CC email addresses (comma-separated)</param>
    /// <param name="requestId">Optional HR Request ID for audit logging</param>
    /// <returns>Success status</returns>
    Task<ApiResponse<bool>> SendEmailFromTemplateNameAsync(
        string templateName,
        CreateNewHireRequestDto requestData,
        string toEmail,
        string? ccEmail = null,
        int? requestId = null);

    /// <summary>
    /// Send an email using an EmailTemplate by template name for Promotion requests
    /// </summary>
    Task<ApiResponse<bool>> SendEmailFromTemplateNameForPromotionAsync(
        string templateName,
        CreatePromotionRequestDto requestData,
        string toEmail,
        string? ccEmail = null,
        int? requestId = null);

    /// <summary>
    /// Send an email using an EmailTemplate by template name for Termination requests
    /// </summary>
    Task<ApiResponse<bool>> SendEmailFromTemplateNameForTerminationAsync(
        string templateName,
        TerminationEmailDataDto requestData,
        string toEmail,
        string? ccEmail = null,
        int? requestId = null);

    /// <summary>
    /// Send an email using an EmailTemplate by template name for Layoff requests
    /// </summary>
    Task<ApiResponse<bool>> SendEmailFromTemplateNameForLayoffAsync(
        string templateName,
        LayoffEmailDataDto requestData,
        string toEmail,
        string? ccEmail = null,
        int? requestId = null);

    /// <summary>
    /// Send an email using an EmailTemplate by template name for ReturnToWork requests
    /// </summary>
    Task<ApiResponse<bool>> SendEmailFromTemplateNameForReturnToWorkAsync(
        string templateName,
        ReturnToWorkEmailDataDto requestData,
        string toEmail,
        string? ccEmail = null,
        int? requestId = null);
}
