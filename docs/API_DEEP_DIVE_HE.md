# פירוק עומק ל-API

המסמך הזה מסביר את שכבת השרת של הפרויקט:

- [TriviaGame.Api/Program.cs](../TriviaGame.Api/Program.cs)
- [TriviaGame.Api/Controllers](../TriviaGame.Api/Controllers)
- [TriviaGame.Api/Services](../TriviaGame.Api/Services)
- [TriviaGame.Api/Contracts/ApiContracts.cs](../TriviaGame.Api/Contracts/ApiContracts.cs)

המטרה כאן היא להבין לא רק מה כל endpoint עושה, אלא גם למה המבנה של השרת נראה כך.

---

## התפקיד של ה-API

ה-API הוא השכבה שמקבלת בקשות HTTP מהלקוח, מפעילה לוגיקה עסקית, ומחזירה JSON.

הוא לא עובד ישירות עם UI.
הוא גם לא מחזיק state של מסך.

הוא עושה שלושה דברים:

1. מקבל בקשה מהלקוח.
2. מעביר אותה לשירות המתאים.
3. מחזיר תשובה מסודרת ללקוח.

---

## `Program.cs`

זה קובץ האתחול של השרת.

### מה הוא עושה

- רושם controllers
- רושם OpenAPI
- רושם `HttpClient`
- רושם CORS
- רושם services ב-DI
- בונה את ה-app
- מפעיל בדיקות middleware
- ממפה controllers
- מריץ את השרת

### למה זה חשוב

כי כאן נקבע איך השרת יפעל עוד לפני שהבקשות עצמן מתחילות להגיע.

### DI

השרת משתמש ב-Dependency Injection כדי ליצור מופעים של services בצורה מסודרת.

למשל:

- `AuthDomainService`
- `RoomsDomainService`
- `GameDomainService`
- `UsersDomainService`
- `AssistantDomainService`
- `EmailService`

### CORS

הפרויקט פותח את CORS כדי שהלקוח יוכל לשלוח בקשות לשרת ממקור אחר.

### `X-App-Code`

יש middleware שבודק שכמעט כל בקשה ל-`/api` נושאת header בשם `X-App-Code`.

מה המטרה:

- להוסיף שכבת בדיקה בסיסית
- להבדיל בין בקשה תקינה לבין בקשה אקראית
- להשאיר את `/api/health` פתוח לבדיקה

### מה צריך להסביר בבחינה

- למה `Program.cs` הוא bootstrap ולא לוגיקה עסקית
- למה יש CORS
- למה יש OpenAPI
- למה יש `X-App-Code`
- למה `GET /api/health` נשאר פתוח

---

## `ApiContracts.cs`

זהו החוזה בין הלקוח לשרת.

במקום להעביר מודלים פנימיים, משתמשים ב-DTOים ברורים:

- `LoginRequest`
- `RegisterRequest`
- `CreateRoomRequest`
- `JoinRoomRequest`
- `StartGameRequest`
- `SubmitAnswerRequest`
- `AssistantAdviceRequest`
- `AssistantChatRequest`
- `AuthResultResponse`
- `CurrentUserResponse`

### למה זה חשוב

- זה שומר הפרדה בין שכבות
- זה מונע חשיפת שדות פנימיים
- זה מבהיר אילו נתונים כל endpoint צריך

### מה צריך להסביר בבחינה

- מה זה DTO
- למה לא שולחים את `User` או `Room` ישירות לכל endpoint
- למה request ו-response הם טיפוסים נפרדים

---

## זרימת Auth

### `AuthController`

ה-controller הזה מטפל ב:

