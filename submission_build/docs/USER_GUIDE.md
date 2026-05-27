# User Guide

## Running Backend
1. Open terminal in `gaDih2`.
2. Run: `dotnet run --project TriviaGame\TriviaGame.csproj --launch-profile http`
3. Open: `http://localhost:5038/login`

## Main Screens
- `Login`: sign in with email/password.
- `Forgot Password`: request reset link by email.
- `Reset Password`: set a new password using token.
- `Menu`: main navigation, buttons depend on role.
- `Rooms/Lobby/Play/Results`: multiplayer game flow.
- `Admin Users` (admin only): change roles for users.

## MAUI Mobile
1. Run mobile project.
2. Set backend URL.
3. Tap `Load` to open web app in WebView.
4. Tap `Check Backend Health` for native connectivity check.
