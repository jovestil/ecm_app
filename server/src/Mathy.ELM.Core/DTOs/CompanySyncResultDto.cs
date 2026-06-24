namespace Mathy.ELM.Core.DTOs;

public class CompanySyncResultDto
{
    public int TotalViewpointCompanies { get; set; }
    public int NewCompaniesAdded { get; set; }
    public int ExistingCompaniesUpdated { get; set; }
    public int CompaniesDeactivated { get; set; }
    public DateTime SyncDate { get; set; }
    public TimeSpan SyncDuration { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
    public bool Success => Errors.Count == 0;
    
    public string Summary => $"Sync completed: {NewCompaniesAdded} added, {ExistingCompaniesUpdated} updated, {CompaniesDeactivated} deactivated from {TotalViewpointCompanies} Viewpoint companies";
}