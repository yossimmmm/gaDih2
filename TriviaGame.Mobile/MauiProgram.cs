using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		// יצירת builder ראשי של אפליקציית MAUI.
		var builder = MauiApp.CreateBuilder();
		builder
			// הגדרת מחלקת App כשורש האפליקציה.
			.UseMauiApp<App>()
			// רישום פונטים לשימוש בכל המסכים.
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// טעינת קובץ הגדרות סביבות API (dev/staging/prod).
		builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

		// רישום תשתית HTTP ושירותי API.
		builder.Services.AddSingleton<ApiEndpointResolver>();
		builder.Services.AddSingleton<AuthSessionStore>();
		builder.Services.AddSingleton(sp =>
		{
			// HttpClient יחיד לכל האפליקציה לצמצום overhead של sockets.
			return new HttpClient();
		});
		builder.Services.AddSingleton<ApiClient>();
		builder.Services.AddSingleton<TriviaApiClient>();
		builder.Services.AddSingleton<MainPage>();

#if DEBUG
		// הפעלת לוג דיבוג בסביבת פיתוח.
		builder.Logging.AddDebug();
#endif

		// בנייה והחזרה של מופע האפליקציה המוכן להרצה.
		return builder.Build();
	}
}
