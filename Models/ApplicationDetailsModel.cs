#pragma warning disable

namespace TrustGuard.Models
{
    public class ApplicationDetailsModel
    {
        public int Id { get; set; }
        public string AppName { get; set; }
        public string Description { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public int AccountId { get; set; }
        public int ConnectedUsers { get; set; }
        public DateTime ModifiedAt { get; set; }
        public bool IsDeleted { get; set; }
        public int AppType { get; set; }
    }
}
