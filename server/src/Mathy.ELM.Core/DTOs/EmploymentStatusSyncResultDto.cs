namespace Mathy.ELM.Core.DTOs;

public class EmploymentStatusSyncResultDto
{
    public int TotalViewpointEmploymentStatuses { get; set; }
    public int NewEmploymentStatusesAdded { get; set; }
    public int ExistingEmploymentStatusesUpdated { get; set; }
    public int EmploymentStatusesDeactivated { get; set; }
    public DateTime SyncDate { get; set; }
    public TimeSpan SyncDuration { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
    public bool Success => Errors.Count == 0;

    public string Summary => $"Sync completed: {NewEmploymentStatusesAdded} added, {ExistingEmploymentStatusesUpdated} updated, {EmploymentStatusesDeactivated} deactivated from {TotalViewpointEmploymentStatuses} Viewpoint employment statuses";
}
