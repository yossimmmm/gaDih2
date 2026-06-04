using Microsoft.Extensions.DependencyInjection;

namespace TriviaGame.Mobile;

// App הוא אובייקט השורש של MAUI.
// ממנו מתחיל כל lifecycle של האפליקציה, והוא יוצר את החלון הראשי.
public partial class App : Application
{
	public App()
	{
		// טוען את המשאבים הגלובליים מה-XAML:
		// צבעים, styles, font resources וכל מה שמשותף לכל המסכים.
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// MAUI מבקש מאיתנו חלון ראשון.
		// אנחנו מחזירים חלון שמכיל את AppShell, כלומר ה-container של כל הניווט.
		return new Window(new AppShell());
	}
}
