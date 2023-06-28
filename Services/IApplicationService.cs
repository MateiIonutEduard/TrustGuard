using TrustGuard.Data;
using TrustGuard.Environment;
using TrustGuard.Models;

namespace TrustGuard.Services
{
    public interface IApplicationService
    {
        Task<Application?> GetApplicationAsync(int id);
        Task<bool?> RemoveApplicationAsync(bool complete, int userId, int appId);
        Task<ApplicationResultModel> GetApplicationsAsync(bool complete, string? userId, int? page);
		Task<ApplicationResultModel> GetAppsByFilterAsync(AppQueryFilter filter, string? userId, int? page);
        Task<bool> CreateApplicationAsync(ApplicationModel appModel);
        Task UpdateDatabase(ECParams[]? args);
    }
}
