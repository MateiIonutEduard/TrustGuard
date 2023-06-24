using Newtonsoft.Json;
using TrustGuard.Services;

namespace TrustGuard.Environment
{
    public class Guard
    {
        public static async Task UpdateDomainParameters(IServiceProvider serviceProvider)
        {
            var buffer = File.ReadAllText("ellipticCurves.json");
            ECParams[]? args = JsonConvert.DeserializeObject<ECParams[]?>(buffer);

            /* add elliptic curve parameters if they does not exists */
            var db = serviceProvider.GetRequiredService<IApplicationService>();
            await db.UpdateDatabase(args);
        }
    }
}
