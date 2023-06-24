namespace TrustGuard.Models
{
    public interface IAppSettings
    {
        string key { get; set; }
        string salt { get; set; }
        bool? EnableBrowserSupport { get; set; }
    }
}
