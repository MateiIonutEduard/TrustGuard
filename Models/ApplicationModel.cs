#pragma warning disable

namespace TrustGuard.Models
{
    public class ApplicationModel
    {
        public int? AccountId { get; set; }
        public string appName { get; set; }
        public string description { get; set; }
        public IFormFile? appLogo { get; set; }
        public int appType { get; set; }
    }
}
