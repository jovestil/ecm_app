using System.ComponentModel.DataAnnotations;

namespace Mathy.ELM.Core.Entities;

public class SyncScheduleConfig : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string SyncType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Schedule { get; set; } = "disabled";

    [MaxLength(100)]
    public string? CronExpression { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(200)]
    public string? Description { get; set; }

    public DateTime? LastExecuted { get; set; }

    [MaxLength(500)]
    public string? LastExecutionResult { get; set; }
}