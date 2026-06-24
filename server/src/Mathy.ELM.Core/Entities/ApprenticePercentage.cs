namespace Mathy.ELM.Core.Entities;

public class ApprenticePercentage : BaseEntity
{
    public string AppPercentage { get; set; } = string.Empty;
    public string AppDescription { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}