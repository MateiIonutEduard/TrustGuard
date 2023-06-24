using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Security.Principal;

namespace TrustGuard.Data
{
    public class TrustGuardContext : DbContext
    {
        public TrustGuardContext(DbContextOptions<TrustGuardContext> options) : base(options)
        { }

        public DbSet<Account> Account { get; set; }
        public DbSet<Domain> Domain { get; set; }
        public DbSet<BasePoint> BasePoint { get; set; }
        public DbSet<Application> Application { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>().ToTable(nameof(Account));
            modelBuilder.Entity<Domain>().ToTable(nameof(Domain));
            modelBuilder.Entity<BasePoint>().ToTable(nameof(BasePoint));
            modelBuilder.Entity<Application>().ToTable(nameof(Application));
        }
    }
}
