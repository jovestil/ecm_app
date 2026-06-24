namespace Mathy.ELM.Core.DTOs;

public class UnionCraftDto
{
    public int Id { get; set; }
    public int CompanyCode { get; set; }
    public string CraftCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? ViewpointSyncDate { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}