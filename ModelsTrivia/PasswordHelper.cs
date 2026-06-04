using System.Security.Cryptography;
using System.Text;

namespace Models
{
    // עזר ל-hashing של סיסמאות.
    // הפרויקט שומר hash ולא סיסמה רגילה.
    public static class PasswordHelper
    {
        // מחשב hash של סיסמה רגילה באמצעות SHA-256.
        public static string Hash(string plain)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(plain ?? ""));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        // משווה סיסמה רגילה מול hash קיים במסד.
        public static bool Verify(string plain, string hash) => Hash(plain) == (hash ?? "");
    }
}
