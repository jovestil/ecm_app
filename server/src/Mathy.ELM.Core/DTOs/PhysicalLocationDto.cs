namespace Mathy.ELM.Core.DTOs;

public class PhysicalLocationDto
{
    public int Id { get; set; }
    public int LocationCode { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? ViewpointSyncDate { get; set; }
}