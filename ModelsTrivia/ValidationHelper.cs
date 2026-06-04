using System;
using System.Text.RegularExpressions;

namespace Models
{
    // Input validation and light sanitization helpers.
    // These checks run before values are sent to the database or rendered in the UI.
    public static class ValidationHelper
    {
        // Simple email pattern used for basic validation.
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Returns true only when the email has a valid basic shape.
        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return EmailRegex.IsMatch(email.Trim());
        }

        // Password rules used by the app.
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

        // Username rules.
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

        // Full name is optional, but if provided it must not be too long.
        public static (bool IsValid, string ErrorMessage) ValidateFullName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (true, string.Empty);

            if (fullName.Length > 100)
                return (false, "Full name cannot exceed 100 characters.");

            return (true, string.Empty);
        }

        // Room name rules.
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

        // Room code rules.
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

        // Nickname rules.
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

        // Basic HTML-oriented sanitization for user-entered text.
        // This does not replace parameterized SQL, but it helps keep text safe in the UI.
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
