using System;
using System.Security.Cryptography;
using System.Text;

namespace Private_Clinic.Helpers
{
    public static class PasswordHelper
    {
        public static string Hash(string plain)
        {
            var saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(saltBytes);
            var salt = Convert.ToBase64String(saltBytes);
            return $"{salt}:{ComputeHash(plain, salt)}";
        }

        public static bool Verify(string plain, string stored)
        {
            if (string.IsNullOrWhiteSpace(stored) || !stored.Contains(":")) return false;
            var parts = stored.Split(':');
            return ComputeHash(plain, parts[0]).Equals(parts[1], StringComparison.Ordinal);
        }

        private static string ComputeHash(string plain, string saltBase64)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(saltBase64 + plain));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
