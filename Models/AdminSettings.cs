#pragma warning disable
namespace TrustGuard.Models
{
    public class AdminSettings : IAdminSettings
    {
        public string host { get; set; }
        public int? port { get; set; }
        public string client { get; set; }
        public string secret { get; set; }
    }
}
