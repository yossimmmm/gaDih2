$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "TriviaGame\TriviaGame.csproj"
$ports = @(5038, 7060)

# עוצר אתר ישן לפי שם.
# אם האתר כבר רץ, הוא מחזיק DLL פתוחים וגם תופס את הפורט.
Get-Process -Name "TriviaGame" -ErrorAction SilentlyContinue |
    Stop-Process -Force

# עוצר כל תהליך שעדיין מחזיק את פורטי האתר.
# בלי זה Kestrel נכשל עם Bind כי localhost:5038 כבר תפוס.
foreach ($port in $ports) {
    Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
        ForEach-Object {
            $owner = Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue
            if ($owner) {
                Stop-Process -Id $owner.Id -Force
            }
        }
}

Start-Sleep -Milliseconds 500

$userGeminiKey = [Environment]::GetEnvironmentVariable(
    "GEMINI_API_KEY",
    "User"
)

if (-not [string]::IsNullOrWhiteSpace($userGeminiKey)) {
    $env:GEMINI_API_KEY = $userGeminiKey
}

dotnet run --project $projectPath --launch-profile http
