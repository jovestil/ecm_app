namespace Mathy.ELM.Core.Entities;

public class PayrollDepartmentShortName : BaseEntity
{
    public int CompanyCode { get; set; }
    public int DeptCode { get; set; }
    public string DeptShortName { get; set; } = string.Empty;
}