using System.Text.Json.Serialization;

namespace Mathy.ELM.Core.DTOs;

/// <summary>
/// Initial response from Viewpoint action API (queued action)
/// </summary>
public class ViewpointActionResponseDto
{
    /// <summary>
    /// Unique identifier for the action
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Operation type (e.g., "QueueAction")
    /// </summary>
    [JsonPropertyName("operation")]
    public string? Operation { get; set; }

    /// <summary>
    /// Initial status (e.g., "Successful" means successfully queued)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>
/// Detailed response from Viewpoint action status check API
/// </summary>
public class ViewpointActionDetailResponseDto
{
    /// <summary>
    /// Unique identifier for the action
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Action path (e.g., "vista/hr/2/resources/update")
    /// </summary>
    [JsonPropertyName("actionPath")]
    public string? ActionPath { get; set; }

    /// <summary>
    /// Data object code (e.g., "resources")
    /// </summary>
    [JsonPropertyName("dataObjectCode")]
    public string? DataObjectCode { get; set; }

    /// <summary>
    /// Subscriber code
    /// </summary>
    [JsonPropertyName("subscriberCode")]
    public string? SubscriberCode { get; set; }

    /// <summary>
    /// Action code (e.g., "update")
    /// </summary>
    [JsonPropertyName("actionCode")]
    public string? ActionCode { get; set; }

    /// <summary>
    /// The data that was sent in the update request
    /// </summary>
    [JsonPropertyName("data")]
    public ViewpointActionDataDto? Data { get; set; }

    /// <summary>
    /// When the action was created (UTC)
    /// </summary>
    [JsonPropertyName("createdUtc")]
    public string? CreatedUtc { get; set; }

    /// <summary>
    /// When the action was last modified (UTC)
    /// </summary>
    [JsonPropertyName("modifiedUtc")]
    public string? ModifiedUtc { get; set; }

    /// <summary>
    /// Final status of the action (e.g., "Successful", "Failed")
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Result of the action execution
    /// </summary>
    [JsonPropertyName("result")]
    public ViewpointActionResultDto? Result { get; set; }

    /// <summary>
    /// Context information about the action execution
    /// </summary>
    [JsonPropertyName("contextJson")]
    public object? ContextJson { get; set; }
}

/// <summary>
/// Data object from Viewpoint action verification response
/// </summary>
public class ViewpointActionDataDto
{
    [JsonPropertyName("__key")]
    public ViewpointActionKeyDto? Key { get; set; }

    [JsonPropertyName("PRCo")]
    public int? PRCo { get; set; }

    [JsonPropertyName("PRGroup")]
    public int? PRGroup { get; set; }

    [JsonPropertyName("PRDept")]
    public string? PRDept { get; set; }

    [JsonPropertyName("PositionCode")]
    public string? PositionCode { get; set; }

    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    [JsonPropertyName("__custom_fields")]
    public ViewpointActionCustomFieldsDto? CustomFields { get; set; }
}

/// <summary>
/// Key object from Viewpoint action data
/// </summary>
public class ViewpointActionKeyDto
{
    [JsonPropertyName("HRCo")]
    public int? HRCo { get; set; }

    [JsonPropertyName("HRRef")]
    public int? HRRef { get; set; }
}

/// <summary>
/// Custom fields from Viewpoint action data
/// </summary>
public class ViewpointActionCustomFieldsDto
{
    [JsonPropertyName("udSupervisor")]
    public int? udSupervisor { get; set; }

    [JsonPropertyName("udPhysicalLocation")]
    public int? udPhysicalLocation { get; set; }
}

/// <summary>
/// Result details from a successful Viewpoint action
/// </summary>
public class ViewpointActionResultDto
{
    /// <summary>
    /// Internal key ID
    /// </summary>
    [JsonPropertyName("KeyID")]
    public int? KeyID { get; set; }

    /// <summary>
    /// Company code
    /// </summary>
    [JsonPropertyName("HRCo")]
    public int? HRCo { get; set; }

    /// <summary>
    /// Employee reference number
    /// </summary>
    [JsonPropertyName("HRRef")]
    public int? HRRef { get; set; }
}

/// <summary>
/// Comprehensive result for new hire employee update operation
/// </summary>
public class UpdateEmployeeNewHireResultDto
{
    /// <summary>
    /// Whether the update was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Whether the employee was found in Viewpoint
    /// </summary>
    public bool EmployeeFound { get; set; }

    /// <summary>
    /// The employee data from Viewpoint (if found)
    /// </summary>
    public ViewpointEmployeeDto? Employee { get; set; }

    /// <summary>
    /// Whether the update was queued successfully
    /// </summary>
    public bool UpdateQueued { get; set; }

    /// <summary>
    /// Action ID for tracking the update
    /// </summary>
    public string? ActionId { get; set; }

    /// <summary>
    /// Final status of the update action
    /// </summary>
    public string? ActionStatus { get; set; }

    /// <summary>
    /// Detailed action response from Viewpoint
    /// </summary>
    public ViewpointActionDetailResponseDto? ActionDetails { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
