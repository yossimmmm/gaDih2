using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Models;
using TriviaGame.Services;
using System.Threading.Tasks;

namespace TriviaGame.Components
{
    public abstract class AuthPageBase : ComponentBase
    {
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        [Inject] protected NavigationManager Nav { get; set; } = default!;
        [Inject] protected UserSessionState Session { get; set; } = default!;
        [Inject] protected AuthService Auth { get; set; } = default!;

        private bool _authChecked;

        protected virtual Task OnAuthedAsync()
        {
            return Task.CompletedTask;
        }

        protected override async Task OnInitializedAsync()
        {
            if (Session.CurrentUserId is null)
            {
                var state = await Auth.TryGetAuthStateFromHttpContextAsync();
                if (state.HasValue)
                {
                    if (state.Value.UserId > 0)
                    {
                        Session.CurrentUserId = state.Value.UserId;
                        Session.CurrentRole = state.Value.Role;
                    }
                    else
                    {
                        Nav.NavigateTo("/login", true);
                    }
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_authChecked)
                return;

            _authChecked = true;

            if (Session.CurrentUserId is null)
            {
                var me = await JS.InvokeAsync<AuthMeResponse>("auth.me");
                if (me is not null && me.UserId > 0)
                {
                    Session.CurrentUserId = me.UserId;
                    Session.CurrentRole = ParseRole(me.Role);
                }
            }

            if (Session.CurrentUserId is null)
            {
                Nav.NavigateTo("/login", true);
                return;
            }

            await OnAuthedAsync();
            StateHasChanged();
        }

        private sealed class AuthMeResponse
        {
            public int UserId { get; set; }
            public string Role { get; set; } = "User";
        }

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
