namespace Mathy.ELM.Core.DTOs;

public class CompanyDto
{
    public int Id { get; set; }
    public int CompanyCode { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? ViewpointSyncDate { get; set; }
}