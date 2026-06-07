using TriviaGame.Mobile.Pages;

namespace TriviaGame.Mobile;

// AppShell מגדיר את מבנה הניווט של אפליקציית MAUI.
// במקום MainPage אחד ענק, יש routes נפרדים: login, menu, rooms, play, stats, settings.
public partial class AppShell : Shell
{
	public AppShell()
	{
		// טוען את AppShell.xaml ואת ה-ShellContent שמוגדרים בו.
		InitializeComponent();

		// רישום routes גם בקוד מקל על ניווט עתידי עם Shell.Current.GoToAsync.
		// כרגע גם ה-XAML מכיל Route, אבל כאן רואים בבירור איזה דפים קיימים באפליקציה.
		Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
		Routing.RegisterRoute(nameof(MenuPage), typeof(MenuPage));
		Routing.RegisterRoute(nameof(RoomsPage), typeof(RoomsPage));
		Routing.RegisterRoute(nameof(PlayPage), typeof(PlayPage));
		Routing.RegisterRoute(nameof(StatsPage), typeof(StatsPage));
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
	}
}
