using System.Text.Json.Serialization;

namespace Mathy.ELM.Core.DTOs;

public class ViewpointSearchResponseDto
{
    [JsonPropertyName("data")]
    public List<ViewpointEmployeeDto> Data { get; set; } = new();

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}