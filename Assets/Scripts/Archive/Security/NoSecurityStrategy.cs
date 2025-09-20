namespace Archive.Security
{
    public class NoSecurityStrategy : ISecurityStrategy
    {
        public string Encrypt(string clearText)
        {
            return clearText;
        }

        public string Decrypt(string cipherText)
        {
            return cipherText;
        }
    }
}