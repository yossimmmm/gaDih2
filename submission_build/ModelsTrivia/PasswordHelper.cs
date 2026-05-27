using System.Security.Cryptography;
using System.Text;

namespace Models
{
    public static class PasswordHelper
    {
        public static string Hash(string plain)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(plain ?? ""));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        public static bool Verify(string plain, string hash) => Hash(plain) == (hash ?? "");
    }
}
