namespace TrustGuard.Services
{
    public interface IBrowserSupportService
    {
        bool IsBrowserSupported(string userAgent);
    }
}
