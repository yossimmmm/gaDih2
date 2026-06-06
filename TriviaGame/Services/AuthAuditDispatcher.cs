namespace TriviaGame.Services
{
    // אירוע לוג קטן שמתאר פעולה במערכת האימות.
    public sealed record AuthAuditEvent(string Action, int? UserId, string Email, string Outcome, DateTime OccurredAtUtc);

    // דלגט שמייצג מאזין לאירועי audit.
    public delegate Task AuthAuditHandler(AuthAuditEvent auditEvent);

    // מפיץ אירועי audit לכל מי שנרשם אליהם.
    public sealed class AuthAuditDispatcher
    {
        // אירוע צבירה: מי שמתחבר אליו מקבל כל הודעת audit.
        public event AuthAuditHandler? OnAuditAsync;

        // שולח את האירוע לכל המאזינים שנרשמו.
        public async Task PublishAsync(AuthAuditEvent auditEvent)
        {
            var handlers = OnAuditAsync;
            if (handlers is null)
                return;

            foreach (var del in handlers.GetInvocationList())
            {
                if (del is AuthAuditHandler handler)
                    await handler(auditEvent);
            }
        }
    }
}
