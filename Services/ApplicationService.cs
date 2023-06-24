using Microsoft.EntityFrameworkCore;
using Eduard;
using TrustGuard.Data;
using TrustGuard.Environment;

namespace TrustGuard.Services
{
    public class ApplicationService : IApplicationService
    {
        readonly TrustGuardContext guardContext;

        public ApplicationService(TrustGuardContext guardContext)
        { this.guardContext = guardContext; }

        public async Task UpdateDatabase(ECParams[]? args)
        {
            if(args != null && args.Length > 0)
            {
                List<Domain> domainList = new List<Domain>();

                foreach(ECParams arg in args)
                {
                    Domain? domain = await guardContext.Domain.FirstOrDefaultAsync(e => e.webcode.CompareTo(arg.Guid) == 0);

                    if(domain == null)
                    {
                        domain = new Domain
                        {
                            a = arg.a,
                            b = arg.b,
                            p = arg.p,
                            N = arg.N,
                            x = arg.x,
                            y = arg.y,
                            webcode = arg.Guid
                        };

                        domainList.Add(domain);
                    }
                }

                /* save only if ec domain parameters does not found */
                if(domainList.Count > 0)
                {
                    await guardContext.Domain.AddRangeAsync(domainList);
                    await guardContext.SaveChangesAsync();
                }
            }
        }
    }
}
