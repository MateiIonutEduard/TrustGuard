#pragma warning disable

namespace TrustGuard.Models
{
    public class AppSettings : IAppSettings
    {
        public string key { get; set; }
        public string salt { get; set; }
        public bool? EnableBrowserSupport { get; set; }
        public bool? EnableDemandSending { get; set; }
        public bool? EnableSigninTrials { get; set; }
        public int? AccountSigninTrials { get; set; }
    }
}
