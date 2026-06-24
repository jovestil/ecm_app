namespace Mathy.ELM.Core.DTOs;

public class EmployeeSalaryTypeSyncResultDto
{
    public int TotalViewpointSalaryTypes { get; set; }
    public int NewSalaryTypesAdded { get; set; }
    public int ExistingSalaryTypesUpdated { get; set; }
    public int SalaryTypesDeactivated { get; set; }
    public DateTime SyncDate { get; set; }
    public TimeSpan SyncDuration { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
    public bool Success => Errors.Count == 0;

    public string Summary => $"Sync completed: {NewSalaryTypesAdded} added, {ExistingSalaryTypesUpdated} updated, {SalaryTypesDeactivated} deactivated from {TotalViewpointSalaryTypes} Viewpoint salary types";
}
