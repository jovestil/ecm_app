namespace Mathy.ELM.Core.Entities;

/// <summary>
/// Maps employment statuses between different request types (Active, LayOff, ReturnToWork, Termination)
/// </summary>
public class EmploymentStatusMapper
{
    public int Id { get; set; }

    /// <summary>
    /// The active employment status (e.g., "FULL TIME", "PART-TIME", "U-ACTIVE")
    /// </summary>
    public string ActiveStatus { get; set; } = string.Empty;

    /// <summary>
    /// The corresponding layoff status (e.g., "LAYOFF-B", "LAYOFF-NB", "U-LAYOFF")
    /// </summary>
    public string LayOffStatus { get; set; } = string.Empty;

    /// <summary>
    /// The return to work status (typically same as ActiveStatus)
    /// </summary>
    public string ReturnToWorkStatus { get; set; } = string.Empty;

    /// <summary>
    /// The termination status (e.g., "TERM", "U-TERM")
    /// </summary>
    public string TerminationStatus { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is a union status mapping
    /// </summary>
    public bool IsUnion { get; set; } = false;

    // Audit Fields
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
