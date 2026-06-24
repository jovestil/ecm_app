using System.Text.Json.Serialization;

namespace Mathy.ELM.Core.DTOs;

public class ViewpointEmploymentStatusDto
{
    [JsonPropertyName("__ryvitKeys")]
    public List<string>? RyvitKeys { get; set; }

    [JsonPropertyName("__modifiedUTC")]
    public string? ModifiedUTC { get; set; }

    [JsonPropertyName("HRCo")]
    public int? HRCo { get; set; }

    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    // Alternative property name that might be used by the API
    [JsonPropertyName("Code")]
    public string? Code { get; set; }

    [JsonPropertyName("Notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("ActiveYN")]
    public string? ActiveYN { get; set; }

    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    // Computed property to get the status code from either Status or Code field
    [JsonIgnore]
    public string? StatusCode => !string.IsNullOrWhiteSpace(Status) ? Status : Code;
}
