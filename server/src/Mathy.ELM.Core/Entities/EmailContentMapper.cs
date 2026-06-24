namespace Mathy.ELM.Core.Entities;

public class EmailContentMapper : BaseEntity
{
    public string? ContentCode { get; set; } // EMPLOYEE-NAME, START-DATE
    public string? ContentField { get; set; } // EMPLOYEE-NAME --> fullname
    public string? ContentPartType { get; set; } // Body, Subject, Recipients
    public string? ContentLabel { get; set; } // Start Date, Employee Name
    public string? ContentSource { get; set; } // NEWHIRE, APPLICATION, COMPANYDL
}
