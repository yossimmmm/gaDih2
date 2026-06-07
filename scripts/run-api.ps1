$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "TriviaGame.Api\TriviaGame.Api.csproj"
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

# בונים לפני run כדי שהשגיאה תופיע נקי אם יש בעיית קוד.
dotnet build $projectPath --no-restore

# מריצים את ה-API בפרופיל http.
# הכתובת הצפויה: http://localhost:5297
dotnet run --project $projectPath --no-build --launch-profile http
