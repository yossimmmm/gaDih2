# רשימת משימות ללימוד לקראת הבחינה

## קבצים מרכזיים ללימוד

### מודלים ומשותף
- [ ] [ModelsTrivia/User.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/ModelsTrivia/User.cs>) - להבין משתמש, תפקידים ושדות הזדהות
- [ ] [ModelsTrivia/Room.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/ModelsTrivia/Room.cs>) - להבין חדר, קוד חדר, מצב ציבורי/פעיל
- [ ] [ModelsTrivia/RoomPlayer.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/ModelsTrivia/RoomPlayer.cs>) - להבין קשר משתמש-חדר
- [ ] [ModelsTrivia/QuestionType.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/ModelsTrivia/QuestionType.cs>) - להבין סוג שאלה
- [ ] [ModelsTrivia/Question.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/ModelsTrivia/Question.cs>) - להבין שאלה, זמן, אפשרויות
- [ ] [ModelsTrivia/QuestionOption.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/ModelsTrivia/QuestionOption.cs>) - להבין אפשרות תשובה
- [ ] [ModelsTrivia/PlayerStatsBase.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/ModelsTrivia/PlayerStatsBase.cs>) - להבין ירושה לסטטיסטיקות
- [ ] [ModelsTrivia/ScoreRow.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/ModelsTrivia/ScoreRow.cs>) - להבין שורת ניקוד לחדר
- [ ] [ModelsTrivia/TopPlayerRow.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/ModelsTrivia/TopPlayerRow.cs>) - להבין מובילי משחק
- [ ] [ModelsTrivia/ValidationHelper.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/ModelsTrivia/ValidationHelper.cs>) - להבין בדיקות קלט וסניטציה
- [ ] [ModelsTrivia/PasswordHelper.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/ModelsTrivia/PasswordHelper.cs>) - להבין hash ואימות סיסמה
- [ ] [ModelsTrivia/UserSession.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/ModelsTrivia/UserSession.cs>) - להבין מצב משתמש גלובלי

### מסד נתונים ושכבת DBL
- [ ] [trivia_game.sql](</C:/Users/yosic/OneDrive/Desktop/gaDih2/trivia_game.sql>) - להבין את כל הטבלאות והקשרים
- [ ] [TriviaDBL/DB.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaDBL/DB.cs>) - להבין חיבור למסד נתונים
- [ ] [TriviaDBL/UserDB.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaDBL/UserDB.cs>) - להבין CRUD למשתמשים, reset password
- [ ] [TriviaDBL/RoomDB.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaDBL/RoomDB.cs>) - להבין יצירת חדר, הצטרפות, רשימת שחקנים
- [ ] [TriviaDBL/SessionDB.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaDBL/SessionDB.cs>) - להבין session tokens
- [ ] [TriviaDBL/GameDB.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaDBL/GameDB.cs>) - להבין בחירת שאלות, תשובות, scoreboard, סטטיסטיקות
- [ ] [TriviaDBL/QuestionTypeDB.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaDBL/QuestionTypeDB.cs>) - להבין שליפת סוגי שאלות
- [ ] [TriviaDBL/SeedData.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaDBL/SeedData.cs>) - להבין נתוני התחלה

