# TriviaGame.Mobile appsettings

- `Api.AppCode`: הקוד שכל בקשה מהאפליקציה שולחת ל-API ב-header בשם `X-App-Code`.
- `Api.Environment`: הסביבה הנוכחית שהאפליקציה משתמשת בה, למשל Development.
- `Api.Development.DesktopBaseUrl`: הכתובת של ה-API כשמריצים את MAUI על Windows.
- `Api.Development.AndroidEmulatorBaseUrl`: הכתובת המיוחדת ל-Android emulator, בדרך כלל `10.0.2.2`.
- `Api.Development.DeviceBaseUrl`: כתובת LAN של המחשב כשמריצים על טלפון אמיתי.
- `Api.Staging` / `Api.Production`: כתובות מוכנות לסביבות אחרות.

## איך זה עובד
1. `MauiProgram` טוען את הקובץ הזה בזמן הפעלה.
2. `ApiEndpointResolver` קורא ממנו את הערכים.
3. `ApiClient` בונה כתובת מלאה לכל endpoint.
4. כל כפתור ב-UI שולח את הבקשה ל-API דרך אותו נתיב.
