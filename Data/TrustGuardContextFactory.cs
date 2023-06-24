using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TrustGuard.Data
{
    public class TrustGuardContextFactory : IDesignTimeDbContextFactory<TrustGuardContext>
    {
        /* Required fix at Entity Framework to successful create database migrations. */
        public TrustGuardContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();

            var builderContext = new DbContextOptionsBuilder<TrustGuardContext>();
            var connectionString = configurationRoot.GetConnectionString("TrustGuard");

            builderContext.UseNpgsql(connectionString);
            return new TrustGuardContext(builderContext.Options);
        }
    }
}