### שרת API
- [ ] [TriviaGame.Api/Program.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Program.cs>) - להבין startup, DI, CORS, X-App-Code
- [ ] [TriviaGame.Api/Contracts/ApiContracts.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Contracts/ApiContracts.cs>) - להבין DTOs בין לקוח לשרת
- [ ] [TriviaGame.Api/Controllers/SystemController.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Controllers/SystemController.cs>) - להבין health check
- [ ] [TriviaGame.Api/Controllers/AuthController.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Controllers/AuthController.cs>) - להבין login/register/reset me
- [ ] [TriviaGame.Api/Controllers/RoomsController.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Controllers/RoomsController.cs>) - להבין חדרים, הצטרפות, יציאה, players
- [ ] [TriviaGame.Api/Controllers/GameController.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Controllers/GameController.cs>) - להבין start game, current question, answer, scoreboard
- [ ] [TriviaGame.Api/Controllers/UsersController.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Controllers/UsersController.cs>) - להבין profile, password, stats, recent results
- [ ] [TriviaGame.Api/Controllers/AdminController.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Controllers/AdminController.cs>) - להבין ניהול אדמין
- [ ] [TriviaGame.Api/Controllers/AssistantController.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Controllers/AssistantController.cs>) - להבין AI assistant
- [ ] [TriviaGame.Api/Services/AuthDomainService.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Services/AuthDomainService.cs>) - להבין לוגיקת auth
- [ ] [TriviaGame.Api/Services/UsersDomainService.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Services/UsersDomainService.cs>) - להבין לוגיקת פרופיל והרשאות
- [ ] [TriviaGame.Api/Services/RoomsDomainService.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Services/RoomsDomainService.cs>) - להבין לוגיקת חדרים
- [ ] [TriviaGame.Api/Services/GameDomainService.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Services/GameDomainService.cs>) - להבין לוגיקת משחק וסטטיסטיקות
- [ ] [TriviaGame.Api/Services/AssistantDomainService.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Services/AssistantDomainService.cs>) - להבין קריאה ל-Gemini
- [ ] [TriviaGame.Api/Services/EmailService.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Services/EmailService.cs>) - להבין שליחת מייל
- [ ] [TriviaGame.Api/Services/SmtpSettings.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/Services/SmtpSettings.cs>) - להבין הגדרות SMTP
- [ ] [TriviaGame.Api/appsettings.json](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Api/appsettings.json>) - להבין הגדרות שרת ו־AI

### לקוח MAUI
- [ ] [TriviaGame.Mobile/TriviaGame.Mobile.csproj](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Mobile/TriviaGame.Mobile.csproj>) - להבין פלטפורמות ו־packages
- [ ] [TriviaGame.Mobile/MauiProgram.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Mobile/MauiProgram.cs>) - להבין DI וטעינת appsettings
- [ ] [TriviaGame.Mobile/MainPage.xaml](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Mobile/MainPage.xaml>) - להבין את הממשק
- [ ] [TriviaGame.Mobile/MainPage.xaml.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Mobile/MainPage.xaml.cs>) - להבין לוגיקת הכפתורים והזרימה
- [ ] [TriviaGame.Mobile/Models/ApiModels.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Mobile/Models/ApiModels.cs>) - להבין DTOs של הלקוח
- [ ] [TriviaGame.Mobile/Services/ApiClient.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Mobile/Services/ApiClient.cs>) - להבין HTTP, retries, timeout
- [ ] [TriviaGame.Mobile/Services/TriviaApiClient.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Mobile/Services/TriviaApiClient.cs>) - להבין מיפוי endpoints
- [ ] [TriviaGame.Mobile/Services/ApiEndpointResolver.cs](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Mobile/Services/ApiEndpointResolver.cs>) - להבין בחירת כתובת שרת
- [ ] [TriviaGame.Mobile/appsettings.json](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Mobile/appsettings.json>) - להבין כתובות וסביבות
- [ ] [TriviaGame.Mobile/README.md](</C:/Users/yosic/OneDrive/Desktop/gaDih2/TriviaGame.Mobile/README.md>) - להבין איך מפעילים את הלקוח

