using System.Security.Cryptography;
using System.Text;

namespace EBill.Services
{
    // Generates the opaque refresh token handed to an external app (e.g. Stock
    // Management) when it connects to a company's Ebill account through the
    // login/register "connect" flow. Only the hash is ever persisted (see
    // Company.RefreshTokenHash) — the plaintext value is returned to the caller
    // exactly once, at connect time.
    public static class RefreshTokenGenerator
    {
        public static (string PlainText, string Hash) Generate()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var plainText = Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            return (plainText, Hash(plainText));
        }

        public static string Hash(string plainText)
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainText));
            return Convert.ToHexString(hashBytes);
        }
    }
}
