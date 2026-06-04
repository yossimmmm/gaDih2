using Foundation;

namespace TriviaGame.Mobile;

// נקודת הכניסה של iOS.
// iOS קורא ל-AppDelegate הזה, והוא מחזיר את אותו MAUI app משותף.
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
