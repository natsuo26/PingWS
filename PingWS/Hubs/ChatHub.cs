using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace ChatWS.Hubs
{
    public class ChatHub:Hub
    {
        private static ConcurrentDictionary<string, string> ConnectedUsers = new();
        private static ConcurrentDictionary<string, List<string>> Rooms = new();

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
