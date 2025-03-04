using System.Diagnostics;
using System.Text.Json;
using System.Web;

// ReSharper disable once CheckNamespace
namespace END.Helper;

    public class CryptoUtil
    {
        private static string? _cipherKey;

        public CryptoUtil()
        {
            _cipherKey = GetCipherKey();

            if (string.IsNullOrEmpty(_cipherKey))
                _cipherKey = GenerateKey();
        }

        public CryptoUtil(string cipherKey)
        {
            if (string.IsNullOrEmpty(cipherKey))
                cipherKey = GetCipherKey();

            if (string.IsNullOrEmpty(cipherKey))
                cipherKey = GenerateKey();

            _cipherKey = cipherKey;
        }

        [DebuggerStepThrough]
        private static string GenerateKey()
        {
            return Aes256Cipher.GenerateNewKey();
        }
        
        private static string GetCipherKey()
        {
            var appPath = Config.GetAppPath();
            var envFile = Path.Combine(appPath, "bin", Const.EnvFile);

            if (!File.Exists(envFile))
                _ = new EnvironmentSettings();

            if (!File.Exists(envFile))
                return string.Empty;

            var json = File.ReadAllText(envFile);
            var je = JsonDocument.Parse(json).RootElement;
            var cipherKey = je.GetProperty("CipherKey").GetString();

            return cipherKey ?? "";
        }

        public string EncryptValue(string value)
        {
            if (_cipherKey == null)
                return "";

            var cipher = new Aes256Cipher(_cipherKey);
            var encrypt = "CipherText:" + cipher.Encrypt(value);
            return encrypt;
        }

        public string DecryptValue(string value)
        {
            var cipherText = value.Replace("CipherText:", "");
            
            if (_cipherKey == null)
                return "";

            var cipher = new Aes256Cipher(_cipherKey);
            return cipher.Decrypt(cipherText);
        }
    }
