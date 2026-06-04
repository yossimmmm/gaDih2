using Foundation;

namespace TriviaGame.Mobile;

// נקודת הכניסה של Mac Catalyst.
// כמו ב-iOS, גם כאן הפלטפורמה משתמשת באותו MAUI builder משותף.
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
