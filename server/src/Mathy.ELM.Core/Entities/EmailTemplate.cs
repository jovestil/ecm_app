namespace Mathy.ELM.Core.Entities;

public class EmailTemplate : BaseEntity
{
    public string TemplateName { get; set; } = string.Empty; // 'NEWHIRE-Confirmation', 'NEWHIRE-TaskEmail-01', ..., 'NEWHIRE-TaskEmail-07', 'NEWHIRE-ReminderEmail', ...
    public string RequestType { get; set; } = string.Empty; // 'NEWHIRE', 'LAY-OFF', 'TERMINATION'
    public string EmailType { get; set; } = string.Empty; // 'NOTIFICATION', 'ERROR', 'WARNING'
    public string Recipients { get; set; } = string.Empty; // 'ITDL, HRDL, EMPLOYEE'
    public string Subject { get; set; } = string.Empty; // 'see NOTES'
    public string Body { get; set; } = string.Empty; // 'see CONTENT tab'
    public string TriggerType { get; set; } = string.Empty; // 'Immediate', 'Scheduled'
    public int? SubmissionFreq { get; set; } // -3 days before, 7 days after, 0 on that day
    public string ContentStyling { get; set; } = string.Empty; // TableColumn, TableRow, Text
    public bool IsActive { get; set; } = true;

    public virtual ICollection<NotificationQueue> Notifications { get; set; } = new List<NotificationQueue>();
}