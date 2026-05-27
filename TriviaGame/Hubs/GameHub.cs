// SignalR Hub לניהול עדכוני זמן אמת של חדרים (לובי/משחק/יציאה/סגירה)

using DBL;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TriviaGame.Hubs
{
    public class GameHub : Hub
    {
        // יצירת שם Group קבוע עבור כל קוד חדר
        private static string RoomGroup(string roomCode) => $"room:{roomCode}";
        // מיפוי זמני בין ConnectionId ל-(קוד חדר, מזהה משתמש) לצורך ניתוק/ניקוי
        private static readonly ConcurrentDictionary<string, (string RoomCode, int UserId)> Connections = new();
        // גישה לשכבת חדרים במסד
        private readonly RoomDB roomDB = new();

        // הצטרפות לקבוצת SignalR של חדר (לובי או משחק)
        public async Task JoinRoomGroup(string roomCode, int userId)
        {
            // ולידציה בסיסית לקלט
            if (string.IsNullOrWhiteSpace(roomCode) || userId <= 0) return;
            // נרמול קוד חדר לשימוש אחיד
            var trimmed = roomCode.Trim().ToUpperInvariant();
            // שמירת מיפוי החיבור הנוכחי לזיהוי עתידי
            Connections[Context.ConnectionId] = (trimmed, userId);
            // הוספת החיבור לקבוצת החדר
            await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(trimmed));
        }

        // יציאה יזומה מחדר מצד הלקוח
        public async Task LeaveRoomGroup(string roomCode)
        {
            // אם קוד חדר לא תקין לא ממשיכים
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            var trimmed = roomCode.Trim().ToUpperInvariant();
            // הסרת החיבור מהמיפוי
            Connections.TryRemove(Context.ConnectionId, out var info);
            // הוצאה מקבוצת החדר
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroup(trimmed));

            try
            {
                // שליפת החדר מהמסד לפי קוד
                var room = await roomDB.GetRoomByCodeAsync(trimmed);
                if (room is null)
                    return;

                // טיפול ביציאת משתמש לפי סוג המשתמש (מארח/שחקן רגיל)
                if (info.UserId > 0)
                {
                    if (room.HostID == info.UserId)
                    {
                        // אם המארח יצא - סוגרים את החדר לכל המשתתפים
                        await roomDB.DeleteRoomAsync(room.RoomID);
                        await Clients.Group(RoomGroup(trimmed)).SendAsync("RoomClosed", trimmed);
                    }
                    else
                    {
                        // אם שחקן רגיל יצא - מוחקים אותו מהחדר
                        await roomDB.RemovePlayerAsync(room.RoomID, info.UserId);
                        // אם לא נשארו שחקנים - מוחקים את החדר
                        var deleted = await roomDB.DeleteRoomIfNoPlayersAsync(room.RoomID);
                        if (deleted)
                            await Clients.Group(RoomGroup(trimmed)).SendAsync("RoomClosed", trimmed);
                        else
                            // אחרת מודיעים שנשארו שחקנים אך אחד עזב
                            await Clients.Group(RoomGroup(trimmed)).SendAsync("PlayerLeft", trimmed);
                    }
                }

                // רענון רשימת חדרים ציבוריים לכל הלקוחות
                await Clients.All.SendAsync("PublicRoomChanged");
            }
            catch
            {
                // ניקוי best-effort בלבד; לא זורקים שגיאה החוצה
            }
        }

        // אירוע שחקן הצטרף: רענון לובי לכל הקבוצה
        public async Task PlayerJoined(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            await Clients.Group(RoomGroup(roomCode.Trim()))
                .SendAsync("PlayerJoined", roomCode.Trim());
        }

        // אירוע התחלת משחק: מעבר כל חברי החדר למסך משחק
        public async Task GameStarted(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            await Clients.Group(RoomGroup(roomCode.Trim()))
                .SendAsync("GameStarted", roomCode.Trim());
        }

        // אירוע שאלה הבאה: רענון שאלה נוכחית לכל השחקנים
        public async Task NextQuestion(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            await Clients.Group(RoomGroup(roomCode.Trim()))
                .SendAsync("NextQuestion", roomCode.Trim());
        }

        // אירוע תשובה נשלחה: עדכון מוני תשובות בצד לקוח
        public async Task AnswerSubmitted(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            await Clients.Group(RoomGroup(roomCode.Trim()))
                .SendAsync("AnswerSubmitted", roomCode.Trim());
        }

        // רענון גלובלי של רשימת חדרים ציבוריים
        public async Task PublicRoomChanged()
        {
            await Clients.All.SendAsync("PublicRoomChanged");
        }

        // Heartbeat מהלקוח לשמירת פעילות חדר ושחקן
        public async Task RoomHeartbeat(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            try
            {
                // עדכון פעילות חדר לפי קוד
                await roomDB.UpdateRoomLastSeenAsync(roomCode.Trim());
                if (Connections.TryGetValue(Context.ConnectionId, out var info))
                {
                    // עדכון פעילות שחקן לפי ConnectionId שמופה מראש
                    await roomDB.UpdateRoomPlayerLastSeenAsync(info.RoomCode, info.UserId);
                }
            }
            catch
            {
                // Heartbeat הוא best-effort; לא חוסמים בגלל כשל נקודתי
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // בעת ניתוק, מנסים לאתר מאיזה חדר/משתמש הגיע החיבור
            if (Connections.TryRemove(Context.ConnectionId, out var info))
            {
                try
                {
                    // שליפת החדר כדי לנקות נוכחות בצורה נכונה
                    var room = await roomDB.GetRoomByCodeAsync(info.RoomCode);
                    if (room != null)
                    {
                        if (room.HostID == info.UserId)
                        {
                            // ניתוק מארח סוגר חדר
                            await roomDB.DeleteRoomAsync(room.RoomID);
                            await Clients.Group(RoomGroup(info.RoomCode))
                                .SendAsync("RoomClosed", info.RoomCode);
                        }
                        else
                        {
                            // ניתוק שחקן רגיל מסיר אותו מהחדר
                            await roomDB.RemovePlayerAsync(room.RoomID, info.UserId);
                            var deleted = await roomDB.DeleteRoomIfNoPlayersAsync(room.RoomID);
                            if (deleted)
                            {
                                // אם החדר התרוקן - מודיעים על סגירתו
                                await Clients.Group(RoomGroup(info.RoomCode))
                                    .SendAsync("RoomClosed", info.RoomCode);
                            }
                            else
                            {
                                // אחרת מודיעים ששחקן יצא
                                await Clients.Group(RoomGroup(info.RoomCode))
                                    .SendAsync("PlayerLeft", info.RoomCode);
                            }
                        }

                        // כל שינוי בהרכב חדרים דורש רענון רשימה ציבורית
                        await Clients.All.SendAsync("PublicRoomChanged");
                    }
                }
                catch
                {
                    // ניקוי best-effort כדי לא לעכב תהליך ניתוק
                }
            }

            // המשך תהליך ניתוק ברירת מחדל של SignalR
            await base.OnDisconnectedAsync(exception);
        }
    }
}
