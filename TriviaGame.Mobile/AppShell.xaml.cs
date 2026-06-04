namespace TriviaGame.Mobile;

// AppShell מגדיר את מבנה הניווט של האפליקציה.
// בפרויקט הזה יש כרגע מסך יחיד, ולכן ה-Shell פשוט מאוד.
public partial class AppShell : Shell
{
	public AppShell()
	{
		// טוען את ה-XAML של ה-Shell ואת הגדרת ה-routing.
		InitializeComponent();
	}
}
