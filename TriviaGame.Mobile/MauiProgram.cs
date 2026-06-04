using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile;

// MauiProgram הוא קובץ ההתחלה של MAUI.
// כאן אנחנו בונים את האפליקציה, טוענים קונפיגורציה, ורושמים services ל-DI.
public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		// מתחילים Builder שמרכז את כל ההגדרות של האפליקציה.
		var builder = MauiApp.CreateBuilder();
		builder
			// מחברים את מחלקת App הראשית ל-MAUI runtime.
			.UseMauiApp<App>()
			// רושמים fonts כך שה-UI יכול להשתמש בהם דרך שמות לוגיים.
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// טוענים את appsettings.json כדי שהאפליקציה תדע לאן לשלוח בקשות.
		builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

		// ApiEndpointResolver הוא מקור האמת לבחירת base URL ו-app code.
		builder.Services.AddSingleton<ApiEndpointResolver>();

		// HttpClient אחד לכל האפליקציה.
		// כל קריאה ל-API תעבור דרכו, וה-ApiClient יוסיף עליו את הלוגיקה הנדרשת.
		builder.Services.AddSingleton(_ => new HttpClient());

		// השכבה הנמוכה שמבצעת HTTP בפועל, מוסיפה headers ומטפלת בשגיאות.
		builder.Services.AddSingleton<ApiClient>();

		// שכבת ה-API העסקית של ה-MAUI:
		// כאן נמצאות מתודות כמו Login, JoinRoom, SubmitAnswer, וכו'.
		builder.Services.AddSingleton<TriviaApiClient>();

		// רושמים את המסך הראשי כך שהוא יקבל את השירותים דרך DI.
		builder.Services.AddSingleton<MainPage>();

#if DEBUG
		// ב-debug מוסיפים לוגים כדי לראות מה קורה בזמן ריצה.
		builder.Logging.AddDebug();
#endif

		// בונים את האפליקציה המלאה.
		return builder.Build();
	}
}
