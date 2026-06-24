using Microsoft.AspNetCore.SignalR;
using Mathy.ELM.Api.Hubs;
using Mathy.ELM.Core.Services;

namespace Mathy.ELM.Api.Services;

/// <summary>
/// Service for sending real-time notifications via SignalR
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IHubContext<HRRequestStatusHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<HRRequestStatusHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendHRRequestStatusUpdateAsync(string userId, int hrRequestDetailId, string status, string employeeName, string? message = null)
    {
        try
        {
            var notification = new
            {
                Type = "HRRequestStatusUpdate",
                HRRequestDetailId = hrRequestDetailId,
                Status = status,
                EmployeeName = employeeName,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            // Send to all connected clients instead of specific user group
            await _hubContext.Clients.All.SendAsync("HRRequestStatusUpdate", notification);
            
            _logger.LogInformation("Sent HR request status update notification to all users for request {HRRequestDetailId} with status {Status} (originally for user {UserId})", 
                hrRequestDetailId, status, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send HR request status update notification to all users for request {HRRequestDetailId} (originally for user {UserId})", 
                hrRequestDetailId, userId);
        }
    }

    public async Task SendHRRequestCompletionNotificationAsync(string userId, int hrRequestDetailId, string employeeName, bool isSuccess, string? message = null)
    {
        try
        {
            var notification = new
            {
                Type = "HRRequestCompletion",
                HRRequestDetailId = hrRequestDetailId,
                EmployeeName = employeeName,
                IsSuccess = isSuccess,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            // Send to all connected clients instead of specific user group
            await _hubContext.Clients.All.SendAsync("HRRequestCompletion", notification);
            
            _logger.LogInformation("Sent HR request completion notification to all users for request {HRRequestDetailId}, success: {IsSuccess} (originally for user {UserId})", 
                hrRequestDetailId, isSuccess, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send HR request completion notification to all users for request {HRRequestDetailId} (originally for user {UserId})", 
                hrRequestDetailId, userId);
        }
    }
}