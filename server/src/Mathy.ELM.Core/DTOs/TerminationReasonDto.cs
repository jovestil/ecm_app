namespace Mathy.ELM.Core.DTOs;

public class TerminationReasonDto
{
    public string? Notes { get; set; }
    public int Id { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string ReasonDescription { get; set; } = string.Empty;
    public int CompanyCode { get; set; }
    public bool IsActive { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}