# TriviaGame.Mobile

מעטפת מובייל ב-`.NET MAUI` עבור מערכת `TriviaGame` הקיימת ב-Web.

## איך זה עובד

- האפליקציה טוענת את שרת ה-Web שלך בתוך `WebView`.
- המשתמש מזין כתובת Backend ולוחץ על `Load`.
- יש גם בדיקת `Health` Native כדי לוודא שהשרת נגיש.

## ארכיטקטורת חיבור לשרת (בקצרה)

האפליקציה המובייל היא לקוח:
1. השרת רץ בפרויקט `TriviaGame` על פורט `5038`.
2. במסך המובייל המשתמש מזין כתובת Backend (לדוגמה `http://10.0.2.2:5038`).
3. בלחיצה על `Load`, האפליקציה טוענת את הכתובת ל-`WebView`.
4. בתוך ה-`WebView` נטענת אפליקציית ה-Web המלאה (Login/Menu/Rooms/Play וכו').
5. פעולות משתמש ב-WebView קוראות ל-API של השרת (למשל התחברות, חדרים, איפוס סיסמה).
6. בנוסף, בלחיצה על `Check Backend Health` האפליקציה מבצעת קריאת HTTP ישירה ל-`/api/health` ומציגה סטטוס.

כלומר:
- לוגיקת המשחק והנתונים מתבצעת בשרת.
- אפליקציית MAUI משמשת מעטפת מובייל עם טעינה, ניווט, ובדיקת קישוריות.

## מה כל כפתור עושה במסך המובייל

- `Load`:
  - קורא את כתובת השרת מה-Entry.
  - מאמת שהכתובת חוקית (`http/https`).
  - שומר את הכתובת ב-`Preferences` מקומיים.
  - מטעין את ה-URL בתוך `GameWebView`.

- `Check Backend Health`:
  - קורא את אותה כתובת.
  - שולח `GET` ל-`<backend>/api/health`.
  - אם קיבל `200 OK` מציג שהשרת זמין.
  - אם יש שגיאת רשת/Timeout/Status אחר, מציג הודעת כשל.

## דוגמת זרימת בקשות אמיתית

אחרי שהמשתמש לוחץ `Load` ונכנס ל-Login בתוך ה-WebView:
1. המשתמש מזין אימייל/סיסמה.
2. ה-Web client שולח `POST /api/auth/login`.
3. השרת מחזיר סטטוס התחברות ויוצר session cookie.
4. המשתמש עובר ל-`/menu` וממשיך ל-Rooms/Play.
5. קריאות נוספות לשרת מתבצעות לפי המסכים (`/api/auth/me`, חדרים, תוצאות וכו').

## הרצת השרת (לפני הרצת המובייל)

מתיקיית השורש של הפרויקט:

```powershell
dotnet run --project .\TriviaGame\TriviaGame.csproj --launch-profile http
```

שרת ברירת מחדל:
- `http://localhost:5038`

## בנייה/הרצה של אפליקציית MAUI

### Android
```powershell
dotnet build .\TriviaGame.Mobile\TriviaGame.Mobile.csproj -f net10.0-android -t:Run
```

### Windows
```powershell
dotnet build .\TriviaGame.Mobile\TriviaGame.Mobile.csproj -f net10.0-windows10.0.19041.0 -t:Run
```

## כתובת Backend לפי פלטפורמה

- אמולטור Android: `http://10.0.2.2:5038`
- Windows על אותו מחשב: `http://localhost:5038`
- טלפון אמיתי באותה רשת: `http://<כתובת-LAN-של-המחשב>:5038`

## הערות חשובות

- בטלפון אמיתי אי אפשר להשתמש ב-`localhost`.
- אם חומת אש חוסמת, יש לפתוח גישה לפורט `5038`.
- אם יש בעיות תצוגה, להריץ מחדש אחרי עדכון כתובת ולחיצה על `Load`.
