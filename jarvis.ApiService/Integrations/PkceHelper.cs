using System;
using System.Security.Cryptography;
using System.Text;

namespace jarvis.ApiService.Integrations
{


    public static class PkceHelper
    {
        public static string GenerateCodeVerifier()
        {
            // Code Verifier: Zufälliger String, mindestens 43 Zeichen
            const int length = 128;
            var random = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(random);
            }

            return Convert.ToBase64String(random)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static string GenerateCodeChallenge(string codeVerifier)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                return Convert.ToBase64String(hash)
                    .TrimEnd('=')
                    .Replace('+', '-')
                    .Replace('/', '_');
            }
        }
    }

}
