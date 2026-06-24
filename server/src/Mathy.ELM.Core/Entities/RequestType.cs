namespace Mathy.ELM.Core.Entities;

public class RequestType : BaseEntity
{
    public string RequestTypeName { get; set; } = string.Empty;
    public string? RequestTypeDescription { get; set; }
    public bool IsActive { get; set; } = true;
    
    public virtual ICollection<HRRequestDetail> HRRequestDetails { get; set; } = new List<HRRequestDetail>();
}