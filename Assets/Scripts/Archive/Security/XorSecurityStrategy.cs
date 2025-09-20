using System;
using System.Text;
using Framework.Common.Debug;

namespace Archive.Security
{
    public class XorSecurityStrategy : ISecurityStrategy
    {
        private readonly string _salt;

        public XorSecurityStrategy(string salt)
        {
            _salt = salt;
        }

        public string Encrypt(string clearText)
        {
            return GetXorString(clearText, _salt);
        }

        public string Decrypt(string cipherText)
        {
            return GetXorString(cipherText, _salt);
        }

        public string GetXorString(string str1, string str2)
        {
            var repeatCount = MathF.Max(str1.Length, str2.Length);
            StringBuilder stringBuilder = new();
            for (var i = 0; i < repeatCount; i++)
            {
                stringBuilder.Append((char)(str1[i % str1.Length] ^ str2[i % str2.Length]));
            }

            return stringBuilder.ToString();
        }
    }
}