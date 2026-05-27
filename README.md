# Trivia Game - מדריך הרצה והגשה

מאגר זה כולל פרויקט שרת-לקוח מלא:
- צד שרת + לקוח Web ב-`TriviaGame` (ASP.NET Core + Blazor)
- שכבת מסד נתונים ב-`TriviaDBL`
- מודלים משותפים ב-`ModelsTrivia`
- לקוח מובייל ב-`TriviaGame.Mobile` (MAUI)

## 1) דרישות מקדימות

- .NET SDK 10
- MySQL Server 8 ומעלה
- Windows + Visual Studio / VS Code
- עבור MAUI: התקנת MAUI workload

## 2) הקמת מסד נתונים

1. לייבא את הקובץ:
   - `trivia_game.sql`
2. לוודא ש-MySQL רץ.
3. לוודא מחרוזת חיבור נכונה בקבצים:
   - `TriviaDBL/UserDB.cs`
   - `TriviaDBL/DB.cs` (אם בשימוש)

## 3) הגדרת SMTP (שחזור סיסמה)

להגדיר משתני סביבה לפני הרצת השרת:

```powershell
$env:SMTP_FROM="info@inverra.co"
$env:SMTP_HOST="mail.spacemail.com"
$env:SMTP_USER="info@inverra.co"
$env:SMTP_PASS="***"
$env:SMTP_PORT="465"
$env:SMTP_SECURE="true"
```

הערה: את הסיסמה האמיתית לשמור רק מקומית, לא להעלות ל-Git.

## 4) הרצת Backend + Web (localhost)

מהתיקייה הראשית:

```powershell
dotnet run --project .\TriviaGame\TriviaGame.csproj --launch-profile http
```

כניסה דרך הדפדפן:
- `http://localhost:5038/login`

## 5) אם פורט 5038 תפוס

לעצור תהליך קיים ולהריץ מחדש:

```powershell
Get-Process TriviaGame -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet run --project .\TriviaGame\TriviaGame.csproj --launch-profile http
```

## 6) הרצת לקוח MAUI

### Windows
```powershell
dotnet build .\TriviaGame.Mobile\TriviaGame.Mobile.csproj -f net10.0-windows10.0.19041.0 -t:Run
```

### Android
```powershell
dotnet build .\TriviaGame.Mobile\TriviaGame.Mobile.csproj -f net10.0-android -t:Run
```

כתובת שרת למובייל:
- אמולטור Android: `http://10.0.2.2:5038`
- מכשיר אמיתי: `http://<כתובת-LAN-של-המחשב>:5038`

## 7) מה להדגים בבחינה

- הרשמה / התחברות / התנתקות
- שכחתי סיסמה + איפוס דרך מייל
- הרשאות לפי תפקיד (`User` / `Manager` / `Admin`)
- זרימת משחק מלאה: Rooms -> Lobby -> Play -> Results
- עמוד ניהול משתמשים לאדמין:
  - עריכת פרטי משתמש
  - שינוי תפקיד
  - מחיקת משתמש
- עמוד Assistant (AI)
- לקוח MAUI + בדיקת זמינות שרת

## 8) קבצים להגשה

- ספר פרויקט (Word):
  - `docs/PROJECT_BOOK_HE.docx`
  - אם נעול/ישן: `docs/PROJECT_BOOK_HE_FIXED.docx`
- מסמכי תיעוד:
  - `docs/*.md`
- מסד נתונים:
  - `trivia_game.sql`
- קוד מקור:
  - `TriviaGame`
  - `TriviaDBL`
  - `ModelsTrivia`
  - `TriviaGame.Mobile`

