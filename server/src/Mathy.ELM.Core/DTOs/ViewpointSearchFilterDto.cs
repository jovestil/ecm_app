using System.Text.Json.Serialization;

namespace Mathy.ELM.Core.DTOs;

public class ViewpointSearchFilterDto
{
    [JsonPropertyName("propertyName")]
    public string PropertyName { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public int Value { get; set; }

    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "Equal";
}