$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "TriviaGame.Mobile\TriviaGame.Mobile.csproj"
$framework = "net10.0-windows10.0.19041.0"

# עוצר אפליקציית MAUI ישנה.
# אם היא פתוחה, קובץ TriviaGame.Mobile.exe נעול וה-build נכשל.
Get-Process -Name "TriviaGame.Mobile" -ErrorAction SilentlyContinue |
    Stop-Process -Force

# נותנים ל-Windows לשחרר את קובצי ה-exe וה-DLL לפני הרצה מחדש.
Start-Sleep -Milliseconds 500

# מריצים את יעד Windows במפורש כי פרויקט MAUI מכוון לכמה frameworks.
dotnet run --project $projectPath --framework $framework
