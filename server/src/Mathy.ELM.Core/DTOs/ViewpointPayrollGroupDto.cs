using System.Text.Json.Serialization;
using Mathy.ELM.Core.Converters;

namespace Mathy.ELM.Core.DTOs;

public class ViewpointPayrollGroupDto
{
    [JsonPropertyName("__ryvitKeys")]
    public List<string>? RyvitKeys { get; set; }

    [JsonPropertyName("__modifiedUTC")]
    public string? ModifiedUTC { get; set; }

    [JsonPropertyName("PRCo")]
    public int? PRCo { get; set; }

    [JsonPropertyName("PRGroup")]
    public int? PRGroup { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("PayFreq")]
    public string? PayFreq { get; set; }

    [JsonPropertyName("GLCo")]
    public int? GLCo { get; set; }

    [JsonPropertyName("GLAcct")]
    public string? GLAcct { get; set; }

    [JsonPropertyName("CMCo")]
    public int? CMCo { get; set; }

    [JsonPropertyName("CMAcct")]
    public int? CMAcct { get; set; }

    [JsonPropertyName("Notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("UniqueAttchID")]
    public string? UniqueAttchID { get; set; }

    [JsonPropertyName("KeyID")]
    public int? KeyID { get; set; }

    [JsonPropertyName("AttachGLLedgerRpts")]
    public string? AttachGLLedgerRpts { get; set; }

    [JsonPropertyName("AttachTypeID")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? AttachTypeID { get; set; }

    [JsonPropertyName("PayPeriodsPerYear")]
    public int? PayPeriodsPerYear { get; set; }

    [JsonPropertyName("__custom_fields")]
    public ViewpointPayrollGroupCustomFields? CustomFields { get; set; }
}

public class ViewpointPayrollGroupCustomFields
{
    [JsonPropertyName("udCoName")]
    public string? CoName { get; set; }

    [JsonPropertyName("udMNGroup")]
    public string? MNGroup { get; set; }

    [JsonPropertyName("udCompSummMemo")]
    public string? CompSummMemo { get; set; }
}