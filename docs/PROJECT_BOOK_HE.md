# תיק פרויקט - חלופת שירותי אינטרנט, תכנות א-סינכרוני ומסדי נתונים

## שער
- שם בית הספר: `אורט רוגוזין`
- לוגו בית הספר: `[להוסיף תמונה]`
- שם העבודה: `Trivia Game - מערכת משחק טריוויה מרובת משתמשים`
- שם התלמיד: `יוסף יצחק משלחיס`
- ת.ז.: `345137616`
- שם המנחה: `גדי הרמן וזאב בנקבצר`
- שם החלופה: `שירותי אינטרנט / תכנות א-סינכרוני / מסדי נתונים`
- תאריך הגשה: `15/05/2026`

## כותרת עליונה / תחתונה
- Header מומלץ: `יוסף יצחק משלחיס | Trivia Game`
- Footer מומלץ: `מספר עמוד`
- גופן מומלץ במסמך הסופי (Word/Docs): `Arial` או `David`, גודל `12`.

## תוכן עניינים
יש ליצור אוטומטית ב-Word או Google Docs בעזרת Heading 1/2/3.

---

## מבוא

### רקע לפרויקט
שם הפרויקט: `Trivia Game`.
מדובר במערכת שרת-לקוח לניהול משחק טריוויה בזמן אמת דרך האינטרנט, כולל חדרים, לובי, משחק חי, תוצאות וסטטיסטיקות.

### תיאור קצר
המערכת מאפשרת למשתמשים להירשם, להתחבר, ליצור חדרים, להצטרף לחדרים ציבוריים, לשחק יחד בשאלות, לקבל תוצאות, ולצפות בנתונים אישיים (אחוזי הצלחה/ניצחונות).

### קהל יעד
- תלמידים ושחקנים שרוצים משחק טריוויה תחרותי.
- משתמשים פרטיים וקבוצות קטנות.
- מנהל מערכת לצורך בקרה והרשאות.

### סיבות לבחירת הנושא
- שילוב בין Web + Mobile + Database.
- התאמה מלאה לפרויקט שרת-לקוח א-סינכרוני.
- אפשרות להדגים הרשאות, אבטחה, SignalR, ואינטגרציית AI.

### מטרות המערכת
מטרות על:
1. בניית מערכת טריוויה אינטרנטית אמינה בזמן אמת.
2. ניהול מידע מתמשך במסד נתונים רלציוני.

מטרות נלוות:
1. תמיכה בשחזור סיסמה דרך מייל.
2. תמיכה ברמות הרשאה (User/Manager/Admin).
3. לקוח מובייל בנוסף ללקוח Web.

### תיאור המערכת
- צד שרת: `ASP.NET Core` + `SignalR` + APIs.
- צד לקוח Web: `Blazor`.
- צד לקוח מובייל: `.NET MAUI` (WebView + בדיקת בריאות שרת Native).
- מסד נתונים: `MySQL`.

### גבולות המערכת לכל לקוח
- לקוח Web: כל היכולות העסקיות והניהוליות.
- לקוח MAUI: גישה מהטלפון, טעינת המערכת ב-WebView, ובדיקת זמינות Backend בצורה Native.

### סביבות פיתוח
- Visual Studio / VS Code
- .NET 10
- MySQL

### שפות תכנות
- C#
- Razor
- JavaScript
- SQL
- XAML (MAUI)

### שכבות
1. Model (`ModelsTrivia`)
2. Data Access / DBL (`TriviaDBL`)
3. Server/API/Hub (`TriviaGame`)
4. Client Web (`TriviaGame/Components/Pages`)
5. Client Mobile (`TriviaGame.Mobile`)

### פלטפורמות לקוח ולמה נבחרו
1. Web (Blazor): פיתוח מהיר, UI עשיר, ריצה בדפדפן.
2. Mobile (MAUI): הדגמת ריבוי פלטפורמות וניידות.

### אתגרים מרכזיים
1. סנכרון בין שחקנים בחדר בזמן אמת.
2. מניעת פעולות לא מורשות לפי Role.
3. טיפול בשחזור סיסמה כולל תוקף טוקן.
4. התאמת תצוגה למובייל בתוך WebView.

