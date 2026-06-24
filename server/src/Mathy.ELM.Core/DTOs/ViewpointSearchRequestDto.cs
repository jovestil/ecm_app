using System.Text.Json.Serialization;

namespace Mathy.ELM.Core.DTOs;

public class ViewpointSearchRequestDto
{
    [JsonPropertyName("filters")]
    public List<ViewpointSearchFilterDto> Filters { get; set; } = new();
}