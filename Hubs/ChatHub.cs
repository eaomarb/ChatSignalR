using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace ChatSignalR.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> users = new(); // connId -> username
        private static readonly ConcurrentDictionary<string, HashSet<string>> rooms = new(); // room -> connIds
        private static readonly ConcurrentDictionary<string, HashSet<string>> userRooms = new(); // connId -> rooms
        private static readonly ConcurrentDictionary<string, List<ChatMessage>> roomMessages = new(); // room -> messages

        public async Task JoinRoom(string room, string username)
        {
            if (string.IsNullOrEmpty(room) || string.IsNullOrEmpty(username))
                return;

            users[Context.ConnectionId] = username;

            if (!userRooms.ContainsKey(Context.ConnectionId))
                userRooms[Context.ConnectionId] = new HashSet<string>();

            userRooms[Context.ConnectionId].Add(room);

            if (!rooms.ContainsKey(room))
                rooms[room] = new HashSet<string>();

            rooms[room].Add(Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, room);

            if (!roomMessages.ContainsKey(room))
                roomMessages[room] = new List<ChatMessage>();

            var joinMessage = new ChatMessage { User = "Sistema", Message = $"{username} se ha unido a la sala." };
            roomMessages[room].Add(joinMessage);
            await Clients.Group(room).SendAsync("ReceiveMessage", room, joinMessage.User, joinMessage.Message);

            if (roomMessages.TryGetValue(room, out var messages))
            {
                var validMessages = messages.Where(m => 
                    !string.IsNullOrEmpty(m.User) && 
                    !string.IsNullOrEmpty(m.Message) && 
                    m.User != "undefined" && 
                    m.Message != "undefined"
                ).ToList();
                
                if (validMessages.Any())
                {
                    await Clients.Caller.SendAsync("ReceiveHistory", room, validMessages);
                }
            }
        }

        public async Task SendMessageToRoom(string room, string message)
        {
            if (string.IsNullOrEmpty(room) || string.IsNullOrEmpty(message))
                return;

            if (users.TryGetValue(Context.ConnectionId, out var username))
            {
                if (!roomMessages.ContainsKey(room))
                    roomMessages[room] = new List<ChatMessage>();

                var chatMessage = new ChatMessage { User = username, Message = message };
                roomMessages[room].Add(chatMessage);

                await Clients.Group(room).SendAsync("ReceiveMessage", room, username, message);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (users.TryRemove(Context.ConnectionId, out var username))
            {
                if (userRooms.TryRemove(Context.ConnectionId, out var joinedRooms))
                {
                    foreach (var room in joinedRooms)
                    {
                        if (rooms.TryGetValue(room, out var connSet))
                        {
                            connSet.Remove(Context.ConnectionId);
                            await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
                            
                            if (roomMessages.TryGetValue(room, out var messages))
                            {
                                var leaveMessage = new ChatMessage { User = "Sistema", Message = $"{username} ha salido de la sala." };
                                messages.Add(leaveMessage);
                                await Clients.Group(room).SendAsync("ReceiveMessage", room, leaveMessage.User, leaveMessage.Message);
                            }
                        }
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

    public class ChatMessage
    {
        public string User { get; set; }
        public string Message { get; set; }
    }
}