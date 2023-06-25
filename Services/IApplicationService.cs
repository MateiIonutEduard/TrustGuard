using TrustGuard.Data;
using TrustGuard.Environment;
using TrustGuard.Models;

namespace TrustGuard.Services
{
    public interface IApplicationService
    {
        Task<Application?> GetApplicationAsync(int id);
        Task<ApplicationResultModel> GetApplicationsAsync(int? page);
        Task<bool> CreateProductAsync(ApplicationModel appModel);
        Task UpdateDatabase(ECParams[]? args);
    }
}
