using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Models;
using TriviaGame.Services;
using System.Threading.Tasks;

namespace TriviaGame.Components
{
    public abstract class AuthPageBase : ComponentBase
    {
        // שירות JSInterop לקריאות auth.js בצד לקוח
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        // שירות ניווט בין עמודים
        [Inject] protected NavigationManager Nav { get; set; } = default!;
        // מצב סשן מקומי של המשתמש המחובר
        [Inject] protected UserSessionState Session { get; set; } = default!;
        // שירות עזר לאימות משתמש מתוך HttpContext
        [Inject] protected AuthService Auth { get; set; } = default!;

        // דגל שמבטיח שבדיקת auth בצד לקוח תבוצע פעם אחת בלבד
        private bool _authChecked;

        // נקודת הרחבה למחלקות יורשות לאחר שהמשתמש אומת
        protected virtual Task OnAuthedAsync()
        {
            return Task.CompletedTask;
        }

        protected override async Task OnInitializedAsync()
        {
            // ניסיון ראשון לאימות בצד שרת/HttpContext (SSR)
            if (Session.CurrentUserId is null)
            {
                var state = await Auth.TryGetAuthStateFromHttpContextAsync();
                if (state.HasValue)
                {
                    if (state.Value.UserId > 0)
                    {
                        // שמירת פרטי משתמש בסשן המקומי
                        Session.CurrentUserId = state.Value.UserId;
                        Session.CurrentRole = state.Value.Role;
                    }
                    else
                    {
                        // אין משתמש מחובר -> מעבר ללוגין
                        Nav.NavigateTo("/login", true);
                    }
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // מניעת קריאות כפולות לבדיקת auth.me
            if (_authChecked)
                return;

            _authChecked = true;

            // ניסיון שני לאימות דרך JS (עבור הידרציה/לקוח)
            if (Session.CurrentUserId is null)
            {
                var me = await JS.InvokeAsync<AuthMeResponse>("auth.me");
                if (me is not null && me.UserId > 0)
                {
                    // עדכון סשן מקומי מהתשובה
                    Session.CurrentUserId = me.UserId;
                    Session.CurrentRole = ParseRole(me.Role);
                }
            }

            // אם עדיין אין משתמש מחובר - חוזרים ללוגין
            if (Session.CurrentUserId is null)
            {
                Nav.NavigateTo("/login", true);
                return;
            }

            // קריאה להוק של העמוד היורש אחרי אימות מלא
            await OnAuthedAsync();
            // רענון תצוגה כדי לשקף מצב auth שנקבע
            StateHasChanged();
        }

        // DTO לתשובת auth.me מה-JS
        private sealed class AuthMeResponse
        {
            // מזהה משתמש מחובר
            public int UserId { get; set; }
            // תפקיד משתמש טקסטואלי
            public string Role { get; set; } = "User";
        }

        private static UserRole ParseRole(string? raw)
        {
            // המרת מחרוזת role ל-enum פנימי
            return raw?.Trim().ToLowerInvariant() switch
            {
                "admin" => UserRole.Admin,
                "manager" => UserRole.Manager,
                // ברירת מחדל לתפקיד משתמש רגיל
                _ => UserRole.User
            };
        }
    }
}
