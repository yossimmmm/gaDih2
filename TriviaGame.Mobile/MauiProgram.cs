using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TriviaGame.Mobile.Pages;
using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile;

// MauiProgram הוא קובץ ההפעלה של אפליקציית MAUI.
// כאן MAUI בונה את האפליקציה, טוען הגדרות, ורושם services ל-dependency injection.
public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		// builder הוא המקום שבו מרכזים את כל ההגדרות לפני שהאפליקציה נבנית.
		var builder = MauiApp.CreateBuilder();
		builder
			// מחבר את מחלקת App הראשית ל-MAUI runtime.
			.UseMauiApp<App>()
			// רושם פונטים שאפשר להשתמש בהם ב-XAML לפי שמות לוגיים.
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// טוען appsettings.json של MAUI.
		// שם מוגדרים base URLs וקוד אפליקציה שה-ApiEndpointResolver צריך.
		builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

		// #api-settings #api-url
		// ApiEndpointResolver מחליט לאיזה API לפנות: localhost, Android emulator, device LAN או override ידני.
		builder.Services.AddSingleton<ApiEndpointResolver>();

		// HttpClient אחד לכל האפליקציה.
		// לא יוצרים HttpClient חדש בכל לחיצה, כדי לא לבזבז sockets ומשאבי רשת.
		builder.Services.AddSingleton(_ => new HttpClient());

		// ApiClient הוא שכבת HTTP כללית:
		// הוא בונה URL מלא, מוסיף headers, שולח JSON ומפרק JSON שחוזר מהשרת.
		builder.Services.AddSingleton<ApiClient>();

		// TriviaApiClient הוא שכבה טיפוסית מעל ApiClient:
		// במקום שהדפים יכתבו URLs, הם קוראים לפונקציות כמו LoginAsync או SubmitAnswerAsync.
		builder.Services.AddSingleton<TriviaApiClient>();

		// MobileSessionState שומר את מצב המשתמש והמשחק בזיכרון של MAUI.
		// LoginPage שומר כאן משתמש, RoomsPage שומר חדר ושחקן, PlayPage משתמש בזה למשחק.
		builder.Services.AddSingleton<MobileSessionState>();

		// הדפים החדשים של MAUI.
		// Shell מציג אותם, והם שולפים את services מה-container שנבנה כאן.
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<MenuPage>();
		builder.Services.AddTransient<RoomsPage>();
		builder.Services.AddTransient<PlayPage>();
		builder.Services.AddTransient<StatsPage>();
		builder.Services.AddTransient<SettingsPage>();

		// MainPage הישן נשאר רשום רק כ-client בדיקות אם נרצה לפתוח אותו בעתיד.
		// AppShell כבר לא משתמש בו כמסך הראשי.
		builder.Services.AddSingleton<MainPage>();

#if DEBUG
		// בזמן פיתוח מוסיפים logging כדי לראות הודעות debug של MAUI ושל HTTP.
		builder.Logging.AddDebug();
#endif

		// בונה ומחזיר את אפליקציית MAUI הסופית.
		return builder.Build();
	}
}
