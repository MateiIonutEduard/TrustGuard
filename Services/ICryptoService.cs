namespace TrustGuard.Services
{
    public interface ICryptoService
    {
        string EncryptPassword(string data);
        string ComputeHash(byte[] buffer);
        string DecryptPassword(string data);
    }
}
