namespace Mathy.ELM.Core.DTOs;

public class PayrollDepartmentShortNameDto
{
    public int Id { get; set; }
    public int CompanyCode { get; set; }
    public int DeptCode { get; set; }
    public string DeptShortName { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}