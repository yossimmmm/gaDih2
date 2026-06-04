using System.Security.Cryptography;
using System.Text;

namespace Models
{
    // עזר בסיסי ל-hash ולאימות סיסמאות.
    // לא שומרים סיסמה רגילה, רק ערך hash להשוואה.
    public static class PasswordHelper
    {
        // ממיר טקסט סיסמה ל-hash SHA256.
        public static string Hash(string plain)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(plain ?? ""));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        // משווה את הסיסמה הרגילה ל-hash השמור.
        public static bool Verify(string plain, string hash) => Hash(plain) == (hash ?? "");
    }
}
