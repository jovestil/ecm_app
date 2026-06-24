namespace Mathy.ELM.Core.DTOs;

public class ApprenticePercentageDto
{
    public int Id { get; set; }
    public string AppPercentage { get; set; } = string.Empty;
    public string AppDescription { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}