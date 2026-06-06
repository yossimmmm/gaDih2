namespace TriviaGame.Services
{
    public sealed record AuthAuditEvent(string Action, int? UserId, string Email, string Outcome, DateTime OccurredAtUtc);

    public delegate Task AuthAuditHandler(AuthAuditEvent auditEvent);

    public sealed class AuthAuditDispatcher
    {
        public event AuthAuditHandler? OnAuditAsync;

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
