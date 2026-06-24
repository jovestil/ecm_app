namespace Mathy.ELM.Core.DTOs;

public class EmployeeLicenseClassDto
{
    public int Id { get; set; }
    public string LicenseClass { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsUnion { get; set; }
    public DateTime? ViewpointSyncDate { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}