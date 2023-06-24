using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Security.Principal;

namespace TrustGuard.Data
{
    public class TrustGuardContext : DbContext
    {
        public TrustGuardContext(DbContextOptions<TrustGuardContext> options) : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
        }
    }
}
