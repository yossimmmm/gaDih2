$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "TriviaGame\TriviaGame.csproj"

# תהליך אתר פעיל מחזיק את קובצי ה-DLL פתוחים, ולכן עוצרים אותו לפני בנייה מחדש.
Get-Process -Name "TriviaGame" -ErrorAction SilentlyContinue |
    Stop-Process -Force

Start-Sleep -Milliseconds 300

$userGeminiKey = [Environment]::GetEnvironmentVariable(
    "GEMINI_API_KEY",
    "User"
)

if (-not [string]::IsNullOrWhiteSpace($userGeminiKey)) {
    $env:GEMINI_API_KEY = $userGeminiKey
}

dotnet run --project $projectPath --launch-profile http
