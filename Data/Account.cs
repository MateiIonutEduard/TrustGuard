#pragma warning disable
using System.ComponentModel.DataAnnotations.Schema;

namespace TrustGuard.Data
{
    public class Account
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Address { get; set; }
        public string? Avatar { get; set; }
        public string? SecurityCode { get; set; }
        [ForeignKey("AccountId")]
        public virtual ICollection<Application> Applications { get; set; }
    }
}
