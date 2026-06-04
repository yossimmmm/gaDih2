# רשימת משימות ללימוד לקראת הבחינה

## קבצים מרכזיים ללימוד

### מודלים ומשותף
- [ ] [ModelsTrivia/User.cs](ModelsTrivia/User.cs) - להבין משתמש, תפקידים ושדות הזדהות
- [ ] [ModelsTrivia/Room.cs](ModelsTrivia/Room.cs) - להבין חדר, קוד חדר, מצב ציבורי/פעיל
- [ ] [ModelsTrivia/RoomPlayer.cs](ModelsTrivia/RoomPlayer.cs) - להבין קשר משתמש-חדר
- [ ] [ModelsTrivia/QuestionType.cs](ModelsTrivia/QuestionType.cs) - להבין סוג שאלה
- [ ] [ModelsTrivia/Question.cs](ModelsTrivia/Question.cs) - להבין שאלה, זמן, אפשרויות
- [ ] [ModelsTrivia/QuestionOption.cs](ModelsTrivia/QuestionOption.cs) - להבין אפשרות תשובה
- [ ] [ModelsTrivia/PlayerStatsBase.cs](ModelsTrivia/PlayerStatsBase.cs) - להבין ירושה לסטטיסטיקות
- [ ] [ModelsTrivia/ScoreRow.cs](ModelsTrivia/ScoreRow.cs) - להבין שורת ניקוד לחדר
- [ ] [ModelsTrivia/TopPlayerRow.cs](ModelsTrivia/TopPlayerRow.cs) - להבין מובילי משחק
- [ ] [ModelsTrivia/ValidationHelper.cs](ModelsTrivia/ValidationHelper.cs) - להבין בדיקות קלט וסניטציה
- [ ] [ModelsTrivia/PasswordHelper.cs](ModelsTrivia/PasswordHelper.cs) - להבין hash ואימות סיסמה
- [ ] [ModelsTrivia/UserSession.cs](ModelsTrivia/UserSession.cs) - להבין מצב משתמש גלובלי

### מסד נתונים ושכבת DBL
- [ ] [trivia_game.sql](trivia_game.sql) - להבין את כל הטבלאות והקשרים
- [ ] [TriviaDBL/DB.cs](TriviaDBL/DB.cs) - להבין חיבור למסד נתונים
- [ ] [TriviaDBL/UserDB.cs](TriviaDBL/UserDB.cs) - להבין CRUD למשתמשים, reset password
- [ ] [TriviaDBL/RoomDB.cs](TriviaDBL/RoomDB.cs) - להבין יצירת חדר, הצטרפות, רשימת שחקנים
- [ ] [TriviaDBL/SessionDB.cs](TriviaDBL/SessionDB.cs) - להבין session tokens
- [ ] [TriviaDBL/GameDB.cs](TriviaDBL/GameDB.cs) - להבין בחירת שאלות, תשובות, scoreboard, סטטיסטיקות
- [ ] [TriviaDBL/QuestionTypeDB.cs](TriviaDBL/QuestionTypeDB.cs) - להבין שליפת סוגי שאלות
- [ ] [TriviaDBL/SeedData.cs](TriviaDBL/SeedData.cs) - להבין נתוני התחלה

