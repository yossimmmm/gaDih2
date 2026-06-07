using DBL;

namespace TriviaGame.Services;

// #room-cleanup #disconnect #heartbeat #last-seen
// שירות רקע שרץ בזמן שהאתר פתוח ומנקה חדרים/שחקנים שנשארו בגלל ניתוק לא מסודר.
// זה משלים את GameHub.OnDisconnectedAsync: ה-Hub מטפל בניתוק רגיל, והשירות הזה מטפל במקרים שבהם הניתוק לא הגיע נקי לשרת.
public sealed class RoomCleanupService : BackgroundService
{
    // כל כמה זמן מריצים את הניקוי.
    // דקה אחת מספיק קרובה כדי שחדרים מתים לא יישארו הרבה זמן, אבל לא מכבידה על המסד.
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);

    private readonly ILogger<RoomCleanupService> logger;

    public RoomCleanupService(ILogger<RoomCleanupService> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // השירות מתחיל יחד עם האתר ונשאר בלולאה עד שהשרת נסגר.
        // stoppingToken מסמן שהאפליקציה בתהליך כיבוי ולכן צריך לעצור בצורה נקייה.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // RoomDB הוא אובייקט DBL פשוט; כל פעולה שלו פותחת חיבור MySQL, מריצה SQL וסוגרת את החיבור.
                var roomDb = new RoomDB();

                // כאן מתבצע הניקוי בפועל:
                // 1. חדרים ישנים מדי
                // 2. שחקנים שלא שלחו heartbeat
                // 3. חדרים שנשארו בלי שחקנים
                var deletedRows = await roomDb.CleanupDisconnectedRoomsAsync();

                // לא מדפיסים לוג כל דקה אם לא קרה כלום, כדי לא להציף את ה-console.
                if (deletedRows > 0)
                {
                    logger.LogInformation("Room cleanup removed {DeletedRows} stale room records.", deletedRows);
                }
            }
            catch (Exception ex)
            {
                // ניקוי רקע לא אמור להפיל את האתר.
                // אם MySQL נפל רגעית או יש שגיאה זמנית, רושמים לוג ומנסים שוב בסיבוב הבא.
                logger.LogWarning(ex, "Room cleanup failed.");
            }

            // מחכים למחזור הבא.
            // אם השרת נסגר בזמן ההמתנה, stoppingToken יבטל את ההמתנה.
            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }
}
