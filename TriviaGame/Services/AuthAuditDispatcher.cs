namespace TriviaGame.Services
{
    // אובייקט אירוע audit בסיסי לפעולות אימות (login/reset וכו')
    public sealed record AuthAuditEvent(string Action, int? UserId, string Email, string Outcome, DateTime OccurredAtUtc);

    // delegate לאזנה לאירועי audit בצורה א-סינכרונית
    public delegate Task AuthAuditHandler(AuthAuditEvent auditEvent);

    public sealed class AuthAuditDispatcher
    {
        // event שאליו שירותים יכולים להירשם כדי לקבל אירועי auth
        public event AuthAuditHandler? OnAuditAsync;

        public async Task PublishAsync(AuthAuditEvent auditEvent)
        {
            // צילום הפניות נוכחיות ל-handlers כדי למנוע race condition
            var handlers = OnAuditAsync;
            if (handlers is null)
                return;

            // מעבר על כל מאזין והפעלה א-סינכרונית שלו
            foreach (var del in handlers.GetInvocationList())
            {
                if (del is AuthAuditHandler handler)
                    await handler(auditEvent);
            }
        }
    }
}
