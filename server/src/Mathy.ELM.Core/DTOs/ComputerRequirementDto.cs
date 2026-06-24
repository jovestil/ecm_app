namespace Mathy.ELM.Core.DTOs;

public class ComputerRequirementDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool? IsChild { get; set; }
    public int? ParentId { get; set; }
    public bool IsActive { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}