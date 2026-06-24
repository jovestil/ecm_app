using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace Mathy.ELM.Api.Hubs;

/// <summary>
/// SignalR hub for real-time HR request status notifications
/// </summary>
// [Authorize] // Temporarily disabled for testing
public class HRRequestStatusHub : Hub
{
    private readonly ILogger<HRRequestStatusHub> _logger;

    public HRRequestStatusHub(ILogger<HRRequestStatusHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Join a group to receive notifications for a specific user's requests
    /// </summary>
    /// <param name="userId">The user ID to subscribe to</param>
    public async Task JoinUserGroup(string userId)
    {
        var groupName = $"user_{userId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("User {UserId} joined group {GroupName} with connection {ConnectionId}", 
            userId, groupName, Context.ConnectionId);
    }

    /// <summary>
    /// Leave a user group
    /// </summary>
    /// <param name="userId">The user ID to unsubscribe from</param>  
    public async Task LeaveUserGroup(string userId)
    {
        var groupName = $"user_{userId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("User {UserId} left group {GroupName} with connection {ConnectionId}", 
            userId, groupName, Context.ConnectionId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}, User: {User}", 
            Context.ConnectionId, Context.User?.Identity?.Name ?? "Anonymous");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}, Exception: {Exception}", 
            Context.ConnectionId, exception?.Message);
        await base.OnDisconnectedAsync(exception);
    }
}