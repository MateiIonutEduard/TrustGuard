using TrustGuard.Data;
using TrustGuard.Environment;
using TrustGuard.Models;

namespace TrustGuard.Services
{
    public interface IApplicationService
    {
        Task<Application?> GetApplicationAsync(int id);
        Task<bool?> RestoreApplicationAsync(int userId, int appId);
        Task<bool?> RemoveApplicationAsync(bool complete, int userId, int appId);
        Task<TokenViewModel> AuthenticateAsync(string? userId, string? clientId, string? clientSecret);
        Task<ApplicationResultModel> GetApplicationsAsync(bool complete, string? userId, int? page);
		Task<ApplicationResultModel> GetAppsByFilterAsync(bool complete, AppQueryFilter filter, string? userId, int? page);
        Task<bool> CreateApplicationAsync(ApplicationModel appModel);
        Task UpdateDatabase(ECParams[]? args);
    }
}