### חידושים/התאמות
1. מנגנון הרשאות רב-רמות (Admin/Manager/User).
2. מנגנון Delegate מפורש לבקרת אירועי Authentication (`AuthAuditDispatcher`).
3. Native health check בלקוח MAUI.
4. אינטגרציה לשירות AI (Gemini) בעמוד Assistant.

---

## ניתוח מערכת

### מסמך ייזום
המערכת נבנתה כדי לספק משחק טריוויה רב-משתתפים עם ניהול נתונים מלא, ממשק מודרני, ותמיכה במספר פלטפורמות.

### מצב קיים
לפני המערכת לא הייתה פלטפורמה אחודה שמרכזת: משתמשים, חדרים, משחק חי, סטטיסטיקות והרשאות.

### מצב עתידי
הרחבות אפשריות:
- דשבורד ניהולי מתקדם.
- העלאת קבצי מדיה לשאלות.
- מנגנוני אנטי-צ'יט/ניטור.

### ERD / DFD / Use Case / עץ תהליכים
ראה קבצים משלימים:
- [ERD.md](/C:/Users/yosic/OneDrive/Desktop/gaDih2/docs/ERD.md)
- [USE_CASES.md](/C:/Users/yosic/OneDrive/Desktop/gaDih2/docs/USE_CASES.md)
- [ACTIVITY_DIAGRAMS.md](/C:/Users/yosic/OneDrive/Desktop/gaDih2/docs/ACTIVITY_DIAGRAMS.md)

עץ תהליכים מרכזי:
1. Authentication
2. Room Management
3. Gameplay
4. Results & Stats
5. Admin Role Management

---

## בסיס נתונים (DataBase)

### תרשים קשרים
מצורף בקובץ [ERD.md](/C:/Users/yosic/OneDrive/Desktop/gaDih2/docs/ERD.md).

### טבלאות ראשיות (דוגמאות)
1. `users`
   - מזהה משתמש, שם, אימייל, hash סיסמה, role.
2. `rooms`
   - מזהה חדר, קוד חדר, מארח, נושא, האם ציבורי.
3. `questions`
   - שאלה, סוג שאלה, רמת קושי, יוצר.
4. `game_results`
   - תוצאות לכל משתמש בכל משחק.

### טבלת קישור (חובה)
- `room_players` - טבלת קישור בין `users` ל-`rooms`.

### אבטחת SQL Injection
כל הקריאות ל-DB משתמשות בפרמטרים (`MySqlCommand.Parameters.AddWithValue`) ולא בשרשור SQL.

---

## מימוש הפרויקט

## צד שרת

### שכבת Model
- מחלקות ב-`ModelsTrivia` (למשל `User`, `Room`, `Question`, `UserRole`).
- UML מצורף: [UML.md](/C:/Users/yosic/OneDrive/Desktop/gaDih2/docs/UML.md)

### שכבת נתונים (DBL / ViewModel בהיבט נתונים)
- מחלקות `UserDB`, `RoomDB`, `GameDB`, `SessionDB`, `SeedData`.
- פעולות CRUD קיימות (שליפה, הוספה, עדכון, מחיקה) באזורים הרלוונטיים.

### שכבת שירותי רשת
- API endpoints ב-`Program.cs`:
  - התחברות / התנתקות / זיהוי משתמש
  - שחזור סיסמה / איפוס סיסמה
  - Admin Users + שינוי תפקיד
  - Health Check
- SignalR Hub: `GameHub` עבור תהליכים בזמן אמת.

### תכנות א-סינכרוני
- שימוש עקבי ב-`async/await` בכל שכבות הגישה לנתונים וב-API.
- תהליכים חיים עם SignalR.

### Delegate מפורש
- `AuthAuditDispatcher`:
  - הגדרת `delegate`
  - `event`
  - הפעלת handlers א-סינכרוניים לרישום אירועי auth.

### אבטחת מידע
- סיסמאות נשמרות כ-Hash.
- טוקן איפוס נשמר כ-Hash עם תוקף.
- session מבוסס Cookie HttpOnly.

