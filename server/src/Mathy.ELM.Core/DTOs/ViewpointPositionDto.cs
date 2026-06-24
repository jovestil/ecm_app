using System.Text.Json.Serialization;
using Mathy.ELM.Core.Converters;

namespace Mathy.ELM.Core.DTOs;

public class ViewpointPositionDto
{
    [JsonPropertyName("__ryvitKeys")]
    public List<string>? RyvitKeys { get; set; }

    [JsonPropertyName("__modifiedUTC")]
    public string? ModifiedUTC { get; set; }

    [JsonPropertyName("HRCo")]
    public int? HRCo { get; set; }

    [JsonPropertyName("PositionCode")]
    public string? PositionCode { get; set; }

    [JsonPropertyName("JobTitle")]
    public string? JobTitle { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("BegSalary")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? BegSalary { get; set; }

    [JsonPropertyName("EndSalary")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? EndSalary { get; set; }

    [JsonPropertyName("PartTimeYN")]
    public string? PartTimeYN { get; set; }

    [JsonPropertyName("PartimeHrs")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? PartimeHrs { get; set; }

    [JsonPropertyName("AdYN")]
    public string? AdYN { get; set; }

    [JsonPropertyName("AdMode")]
    public string? AdMode { get; set; }

    [JsonPropertyName("ClosingDate")]
    public string? ClosingDate { get; set; }

    [JsonPropertyName("BonusYN")]
    public string? BonusYN { get; set; }

    [JsonPropertyName("BonusPct")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? BonusPct { get; set; }

    [JsonPropertyName("ReportLevel")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? ReportLevel { get; set; }

    [JsonPropertyName("ReportPosition")]
    public string? ReportPosition { get; set; }

    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    [JsonPropertyName("OpenJobs")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? OpenJobs { get; set; }

    [JsonPropertyName("Contact")]
    public string? Contact { get; set; }

    [JsonPropertyName("ContactPhone")]
    public string? ContactPhone { get; set; }

    [JsonPropertyName("ContactEmail")]
    public string? ContactEmail { get; set; }

    [JsonPropertyName("ContactFax")]
    public string? ContactFax { get; set; }

    [JsonPropertyName("Notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("UniqueAttchID")]
    public string? UniqueAttchID { get; set; }

    [JsonPropertyName("KeyID")]
    public int? KeyID { get; set; }

    [JsonPropertyName("__custom_fields")]
    public ViewpointPositionCustomFields? CustomFields { get; set; }
}

public class ViewpointPositionCustomFields
{
    [JsonPropertyName("udSafetyCategory")]
    public string? SafetyCategory { get; set; }
}