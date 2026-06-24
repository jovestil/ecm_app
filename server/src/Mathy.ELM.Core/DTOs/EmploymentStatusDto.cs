namespace Mathy.ELM.Core.DTOs;

public class EmploymentStatusDto
{
    public int Id { get; set; }
    public int CompanyCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? ViewpointSyncDate { get; set; }
}