### שרת API
- [ ] [TriviaGame.Api/Program.cs](TriviaGame.Api/Program.cs) - להבין startup, DI, CORS, X-App-Code
- [ ] [TriviaGame.Api/Contracts/ApiContracts.cs](TriviaGame.Api/Contracts/ApiContracts.cs) - להבין DTOs בין לקוח לשרת
- [ ] [TriviaGame.Api/Controllers/SystemController.cs](TriviaGame.Api/Controllers/SystemController.cs) - להבין health check
- [ ] [TriviaGame.Api/Controllers/AuthController.cs](TriviaGame.Api/Controllers/AuthController.cs) - להבין login/register/reset me
- [ ] [TriviaGame.Api/Controllers/RoomsController.cs](TriviaGame.Api/Controllers/RoomsController.cs) - להבין חדרים, הצטרפות, יציאה, players
- [ ] [TriviaGame.Api/Controllers/GameController.cs](TriviaGame.Api/Controllers/GameController.cs) - להבין start game, current question, answer, scoreboard
- [ ] [TriviaGame.Api/Controllers/UsersController.cs](TriviaGame.Api/Controllers/UsersController.cs) - להבין profile, password, stats, recent results
- [ ] [TriviaGame.Api/Controllers/AdminController.cs](TriviaGame.Api/Controllers/AdminController.cs) - להבין ניהול אדמין
- [ ] [TriviaGame.Api/Controllers/AssistantController.cs](TriviaGame.Api/Controllers/AssistantController.cs) - להבין AI assistant
- [ ] [TriviaGame.Api/Services/AuthDomainService.cs](TriviaGame.Api/Services/AuthDomainService.cs) - להבין לוגיקת auth
- [ ] [TriviaGame.Api/Services/UsersDomainService.cs](TriviaGame.Api/Services/UsersDomainService.cs) - להבין לוגיקת פרופיל והרשאות
- [ ] [TriviaGame.Api/Services/RoomsDomainService.cs](TriviaGame.Api/Services/RoomsDomainService.cs) - להבין לוגיקת חדרים
- [ ] [TriviaGame.Api/Services/GameDomainService.cs](TriviaGame.Api/Services/GameDomainService.cs) - להבין לוגיקת משחק וסטטיסטיקות
- [ ] [TriviaGame.Api/Services/AssistantDomainService.cs](TriviaGame.Api/Services/AssistantDomainService.cs) - להבין קריאה ל-Gemini
- [ ] [TriviaGame.Api/Services/EmailService.cs](TriviaGame.Api/Services/EmailService.cs) - להבין שליחת מייל
- [ ] [TriviaGame.Api/Services/SmtpSettings.cs](TriviaGame.Api/Services/SmtpSettings.cs) - להבין הגדרות SMTP
- [ ] [TriviaGame.Api/appsettings.json](TriviaGame.Api/appsettings.json) - להבין הגדרות שרת ו-AI

### לקוח MAUI
- [ ] [TriviaGame.Mobile/TriviaGame.Mobile.csproj](TriviaGame.Mobile/TriviaGame.Mobile.csproj) - להבין פלטפורמות ו-packages
- [ ] [TriviaGame.Mobile/MauiProgram.cs](TriviaGame.Mobile/MauiProgram.cs) - להבין DI וטעינת appsettings
- [ ] [TriviaGame.Mobile/MainPage.xaml](TriviaGame.Mobile/MainPage.xaml) - להבין את המסך
- [ ] [TriviaGame.Mobile/MainPage.xaml.cs](TriviaGame.Mobile/MainPage.xaml.cs) - להבין לוגיקת הכפתורים והזרימה
- [ ] [TriviaGame.Mobile/Models/ApiModels.cs](TriviaGame.Mobile/Models/ApiModels.cs) - להבין DTOs של הלקוח
- [ ] [TriviaGame.Mobile/Services/ApiClient.cs](TriviaGame.Mobile/Services/ApiClient.cs) - להבין HTTP, retries, timeout
- [ ] [TriviaGame.Mobile/Services/TriviaApiClient.cs](TriviaGame.Mobile/Services/TriviaApiClient.cs) - להבין מיפוי endpoints
- [ ] [TriviaGame.Mobile/Services/ApiEndpointResolver.cs](TriviaGame.Mobile/Services/ApiEndpointResolver.cs) - להבין בחירת כתובת שרת
- [ ] [TriviaGame.Mobile/appsettings.json](TriviaGame.Mobile/appsettings.json) - להבין כתובות וסביבות
- [ ] [TriviaGame.Mobile/README.md](TriviaGame.Mobile/README.md) - להבין איך מפעילים את הלקוח