### מסמכי עזר
- [ ] [docs/SYSTEM_ANALYSIS.md](</C:/Users/yosic/OneDrive/Desktop/gaDih2/docs/SYSTEM_ANALYSIS.md>) - להבין את הבעיה והמטרות
- [ ] [docs/USER_GUIDE.md](</C:/Users/yosic/OneDrive/Desktop/gaDih2/docs/USER_GUIDE.md>) - להבין את מדריך המשתמש
- [ ] [docs/ERD.md](</C:/Users/yosic/OneDrive/Desktop/gaDih2/docs/ERD.md>) - להבין את התרשים בין הטבלאות
- [ ] [docs/UML.md](</C:/Users/yosic/OneDrive/Desktop/gaDih2/docs/UML.md>) - להבין את התרשים בין המחלקות
- [ ] [docs/USE_CASES.md](</C:/Users/yosic/OneDrive/Desktop/gaDih2/docs/USE_CASES.md>) - להבין תרחישי שימוש
- [ ] [docs/ACTIVITY_DIAGRAMS.md](</C:/Users/yosic/OneDrive/Desktop/gaDih2/docs/ACTIVITY_DIAGRAMS.md>) - להבין תרשימי פעילות
- [ ] [docs/FINAL_100_CHECKLIST_EN.md](</C:/Users/yosic/OneDrive/Desktop/gaDih2/docs/FINAL_100_CHECKLIST_EN.md>) - להבין מה נדרש לציון מלא
- [ ] [docs/PROJECT_BOOK_HE.docx](</C:/Users/yosic/OneDrive/Desktop/gaDih2/docs/PROJECT_BOOK_HE.docx>) - לראות איך הספר בנוי

## 1. להבין את המבנה הכללי של הפרויקט
- שאלות תרגול:
  - [ ] איך הפרויקט מחולק לארבע שכבות ומה תפקיד כל שכבה?
  - [ ] מה הזרימה המלאה של מידע מהלקוח עד למסד הנתונים וחזרה?
- מה צריך להסביר:
  - [ ] למה יש הפרדה בין Models, DBL, API ולקוח.
  - [ ] איפה נמצא ה־business logic ואיפה נשמרים הנתונים.
- [ ] להבין שיש 4 שכבות/פרויקטים עיקריים:
  - `ModelsTrivia` - מחלקות מודל, ולידציה, וסיסמאות
  - `TriviaDBL` - גישה ישירה למסד הנתונים
  - `TriviaGame.Api` - שרת API, לוגיקה עסקית, ובקרי HTTP
  - `TriviaGame.Mobile` - לקוח MAUI שמדבר עם ה־API
- [ ] להבין מה תפקיד כל שכבה במערכת
- [ ] לדעת להסביר את זרימת הנתונים: לקוח -> API -> DB -> חזרה ללקוח
- [ ] לדעת להסביר למה הפרויקט מחולק לשכבות

## 2. להבין את דרישות הבחינה
- שאלות תרגול:
  - [ ] אילו חלקים חובה להציג בבחינה?
  - [ ] אילו הרחבות הפרויקט הזה מממש?
- מה צריך להסביר:
  - [ ] מה נדרש בתיק הפרויקט לפי המסמך.
  - [ ] איך הפרויקט הזה עונה על דרישות של צד שרת, צד לקוח ובסיס נתונים.
- [ ] לקרוא את שני מסמכי הבחינה ולהבין מה בודקים
- [ ] לדעת להסביר:
  - [ ] ניתוח מערכת
  - [ ] בסיס נתונים
  - [ ] מימוש צד שרת
  - [ ] מימוש צד לקוח
  - [ ] מדריך למשתמש
  - [ ] רפלקציה
- [ ] לדעת אילו דרישות חובה קיימות בבדיקה
- [ ] לדעת אילו הרחבות קיימות בפרויקט הזה

## 3. להבין את `ModelsTrivia`
- שאלות תרגול:
  - [ ] מה ההבדל בין `User`, `Room`, `Question` ו־`QuestionOption`?
  - [ ] למה `ScoreRow` ו־`TopPlayerRow` יורשות מ־`PlayerStatsBase`?
- מה צריך להסביר:
  - [ ] תפקיד של כל מחלקה.
  - [ ] אילו שדות מייצגים ישות אמיתית ואילו שדות הם עזר/תצוגה.
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
- [ ] להבין את `ValidationHelper`
- [ ] להבין את `PasswordHelper`
- [ ] להבין את `UserSession`
- [ ] לדעת להסביר למה המחלקות האלו משמשות גם ב־API וגם בלקוח

