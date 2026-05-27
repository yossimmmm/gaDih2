// אתחול אובייקט auth גלובלי לפעולות אימות בצד לקוח
window.auth = window.auth || {};

// התחברות משתמש: שולח אימייל/סיסמה לשרת ומחזיר DTO אחיד
window.auth.login = async function (email, password) {
  const res = await fetch("/api/auth/login", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password })
  });

  // אם השרת החזיר כשל אימות
  if (!res.ok) {
    return { ok: false, message: "Invalid email or password.", userId: 0, role: "User" };
  }

  // אם הצליח - מחזירים תשובת שרת
  return await res.json();
};

// בדיקת משתמש מחובר לפי session cookie קיים
window.auth.me = async function () {
  try {
    const res = await fetch("/api/auth/me", { credentials: "include" });
    if (!res.ok) return { userId: 0, role: "User" };
    const data = await res.json();
    return { userId: data.userId || 0, role: data.role || "User" };
  } catch (e) {
    // במקרה כשל רשת מחזירים מצב לא מחובר
    return { userId: 0, role: "User" };
  }
};

// התנתקות משתמש בצד שרת
window.auth.logout = async function () {
  try {
    await fetch("/api/auth/logout", { method: "POST", credentials: "include" });
  } catch (e) {
    // מתעלמים משגיאת ניתוק כדי לא לתקוע את ה-UI
  }
};

// בקשת "שכחתי סיסמה": יצירת טוקן ושליחת מייל
window.auth.forgotPassword = async function (email) {
  // timeout ידני כדי לא להיתקע אם שרת SMTP איטי
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), 20000);

  try {
    const res = await fetch("/api/auth/forgot-password", {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email }),
      signal: controller.signal
    });

    // ניסיון קריאת payload; אם אין JSON נחזיר אובייקט ריק
    const payload = await res.json().catch(() => ({}));
    return { ok: res.ok, message: payload.message || "" };
  } catch (err) {
    // טיפול מובחן במקרה timeout
    if (err && err.name === "AbortError") {
      return { ok: false, message: "Request timed out. Check SMTP settings and try again." };
    }
    // טיפול בכשל רשת כללי
    return { ok: false, message: "Network error while sending reset email." };
  } finally {
    // ניקוי timeout בכל תרחיש
    clearTimeout(timeoutId);
  }
};

// איפוס סיסמה בפועל לפי טוקן + סיסמה חדשה
window.auth.resetPassword = async function (token, newPassword) {
  const res = await fetch("/api/auth/reset-password", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token, newPassword })
  });

  // קריאת payload בצורה בטוחה והחזרת תשובה אחידה
  const payload = await res.json().catch(() => ({}));
  return { ok: res.ok, message: payload.message || "" };
};
