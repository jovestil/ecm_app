namespace Mathy.ELM.Core.DTOs;

/// <summary>
/// Result of a Viewpoint employee status update operation
/// </summary>
public class ViewpointUpdateResult
{
    /// <summary>
    /// Whether the update operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The actual status that was sent to Viewpoint after transformation
    /// This may differ from the requested status due to employee-specific transformations
    /// </summary>
    public string ActualStatusUsed { get; set; } = string.Empty;

    /// <summary>
    /// Number of employees successfully updated
    /// </summary>
    public int SuccessfulUpdateCount { get; set; }

    /// <summary>
    /// Total number of employees in the update request
    /// </summary>
    public int TotalEmployeeCount { get; set; }

    /// <summary>
    /// Action ID for tracking the update in Viewpoint (for single employee updates)
    /// </summary>
    public string? ActionId { get; set; }

    /// <summary>
    /// Action IDs for tracking multiple employee updates in Viewpoint
    /// Each employee update returns its own action ID
    /// </summary>
    public List<string> ActionIds { get; set; } = new List<string>();

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}