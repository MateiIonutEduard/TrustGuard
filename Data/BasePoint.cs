#pragma warning disable

namespace TrustGuard.Data
{
    public class BasePoint
    {
        public int Id { get; set; }
        public string x { get; set; }
        public string y { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool? IsDeleted { get; set; }
        public int ApplicationId { get; set; }
        public Application Application { get; set; }
        public int DomainId { get; set; }
        public bool? IsSuspicious { get; set; }
    }
}