### מסמכי עזר
- [ ] [docs/API_DEEP_DIVE_HE.md](docs/API_DEEP_DIVE_HE.md) - להבין את שרת ה-API לעומק
- [ ] [docs/SYSTEM_ANALYSIS.md](docs/SYSTEM_ANALYSIS.md) - להבין את הבעיה והפתרון
- [ ] [docs/USER_GUIDE.md](docs/USER_GUIDE.md) - להבין את מדריך המשתמש
- [ ] [docs/ERD.md](docs/ERD.md) - להבין את התרשים בין הטבלאות
- [ ] [docs/UML.md](docs/UML.md) - להבין את התרשים בין המחלקות
- [ ] [docs/USE_CASES.md](docs/USE_CASES.md) - להבין תרחישי שימוש
- [ ] [docs/ACTIVITY_DIAGRAMS.md](docs/ACTIVITY_DIAGRAMS.md) - להבין תרשימי פעילות
- [ ] [docs/FINAL_100_CHECKLIST_EN.md](docs/FINAL_100_CHECKLIST_EN.md) - להבין מה נדרש לציון מלא
- [ ] [docs/PROJECT_BOOK_HE.docx](docs/PROJECT_BOOK_HE.docx) - להבין איך הספר בנוי

## 1. להבין את המבנה הכללי של הפרויקט
- שאלות תרגול:
  - [ ] איך הפרויקט מחולק לארבע שכבות ומה התפקיד של כל שכבה?
  - [ ] מה הזרימה המלאה של המידע מהלקוח ועד למסד הנתונים וחזרה?
- מה צריך להסביר:
  - [ ] למה יש הפרדה בין Models, DBL, API ולקוח.
  - [ ] איפה נמצא ה-business logic ואיפה נשמרים הנתונים.
- [ ] להבין שיש 4 שכבות/פרויקטים עיקריים:
  - `ModelsTrivia` - מחלקות מודל, ולידציה, וסיסמאות
  - `TriviaDBL` - גישה ישירה למסד הנתונים
  - `TriviaGame.Api` - שירותי API, לוגיקה עסקית, ובקרי HTTP
  - `TriviaGame.Mobile` - לקוח MAUI שמדבר עם ה-API
- [ ] להבין מה תפקיד כל שכבה במערכת
- [ ] לדעת להסביר את זרימת הנתונים: לקוח -> API -> DB -> חזרה ללקוח
- [ ] לדעת להסביר למה הפרויקט מחולק לשכבות

## 2. להבין את דרישות הבחינה
- שאלות תרגול:
  - [ ] אילו חלקים חובה להציג בבחינה?
  - [ ] אילו חלקים בפרויקט הזה מייצגים עבודה מלאה?
- מה צריך להסביר:
  - [ ] מה נדרש בתיק הפרויקט לפי המסמך.
  - [ ] איך הפרויקט עונה על דרישות של צד שרת, צד לקוח ובסיס נתונים.
- [ ] לקרוא את שני מסמכי הבחינה ולהבין מה בודקים
- [ ] להבין מה בודקים בכל חלק של הפרויקט
- [ ] לדעת להסביר:
  - [ ] ניתוח מערכת
  - [ ] בסיס נתונים
  - [ ] מימוש צד שרת
  - [ ] מימוש צד לקוח
  - [ ] מדריך למשתמש
  - [ ] רפליקציה
- [ ] לדעת אילו דרישות קיימות בבדיקה
- [ ] לדעת אילו הרחבות קיימות בפרויקט הזה

## 3. להבין את `ModelsTrivia`
- שאלות תרגול:
  - [ ] מה ההבדל בין `User`, `Room`, `Question` ו-`QuestionOption`?
  - [ ] למה `ScoreRow` ו-`TopPlayerRow` יורשות מ-`PlayerStatsBase`?
- מה צריך להסביר:
  - [ ] תפקיד של כל מחלקה.
  - [ ] אילו שדות מייצגים ישות אמיתית ואילו שדות הם נגזרים.
- [ ] לקרוא ולהבין את `UserRole`
- [ ] לקרוא ולהבין את `User`
- [ ] לקרוא ולהבין את `Room`
- [ ] לקרוא ולהבין את `RoomPlayer`
- [ ] לקרוא ולהבין את `QuestionType`
- [ ] לקרוא ולהבין את `Question`
- [ ] לקרוא ולהבין את `QuestionOption`
- [ ] לקרוא ולהבין את `PlayerStatsBase`
- [ ] לקרוא ולהבין את `ScoreRow`
- [ ] לקרוא ולהבין את `TopPlayerRow`
- [ ] להבין את הירושה בין `PlayerStatsBase` לבין המחלקות היורשות
- [ ] לקרוא ולהבין את `ValidationHelper`
- [ ] לקרוא ולהבין את `PasswordHelper`
- [ ] לקרוא ולהבין את `UserSession`
- [ ] לדעת להסביר למה המחלקות האלה משמשות גם ב-API וגם בלקוח