### שירות רשת חיצוני
- Gemini API לשאלות/תובנות בעמוד Assistant.

## צד לקוח

### ממשקים
- עמודים מרכזיים:
  - Login / Register / ForgotPassword / ResetPassword
  - Menu / Rooms / Lobby / Play / Results / Stats / TopPlayers
  - Assistant
  - AdminUsers

### לפחות שני סוגי משתמשים
1. `User` - משחק ונתונים אישיים.
2. `Manager` - כולל יכולות יצירת חדר.
3. `Admin` - כולל ניהול תפקידי משתמשים.

### ריבוי פלטפורמות
1. Web Client
2. MAUI Mobile Client (כולל פעולה Native נוספת)

### בדיקות תקינות קלט ואירועים
- ולידציות קלט בטפסים.
- הודעות שגיאה למשתמש.
- חסימות כפתורים בזמן טעינה.

---

## מדריך למשתמש

### גרסאות וסביבות בדיקה
- Backend: `http://localhost:5038`
- Web דרך דפדפן
- Mobile דרך MAUI (Windows/Android emulator)

### אופן הפעלה
1. להריץ שרת:
   `dotnet run --project TriviaGame\TriviaGame.csproj --launch-profile http`
2. לפתוח:
   `http://localhost:5038/login`
3. להתחבר / להירשם.
4. לבחור משחק יחיד או חדר רב משתתפים.

### הודעות למשתמש
- שגיאות אימייל/סיסמה בלוגין.
- הודעות הצלחה/כישלון בשחזור סיסמה.
- הודעת כשל רשת במייל/API.

### מגבלות ואילוצים
- חובה חיבור ל-MySQL.
- חובה SMTP תקין כדי לשלוח reset link.
- במובייל אמיתי צריך URL של LAN ולא localhost.

### צילומי מסך
יש לצרף למסמך Word/Docs:
1. Login
2. Forgot Password
3. Reset Password
4. Menu לפי Role
5. Rooms/Lobby/Play/Results
6. Admin Users
7. MAUI Health Check

---

## סיכום אישי / רפלקציה

יש למלא אישית (חצי עמוד לפחות). תבנית מוצעת:
1. תהליך עבודה: איך התקדמת משלב הרעיון עד מוצר עובד.
2. הצלחות: מה עבד טוב (למשל real-time, auth, roles).
3. קשיים: בעיות שנתקלת בהן (UI mobile, SMTP, race conditions).
4. פתרונות: מה שינית ולמה.
5. למידה עצמאית: טכנולוגיות חדשות שלמדת.
6. מה היית משפר בעתיד.

---

## ביבליוגרפיה (APA)

דוגמאות:
1. Microsoft. (2025). *ASP.NET Core documentation*. https://learn.microsoft.com/aspnet/core
2. Microsoft. (2025). *SignalR for ASP.NET Core*. https://learn.microsoft.com/aspnet/core/signalr
3. Oracle. (2025). *MySQL 8.0 Reference Manual*. https://dev.mysql.com/doc/
4. Google. (2025). *Gemini API documentation*. https://ai.google.dev/

יש לעדכן לתאריכי הגישה האמיתיים שלך.

---

## נספחים

1. קוד מקור מלא של המחלקות (כטקסט, לא צילום).
2. תרשימים (ERD/UML/UseCase/Activity/DFD0).
3. תיעוד API (Endpoints עיקריים).
4. צילומי מסך של כל המסכים המרכזיים.

### מיפוי מהיר בין דרישות למסמכים
1. מבוא -> פרק "מבוא" במסמך זה.
2. ניתוח מערכת -> פרק "ניתוח מערכת" + קבצי תרשימים.
3. בסיס נתונים -> פרק DB + ERD.
4. מימוש צד שרת/לקוח -> פרק מימוש + קבצי קוד.
5. מדריך משתמש -> פרק מדריך + צילומי מסך.
6. רפלקציה -> פרק רפלקציה (להשלמה אישית).
7. ביבליוגרפיה -> פרק ביבליוגרפיה.
8. נספחים -> פרק נספחים.
