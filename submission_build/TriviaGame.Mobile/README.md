# TriviaGame.Mobile

MAUI mobile shell for the existing `TriviaGame` web backend.

## How it works
- The app loads your running Trivia web server inside a `WebView`.
- You set the backend URL in the app and tap **Load**.

## Start backend for phone access
From repo root:

```powershell
dotnet run --project .\TriviaGame\TriviaGame.csproj --urls "http://0.0.0.0:5038"
```

Use your computer LAN IP in the mobile app, for example:
- `http://192.168.1.23:5038`

## Build / Run MAUI app

### Android
```powershell
dotnet build .\TriviaGame.Mobile\TriviaGame.Mobile.csproj -f net10.0-android
```

Run on connected Android device/emulator:
```powershell
dotnet build .\TriviaGame.Mobile\TriviaGame.Mobile.csproj -f net10.0-android -t:Run
```

### Windows (desktop test)
```powershell
dotnet build .\TriviaGame.Mobile\TriviaGame.Mobile.csproj -f net10.0-windows10.0.19041.0
```

## Notes
- Android emulator uses `http://10.0.2.2:5038` for your host machine.
- Physical phones cannot use `localhost`; use your PC LAN IP.
- Ensure firewall allows inbound TCP 5038.