## 4. להבין את מסד הנתונים
- שאלות תרגול:
  - [ ] מה תפקיד כל טבלה ומה הקשר שלה לטבלאות האחרות?
  - [ ] איזו טבלה היא טבלת קישור ואיזו טבלה שומרת תוצאות?
- מה צריך להסביר:
  - [ ] מפתחות ראשיים, מפתחות זרים, ייחודיות, וקשרים.
  - [ ] איך המשחק נשמר במסד לאורך זמן.
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
- [ ] לדעת להסביר את הקשרים בין הטבלאות
- [ ] לדעת לזהות טבלת קישור
- [ ] לדעת להסביר מפתחות ראשיים
- [ ] לדעת להסביר מפתחות זרים
- [ ] לדעת להסביר ייחודיות של `room_code`
- [ ] לדעת להסביר למה `room_players` ו־`room_questions` הן טבלאות קישור
- [ ] לדעת להסביר איך נשמרים תוצאות משחקים

## 5. להבין את שכבת הגישה לנתונים (`TriviaDBL`)
- שאלות תרגול:
  - [ ] למה בכלל צריך שכבת DBL ולא לגשת ל־SQL ישירות מה־API?
  - [ ] איך שיטות כמו `Insert`, `Update`, `Delete`, `Select` ממומשות כאן?
- מה צריך להסביר:
  - [ ] איך נוצרים connection, command ו־reader.
  - [ ] איפה יש שימוש בטרנזקציות ואיפה יש פרמטרים נגד injection.
- [ ] להבין את `DB.cs`
- [ ] להבין את `UserDB.cs`
- [ ] להבין את `RoomDB.cs`
- [ ] להבין את `SessionDB.cs`
- [ ] להבין את `GameDB.cs`
- [ ] להבין את `QuestionTypeDB.cs`
- [ ] להבין את `SeedData.cs`
- [ ] לדעת להסביר למה שכבת DBL עובדת ישירות מול MySQL
- [ ] לדעת להסביר שימוש ב־SQL פרמטרי
- [ ] לדעת להסביר איך נמנעים מ־SQL Injection
- [ ] לדעת להסביר טרנזקציות כשיש כמה פעולות תלויות זו בזו
- [ ] לדעת להסביר async/await בגישה למסד הנתונים

## 6. להבין את זרימת האותנטיקציה
- שאלות תרגול:
  - [ ] איך `Login` ו־`Register` עובדים מהקליטה עד לתוצאה?
  - [ ] איך איפוס סיסמה עובד מהטוקן ועד לעדכון הסיסמה?
- מה צריך להסביר:
  - [ ] hashing של סיסמה, בדיקת תקינות, ושמירת סשן/טוקן.
  - [ ] למה לא שומרים סיסמה רגילה, ולמה משתמשים ב־email flow.
- [ ] להבין איך `LoginAsync` עובד
- [ ] להבין איך `RegisterAsync` עובד
- [ ] להבין איך `ForgotPasswordAsync` עובד
- [ ] להבין איך `ResetPasswordAsync` עובד
- [ ] להבין איך `CreateSessionAsync` עובד
- [ ] להבין איך `GetUserIdByTokenAsync` עובד
- [ ] להבין איך `DeleteSessionAsync` עובד
- [ ] לדעת להסביר hashing של סיסמה
- [ ] לדעת להסביר למה לא שומרים סיסמה רגילה במסד
- [ ] לדעת להסביר את תהליך איפוס הסיסמה
- [ ] לדעת להסביר שימוש ב־SMTP

## 7. להבין את ניהול החדרים
- שאלות תרגול:
  - [ ] איך נוצר חדר חדש ואיך משתמש מצטרף לחדר קיים?
  - [ ] מה קורה כשמישהו יוצא מהחדר?
