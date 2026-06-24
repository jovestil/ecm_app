namespace Mathy.ELM.Core.DTOs;

public class SyncScheduleConfigDto
{
    public string Companies { get; set; } = "disabled";
    public string Departments { get; set; } = "disabled";
    public string Positions { get; set; } = "disabled";
    public string PayrollGroups { get; set; } = "disabled";
    public string UnionCrafts { get; set; } = "disabled";
    public string Employees { get; set; } = "disabled";

    public DateTime LastUpdated { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class SyncScheduleResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> ScheduledJobs { get; set; } = new List<string>();
}