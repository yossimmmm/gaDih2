// Namespace גלובלי קטן לפונקציות האימות של האתר.
window.auth = window.auth || {};

// שולחת בקשת התחברות לשרת ומחזירה מבנה אחיד ללקוח.
// #login #cookie #session_token
window.auth.login = async function (email, password) {
  // Fetch ישיר ל-endpoint של login; ה-cookie חוזר מהשרת כשיש הצלחה.
  const res = await fetch("/api/auth/login", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password })
  });

  // אם השרת דחה את ההתחברות, מחזירים הודעת ברירת מחדל.
  if (!res.ok) {
    return { ok: false, message: "Invalid email or password.", userId: 0, role: "User" };
  }

  // בהצלחה מחזירים את ה-JSON של השרת כמו שהוא.
  return await res.json();
};

// בודקת מי המשתמש הנוכחי לפי ה-cookie של session.
// #cookie #session_token #auth-me
window.auth.me = async function () {
  try {
    // credentials: include חשוב כדי שהדפדפן ישלח את ה-cookie לשרת.
    const res = await fetch("/api/auth/me", { credentials: "include" });
    // אם אין session תקף, מחזירים משתמש אנונימי.
    if (!res.ok) return { userId: 0, role: "User" };
    // מנסים לקרוא JSON תקני מהשרת.
    const data = await res.json();
    // מנרמלים את התשובה כדי שה-client לא יצטרך לטפל ב-undefined.
    return { userId: data.userId || 0, role: data.role || "User" };
  } catch (e) {
    // אם יש תקלה ברשת, מתנהגים כאילו אין משתמש מחובר.
    return { userId: 0, role: "User" };
  }
};

// מבצעת logout בצד השרת ומאפשרת לנקות session cookie.
// #logout #cookie #session_token #sign-out
window.auth.logout = async function () {
  try {
    // השרת מוחק את ה-session מהמסד וגם מה-cookie.
    await fetch("/api/auth/logout", { method: "POST", credentials: "include" });
  } catch (e) {
    // logout הוא best-effort; לא עוצרים את ה-UI אם הרשת נפלה.
  }
};

// שולחת בקשה ליצירת reset link למייל של המשתמש.
// #forgot-password #email #reset-link
window.auth.forgotPassword = async function (email) {
  // timeout מונע מצב שהמסך יישאר "תקוע" אם השרת לא מגיב.
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), 20000);

  try {
    // השרת יקבל רק את האימייל, ייצור טוקן וישלח מייל.
    const res = await fetch("/api/auth/forgot-password", {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email }),
      signal: controller.signal
    });

    // אפילו אם השרת מחזיר שגיאה, ננסה לקרוא payload כדי להציג message.
    const payload = await res.json().catch(() => ({}));
    // ok משקף את סטטוס ה-HTTP, ולא רק את קיום ה-JSON.
    return { ok: res.ok, message: payload.message || "" };
  } catch (err) {
    // AbortError = בקשה שחרגה מה-timeout.
    if (err && err.name === "AbortError") {
      return { ok: false, message: "Request timed out. Check SMTP settings and try again." };
    }
    // כל שגיאת רשת אחרת.
    return { ok: false, message: "Network error while sending reset email." };
  } finally {
    // מנקים את ה-timeout בכל מקרה.
    clearTimeout(timeoutId);
  }
};

// שולחת טוקן + סיסמה חדשה כדי לבצע את האיפוס בפועל.
// #reset-password #token
window.auth.resetPassword = async function (token, newPassword) {
  // בקשה ל-endpoint של reset-password.
  const res = await fetch("/api/auth/reset-password", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token, newPassword })
  });

  // מחזירים תשובה אחידה גם אם השרת החזיר status לא-200.
  const payload = await res.json().catch(() => ({}));
  // גם כאן אנחנו שומרים על צורה אחידה ללקוח: ok + message.
  return { ok: res.ok, message: payload.message || "" };
};
