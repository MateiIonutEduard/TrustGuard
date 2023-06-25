#pragma warning disable
using System.ComponentModel.DataAnnotations.Schema;

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
        public Domain Domain { get; set; }
        [ForeignKey("ApplicationId")]
        public virtual ICollection<BasePoint> BasePoints { get; set; }
        public string Description { get; set; }
        public string? AppLogo { get; set; }
        public int AccountId { get; set; }
        public Account Account { get; set; }
        public int AppType { get; set; }
    }
}
