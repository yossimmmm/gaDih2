using Microsoft.UI.Xaml;

namespace TriviaGame.Mobile.WinUI;

// הקובץ הזה הוא נקודת הכניסה של גרסת Windows.
// הוא מחבר את חלון WinUI אל ה-MAUI app builder שבנינו ב-MauiProgram.
public partial class App : MauiWinUIApplication
{
	// כאן נוצר מופע האפליקציה הראשון ב-Windows.
	// זה המקבילה של main()/WinMain() בעולם של MAUI על Windows.
	public App()
	{
		this.InitializeComponent();
	}

	// מחזיר את MAUI app המלא שנבנה בקוד המשותף.
	// כל הפלטפורמות משתמשות באותו builder כדי לשמור על התנהגות זהה.
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
