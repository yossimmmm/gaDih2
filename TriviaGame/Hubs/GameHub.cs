using DBL;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TriviaGame.Hubs
{
    // Hub של SignalR שמטפל בעדכונים חיים בין כל הלקוחות באותו חדר.
    public class GameHub : Hub
    {
        // כל חדר מקבל group משלו כדי שאפשר יהיה לשדר רק למי שבתוך אותו חדר.
        private static string RoomGroup(string roomCode) => $"room:{roomCode}";
        // זיכרון זמני של חיבורים פעילים: איזה connection שייך לאיזה חדר ואיזה משתמש.
        private static readonly ConcurrentDictionary<string, (string RoomCode, int UserId)> Connections = new();
        // שכבת הנתונים שבה נשמרים החדרים והשחקנים.
        private readonly RoomDB roomDB = new();

        // נקרא כשלקוח נכנס לחדר, כדי לצרף אותו ל-group הנכון.
        public async Task JoinRoomGroup(string roomCode, int userId)
        {
            // אם אין קוד חדר או userId תקין - לא עושים כלום.
            if (string.IsNullOrWhiteSpace(roomCode) || userId <= 0) return;
            var trimmed = roomCode.Trim().ToUpperInvariant();
            // נרמול של קוד החדר מונע מצב של אותו חדר בכמה כתיבות שונות.
            // שומרים את המיפוי כדי שנדע מי מחובר לאיזה חדר גם בזמן disconnect.
            Connections[Context.ConnectionId] = (trimmed, userId);
            // מצרפים את החיבור הנוכחי לקבוצה של החדר.
            await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(trimmed));
        }

        // נקרא כשלקוח עוזב חדר, כדי לנקות את החיבור ולעדכן את שאר המשתמשים.
        public async Task LeaveRoomGroup(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            var trimmed = roomCode.Trim().ToUpperInvariant();
            // Leave הוא ניתוק יזום; disconnect יכול לקרות גם בלי קריאה מפורשת.
            // מסירים את המיפוי מהזיכרון המקומי של ה-hub.
            Connections.TryRemove(Context.ConnectionId, out var info);
            // מוציאים את connection מה-group של החדר.
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroup(trimmed));

            try
            {
                // מאתרים את החדר במסד כדי לדעת אם צריך למחוק אותו או רק לעדכן נוכחות.
                var room = await roomDB.GetRoomByCodeAsync(trimmed);
                if (room is null)
                    return;

                if (info.UserId > 0)
                {
                    // אם מי שעוזב הוא המארח, מוחקים את החדר כולו.
                    if (room.HostID == info.UserId)
                    {
                        // כשהמארח עוזב, החדר כולו נסגר.
                        await roomDB.DeleteRoomAsync(room.RoomID);
                        await Clients.Group(RoomGroup(trimmed)).SendAsync("RoomClosed", trimmed);
                    }
                    else
                    {
                        // שחקן רגיל יוצא מהרשימה, ואם לא נשאר אף אחד - סוגרים את החדר.
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
                // ניקוי best-effort: אם יש תקלה, לא עוצרים את ה-disconnect.
            }
        }

        // משדר לכל מי שבחדר שמישהו הצטרף, כדי לרענן רשימת שחקנים.
        public async Task PlayerJoined(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            // השידור הזה נשלח רק לחברים של החדר.
            await Clients.Group(RoomGroup(roomCode.Trim()))
                .SendAsync("PlayerJoined", roomCode.Trim());
        }

        // משדר התחלת משחק לכל הלקוחות בחדר.
        public async Task GameStarted(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            // כל מי שנמצא בחדר צריך לעבור יחד למסך המשחק.
            await Clients.Group(RoomGroup(roomCode.Trim()))
                .SendAsync("GameStarted", roomCode.Trim());
        }

        // משדר מעבר לשאלה הבאה לכל הלקוחות בחדר.
        public async Task NextQuestion(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            // המעבר לשאלה הבאה חייב להיות מסונכרן.
            await Clients.Group(RoomGroup(roomCode.Trim()))
                .SendAsync("NextQuestion", roomCode.Trim());
        }

        // משדר שאחת התשובות נשלחה כדי שהשאר יראו עדכון מיידי.
        public async Task AnswerSubmitted(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            // עדכון בזמן אמת של תהליך התשובות בחדר.
            await Clients.Group(RoomGroup(roomCode.Trim()))
                .SendAsync("AnswerSubmitted", roomCode.Trim());
        }

        // משדר לכל הלקוחות שהרשימה הציבורית של החדרים השתנתה.
        public async Task PublicRoomChanged()
        {
            // כאן משדרים לכל הלקוחות כי רשימת החדרים הציבורית גלובלית.
            await Clients.All.SendAsync("PublicRoomChanged");
        }

        // פעימת לב תקופתית מהלקוח כדי לסמן שהחדר והשחקן עדיין פעילים.
        public async Task RoomHeartbeat(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            try
            {
                // מעדכנים last_seen של החדר במסד.
                await roomDB.UpdateRoomLastSeenAsync(roomCode.Trim());
                // אם יודעים מי המשתמש הזה, מעדכנים גם את זמן הפעילות שלו.
                if (Connections.TryGetValue(Context.ConnectionId, out var info))
                {
                    await roomDB.UpdateRoomPlayerLastSeenAsync(info.RoomCode, info.UserId);
                }
            }
            catch
            {
                // heartbeat הוא best-effort; כשל קצר לא אמור להפיל את ה-UI.
            }
        }

        // נקרא כשהחיבור מתנתק, למשל אם המשתמש סגר טאב או איבד רשת.
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // מנסים לנקות את ה-connection מהזיכרון ולפעול לפי תפקיד המשתמש.
            if (Connections.TryRemove(Context.ConnectionId, out var info))
            {
                try
                {
                    // בודקים אם החדר עדיין קיים במסד.
                    var room = await roomDB.GetRoomByCodeAsync(info.RoomCode);
                    if (room != null)
                    {
                        // אם המארח התנתק, סוגרים את החדר לכל המשתתפים.
                        if (room.HostID == info.UserId)
                        {
                            await roomDB.DeleteRoomAsync(room.RoomID);
                            await Clients.Group(RoomGroup(info.RoomCode))
                                .SendAsync("RoomClosed", info.RoomCode);
                        }
                        else
                        {
                            // אם שחקן רגיל התנתק, מסירים אותו מהחדר.
                            await roomDB.RemovePlayerAsync(room.RoomID, info.UserId);
                            var deleted = await roomDB.DeleteRoomIfNoPlayersAsync(room.RoomID);
                            if (deleted)
                            {
                                // אם כבר אין שחקנים, סוגרים את החדר.
                                await Clients.Group(RoomGroup(info.RoomCode))
                                    .SendAsync("RoomClosed", info.RoomCode);
                            }
                            else
                            {
                                // אחרת רק מודיעים לשאר שהוא עזב.
                                await Clients.Group(RoomGroup(info.RoomCode))
                                    .SendAsync("PlayerLeft", info.RoomCode);
                            }
                        }

                        // מעדכנים גם את רשימת החדרים הציבורית.
                        await Clients.All.SendAsync("PublicRoomChanged");
                    }
                }
                catch
                {
                    // ניקוי best-effort; לא עוצרים את ה-disconnect בגלל תקלה משנית.
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
