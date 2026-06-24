namespace Mathy.ELM.Core.DTOs;

public class CompanyTypeLocationDto
{
    public int Id { get; set; }
    public int CompanyCode { get; set; }
    public string LocationType { get; set; } = string.Empty;
    public bool IsUnion { get; set; }
    public string? Domain { get; set; }
    public string? EmailDomain { get; set; }
}