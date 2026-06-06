window.auth = window.auth || {};

window.auth.login = async function (email, password) {
  const res = await fetch("/api/auth/login", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password })
  });

  if (!res.ok) {
    return { ok: false, message: "Invalid email or password.", userId: 0, role: "User" };
  }

  return await res.json();
};

window.auth.me = async function () {
  try {
    const res = await fetch("/api/auth/me", { credentials: "include" });
    if (!res.ok) return { userId: 0, role: "User" };
    const data = await res.json();
    return { userId: data.userId || 0, role: data.role || "User" };
  } catch (e) {
    return { userId: 0, role: "User" };
  }
};

window.auth.logout = async function () {
  try {
    await fetch("/api/auth/logout", { method: "POST", credentials: "include" });
  } catch (e) {}
};

window.auth.forgotPassword = async function (email) {
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

    const payload = await res.json().catch(() => ({}));
    return { ok: res.ok, message: payload.message || "" };
  } catch (err) {
    if (err && err.name === "AbortError") {
      return { ok: false, message: "Request timed out. Check SMTP settings and try again." };
    }
    return { ok: false, message: "Network error while sending reset email." };
  } finally {
    clearTimeout(timeoutId);
  }
};

window.auth.resetPassword = async function (token, newPassword) {
  const res = await fetch("/api/auth/reset-password", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token, newPassword })
  });

  const payload = await res.json().catch(() => ({}));
  return { ok: res.ok, message: payload.message || "" };
};
