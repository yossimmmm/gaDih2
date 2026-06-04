using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile;

// MauiProgram הוא קובץ ההפעלה של אפליקציית ה־MAUI.
// כאן מגדירים את האפליקציה, הטעינה של הקונפיגורציה, וה־services ל־DI.
public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		// מתחילים Builder שמרכז את כל ההגדרות של האפליקציה.
		var builder = MauiApp.CreateBuilder();
		builder
			// מחברים את מחלקת App הראשית ל־MAUI runtime.
			.UseMauiApp<App>()
			// רושמים פונטים כך שה־UI יוכל להשתמש בהם לפי שמות לוגיים.
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// טוענים את appsettings.json כדי שהאפליקציה תדע לאן לשלוח בקשות.
		builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

		// ApiEndpointResolver הוא מקור האמת ל־base URL ולקוד האפליקציה.
		builder.Services.AddSingleton<ApiEndpointResolver>();

		// HttpClient אחד לכל האפליקציה.
		// כל קריאה ל־API עוברת דרכו, ו־ApiClient מוסיף את הלוגיקה המיוחדת של הפרויקט.
		builder.Services.AddSingleton(_ => new HttpClient());

		// עטיפה שמבצעת בקשות HTTP, מוסיפה headers ומטפלת ב־retries ובשגיאות.
		builder.Services.AddSingleton<ApiClient>();

		// שכבת ה־API הטיפוסית של ה־MAUI:
		// דרכה קוראים ל־Login, JoinRoom, SubmitAnswer ועוד.
		builder.Services.AddSingleton<TriviaApiClient>();

		// רושמים את המסך הראשי כדי שיוכל לקבל services דרך DI.
		builder.Services.AddSingleton<MainPage>();

#if DEBUG
		// ב־debug מוסיפים לוגים כדי לראות מה קורה בזמן פיתוח.
		builder.Logging.AddDebug();
#endif

		// בונים את האפליקציה הסופית.
		return builder.Build();
	}
}
