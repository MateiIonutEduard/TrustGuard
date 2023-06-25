#pragma warning disable

namespace TrustGuard.Models
{
    public class ApplicationViewModel
    {
        public int Id { get; set; }
        public string AppName { get; set; }
        public string ClientId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public int AppType { get; set; }
    }
}
