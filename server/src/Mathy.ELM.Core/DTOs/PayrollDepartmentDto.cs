namespace Mathy.ELM.Core.DTOs;

public class PayrollDepartmentDto
{
    public int Id { get; set; }
    public int CompanyCode { get; set; }
    public int DeptCode { get; set; }
    public string DeptName { get; set; } = string.Empty;
    public string? EmailDomain { get; set; }
    public int? HRPartner { get; set; }
    public int? HRRep { get; set; }
    public int? SafetyRep { get; set; }
    public int? PayrollRep { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}