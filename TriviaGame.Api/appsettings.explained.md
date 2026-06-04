# TriviaGame.Api appsettings

## `appsettings.json`
- `Logging`: קובע את רמת הלוגים של השרת.
- `AllowedHosts`: מאפשר לשרת לענות מכל host.
- `Api.AppCode`: הקוד המשותף שכל בקשה מה-MAUI שולחת ב-`X-App-Code`.
- `Gemini`: הגדרות לקישור ל-Gemini עבור ה-assistant endpoint.
- `Smtp`: הגדרות לשליחת מיילים לאיפוס סיסמה.

## `appsettings.Development.json`
- `Logging`: רמת לוגים מקומית לפיתוח.
- `Api.AppCode`: אותו קוד פשוט שמגיע גם ב-MAUI.
- אין כאן סודות או פרטי production, רק ערכי פיתוח.

## איך זה עובד
1. ה-API קורא את הקבצים האלה בזמן startup.
2. הערכים מוזרמים ל-`IConfiguration`.
3. השירותים ב-API קוראים אותם דרך `configuration["..."]`.
4. ה-MAUI משתמש בערכים דומים ב-`TriviaGame.Mobile/appsettings.json` כדי לבחור base URL ו-app code.
