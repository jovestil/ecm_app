namespace Mathy.ELM.Core.DTOs;

public class PositionDto
{
    public int Id { get; set; }
    public int CompanyCode { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string? Type { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ViewpointSyncDate { get; set; }
}