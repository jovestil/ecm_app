namespace Mathy.ELM.Core.Entities;

public class ApplicationRequest : BaseEntity
{
    public int NewHireRequestId { get; set; }
    public int ApplicationId { get; set; }
    public string? AccessNotes { get; set; }
    
    public virtual NewHireRequestDetail NewHireRequest { get; set; } = null!;
    public virtual Application Application { get; set; } = null!;
}