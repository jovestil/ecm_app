namespace Mathy.ELM.Core.Entities;

public class Position : BaseEntity
{
    public int CompanyCode { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string? Type { get; set; }
    public bool IsActive { get; set; } = true;

    // Viewpoint Sync
    public DateTime? ViewpointSyncDate { get; set; }
}