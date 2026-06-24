namespace Mathy.ELM.Core.DTOs;

public class PayrollGroupSyncResultDto
{
    public int TotalViewpointPayrollGroups { get; set; }
    public int NewPayrollGroupsAdded { get; set; }
    public int ExistingPayrollGroupsUpdated { get; set; }
    public int PayrollGroupsDeactivated { get; set; }
    public DateTime SyncDate { get; set; }
    public TimeSpan SyncDuration { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
    public bool Success => Errors.Count == 0;
    
    public string Summary => $"Sync completed: {NewPayrollGroupsAdded} added, {ExistingPayrollGroupsUpdated} updated, {PayrollGroupsDeactivated} deactivated from {TotalViewpointPayrollGroups} Viewpoint payroll groups";
}