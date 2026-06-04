# launchSettings.json

הקובץ הזה שייך רק לפיתוח מקומי.

- `profiles.http.commandName`: מריץ את הפרויקט כיישום רגיל.
- `dotnetRunMessages`: מציג הודעות ריצה של `dotnet run`.
- `launchBrowser`: קובע אם ה-IDE יפתח דפדפן אוטומטית.
- `applicationUrl`: הכתובת המקומית שבה ה-API מאזין.
- `ASPNETCORE_ENVIRONMENT=Development`: מפעיל מצב פיתוח.

## איך זה עובד
1. Visual Studio או `dotnet run` קוראים את הפרופיל הזה.
2. השרת עולה על ה-URL שהוגדר.
3. ה-MAUI משתמש באותה כתובת מקומית בזמן פיתוח.
