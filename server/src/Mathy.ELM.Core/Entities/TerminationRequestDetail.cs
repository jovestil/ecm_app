namespace Mathy.ELM.Core.Entities;

public class TerminationRequestDetail : BaseEntity
{
    public int RequestDetailId { get; set; }
    
    public string ReasonCode { get; set; } = string.Empty; // 'performance', 'misconduct', 'attendance', 'restructuring', 'violation', 'resignation'
    
    // Communication Forwarding
    public string? ForwardEmail { get; set; }
    public string? ForwardDeskPhone { get; set; }
    public string? ForwardCellPhone { get; set; }
    public string? AutoReply { get; set; }
    public string? GiveOneDriveAccessTo { get; set; }

    // Kwik Trip Card
    public bool WithKwikTripCard { get; set; }
    public string? KwikCard4DigitNo { get; set; }

    public virtual HRRequestDetail HRRequestDetail { get; set; } = null!;
}