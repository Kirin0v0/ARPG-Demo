namespace Archive.Security
{
    public interface ISecurityStrategy
    {
        public string Encrypt(string clearText);
        public string Decrypt(string cipherText);
    }
}