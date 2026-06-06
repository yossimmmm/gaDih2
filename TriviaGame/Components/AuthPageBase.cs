using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Models;
using TriviaGame.Services;
using System.Threading.Tasks;

namespace TriviaGame.Components
{
    // מחלקת בסיס לדפי Razor שדורשים משתמש מחובר לפני שמציגים תוכן.
    public abstract class AuthPageBase : ComponentBase
    {
        // גישה ל-JS כדי לקרוא לפונקציות auth בצד הלקוח.
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        // ניווט בין דפים, בעיקר להפניה חזרה ל-login כשאין הרשאה.
        [Inject] protected NavigationManager Nav { get; set; } = default!;
        // מצב session פנימי של המשתמש המחובר.
        [Inject] protected UserSessionState Session { get; set; } = default!;
        // שירות האימות שמנסה לקרוא session מה-HttpContext.
        [Inject] protected AuthService Auth { get; set; } = default!;

        // דגל שמוודא שבודקים את האימות רק פעם אחת.
        private bool _authChecked;

        // דפי יורש יכולים להחליף את המתודה הזו כדי לטעון נתונים רק אחרי אימות.
        protected virtual Task OnAuthedAsync()
        {
            return Task.CompletedTask;
        }

        // רץ בתחילת חיי הקומפוננטה ומנסה לשחזר את מצב המשתמש מהשרת.
        protected override async Task OnInitializedAsync()
        {
            // OnInitializedAsync רץ לפני הרנדר הראשון, ולכן נוח לשחזר בו מצב בסיסי של המשתמש.
            // אם אין כבר userId בזיכרון, מנסים לקרוא אותו מתוך השרת.
            if (Session.CurrentUserId is null)
            {
                // AuthService קורא את ה-session מה-HttpContext של הבקשה הנוכחית.
                var state = await Auth.TryGetAuthStateFromHttpContextAsync();
                if (state.HasValue)
                {
                    // אם נמצא משתמש תקף, שומרים אותו ב-session המקומי.
                    if (state.Value.UserId > 0)
                    {
                        Session.CurrentUserId = state.Value.UserId;
                        Session.CurrentRole = state.Value.Role;
                    }
                    else
                    {
                        // אם השרת לא מכיר משתמש תקף, שולחים ללוגין.
                        Nav.NavigateTo("/login", true);
                    }
                }
                // אם אין state בכלל, OnAfterRenderAsync ינסה fallback דרך JS.
            }
        }

        // אחרי הרנדר הראשון, מוודאים שוב שיש משתמש מחובר לפני שממשיכים.
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // בלי הדגל הזה היינו נכנסים שוב ושוב לאותה בדיקה בכל render מחדש.
            // אם כבר בדקנו אימות, לא עושים את זה שוב.
            if (_authChecked)
                return;

            // מסמנים שהבדיקה הזו בוצעה.
            _authChecked = true;

            // כאן יש fallback לצד הלקוח: אם ה-session לא הגיע מ-HttpContext, שואלים את JS.
            // אם עדיין אין userId, שואלים את ה-API של auth בצד הלקוח.
            if (Session.CurrentUserId is null)
            {
                // auth.me מחזיר userId ו-role מתוך ה-cookie של הדפדפן.
                var me = await JS.InvokeAsync<AuthMeResponse>("auth.me");
                // אם התקבל user תקף, שומרים אותו ב-session המקומי.
                if (me is not null && me.UserId > 0)
                {
                    Session.CurrentUserId = me.UserId;
                    Session.CurrentRole = ParseRole(me.Role);
                }
            }

            // אם גם אחרי fallback אין משתמש, הדף לא מורשה להמשיך.
            // אם גם עכשיו אין משתמש תקף, מעבירים ללוגין.
            if (Session.CurrentUserId is null)
            {
                Nav.NavigateTo("/login", true);
                return;
            }

            // רק אחרי שהאימות עבר, הדף יורש יכול לטעון נתונים רגישים.
            // מכאן והלאה הדף מורשה לעבוד ולמשוך נתונים משלו.
            await OnAuthedAsync();
            StateHasChanged();
        }

        // DTO קטן שמייצג את תשובת auth.me.
        private sealed class AuthMeResponse
        {
            public int UserId { get; set; }
            public string Role { get; set; } = "User";
        }

        // ממיר מחרוזת תפקיד לערך enum של המערכת.
        private static UserRole ParseRole(string? raw)
        {
            return raw?.Trim().ToLowerInvariant() switch
            {
                "admin" => UserRole.Admin,
                _ => UserRole.User
            };
        }
    }
}
