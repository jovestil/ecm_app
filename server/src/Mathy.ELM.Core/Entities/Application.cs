namespace Mathy.ELM.Core.Entities;

public class Application : BaseEntity
{
    public string LocationType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<ApplicationRequest> ApplicationRequests { get; set; } = new List<ApplicationRequest>();
}