- מה צריך להסביר:
  - [ ] תהליך יצירה, קוד חדר, ציבורי/פרטי, ושחקנים בחדר.
  - [ ] הקשר בין `rooms` ל־`room_players`.
- [ ] להבין איך יוצרים חדר
- [ ] להבין איך מצטרפים לחדר
- [ ] להבין איך שולפים חדר לפי קוד
- [ ] להבין איך שולפים חדרים ציבוריים
- [ ] להבין איך שולפים משתתפים בחדר
- [ ] להבין איך יוצאים מחדר
- [ ] לדעת להסביר איך נוצר `room_code`
- [ ] לדעת להסביר איך נשמר `nickname`
- [ ] לדעת להסביר את התנהגות `room_players`

## 8. להבין את מנוע המשחק
- שאלות תרגול:
  - [ ] איך נבחרות השאלות של החדר ואיך יודעים מה השאלה הנוכחית?
  - [ ] איך מחושב scoreboard ומה נשמר בתוצאות הסופיות?
- מה צריך להסביר:
  - [ ] סדר השאלות, זמן לשאלה, ותהליך שליחת תשובה.
  - [ ] איך `player_answers`, `room_questions` ו־`game_results` עובדים יחד.
- [ ] להבין איך בוחרים שאלות לחדר
- [ ] להבין איך שולפים את השאלה הנוכחית
- [ ] להבין איך שומרים תשובה
- [ ] להבין איך מחשבים `scoreboard`
- [ ] להבין איך שומרים תוצאות משחק
- [ ] להבין איך שולפים סטטיסטיקות משתמש
- [ ] להבין איך שולפים Top Players
- [ ] להבין איך שולפים תוצאות אחרונות של משתמש
- [ ] לדעת להסביר `room_questions`
- [ ] לדעת להסביר `player_answers`
- [ ] לדעת להסביר `game_results`

## 9. להבין את שרת ה־API
- שאלות תרגול:
  - [ ] מה עושה `Program.cs` בזמן עליית השרת?
  - [ ] למה יש `X-App-Code` ולמה `GET /api/health` חריג?
- מה צריך להסביר:
  - [ ] DI, CORS, Controllers, OpenAPI, ואבטחת גישה.
  - [ ] ההבדל בין endpoint ציבורי לבין endpoint מוגן.
- [ ] לקרוא את `TriviaGame.Api/Program.cs`
- [ ] להבין `AddControllers`
- [ ] להבין `AddOpenApi`
- [ ] להבין `AddHttpClient`
- [ ] להבין CORS
- [ ] להבין את בדיקת `X-App-Code`
- [ ] להבין למה `GET /api/health` חריג מהבדיקה
- [ ] להבין למה `/api/*` מוגן
- [ ] לדעת להסביר את קובץ `appsettings.json`

## 10. להבין את הבקרים
- שאלות תרגול:
  - [ ] איזה controller מטפל באיזה מסלול?
  - [ ] מה ההבדל בין controller לבין service?
- מה צריך להסביר:
  - [ ] חלוקת האחריות בין routes שונים.
  - [ ] איך הבקרים מחזירים JSON ותוצאות HTTP.
- [ ] להבין את `SystemController`
- [ ] להבין את `AuthController`
- [ ] להבין את `RoomsController`
- [ ] להבין את `GameController`
- [ ] להבין את `UsersController`
- [ ] להבין את `AdminController`
- [ ] להבין את `AssistantController`
- [ ] לדעת להסביר מה כל route עושה
- [ ] לדעת להסביר איזה controller שייך לאיזה תחום

## 11. להבין את שירותי הדומיין
- שאלות תרגול:
  - [ ] למה יש services בין controllers לבין DBL?
  - [ ] איזה service מטפל ב־assistant ואיזה ב־email?
- מה צריך להסביר:
  - [ ] separation of concerns.
  - [ ] איך ה־domain service מחליט מה מותר ומה אסור.
