$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "TriviaGame.Api\TriviaGame.Api.csproj"
$websiteDevSettingsPath = Join-Path $repoRoot "TriviaGame\appsettings.Development.json"
$ports = @(5297, 7045)

# עוצר תהליך API ישן לפי שם.
# זה המקרה הרגיל אחרי שהרצת dotnet run והשרת נשאר פתוח ברקע.
Get-Process -Name "TriviaGame.Api" -ErrorAction SilentlyContinue |
    Stop-Process -Force

# עוצר כל תהליך שעדיין מחזיק את הפורטים של ה-API.
# בלי זה Kestrel נכשל עם Bind כי localhost:5297 כבר תפוס.
foreach ($port in $ports) {
    Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
        ForEach-Object {
            $owner = Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue
            if ($owner) {
                Stop-Process -Id $owner.Id -Force
            }
        }
}

# נותנים ל-Windows לשחרר את ה-DLL ואת ה-port לפני build חדש.
Start-Sleep -Milliseconds 500

# ה-API צריך SMTP בשביל Forgot Password.
# אם אין משתני סביבה, טוענים את הגדרות הפיתוח שכבר קיימות בפרויקט האתר.
# זה לא שומר סיסמה חדשה בסקריפט; הוא רק מעביר את הערכים בזמן הרצה לאותו process.
if (Test-Path $websiteDevSettingsPath) {
    $websiteDevSettings = Get-Content -LiteralPath $websiteDevSettingsPath -Raw | ConvertFrom-Json
    if ($websiteDevSettings.Smtp) {
        if ([string]::IsNullOrWhiteSpace($env:SMTP_FROM)) {
            $env:SMTP_FROM = [string]$websiteDevSettings.Smtp.From
        }

        if ([string]::IsNullOrWhiteSpace($env:SMTP_HOST)) {
            $env:SMTP_HOST = [string]$websiteDevSettings.Smtp.Host
        }

        if ([string]::IsNullOrWhiteSpace($env:SMTP_USER)) {
            $env:SMTP_USER = [string]$websiteDevSettings.Smtp.User
        }

        if ([string]::IsNullOrWhiteSpace($env:SMTP_PASS)) {
            $env:SMTP_PASS = [string]$websiteDevSettings.Smtp.Pass
        }

        if ([string]::IsNullOrWhiteSpace($env:SMTP_PORT)) {
            $env:SMTP_PORT = [string]$websiteDevSettings.Smtp.Port
        }

        if ([string]::IsNullOrWhiteSpace($env:SMTP_SECURE)) {
            $env:SMTP_SECURE = [string]$websiteDevSettings.Smtp.Secure
        }
    }
}

# בונים לפני run כדי שהשגיאה תופיע נקי אם יש בעיית קוד.
dotnet build $projectPath --no-restore

# מריצים את ה-API בפרופיל http.
# הכתובת הצפויה: http://localhost:5297
dotnet run --project $projectPath --no-build --launch-profile http
