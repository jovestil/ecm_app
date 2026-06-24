namespace Mathy.ELM.Core.DTOs;

public class PositionSyncResultDto
{
    public int TotalViewpointPositions { get; set; }
    public int NewPositionsAdded { get; set; }
    public int ExistingPositionsUpdated { get; set; }
    public int PositionsDeactivated { get; set; }
    public DateTime SyncDate { get; set; }
    public TimeSpan SyncDuration { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
    public bool Success => Errors.Count == 0;
    
    public string Summary => $"Sync completed: {NewPositionsAdded} added, {ExistingPositionsUpdated} updated, {PositionsDeactivated} deactivated from {TotalViewpointPositions} Viewpoint positions";
}