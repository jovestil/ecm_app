namespace Mathy.ELM.Core.DTOs;

public class PayrollGroupDto
{
    public int Id { get; set; }
    public int CompanyCode { get; set; }
    public int GroupCode { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
