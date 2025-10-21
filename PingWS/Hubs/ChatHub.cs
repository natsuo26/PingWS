using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace ChatWS.Hubs
{
  [Authorize]
    public class ChatHub:Hub
    {
    private static ConcurrentDictionary<string, string> ConnectedUsers = new();
    private static ConcurrentDictionary<Guid, string> UserConnections = new();
    private static ConcurrentDictionary<string, List<string>> Rooms = new();

    public override async Task OnConnectedAsync()
    {
        // Extract user info from JWT claims
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (Guid.TryParse(userId, out var userGuid) && !string.IsNullOrEmpty(userName))
        {
            ConnectedUsers[Context.ConnectionId] = userName;
            UserConnections[userGuid] = Context.ConnectionId;

            await Clients.Caller.SendAsync("ReceiveMessage", "System", $"You are connected as {userName}!");

            // Notify others that user is online (optional)
            await Clients.Others.SendAsync("UserOnline", userGuid, userName);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = ConnectedUsers.GetValueOrDefault(Context.ConnectionId);

        // Clean up connections
        ConnectedUsers.TryRemove(Context.ConnectionId, out _);

        if (Guid.TryParse(userId, out var userGuid))
        {
            UserConnections.TryRemove(userGuid, out _);

            // Notify others that user is offline (optional)
            await Clients.Others.SendAsync("UserOffline", userGuid, userName);
        }

        // Remove from all rooms
        foreach (var room in Rooms.ToList())
        {
            if (room.Value.Contains(Context.ConnectionId))
            {
                room.Value.Remove(Context.ConnectionId);
                if (room.Value.Count == 0)
                {
                    Rooms.TryRemove(room.Key, out _);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Get current user's ID from JWT
    private Guid GetCurrentUserId()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userId, out var userGuid) ? userGuid : Guid.Empty;
    }

    public async Task SendDirectMessage(Guid recipientUserId, string message)
    {
        var currentUserId = GetCurrentUserId();
        var senderUserName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        if (UserConnections.TryGetValue(recipientUserId, out var recipientConnectionId))
        {
            // Send to recipient
            await Clients.Client(recipientConnectionId).SendAsync("ReceiveDirectMessage",
                currentUserId, senderUserName, message, DateTime.UtcNow);

            // Confirm to sender
            await Clients.Caller.SendAsync("DirectMessageSent", recipientUserId, message, DateTime.UtcNow);
        }
        else
        {
            await Clients.Caller.SendAsync("ReceiveMessage", "System", "User is not online.");
        }
    }

    public async Task RegisterUser(string userName)
        {
            ConnectedUsers[Context.ConnectionId] = userName;
            await Clients.Caller.SendAsync("ReceiveMessage","System", $"You are connected as {userName}!");
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task JoinRoom(string roomName)
        {
            // adds the user to the room with unique ConnectionId to the room with name roomName
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

            // if the room does not exist, create it
            if (!Rooms.ContainsKey(roomName))
            {
                Rooms[roomName] = new List<string>();
            }
            Rooms[roomName].Add(Context.ConnectionId);

            // notify all clients in that room about the new user that joined
            if (ConnectedUsers.TryGetValue(Context.ConnectionId,out var userName))
            {
                await Clients.Group(roomName).SendAsync("ReceiveMessage", "System", $"{userName??Context.ConnectionId} joined {roomName}");
            }
        }

        public async Task LeaveRoom(string roomName)
        {
            // removes the user the room with the unique ConnectionId from the room with name roomName
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);

            // if the room exists, remove the user from the room
            if (Rooms.ContainsKey(roomName))
            {
                Rooms[roomName].Remove(Context.ConnectionId);
            }
            // notify all clients in that room about the user that left
            if (ConnectedUsers.TryGetValue(Context.ConnectionId, out var userName))
            {
                await Clients.Group(roomName).SendAsync("ReceiveMessage", "System", $"{userName ?? Context.ConnectionId} left {roomName}");
            }
        }

        public async Task SendMessageToRoom(string message)
        {
            // get the user with the ConnectionId
            ConnectedUsers.TryGetValue(Context.ConnectionId, out var user);

            // Get the roomName user is in
            var roomName = Rooms.FirstOrDefault(r => r.Value.Contains(Context.ConnectionId)).Key;

            // if the user is not in any room, send a message to the caller
            if (string.IsNullOrEmpty(roomName))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "You are not in any room.");
                return;
            }

            // sends the message to all clients in the room with name roomName
            await Clients.Group(roomName).SendAsync("ReceiveMessage", user, message);
        }
            

    }
}
