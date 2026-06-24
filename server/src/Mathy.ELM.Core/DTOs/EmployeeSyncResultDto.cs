namespace Mathy.ELM.Core.DTOs;

public class EmployeeSyncResultDto
{
    public int TotalProcessed { get; set; }
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DeletedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime SyncStartTime { get; set; }
    public DateTime SyncEndTime { get; set; }
    public TimeSpan SyncDuration => SyncEndTime - SyncStartTime;
    public bool HasMore { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}