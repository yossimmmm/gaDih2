using System;
using System.Text.RegularExpressions;

namespace Models
{
    // מחלקת עזר לאימות ולניקוי קלט.
    // כל מה שנכנס מה-UI או מה-API עובר כאן לפני שמכניסים אותו למסד.
    public static class ValidationHelper
    {
        // regex פשוט לבדיקה שאימייל נראה תקין.
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // בודק אם האימייל לא ריק ונראה כמו כתובת דוא"ל.
        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return EmailRegex.IsMatch(email.Trim());
        }

        // מאמת סיסמה.
        // כרגע הבדיקה פשוטה: חובה, מינימום אורך, ומקסימום אורך.
        public static (bool IsValid, string ErrorMessage) ValidatePassword(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required.");

            if (password.Length < 6)
                return (false, "Password must be at least 6 characters long.");

            if (password.Length > 100)
                return (false, "Password cannot exceed 100 characters.");

            return (true, string.Empty);
        }

        // מאמת username.
        public static (bool IsValid, string ErrorMessage) ValidateUsername(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Username is required.");

            if (username.Length > 50)
                return (false, "Username cannot exceed 50 characters.");

            if (Regex.IsMatch(username, @"[<>""'&]"))
                return (false, "Username contains invalid characters.");

            return (true, string.Empty);
        }

        // מאמת full name.
        // ריק מותר, אבל אם יש ערך הוא לא יכול להיות ארוך מדי.
        public static (bool IsValid, string ErrorMessage) ValidateFullName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (true, string.Empty);

            if (fullName.Length > 100)
                return (false, "Full name cannot exceed 100 characters.");

            return (true, string.Empty);
        }

        // מאמת שם חדר.
        public static (bool IsValid, string ErrorMessage) ValidateRoomName(string? roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
                return (false, "Room name is required.");

            if (roomName.Length < 3)
                return (false, "Room name must be at least 3 characters long.");

            if (roomName.Length > 100)
                return (false, "Room name cannot exceed 100 characters.");

            return (true, string.Empty);
        }

        // מאמת קוד חדר.
        public static (bool IsValid, string ErrorMessage) ValidateRoomCode(string? roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode))
                return (false, "Room code is required.");

            roomCode = roomCode.Trim().ToUpperInvariant();

            if (roomCode.Length != 6)
                return (false, "Room code must be exactly 6 characters.");

            if (!Regex.IsMatch(roomCode, @"^[A-Z0-9]{6}$"))
                return (false, "Room code must contain only letters and numbers.");

            return (true, string.Empty);
        }

        // מאמת nickname.
        public static (bool IsValid, string ErrorMessage) ValidateNickname(string? nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
                return (true, string.Empty);

            if (nickname.Length > 50)
                return (false, "Nickname cannot exceed 50 characters.");

            if (nickname.Length < 2)
                return (false, "Nickname must be at least 2 characters long.");

            if (Regex.IsMatch(nickname, @"[<>""'&]"))
                return (false, "Nickname contains invalid characters.");

            return (true, string.Empty);
        }

        // מנקה טקסט חופשי לפני שמחזירים אותו ל-UI או שומרים ב-DB.
        public static string SanitizeInput(string? input, int maxLength = 500)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var sanitized = input.Trim();
            if (sanitized.Length > maxLength)
                sanitized = sanitized.Substring(0, maxLength);

            sanitized = sanitized.Replace("<", "&lt;")
                                 .Replace(">", "&gt;")
                                 .Replace("\"", "&quot;")
                                 .Replace("'", "&#x27;");

            return sanitized;
        }
    }
}
