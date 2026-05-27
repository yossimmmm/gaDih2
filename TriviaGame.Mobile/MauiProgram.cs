using Microsoft.Extensions.Logging;

namespace TriviaGame.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		// יצירת builder ראשי של אפליקציית MAUI
		var builder = MauiApp.CreateBuilder();
		builder
			// הגדרת מחלקת App כשורש האפליקציה
			.UseMauiApp<App>()
			// רישום פונטים לשימוש בכל המסכים
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		// הפעלת לוג דיבוג בסביבת פיתוח
		builder.Logging.AddDebug();
#endif

		// בנייה והחזרה של מופע האפליקציה המוכן להרצה
		return builder.Build();
	}
}
