namespace Mathy.ELM.Core.Entities;

public class NotificationQueue : BaseEntity
{
    public int RequestId { get; set; }
    public int? TemplateId { get; set; }

    public string ToEmail { get; set; } = string.Empty;
    public string? CcEmail { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    
    public string Status { get; set; } = "Pending"; // 'Pending', 'Sent', 'Failed'
    public int AttemptCount { get; set; } = 0;
    public DateTime? LastAttempt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentDate { get; set; }
    
    public virtual HRRequest HRRequest { get; set; } = null!;
    public virtual EmailTemplate? EmailTemplate { get; set; }
}