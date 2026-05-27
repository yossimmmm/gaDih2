// SignalR hub for live room updates: lobby players, game start, next question, answer submitted

using DBL;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TriviaGame.Hubs
{
    public class GameHub : Hub
    {
        // -----------------------------
        // Groups are per-room
        // -----------------------------
        private static string RoomGroup(string roomCode) => $"room:{roomCode}";
        private static readonly ConcurrentDictionary<string, (string RoomCode, int UserId)> Connections = new();
        private readonly RoomDB roomDB = new();

        // Call when client enters a room (lobby or play)
        public async Task JoinRoomGroup(string roomCode, int userId)
        {
            if (string.IsNullOrWhiteSpace(roomCode) || userId <= 0) return;
            var trimmed = roomCode.Trim().ToUpperInvariant();
            Connections[Context.ConnectionId] = (trimmed, userId);
            await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(trimmed));
        }

        // Optional: call when leaving a room
        public async Task LeaveRoomGroup(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            var trimmed = roomCode.Trim().ToUpperInvariant();
            Connections.TryRemove(Context.ConnectionId, out var info);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroup(trimmed));

            try
            {
                var room = await roomDB.GetRoomByCodeAsync(trimmed);
                if (room is null)
                    return;

                if (info.UserId > 0)
                {
                    if (room.HostID == info.UserId)
                    {
                        await roomDB.DeleteRoomAsync(room.RoomID);
                        await Clients.Group(RoomGroup(trimmed)).SendAsync("RoomClosed", trimmed);
                    }
                    else
                    {
                        await roomDB.RemovePlayerAsync(room.RoomID, info.UserId);
                        var deleted = await roomDB.DeleteRoomIfNoPlayersAsync(room.RoomID);
                        if (deleted)
                            await Clients.Group(RoomGroup(trimmed)).SendAsync("RoomClosed", trimmed);
                        else
                            await Clients.Group(RoomGroup(trimmed)).SendAsync("PlayerLeft", trimmed);
                    }
                }

                await Clients.All.SendAsync("PublicRoomChanged");
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        // -----------------------------
        // Events
        // -----------------------------
        // Broadcast: someone joined -> refresh lobby player list
        public async Task PlayerJoined(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            await Clients.Group(RoomGroup(roomCode.Trim()))
                .SendAsync("PlayerJoined", roomCode.Trim());
        }

        // Broadcast: host started game -> navigate all to /play/{roomCode}
        public async Task GameStarted(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            await Clients.Group(RoomGroup(roomCode.Trim()))
                .SendAsync("GameStarted", roomCode.Trim());
        }

        // Broadcast: move to next question -> refresh current question on clients
        public async Task NextQuestion(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            await Clients.Group(RoomGroup(roomCode.Trim()))
                .SendAsync("NextQuestion", roomCode.Trim());
        }

        // Broadcast: someone answered -> update answered counters / maybe auto-advance
        public async Task AnswerSubmitted(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            await Clients.Group(RoomGroup(roomCode.Trim()))
                .SendAsync("AnswerSubmitted", roomCode.Trim());
        }

        // Broadcast: public room list changed (new/closed)
        public async Task PublicRoomChanged()
        {
            await Clients.All.SendAsync("PublicRoomChanged");
        }

        // Heartbeat from clients to mark room as active
        public async Task RoomHeartbeat(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            try
            {
                await roomDB.UpdateRoomLastSeenAsync(roomCode.Trim());
                if (Connections.TryGetValue(Context.ConnectionId, out var info))
                {
                    await roomDB.UpdateRoomPlayerLastSeenAsync(info.RoomCode, info.UserId);
                }
            }
            catch
            {
                // Best-effort heartbeat; ignore failures.
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Connections.TryRemove(Context.ConnectionId, out var info))
            {
                try
                {
                    var room = await roomDB.GetRoomByCodeAsync(info.RoomCode);
                    if (room != null)
                    {
                        if (room.HostID == info.UserId)
                        {
                            await roomDB.DeleteRoomAsync(room.RoomID);
                            await Clients.Group(RoomGroup(info.RoomCode))
                                .SendAsync("RoomClosed", info.RoomCode);
                        }
                        else
                        {
                            await roomDB.RemovePlayerAsync(room.RoomID, info.UserId);
                            var deleted = await roomDB.DeleteRoomIfNoPlayersAsync(room.RoomID);
                            if (deleted)
                            {
                                await Clients.Group(RoomGroup(info.RoomCode))
                                    .SendAsync("RoomClosed", info.RoomCode);
                            }
                            else
                            {
                                await Clients.Group(RoomGroup(info.RoomCode))
                                    .SendAsync("PlayerLeft", info.RoomCode);
                            }
                        }

                        await Clients.All.SendAsync("PublicRoomChanged");
                    }
                }
                catch
                {
                    // Best-effort cleanup; ignore failures to keep disconnect fast.
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
