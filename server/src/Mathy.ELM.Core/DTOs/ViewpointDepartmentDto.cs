using System.Text.Json.Serialization;
using Mathy.ELM.Core.Converters;

namespace Mathy.ELM.Core.DTOs;

public class ViewpointDepartmentDto
{
    [JsonPropertyName("__ryvitKeys")]
    public List<string>? RyvitKeys { get; set; }

    [JsonPropertyName("__modifiedUTC")]
    public string? ModifiedUTC { get; set; }

    [JsonPropertyName("PRCo")]
    public int? PRCo { get; set; }

    [JsonPropertyName("PRDept")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? PRDept { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("GLCo")]
    public int? GLCo { get; set; }

    [JsonPropertyName("JCFixedRateGLAcct")]
    public string? JCFixedRateGLAcct { get; set; }

    [JsonPropertyName("EMFixedRateGLAcct")]
    public string? EMFixedRateGLAcct { get; set; }

    [JsonPropertyName("Notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("UniqueAttchID")]
    public string? UniqueAttchID { get; set; }

    [JsonPropertyName("KeyID")]
    public int? KeyID { get; set; }

    [JsonPropertyName("SMFixedRateGLAcct")]
    public string? SMFixedRateGLAcct { get; set; }

    [JsonPropertyName("__custom_fields")]
    public ViewpointDepartmentCustomFields? CustomFields { get; set; }
}

public class ViewpointDepartmentCustomFields
{
    [JsonPropertyName("udEEOOfficerHRRef")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? EEOOfficerHRRef { get; set; }

    [JsonPropertyName("udEEOContactHRRef")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? EEOContactHRRef { get; set; }

    [JsonPropertyName("udHRPartner")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? HRPartner { get; set; }

    [JsonPropertyName("udHRRep")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? HRRep { get; set; }

    [JsonPropertyName("udPayrollRep")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? PayrollRep { get; set; }

    [JsonPropertyName("udSafetyRep")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? SafetyRep { get; set; }

    [JsonPropertyName("udDomain")]
    public string? Domain { get; set; }

    [JsonPropertyName("udActive")]
    public string? IsActive { get; set; }
}