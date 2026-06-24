namespace Mathy.ELM.Core.DTOs;

public class UnionCraftSyncResultDto
{
    public int TotalViewpointUnionCrafts { get; set; }
    public int NewUnionCraftsAdded { get; set; }
    public int ExistingUnionCraftsUpdated { get; set; }
    public int UnionCraftsDeactivated { get; set; }
    public DateTime SyncDate { get; set; }
    public TimeSpan SyncDuration { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
    public bool Success => Errors.Count == 0;

    public string Summary => $"Sync completed: {NewUnionCraftsAdded} added, {ExistingUnionCraftsUpdated} updated, {UnionCraftsDeactivated} deactivated from {TotalViewpointUnionCrafts} Viewpoint union crafts";
}
