namespace Mathy.ELM.Core.Services;

/// <summary>
/// Service for sending real-time notifications to clients
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send HR request status update notification to a specific user
    /// </summary>
    /// <param name="userId">The user ID to notify</param>
    /// <param name="hrRequestDetailId">The HR request detail ID</param>
    /// <param name="status">The new status</param>
    /// <param name="employeeName">The employee name</param>
    /// <param name="message">Optional message</param>
    Task SendHRRequestStatusUpdateAsync(string userId, int hrRequestDetailId, string status, string employeeName, string? message = null);

    /// <summary>
    /// Send HR request completion notification to a specific user
    /// </summary>
    /// <param name="userId">The user ID to notify</param>
    /// <param name="hrRequestDetailId">The HR request detail ID</param>
    /// <param name="employeeName">The employee name</param>
    /// <param name="isSuccess">Whether the request completed successfully</param>
    /// <param name="message">Optional message</param>
    Task SendHRRequestCompletionNotificationAsync(string userId, int hrRequestDetailId, string employeeName, bool isSuccess, string? message = null);
}