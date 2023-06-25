using TrustGuard.Environment;
using TrustGuard.Models;

namespace TrustGuard.Services
{
    public interface IApplicationService
    {
        Task<bool> CreateProductAsync(ApplicationModel appModel);
        Task UpdateDatabase(ECParams[]? args);
    }
}