## 4. להבין את מסד הנתונים
- שאלות תרגול:
  - [ ] מה תפקיד של כל טבלה ומה הקשר שלה לטבלאות האחרות?
  - [ ] איזו טבלה היא טבלת קישור ואיזו טבלה שומרת תוצאות?
- מה צריך להסביר:
  - [ ] מפות מפתח ראשי, מפתח זר, ויחסים.
  - [ ] אילו טבלאות שומרות נתונים היסטוריים ואילו טבלאות שומרות מצב חי.
- [ ] לקרוא את `trivia_game.sql`
- [ ] להבין את הטבלה `users`
- [ ] להבין את הטבלה `rooms`
- [ ] להבין את הטבלה `room_players`
- [ ] להבין את הטבלה `question_types`
- [ ] להבין את הטבלה `questions`
- [ ] להבין את הטבלה `question_options`
- [ ] להבין את הטבלה `room_questions`
- [ ] להבין את הטבלה `player_answers`
- [ ] להבין את הטבלה `game_results`
- [ ] להבין את הקשרים בין הטבלאות
- [ ] להבין מזה מפתח זר
- [ ] להבין מפתח ראשי
- [ ] להבין `room_code`
- [ ] להבין את תפקיד `room_players` ו-`room_questions`
- [ ] להבין איך נשמרות תוצאות המשחק

## 5. להבין את שכבת הגישה לנתונים (`TriviaDBL`)
- שאלות תרגול:
  - [ ] למה בכלל צריך שכבת DBL ולא לגשת ל-SQL ישירות מה-API?
  - [ ] איך שיטות כמו `Insert`, `Update`, `Delete`, `Select` ממומשות כאן?
- מה צריך להסביר:
  - [ ] איך נוצרים connection, command ו-reader.
  - [ ] איפה יש טרנזקציות ואיפה יש פרמטרים נגד injection.
- [ ] להבין את `DB.cs`
- [ ] להבין את `UserDB.cs`
- [ ] להבין את `RoomDB.cs`
- [ ] להבין את `SessionDB.cs`
- [ ] להבין את `GameDB.cs`
- [ ] להבין את `QuestionTypeDB.cs`
- [ ] להבין את `SeedData.cs`
- [ ] להבין למה שכבת DBL עובדת ישירות מול MySQL
- [ ] להבין שימוש ב-SQL פרמטרי
- [ ] להבין איך נמנעים מ-SQL Injection
- [ ] להבין טרנזקציות כשיש כמה פעולות תלויות זו בזו
- [ ] להבין async/await בגישה לנתונים

## 6. להבין את זרימת האותנטיקציה
- שאלות תרגול:
  - [ ] איך `Login` ו-`Register` עובדים מהקליינט עד לתוצאה?
  - [ ] איך איפוס סיסמה עובד מהטוקן ועד לעדכון הסיסמה?
- מה צריך להסביר:
  - [ ] hashing של סיסמה, בדיקת תקינות, ושמירת session/token.
  - [ ] למה לא שומרים סיסמה רגילה, ולמה משתמשים ב-email flow.
- [ ] להבין איך `LoginAsync` עובד
- [ ] להבין איך `RegisterAsync` עובד
- [ ] להבין איך `ForgotPasswordAsync` עובד
- [ ] להבין איך `ResetPasswordAsync` עובד
- [ ] להבין איך `CreateSessionAsync` עובד
- [ ] להבין איך `GetUserIdByTokenAsync` עובד
- [ ] להבין איך `DeleteSessionAsync` עובד
- [ ] להבין את hashing של הסיסמה
- [ ] להבין למה לא שומרים סיסמה רגילה במסד
- [ ] להבין את תהליך איפוס הסיסמה
- [ ] להבין שימוש ב-SMTP

## 7. להבין את ניהול החדרים
- שאלות תרגול:
  - [ ] איך נוצר חדר חדש ואיך משתמש מצטרף לחדר קיים?
  - [ ] מה קורה כששחקן יוצא מחדר?
