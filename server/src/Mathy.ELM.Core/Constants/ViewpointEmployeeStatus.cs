using Mathy.ELM.Core.Enums;

namespace Mathy.ELM.Core.Constants;

/// <summary>
/// Constants for Viewpoint employee status values
/// </summary>
public static class ViewpointEmployeeStatus
{
    /// <summary>
    /// Active employee status
    /// </summary>
    public const string Active = "U-ACTIVE";

    /// <summary>
    /// Layoff employee status
    /// </summary>
    public const string Layoff = "U-LAYOFF";

    /// <summary>
    /// Terminated employee status
    /// </summary>
    public const string Terminated = "U-TERM";

    /// <summary>
    /// On leave employee status
    /// </summary>
    public const string OnLeave = "U-LEAVE";

    /// <summary>
    /// Inactive employee status
    /// </summary>
    public const string Inactive = "U-INACTIVE";

    /// <summary>
    /// Gets the appropriate Viewpoint status based on HR request type
    /// </summary>
    /// <param name="requestType">The HR request type</param>
    /// <returns>The corresponding Viewpoint employee status</returns>
    public static string GetStatusForRequestType(RequestType requestType)
    {
        return requestType switch
        {
            RequestType.Promotion => Active,
            RequestType.Layoff => Layoff,
            RequestType.Termination => Terminated,
            RequestType.ReturnToWork => Active,
            _ => throw new ArgumentException($"Unknown request type: {requestType}")
        };
    }
}