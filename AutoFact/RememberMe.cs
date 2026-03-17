using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace autofact
{
    /// <summary>
    /// Persists "remember me" credentials encrypted with Windows DPAPI.
    /// Data is stored in %AppData%\AutoFact\remember.dat
    /// </summary>
    internal static class RememberMe
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AutoFact", "remember.dat");

        private record SavedCredentials(string Email, string Password);

        /// <summary>Saves credentials encrypted for the current Windows user.</summary>
        public static void Save(string email, string password)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                var json    = JsonSerializer.Serialize(new SavedCredentials(email, password));
                var plain   = Encoding.UTF8.GetBytes(json);
                var cipher  = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(FilePath, cipher);
            }
            catch { /* silently ignore — remember-me is non-critical */ }
        }

        /// <summary>Loads and decrypts saved credentials. Returns null if none exist or decryption fails.</summary>
        public static (string Email, string Password)? Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                var cipher = File.ReadAllBytes(FilePath);
                var plain  = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
                var creds  = JsonSerializer.Deserialize<SavedCredentials>(Encoding.UTF8.GetString(plain));
                if (creds is null || string.IsNullOrEmpty(creds.Email)) return null;
                return (creds.Email, creds.Password);
            }
            catch { return null; }
        }

        /// <summary>Deletes any saved credentials.</summary>
        public static void Clear()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); }
            catch { }
        }
    }
}