- מה צריך להסביר:
  - [ ] תהליך יצירת קוד חדר, הצטרפות, ושחקנים בחדר.
  - [ ] הקשר בין `rooms` ל-`room_players`.
- [ ] להבין איך יוצרים חדר
- [ ] להבין איך מצטרפים לחדר
- [ ] להבין איך שולפים שחקנים לחדר
- [ ] להבין איך שומרים חדר ציבורי
- [ ] להבין איך שומרים חדר פרטי
- [ ] להבין איך יוצאים מחדר
- [ ] להבין איך נוצרים `room_code`
- [ ] להבין איך נשמר `nickname`
- [ ] להבין את התנהגות `room_players`

## 8. להבין את מנוע המשחק
- שאלות תרגול:
  - [ ] איך בוחרים את השאלות של החדר ואיך יודעים מה השאלה הנוכחית?
  - [ ] איך מחשבים scoreboard ומה נשמר בתוצאות הסופיות?
- מה צריך להסביר:
  - [ ] סדר השאלה, זמן לשאלה, ותהליך שליחת תשובה.
  - [ ] איך `player_answers`, `room_questions` ו-`game_results` עובדים יחד.
- [ ] להבין איך בוחרים שאלות לחדר
- [ ] להבין איך שולפים את השאלה הנוכחית
- [ ] להבין איך שומרים תשובה
- [ ] להבין איך מחשבים scoreboard
- [ ] להבין איך שומרים תוצאות משחק
- [ ] להבין איך שומרים סטטיסטיקות משתמש
- [ ] להבין איך שולפים Top Players
- [ ] להבין איך שולפים תוצאות אחרונות של משתמש
- [ ] להבין את `room_questions`
- [ ] להבין את `player_answers`
- [ ] להבין את `game_results`

## 9. להבין את שרת ה-API
- שאלות תרגול:
  - [ ] מה עושה `Program.cs` בזמן עליית השרת?
  - [ ] למה יש `X-App-Code` ולמה `GET /api/health` יוצא מהבדיקה?
- מה צריך להסביר:
  - [ ] DI, CORS, Controllers, OpenAPI, והגנת גישה.
  - [ ] ההבדל בין endpoint ציבורי לבין endpoint מוגן.
- [ ] לקרוא את `TriviaGame.Api/Program.cs`
- [ ] להבין `AddControllers`
- [ ] להבין `AddOpenApi`
- [ ] להבין `AddHttpClient`
- [ ] להבין CORS
- [ ] להבין את בדיקת `X-App-Code`
- [ ] להבין למה `GET /api/health` חריג מהבדיקה
- [ ] להבין אילו נתיבים מוגנים
- [ ] להבין את קובץ `appsettings.json`

## 10. להבין את הבקרים
- שאלות תרגול:
  - [ ] איזה controller מטפל באיזה סוג של בקשות?
  - [ ] מה ההבדל בין controller לבין service?
- מה צריך להסביר:
  - [ ] חלוקת אחריות בין routes שונים.
  - [ ] איך הבקרים מחזירים JSON ותשובות HTTP.
- [ ] להבין את `SystemController`
- [ ] להבין את `AuthController`
- [ ] להבין את `RoomsController`
- [ ] להבין את `GameController`
- [ ] להבין את `UsersController`
- [ ] להבין את `AdminController`
- [ ] להבין את `AssistantController`
- [ ] להבין מה עושה כל route
- [ ] להבין איזה controller שייך לאיזה תחום

## 11. להבין את השירותים בשרת
- שאלות תרגול:
  - [ ] למה יש שכבת service בין controller ל-DB?
  - [ ] מה התפקיד של כל service?
- מה צריך להסביר:
  - [ ] לוגיקה עסקית, אימותים, ו-orchestration.
  - [ ] איפה מתבצעים חוקים ולא רק קריאות SQL.
- [ ] להבין את `AuthDomainService`
- [ ] להבין את `UsersDomainService`
- [ ] להבין את `RoomsDomainService`
- [ ] להבין את `GameDomainService`
- [ ] להבין את `AssistantDomainService`
- [ ] להבין את `EmailService`
- [ ] להבין את `SmtpSettings`
- [ ] להבין איך כל service מתקשר עם DBL
- [ ] להבין איך שירותים עוזרים לשמור על סדר

