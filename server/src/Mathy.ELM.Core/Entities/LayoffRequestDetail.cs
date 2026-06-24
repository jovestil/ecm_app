namespace Mathy.ELM.Core.Entities;

public class LayoffRequestDetail : BaseEntity
{
    public int RequestDetailId { get; set; }
    public DateTime LastDayWorked { get; set; }
    
    public virtual HRRequestDetail HRRequestDetail { get; set; } = null!;
}