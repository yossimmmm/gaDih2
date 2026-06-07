$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "TriviaGame.Mobile\TriviaGame.Mobile.csproj"
$framework = "net10.0-windows10.0.19041.0"
$exePath = Join-Path $repoRoot "TriviaGame.Mobile\bin\Debug\$framework\win-x64\TriviaGame.Mobile.exe"

# עוצר אפליקציית MAUI ישנה.
# אם היא פתוחה, קובץ TriviaGame.Mobile.exe נעול וה-build נכשל.
Get-Process -Name "TriviaGame.Mobile" -ErrorAction SilentlyContinue |
    Stop-Process -Force

# נותנים ל-Windows לשחרר את קובצי ה-exe וה-DLL לפני הרצה מחדש.
Start-Sleep -Milliseconds 500

# בונים את יעד Windows במפורש כי פרויקט MAUI מכוון לכמה frameworks.
dotnet build $projectPath --framework $framework --no-restore

# מפעילים את ה-exe ישירות במקום dotnet run.
# כך הסקריפט יכול להחזיר את החלון לקדמת המסך ולא להיתקע על הודעת launch settings.
$process = Start-Process -FilePath $exePath -WorkingDirectory (Split-Path -Parent $exePath) -PassThru

# קוד Windows קטן שמחזיר חלון ממוזער ומעלה אותו לקדמת המסך.
Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class WindowTools
{
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}
"@

# מחכים ש-MAUI יסיים ליצור חלון אמיתי.
for ($i = 0; $i -lt 40; $i++) {
    $process.Refresh()
    if ($process.MainWindowHandle -ne 0) {
        break
    }

    Start-Sleep -Milliseconds 250
}

if ($process.MainWindowHandle -ne 0) {
    # 9 = restore, כלומר להחזיר חלון אם הוא נפתח ממוזער.
    [WindowTools]::ShowWindow($process.MainWindowHandle, 9) | Out-Null
    [WindowTools]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
    Write-Host "MAUI is running. PID=$($process.Id)"
} else {
    Write-Host "MAUI process started, but no window handle was found yet. PID=$($process.Id)"
}
