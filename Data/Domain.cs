#pragma warning disable
using System.ComponentModel.DataAnnotations.Schema;

namespace TrustGuard.Data
{
    public class Domain
    {
        public int Id { get; set; }
        public string webcode { get; set; }
        public string a { get; set; }
        public string b { get; set; }
        public string p { get; set; }
        public string N { get; set; }
        public string x { get; set; }
        public string y { get; set; }
        public int? count { get; set; }
        [ForeignKey("DomainId")]
        public virtual ICollection<Application> Applications { get; set; }
    }
}
