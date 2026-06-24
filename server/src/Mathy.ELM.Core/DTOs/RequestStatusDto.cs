namespace Mathy.ELM.Core.DTOs;

public class RequestStatusDto
{
    public int Id { get; set; }
    public string RequestStatusName { get; set; } = string.Empty;
    public string? RequestDisplayStatusName { get; set; }
    public string? RequestStatusDescription { get; set; }
    public bool IsActive { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}