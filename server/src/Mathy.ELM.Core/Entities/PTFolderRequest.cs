namespace Mathy.ELM.Core.Entities;

public class PTFolderRequest : BaseEntity
{
    public int PTRequestDetailId { get; set; }

    public string FolderType { get; set; } = string.Empty; // 'Shared Folder', 'SharePoint Site', 'Mailbox', 'Distribution List', 'OneDrive Access'
    public string FolderName { get; set; } = string.Empty;

    public virtual PromotionRequestDetail PromotionRequestDetail { get; set; } = null!;
}
