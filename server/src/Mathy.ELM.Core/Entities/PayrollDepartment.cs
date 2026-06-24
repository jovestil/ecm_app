namespace Mathy.ELM.Core.Entities;

public class PayrollDepartment : BaseEntity
{
    public int CompanyCode { get; set; }
    public int DeptCode { get; set; }
    public string DeptName { get; set; } = string.Empty;
    public string? EmailDomain { get; set; }
    public bool IsActive { get; set; } = true;

    // Representative Fields
    public int? HRPartner { get; set; }
    public int? HRRep { get; set; }
    public int? SafetyRep { get; set; }
    public int? PayrollRep { get; set; }

    // Viewpoint Sync
    public DateTime? ViewpointSyncDate { get; set; }
}