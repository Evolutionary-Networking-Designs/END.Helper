using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

// ReSharper disable once CheckNamespace
namespace END.Helper;

    /// <summary>
    /// A tiny wrapper around built-in AES-256 algorithm.
    /// For better security it prefixes randomly generated IV to a cipher text.
    /// Yes, it is secure enough https://security.stackexchange.com/a/85778/207381
    /// </summary>
    public class Aes256Cipher
    {
        private readonly string _key;
        
        public const int HashSize = 32; // size in bytes
        public const int Iterations= 100000; // number of pbkdf2 iterations

        public Aes256Cipher(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new NullReferenceException("The key is empty");
            _key = key;
            
        }
        
        #region "Helper Functions"
        private static byte[] GetSalt(string input)
        {
            // Generate a salt
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var sha = SHA512.Create();
            var salt = sha.ComputeHash(inputBytes);
            sha.Clear();
            return salt;
        }

        private static string DeriveSymetricKey(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var salt = GetSalt(input);            
            var prf = KeyDerivationPrf.HMACSHA512;    // or sha256, or sha1
            var key = KeyDerivation.Pbkdf2(input, salt, prf, Iterations, HashSize);
            return Convert.ToBase64String(key);
        }

        private static (string ciphertext, string nonce) GetCipherParams(string cipher)
        {
            string ciphertext;
            string nonce;

            cipher = cipher.Replace("%7C", "|");
            string[] cipherParms = cipher.Split('|');

            ciphertext = cipherParms[0];
            nonce = cipherParms[1];

            return (ciphertext, nonce);
        }
        
        #endregion
        
        #region "Encrypt / Decrypt methods"

        private static string GetNonce()
        {
            const int nonceLength = 12; // in bytes
            var nonce = new byte[nonceLength];

#if NETSTANDARD2_1
            RandomNumberGenerator.Fill(nonce);
#else
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(nonce);
#endif

            return Convert.ToBase64String(nonce);

        }

        private static string EncryptWithGCM(string plaintext, string keyString, string nonceString)
        {
            var tagLength = 16;
            var key = Convert.FromBase64String(keyString);
            var nonce = Convert.FromBase64String(nonceString);

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertextTagBytes = new byte[plaintextBytes.Length + tagLength];

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), tagLength * 8, nonce);
            cipher.Init(true, parameters);

            var offset = cipher.ProcessBytes(plaintextBytes, 0, plaintextBytes.Length, ciphertextTagBytes, 0);
            cipher.DoFinal(ciphertextTagBytes, offset); // create and append tag: ciphertext | tag

            return Convert.ToBase64String(ciphertextTagBytes);
        }

        private static string DecryptWithGCM(string ciphertextTag, string keyString, string nonceString)
        {
            var tagLength = 16;
            var key = Convert.FromBase64String(keyString);
            var nonce = Convert.FromBase64String(nonceString);

            var ciphertextTagBytes = Convert.FromBase64String(ciphertextTag);
            var plaintextBytes = new byte[ciphertextTagBytes.Length - tagLength];

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), tagLength * 8, nonce);
            cipher.Init(false, parameters);

            var offset = cipher.ProcessBytes(ciphertextTagBytes, 0, ciphertextTagBytes.Length, plaintextBytes, 0);
            cipher.DoFinal(plaintextBytes, offset); // authenticate data via tag

            return Encoding.UTF8.GetString(plaintextBytes);
        }

#endregion

        /// <summary>
        /// Decrypt an encrypted string.
        /// </summary>
        /// <param name="secretKey">Secret Key</param>
        /// <param name="cipherText">Encrypted Text</param>
        /// <returns></returns>
        public string Decrypt(string secretKey, string cipherText)
        {
            var key = DeriveSymetricKey(secretKey);
            var (ciphertext, nonce) = GetCipherParams(cipherText);
            return DecryptWithGCM(ciphertext, key, nonce);
        }

        /// <summary>
        /// Encrypts data with the AES cipher.
        /// </summary>
        /// <param name="secretKey">Encryption Key</param>
        /// <param name="data">Data to be encrypted</param>
        /// <returns></returns>
        public string Encrypt(string secretKey, string data)
        {
            var cipher = string.Empty;

            var key = DeriveSymetricKey(secretKey);
            var nonce = GetNonce();
            var ciphertext = EncryptWithGCM(data, key, nonce);

            cipher = ciphertext;
            cipher += "|";
            cipher += nonce; 

            return cipher;
        }
        
        public string Decrypt(string value)
        {
            return Decrypt(_key, value);
        }

        public string Encrypt(string value)
        {
            return Encrypt(_key, value);
        }

        public static string GenerateNewKey()
        {
            var key = new byte[32];

#if NETSTANDARD2_1
            RandomNumberGenerator.Fill(key);
#else
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(key);
#endif

            return Convert.ToBase64String(key);
        }
    }
