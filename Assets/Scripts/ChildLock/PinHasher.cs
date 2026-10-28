using System.Security.Cryptography;
using System.Text;

namespace TalkingKitty.ChildLock
{
    /// <summary>
    /// Hashes PINs with a constant salt so the plaintext PIN is never stored.
    /// </summary>
    public static class PinHasher
    {
        private const string Salt = "TalkingKitty.v1";

        public static string Hash(string pin)
        {
            var bytes = Encoding.UTF8.GetBytes(Salt + (pin ?? string.Empty));
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(bytes);

            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
    }
}