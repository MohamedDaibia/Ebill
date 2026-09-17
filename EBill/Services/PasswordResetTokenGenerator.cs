using System.Security.Cryptography;
using System.Text;

namespace EBill.Services
{
    // Generates the opaque token emailed to a company when it requests a password
    // reset (AccountController.ForgotPassword). Only the hash is ever persisted
    // (see Company.PasswordResetTokenHash) — the plaintext value is emailed to the
    // company exactly once, at request time, and re-hashed to check against the
    // stored value when the reset link is used (AccountController.ResetPassword).
    // Same shape as RefreshTokenGenerator, kept separate since the two tokens
    // protect different things and expire on different schedules.
    public static class PasswordResetTokenGenerator
    {
        public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

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
