namespace Mathy.ELM.Core.DTOs;

public class BuildingAccessRequirementDto
{
    public int Id { get; set; }
    public int CompanyCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
}