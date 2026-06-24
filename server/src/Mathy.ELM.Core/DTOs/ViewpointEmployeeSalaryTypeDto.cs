using System.Text.Json.Serialization;

namespace Mathy.ELM.Core.DTOs;

public class ViewpointEmployeeSalaryTypeDto
{
    [JsonPropertyName("__ryvitKeys")]
    public List<string>? RyvitKeys { get; set; }

    [JsonPropertyName("__modifiedUTC")]
    public string? ModifiedUTC { get; set; }

    [JsonPropertyName("PRCo")]
    public int? PRCo { get; set; }

    [JsonPropertyName("EarnCode")]
    public int? EarnCode { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("KeyID")]
    public int? KeyID { get; set; }
}
