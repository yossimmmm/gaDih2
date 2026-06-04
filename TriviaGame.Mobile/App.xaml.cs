namespace TriviaGame.Mobile;

// App הוא האובייקט העליון של אפליקציית MAUI.
// הוא יוצר את חלון ההפעלה הראשי ומחבר אותו ל-AppShell.
public partial class App : Application
{
	public App()
	{
		// טוען את המשאבים המשותפים שהוגדרו ב-XAML.
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// MAUI מבקש מאיתנו חלון ראשי.
		// כאן מחזירים Window שמכיל את AppShell.
		return new Window(new AppShell());
	}
}
