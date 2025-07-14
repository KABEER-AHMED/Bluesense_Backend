using ChatApp.Backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ChatApp.Backend.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time chat messaging
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private static readonly Dictionary<string, string> _connectedUsers = new();

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Join a specific group for receiving messages
        /// </summary>
        /// <param name="groupId">Group ID to join</param>
        public async Task JoinGroup(string groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
            
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} joined group {GroupId} with connection {ConnectionId}", 
                userId, groupId, Context.ConnectionId);
            
            // Notify other group members
            await Clients.Group($"group_{groupId}")
                .SendAsync("UserJoined", new { UserId = userId, GroupId = groupId });
        }

        /// <summary>
        /// Leave a specific group
        /// </summary>
        /// <param name="groupId">Group ID to leave</param>
        public async Task LeaveGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");
            
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} left group {GroupId} with connection {ConnectionId}", 
                userId, groupId, Context.ConnectionId);
            
            // Notify other group members
            await Clients.Group($"group_{groupId}")
                .SendAsync("UserLeft", new { UserId = userId, GroupId = groupId });
        }

        /// <summary>
        /// Send a message to a group
        /// </summary>
        /// <param name="groupId">Target group ID</param>
        /// <param name="message">Message DTO</param>
        public async Task SendMessageToGroup(string groupId, MessageDto message)
        {
            var userId = GetUserId();
            
            // Verify the sender is the authenticated user
            if (message.UserId.ToString() != userId)
            {
                _logger.LogWarning("User {UserId} attempted to send message as {MessageUserId}", 
                    userId, message.UserId);
                return;
            }

            // Send message to all users in the group
            await Clients.Group($"group_{groupId}")
                .SendAsync("ReceiveMessage", message);

            _logger.LogInformation("Message {MessageId} sent to group {GroupId} by user {UserId}", 
                message.Id, groupId, userId);
        }

        /// <summary>
        /// Notify group members that a message was edited
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <param name="message">Updated message DTO</param>
        public async Task NotifyMessageEdited(string groupId, MessageDto message)
        {
            await Clients.Group($"group_{groupId}")
                .SendAsync("MessageEdited", message);

            _logger.LogInformation("Message {MessageId} edit notification sent to group {GroupId}", 
                message.Id, groupId);
        }

        /// <summary>
        /// Notify group members that a message was deleted
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <param name="messageId">Deleted message ID</param>
        public async Task NotifyMessageDeleted(string groupId, string messageId)
        {
            await Clients.Group($"group_{groupId}")
                .SendAsync("MessageDeleted", new { MessageId = messageId, GroupId = groupId });

            _logger.LogInformation("Message {MessageId} deletion notification sent to group {GroupId}", 
                messageId, groupId);
        }

        /// <summary>
        /// Send typing indicator to group
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <param name="isTyping">Whether user is typing</param>
        public async Task SendTypingIndicator(string groupId, bool isTyping)
        {
            var userId = GetUserId();
            
            await Clients.GroupExcept($"group_{groupId}", Context.ConnectionId)
                .SendAsync("UserTyping", new { UserId = userId, GroupId = groupId, IsTyping = isTyping });
        }

        /// <summary>
        /// Update user online status
        /// </summary>
        /// <param name="status">User status (Online, Away, Busy, etc.)</param>
        public async Task UpdateStatus(string status)
        {
            var userId = GetUserId();
            
            // Broadcast status update to all connected clients who share groups with this user
            await Clients.All.SendAsync("UserStatusChanged", new { UserId = userId, Status = status });
            
            _logger.LogInformation("User {UserId} status changed to {Status}", userId, status);
        }

        /// <summary>
        /// Handle client connection
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            var username = GetUsername();
            
            _connectedUsers[Context.ConnectionId] = userId;
            
            _logger.LogInformation("User {Username} ({UserId}) connected with connection {ConnectionId}", 
                username, userId, Context.ConnectionId);
            
            // Notify all clients that user is online
            await Clients.All.SendAsync("UserOnline", new { UserId = userId, Username = username });
            
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Handle client disconnection
        /// </summary>
        /// <param name="exception">Disconnection exception if any</param>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            var username = GetUsername();
            
            _connectedUsers.Remove(Context.ConnectionId);
            
            _logger.LogInformation("User {Username} ({UserId}) disconnected from connection {ConnectionId}", 
                username, userId, Context.ConnectionId);
            
            // Notify all clients that user is offline
            await Clients.All.SendAsync("UserOffline", new { UserId = userId, Username = username });
            
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Get the authenticated user's ID from claims
        /// </summary>
        private string GetUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Get the authenticated user's username from claims
        /// </summary>
        private string GetUsername()
        {
            return Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Get all currently connected users
        /// </summary>
        public static Dictionary<string, string> GetConnectedUsers()
        {
            return new Dictionary<string, string>(_connectedUsers);
        }
    }
}