## 12. להבין את הלקוח MAUI
- שאלות תרגול:
  - [ ] איך `MainPage` מחזיקה את כל הזרימה?
  - [ ] איך הלקוח מדבר עם ה-API?
- מה צריך להסביר:
  - [ ] חלוקת מסך, אירועים, binding, וקריאות רשת.
  - [ ] מיפוי בין UI לבין בקשות לשרת.
- [ ] להבין את `TriviaGame.Mobile.csproj`
- [ ] להבין את `MauiProgram.cs`
- [ ] להבין את `MainPage.xaml`
- [ ] להבין את `MainPage.xaml.cs`
- [ ] להבין את `ApiModels.cs`
- [ ] להבין את `ApiClient.cs`
- [ ] להבין את `TriviaApiClient.cs`
- [ ] להבין את `ApiEndpointResolver.cs`
- [ ] להבין את `appsettings.json`
- [ ] להבין את `README.md`

## 13. להבין את מסמכי העזר
- שאלות תרגול:
  - [ ] מה אומר כל מסמך עזר על הפרויקט?
  - [ ] איך משתמשים ב-ERD וב-UML כדי להסביר את המערכת?
- מה צריך להסביר:
  - [ ] כיצד המסמכים תומכים בהבנת המערכת ולא מחליפים את הקוד.
  - [ ] אילו מסמכים מתארים מבנה ואילו מסמכים מתארים תהליך.
- [ ] להבין את `docs/SYSTEM_ANALYSIS.md`
- [ ] להבין את `docs/USER_GUIDE.md`
- [ ] להבין את `docs/ERD.md`
- [ ] להבין את `docs/UML.md`
- [ ] להבין את `docs/USE_CASES.md`
- [ ] להבין את `docs/ACTIVITY_DIAGRAMS.md`
- [ ] להבין את `docs/FINAL_100_CHECKLIST_EN.md`
- [ ] להבין את `docs/PROJECT_BOOK_HE.docx`

## 14. להבין את ההרשאות והאבטחה
- שאלות תרגול:
  - [ ] מה ההבדל בין User, Manager ו-Admin?
  - [ ] איפה בפרויקט יש בדיקות אבטחה?
- מה צריך להסביר:
  - [ ] התפקיד של role.
  - [ ] אימותים, token/session, ולידציה, ופרמטריזציה של SQL.
- [ ] להבין את `UserRole`
- [ ] להבין את `PasswordHelper`
- [ ] להבין את `ValidationHelper`
- [ ] להבין את `SessionDB`
- [ ] להבין את `user_sessions`
- [ ] להבין את `password_reset_tokens`
- [ ] להבין את `X-App-Code`
- [ ] להבין איפה יש הגנה מפני SQL Injection

## 15. להבין את ההפעלה והבדיקות
- שאלות תרגול:
  - [ ] איך מפעילים את השרת והלקוח?
  - [ ] איך בודקים שהמערכת חיה אחרי שינוי?
- מה צריך להסביר:
  - [ ] רצף בדיקות בסיסי אחרי הרצה.
  - [ ] איפה בודקים API, איפה בודקים UI, ואיפה בודקים DB.
- [ ] לדעת להריץ את שרת ה-API
- [ ] לדעת לבדוק `GET /api/health`
- [ ] לדעת לבדוק Login/Register
- [ ] לדעת לבדוק יצירת חדר
- [ ] לדעת לבדוק הצטרפות לחדר
- [ ] לדעת לבדוק התחלת משחק
- [ ] לדעת לבדוק שליפת שאלה נוכחית
- [ ] לדעת לבדוק שליחת תשובה
- [ ] לדעת לבדוק scoreboard
- [ ] לדעת לבדוק statistics
- [ ] לדעת לבדוק top players

## 16. להכין תשובות לבחינה בעל פה
- שאלות תרגול:
  - [ ] תאר את הפרויקט ב-2 דקות בלי להיכנס לקוד.
  - [ ] הסבר למה בחרת את המבנה הזה ולא מבנה אחר.
