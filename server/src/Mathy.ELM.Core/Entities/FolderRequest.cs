namespace Mathy.ELM.Core.Entities;

public class FolderRequest : BaseEntity
{
    public int NewHireRequestId { get; set; }
    
    public string FolderType { get; set; } = string.Empty; // 'Shared Folder', 'SharePoint Site', 'Mailbox', 'Distribution List', 'OneDrive Access'
    public string FolderName { get; set; } = string.Empty;
    
    public virtual NewHireRequestDetail NewHireRequest { get; set; } = null!;
}