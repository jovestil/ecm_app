namespace Mathy.ELM.Core.DTOs;

public class DepartmentSyncResultDto
{
    public int TotalViewpointDepartments { get; set; }
    public int NewDepartmentsAdded { get; set; }
    public int ExistingDepartmentsUpdated { get; set; }
    public int DepartmentsDeactivated { get; set; }
    public DateTime SyncDate { get; set; }
    public TimeSpan SyncDuration { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
    public bool Success => Errors.Count == 0;
    
    public string Summary => $"Sync completed: {NewDepartmentsAdded} added, {ExistingDepartmentsUpdated} updated, {DepartmentsDeactivated} deactivated from {TotalViewpointDepartments} Viewpoint departments";
}