- [ ] להבין את `AuthDomainService`
- [ ] להבין את `UsersDomainService`
- [ ] להבין את `RoomsDomainService`
- [ ] להבין את `GameDomainService`
- [ ] להבין את `AssistantDomainService`
- [ ] להבין את `EmailService`
- [ ] להבין את `SmtpSettings`
- [ ] לדעת להסביר למה יש שכבת שירות בין controller ל־DBL
- [ ] לדעת להסביר separation of concerns

## 12. להבין את הלקוח MAUI
- שאלות תרגול:
  - [ ] איך הלקוח נטען ואיך DI עובד ב־MAUI?
  - [ ] אילו קבצים אחראים על UI, HTTP ו־configuration?
- מה צריך להסביר:
  - [ ] המבנה של הפרויקט והפלטפורמות הנתמכות.
  - [ ] איך הקליינט מדבר עם השרת ולא ניגש למסד ישירות.
- [ ] להבין את `TriviaGame.Mobile.csproj`
- [ ] להבין את `MauiProgram.cs`
- [ ] להבין את `MainPage.xaml`
- [ ] להבין את `MainPage.xaml.cs`
- [ ] להבין את `ApiClient.cs`
- [ ] להבין את `TriviaApiClient.cs`
- [ ] להבין את `ApiEndpointResolver.cs`
- [ ] להבין את `ApiModels.cs`
- [ ] להבין את `appsettings.json`

## 13. להבין את חוויית המשתמש במובייל
- שאלות תרגול:
  - [ ] מה עושה כל אזור במסך הראשי?
  - [ ] מה קורה כשבוחרים סביבת API אחרת?
- מה צריך להסביר:
  - [ ] הסדר של הפעולות שהמשתמש מבצע.
  - [ ] איך הממשק מאפשר בדיקת backend, auth, rooms, game, stats.
- [ ] לדעת להסביר את מסך ה־API settings
- [ ] לדעת להסביר את אזור ה־Auth
- [ ] לדעת להסביר את אזור ה־Rooms
- [ ] לדעת להסביר את אזור ה־Game
- [ ] לדעת להסביר את אזור ה־Stats + Assistant
- [ ] לדעת להסביר את כפתור `Check Backend Health`
- [ ] לדעת להסביר איך הלקוח מחליף כתובת שרת
- [ ] לדעת להסביר איך `Load` טוען את ה־WebView

## 14. להבין את ניהול ה־API מהלקוח
- שאלות תרגול:
  - [ ] איך `ApiClient` בונה בקשה ומטפל בשגיאות?
  - [ ] איך `TriviaApiClient` ממפה פעולה כמו Login או JoinRoom ל־endpoint?
- מה צריך להסביר:
  - [ ] request/response, retry, timeout, headers.
  - [ ] למה יש wrapper מעל `HttpClient`.
- [ ] להבין איך `ApiClient` מוסיף `X-App-Code`
- [ ] להבין איך `ApiClient` מטפל ב־GET/POST/PUT/DELETE
- [ ] להבין איך `ApiClient` עושה retry
- [ ] להבין איך `ApiClient` מטפל ב־timeout
- [ ] להבין איך `TriviaApiClient` ממפה כל פעולה ל־endpoint
- [ ] לדעת להסביר את טיפוסי ה־DTO בצד לקוח

## 15. להבין את האבטחה
- שאלות תרגול:
  - [ ] איפה יש ולידציה, איפה יש סניטציה, ואיפה יש hashing?
  - [ ] איך הפרויקט מגן על SQL Injection ועל גישה לא מורשית?
- מה צריך להסביר:
  - [ ] `ValidationHelper`, `PasswordHelper`, פרמטרים ב־SQL, ו־`X-App-Code`.
  - [ ] מה כן נחשב הגנה ומה רק רמה בסיסית.
- [ ] להבין hashing של סיסמאות
- [ ] להבין ולידציה של קלט
- [ ] להבין סניטציה של קלט
- [ ] להבין SQL parameterization
- [ ] להבין הגנה בסיסית מ־SQL Injection
- [ ] להבין את `X-App-Code`
- [ ] להבין את תהליך איפוס הסיסמה עם token
- [ ] להבין את השימוש ב־SMTP ושליחת מייל

