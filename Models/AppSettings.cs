#pragma warning disable

namespace TrustGuard.Models
{
    public class AppSettings : IAppSettings
    {
        public string key { get; set; }
        public string salt { get; set; }
        public bool? EnableBrowserSupport { get; set; }
    }
}