- מה צריך להסביר:
  - [ ] בעיה, פתרון, שכבות, טבלאות, והרשאות.
  - [ ] דוגמאות לתרחישי שימוש אמיתיים.
- [ ] לדעת להסביר מה הבעיה שהמערכת פותרת
- [ ] לדעת להסביר מי קהל היעד
- [ ] לדעת להסביר מה מטרות המערכת
- [ ] לדעת להסביר מה מצב קיים ומה מצב עתידי
- [ ] לדעת להסביר את ה-ERD
- [ ] לדעת להסביר את ה-UML
- [ ] לדעת להסביר את זרימת ההרשמה והתחברות
- [ ] לדעת להסביר את זרימת יצירת חדר ומשחק
- [ ] לדעת להסביר את זרימת השמירה של תוצאות וסטטיסטיקות
- [ ] לדעת להסביר מהו async/await בפרויקט הזה
- [ ] לדעת להסביר היכן יש ירושה בפרויקט
- [ ] לדעת להסביר היכן יש ולידציה
- [ ] לדעת להסביר היכן יש אבטחה

## 17. להכין את עצמך לתרגול מעשי
- שאלות תרגול:
  - [ ] איזה endpoint אתה בודק קודם כדי לוודא שהשרת חי?
  - [ ] מה רצף הבדיקות שאתה עושה אחרי login?
- מה צריך להסביר:
  - [ ] איך להריץ, לבדוק, ולשחזר זרימת משתמש אמיתית.
  - [ ] איך לזהות איפה תקלה נמצאת: לקוח, API או DB.
- [ ] לדעת לבדוק את שרת ה-API
- [ ] לדעת לבדוק `GET /api/health`
- [ ] לדעת לבדוק Login/Register
- [ ] לדעת לבדוק יצירת חדר
- [ ] לדעת לבדוק הצטרפות לחדר
- [ ] לדעת לבדוק התחלת משחק
- [ ] לדעת לבדוק שליפת השאלה הנוכחית
- [ ] לדעת לבדוק שליחת תשובה
- [ ] לדעת לבדוק scoreboard
- [ ] לדעת לבדוק statistics
- [ ] לדעת לבדוק top players

## 18. להכין סיכום קצר בעל פה
- שאלות תרגול:
  - [ ] מה המשפט הראשון שאתה אומר לבוחן על הפרויקט?
  - [ ] מה שלושת הדברים הכי חשובים בפרויקט?
- מה צריך להסביר:
  - [ ] מבנה, תכלית, והרחבות.
  - [ ] מה למדת ומה היית משפר.
- [ ] לדעת לתאר את הפרויקט ב-30 שניות
- [ ] לדעת לתאר את הפרויקט ב-2 דקות
- [ ] לדעת לתאר את הקוד לפי שכבות
- [ ] לדעת לתאר את מסד הנתונים במשפטים פשוטים
- [ ] לדעת לתאר את תרומת ההרחבות

## 19. נושאים שצריך לזכור במיוחד
- שאלות תרגול:
  - [ ] מה תפקיד `room_questions` ו-`player_answers`?
  - [ ] מה ההבדל בין `ScoreRow` ל-`TopPlayerRow`?
- מה צריך להסביר:
  - [ ] הזיכרון המהיר של מבנה המערכת.
  - [ ] המושגים שתמיד חוזרים בבחינה.
- [ ] `ModelsTrivia` = מודלים משותפים
- [ ] `TriviaDBL` = SQL ישיר מול MySQL
- [ ] `TriviaGame.Api` = לוגיקה + HTTP
- [ ] `TriviaGame.Mobile` = לקוח MAUI
- [ ] `room_players` = טבלת קישור
- [ ] `room_questions` = טבלת קישור
- [ ] `player_answers` = תשובות שחקנים
- [ ] `game_results` = תוצאות סופיות
- [ ] `PlayerStatsBase` = בסיס לסטטיסטיקות
- [ ] `PasswordHelper` = hashing
- [ ] `ValidationHelper` = בדיקת קלט

## מצב נוכחי
- [x] קובץ משימות מלא בעברית
- [ ] להתחיל מעבר מסודר על `ModelsTrivia`
