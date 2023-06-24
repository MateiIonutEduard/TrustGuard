#pragma warning disable

namespace TrustGuard.Data
{
    public class Application
    {
        public int Id { get; set; }
        public string AppName { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public bool? IsDeleted { get; set; }
        public int DomainId { get; set; }
        public string Description { get; set; }
        public string? AppLogo { get; set; }
    }
}
