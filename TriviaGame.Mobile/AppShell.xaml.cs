namespace TriviaGame.Mobile;

// AppShell הוא ה-shell של MAUI:
// הוא מגדיר את מבנה הניווט של האפליקציה.
// בפרויקט הזה יש מסך ראשי אחד, ולכן ה-shell פשוט במיוחד.
public partial class AppShell : Shell
{
	public AppShell()
	{
		// טוען את הגדרת ה-XAML של ה-shell.
		// כאן MAUI בונה את המבנה של המסך וה-routing.
		InitializeComponent();
	}
}
