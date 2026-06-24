using System.ComponentModel.DataAnnotations;

namespace Mathy.ELM.Core.DTOs;

/// <summary>
/// Request DTO for scheduling a Viewpoint status update job
/// </summary>
public class ScheduleViewpointStatusUpdateRequest
{
    /// <summary>
    /// The effective date when the status update should take place
    /// </summary>
    [Required]
    public DateTime EffectiveDate { get; set; }
}