## 16. להבין את השרטוטים והמסמכים
- שאלות תרגול:
  - [ ] איזה מסמך מסביר את ה־ERD ואיזה מסמך מסביר את ה־UML?
  - [ ] איפה מתועדים use cases ומדריך משתמש?
- מה צריך להסביר:
  - [ ] איך המסמכים מתחברים לקוד.
  - [ ] מה התלמיד צריך להראות בתיק הפרויקט.
- [ ] לקרוא את `docs/ERD.md`
- [ ] לקרוא את `docs/UML.md`
- [ ] לקרוא את `docs/SYSTEM_ANALYSIS.md`
- [ ] לקרוא את `docs/USER_GUIDE.md`
- [ ] לקרוא את `docs/USE_CASES.md`
- [ ] לקרוא את `docs/ACTIVITY_DIAGRAMS.md`
- [ ] לקרוא את `docs/FINAL_100_CHECKLIST_EN.md`
- [ ] לדעת להסביר מה מופיע בכל מסמך

## 17. להכין תשובות לבחינה בעל פה
- שאלות תרגול:
  - [ ] תאר את הפרויקט ב־2 דקות בלי להיכנס לקוד.
  - [ ] הסבר למה בחרת את המבנה הזה ולא מבנה אחר.
- מה צריך להסביר:
  - [ ] בעיה, פתרון, שכבות, טבלאות, והרשאות.
  - [ ] דוגמאות לתרחישי שימוש אמיתיים.
- [ ] להסביר מה הבעיה שהמערכת פותרת
- [ ] להסביר מי קהל היעד
- [ ] להסביר מה מטרות המערכת
- [ ] להסביר מה מצב קיים ומה מצב עתידי
- [ ] להסביר את ה־ERD
- [ ] להסביר את ה־UML
- [ ] להסביר את זרימת ההרשמה והתחברות
- [ ] להסביר את זרימת יצירת חדר ומשחק
- [ ] להסביר את זרימת השמירה של תוצאות וסטטיסטיקות
- [ ] להסביר מהו async/await בפרויקט הזה
- [ ] להסביר היכן יש ירושה בפרויקט
- [ ] להסביר היכן יש ולידציה
- [ ] להסביר היכן יש אבטחה

## 18. להכין את עצמך לתרגול מעשי
- שאלות תרגול:
  - [ ] איזה endpoint אתה בודק קודם כדי לוודא שהשרת חי?
  - [ ] מה רצף הבדיקות שאתה עושה אחרי login?
- מה צריך להסביר:
  - [ ] איך להריץ, לבדוק, ולשחזר זרימת משתמש אמיתית.
  - [ ] איך לזהות איפה תקלה נמצאת: לקוח, API או DB.
- [ ] לדעת להריץ את שרת ה־API
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

## 19. להכין סיכום קצר בעל פה
- שאלות תרגול:
  - [ ] מה המשפט הראשון שאתה אומר לבוחן על הפרויקט?
  - [ ] מה שלושת הדברים הכי חשובים בפרויקט?
- מה צריך להסביר:
  - [ ] מבנה, תכלית, והרחבות.
  - [ ] מה למדת ומה היית משפר.
- [ ] לדעת לתאר את הפרויקט ב־30 שניות
- [ ] לדעת לתאר את הפרויקט ב־2 דקות
- [ ] לדעת לתאר את הקוד לפי שכבות
- [ ] לדעת לתאר את מסד הנתונים במשפטים פשוטים
- [ ] לדעת לתאר את תרומת ההרחבות

## 20. נושאים שצריך לזכור במיוחד
- שאלות תרגול:
  - [ ] מה תפקיד `room_questions` ו־`player_answers`?
  - [ ] מה ההבדל בין `ScoreRow` ל־`TopPlayerRow`?
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
