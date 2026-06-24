namespace Mathy.ELM.Core.DTOs;

/// <summary>
/// DTO for email notification data sent to Azure Service Bus queue
/// </summary>
public class EmailNotificationDto
{
    /// <summary>
    /// Primary recipient email address
    /// </summary>
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// Carbon copy email addresses (comma-separated)
    /// </summary>
    public string? CcEmail { get; set; }

    /// <summary>
    /// Email subject line
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Email body content (HTML or plain text)
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Associated HR request ID for tracking
    /// </summary>
    public int? RequestId { get; set; }

    /// <summary>
    /// Email template ID if using template-based email
    /// </summary>
    public int? TemplateId { get; set; }

    /// <summary>
    /// Notification type (e.g., 'Confirmation', 'Task', 'Reminder', 'Welcome', 'Draft')
    /// Maps to notification scenarios from notification_table_md.md
    /// </summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// Priority level for email processing (1=High, 2=Normal, 3=Low)
    /// </summary>
    public int Priority { get; set; } = 2; // Default to Normal priority

    /// <summary>
    /// Template data for dynamic content replacement (key-value pairs)
    /// Used when TemplateId is specified
    /// </summary>
    public Dictionary<string, string>? TemplateData { get; set; }

    /// <summary>
    /// Module that triggered the notification (e.g., 'NewHire', 'Promotion', 'Termination')
    /// </summary>
    public string? Module { get; set; }

    /// <summary>
    /// Specific trigger that caused the notification (e.g., 'OnSubmission', 'StartDate', 'DoorAccessRequested')
    /// </summary>
    public string? Trigger { get; set; }
}
