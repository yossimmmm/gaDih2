using System;
using System.Text.RegularExpressions;

namespace Models
{
    public static class ValidationHelper
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return EmailRegex.IsMatch(email.Trim());
        }

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

        public static (bool IsValid, string ErrorMessage) ValidateUsername(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Username is required.");

            if (username.Length > 50)
                return (false, "Username cannot exceed 50 characters.");

            // Check for invalid characters
            if (Regex.IsMatch(username, @"[<>""'&]"))
                return (false, "Username contains invalid characters.");

            return (true, string.Empty);
        }

        public static (bool IsValid, string ErrorMessage) ValidateFullName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (true, string.Empty); // Full name is optional

            if (fullName.Length > 100)
                return (false, "Full name cannot exceed 100 characters.");

            return (true, string.Empty);
        }

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

        public static (bool IsValid, string ErrorMessage) ValidateNickname(string? nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
                return (true, string.Empty); // Nickname is optional, will use default

            if (nickname.Length > 50)
                return (false, "Nickname cannot exceed 50 characters.");

            if (nickname.Length < 2)
                return (false, "Nickname must be at least 2 characters long.");

            // Check for invalid characters
            if (Regex.IsMatch(nickname, @"[<>""'&]"))
                return (false, "Nickname contains invalid characters.");

            return (true, string.Empty);
        }

        public static string SanitizeInput(string? input, int maxLength = 500)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Trim and limit length
            var sanitized = input.Trim();
            if (sanitized.Length > maxLength)
                sanitized = sanitized.Substring(0, maxLength);

            // Remove potentially dangerous characters for XSS
            sanitized = sanitized.Replace("<", "&lt;")
                                 .Replace(">", "&gt;")
                                 .Replace("\"", "&quot;")
                                 .Replace("'", "&#x27;");

            return sanitized;
        }
    }
}
