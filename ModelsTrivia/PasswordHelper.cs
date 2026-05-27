using System.Security.Cryptography;
using System.Text;

namespace Models
{
    public static class PasswordHelper
    {
        public static string Hash(string plain)
        {
            // יצירת מופע SHA256 לחישוב hash של הסיסמה
            using var sha = SHA256.Create();
            // המרת טקסט לבייטים (UTF8) וחישוב hash
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(plain ?? ""));
            // המרה לפורמט hex קטן לשמירה עקבית במסד
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        // אימות סיסמה: מחשבים hash מחדש ומשווים לערך השמור
        public static bool Verify(string plain, string hash) => Hash(plain) == (hash ?? "");
    }
}