- `POST /api/auth/login`
- `POST /api/auth/register`
- `GET /api/auth/me`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`

### `AuthDomainService`

כאן נמצאת הלוגיקה עצמה:

- בדיקת שדות
- ולידציה
- hash של סיסמה
- בדיקת משתמש קיים
- יצירת טוקן לאיפוס סיסמה
- שליחת מייל

### איך זה עובד

1. הלקוח שולח email/password.
2. השירות מאמת.
3. אם login מצליח, חוזר אובייקט הצלחה.
4. אם register מצליח, נוצר משתמש חדש עם hash לסיסמה.
5. forgot-password יוצר טוקן ושולח מייל.
6. reset-password מאמת טוקן ומחליף סיסמה.

### מה צריך להסביר בבחינה

- למה לא שומרים סיסמה רגילה
- למה יש validation לפני כתיבה למסד
- למה forgot/reset עובדים עם token
- למה `me` מחזיר את המשתמש לפי `userId`

---

## זרימת חדרים

### `RoomsController`

מטפל ב:

- שליפת סוגי שאלות
- יצירת חדר
- רשימת חדרים ציבוריים
- שליפת חדר לפי קוד
- הצטרפות לחדר
- יציאה מחדר
- שליפת שחקנים בחדר

### `RoomsDomainService`

כאן יש לוגיקה עסקית של חדרים:

- בדיקת שם חדר
- בדיקת קוד חדר
- בדיקת nickname
- בדיקה אם החדר קיים ופעיל
- קריאה ל-DBL ליצירה או הצטרפות

### איך זה עובד

1. הלקוח יוצר חדר או מצטרף אליו.
2. השירות בודק תקינות.
3. השירות פונה ל-`RoomDB`.
4. `RoomDB` יוצר/שולף/מעדכן במסד.
5. ה-controller מחזיר JSON ללקוח.

### מה צריך להסביר בבחינה

- למה `roomCode` חשוב
- למה יש הפרדה בין ציבורי לפרטי
- למה `JoinRoom` צריך גם `userId` וגם `nickname`
- למה `LeaveRoom` לא מוחק את המשתמש

---

## זרימת המשחק

### `GameController`

מטפל ב:

- התחלת משחק
- שליפת השאלה הנוכחית
- שליחת תשובה
- שמירת תוצאות
- scoreboard
- top players

### `GameDomainService`

זה ה-layer שמחבר בין controller לבין `GameDB`.

הוא:

- בודק קלט
- מגביל כמויות
- מפעיל את ה-DB logic
- מחזיר tuple פשוט או אובייקטים מוכנים ל-API

### איך זה עובד

1. host מתחיל משחק.
2. השרת בוחר שאלות לחדר.
3. הלקוח מבקש את השאלה הנוכחית.
4. המשתמש שולח תשובה.
5. השרת שומר את התשובה.
6. בסוף המשחק נשמרות תוצאות.
7. scoreboard ו-top players נבנים מהנתונים שנשמרו.

### מה צריך להסביר בבחינה

- למה יש start game נפרד
- למה current question נשלפת רק אחרי התחלה
- למה submit answer גורם גם לשמירת תוצאות
- למה scoreboard שונה מ-top players

---

## זרימת משתמשים ואדמין

### `UsersController`

מטפל ב:

- `me`
- עדכון פרופיל
- שינוי סיסמה
- סטטיסטיקות
- תוצאות אחרונות

### `UsersDomainService`

כאן יש לוגיקת פרופיל:

- שליפת משתמש
- בדיקת סיסמה נוכחית
- עדכון שם/אימייל
- שינוי סיסמה

### `AdminController`

מטפל בפעולות ניהול:

- רשימת משתמשים
- עדכון role
- עדכון משתמש
- מחיקת משתמש

### `UsersDomainService` מול `AdminController`

ה-controller מחליט איזה endpoint נגיש.
ה-service מחליט איזה שינוי מותר ומה נחשב תקין.

### מה צריך להסביר בבחינה

- ההבדל בין פרופיל אישי לבין ניהול משתמשים
- למה admin משתמש ב-DTO שונה
- למה מחיקת משתמש צריכה לשמור על עקביות במסד

---

## ה-AI assistant

### `AssistantController`

מטפל ב:

- `POST /api/assistant/advice`
- `POST /api/assistant/chat`

### `AssistantDomainService`

זה השירות שמכין בקשות ל-Gemini.

הוא:

- בונה payload
- שולח ל-HTTP API חיצוני
- מנתח תשובה
- מחזיר טקסט מוכן ללקוח

### ההבדל בין שני המסלולים

- `advice` מיועד לשאלה בודדת
- `chat` מיועד לשיחה עם היסטוריה

### מה צריך להסביר בבחינה

- למה assistant לא יושב בתוך controller
- למה יש צורך ב-history
- למה משתמשים ב-`HttpClient`
- למה יש CancellationToken

---

## `EmailService`

זה השירות ששולח מייל לאיפוס סיסמה.

### מה הוא עושה

- בונה `MimeMessage`
- ממלא את השולח והנמען
- מכניס subject ו-body
- מתחבר ל-SMTP
- שולח את המייל
- מתנתק

### למה זה חשוב

כי `forgot-password` צריך מנגנון תקשורת אמיתי עם המשתמש.

### מה צריך להסביר בבחינה

- למה SMTP הוא חלק מהשרת
- למה המייל מכיל reset link ולא סיסמה
- למה משתמשים בהגדרות חיצוניות ל-SMTP

---

## איך להסביר את שכבת ה-API במשפט קצר

> ה-API הוא שכבת התיווך בין הלקוח למסד הנתונים. ה-controllers מקבלים בקשות HTTP, ה-services מטפלים בלוגיקה עסקית, וה-DBL עושה את העבודה מול MySQL. `Program.cs` מחבר את כל זה יחד ומגדיר את אבטחת הבסיס וההרצה של השרת.

---

## שאלות שבחינה יכולה לשאול

- למה יש `Program.cs` ומה קורה בו?
- מה ההבדל בין controller ל-service?
- למה יש DTOים?
- איך login/register עובדים?
- איך room creation ו-join עובדים?
- איך מתחיל משחק?
- איך נשמרת תשובה?
- איך מחשבים scoreboard?
- למה assistant מופרד משאר השרת?
- למה email reset שייך לשכבת השרת?

