using System.ComponentModel.DataAnnotations;

namespace Mathy.ELM.Core.Entities;

public class CompanyTypeLocation : BaseEntity
{
    public int CompanyCode { get; set; }
    public string LocationType { get; set; } = string.Empty; // Mathy, Pavement, TNW
    public bool IsUnion { get; set; } = false; // 0 No, 1 Yes

    [MaxLength(200)]
    public string? Domain { get; set; } // ex. mathy.com, pavementmaterials.com, internal.tnw.com
}