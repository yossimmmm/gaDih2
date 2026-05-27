using System;
using System.Text.RegularExpressions;

namespace Models
{
    public static class ValidationHelper
    {
        // Regex בסיסי לבדיקת פורמט אימייל תקין
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsValidEmail(string? email)
        {
            // אם הערך ריק/לבן - אימייל לא תקין
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // בדיקת התאמה לתבנית אימייל אחרי Trim
            return EmailRegex.IsMatch(email.Trim());
        }

        public static (bool IsValid, string ErrorMessage) ValidatePassword(string? password)
        {
            // סיסמה חובה
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required.");

            // מינימום אורך סיסמה
            if (password.Length < 6)
                return (false, "Password must be at least 6 characters long.");

            // מקסימום אורך סיסמה
            if (password.Length > 100)
                return (false, "Password cannot exceed 100 characters.");

            // כל הבדיקות עברו
            return (true, string.Empty);
        }

        public static (bool IsValid, string ErrorMessage) ValidateUsername(string? username)
        {
            // שם משתמש חובה
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Username is required.");

            // הגבלת אורך שם משתמש
            if (username.Length > 50)
                return (false, "Username cannot exceed 50 characters.");

            // חסימת תווים מסוכנים/לא רצויים
            if (Regex.IsMatch(username, @"[<>""'&]"))
                return (false, "Username contains invalid characters.");

            // שם משתמש תקין
            return (true, string.Empty);
        }

        public static (bool IsValid, string ErrorMessage) ValidateFullName(string? fullName)
        {
            // שם מלא לא חובה
            if (string.IsNullOrWhiteSpace(fullName))
                return (true, string.Empty);

            // הגבלת אורך שם מלא
            if (fullName.Length > 100)
                return (false, "Full name cannot exceed 100 characters.");

            // שם מלא תקין
            return (true, string.Empty);
        }

        public static (bool IsValid, string ErrorMessage) ValidateRoomName(string? roomName)
        {
            // שם חדר חובה
            if (string.IsNullOrWhiteSpace(roomName))
                return (false, "Room name is required.");

            // אורך מינימלי לשם חדר
            if (roomName.Length < 3)
                return (false, "Room name must be at least 3 characters long.");

            // אורך מקסימלי לשם חדר
            if (roomName.Length > 100)
                return (false, "Room name cannot exceed 100 characters.");

            // שם חדר תקין
            return (true, string.Empty);
        }

        public static (bool IsValid, string ErrorMessage) ValidateRoomCode(string? roomCode)
        {
            // קוד חדר חובה
            if (string.IsNullOrWhiteSpace(roomCode))
                return (false, "Room code is required.");

            // נרמול הקוד לאותיות גדולות
            roomCode = roomCode.Trim().ToUpperInvariant();

            // קוד חדר חייב 6 תווים בדיוק
            if (roomCode.Length != 6)
                return (false, "Room code must be exactly 6 characters.");

            // קוד חדר חייב להכיל אותיות/ספרות בלבד
            if (!Regex.IsMatch(roomCode, @"^[A-Z0-9]{6}$"))
                return (false, "Room code must contain only letters and numbers.");

            // קוד חדר תקין
            return (true, string.Empty);
        }

        public static (bool IsValid, string ErrorMessage) ValidateNickname(string? nickname)
        {
            // ניקניים לא חובה
            if (string.IsNullOrWhiteSpace(nickname))
                return (true, string.Empty);

            // הגבלת אורך ניקניים
            if (nickname.Length > 50)
                return (false, "Nickname cannot exceed 50 characters.");

            // אורך מינימלי לניקניים
            if (nickname.Length < 2)
                return (false, "Nickname must be at least 2 characters long.");

            // חסימת תווים מסוכנים/לא רצויים
            if (Regex.IsMatch(nickname, @"[<>""'&]"))
                return (false, "Nickname contains invalid characters.");

            // ניקניים תקין
            return (true, string.Empty);
        }

        public static string SanitizeInput(string? input, int maxLength = 500)
        {
            // אם הקלט ריק מחזירים מחרוזת ריקה
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // ניקוי רווחים והגבלת אורך
            var sanitized = input.Trim();
            if (sanitized.Length > maxLength)
                sanitized = sanitized.Substring(0, maxLength);

            // המרת תווים מסוכנים למניעת XSS בסיסי
            sanitized = sanitized.Replace("<", "&lt;")
                                 .Replace(">", "&gt;")
                                 .Replace("\"", "&quot;")
                                 .Replace("'", "&#x27;");

            // החזרת ערך נקי
            return sanitized;
        }
    }
}
