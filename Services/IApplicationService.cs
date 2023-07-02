using TrustGuard.Data;
using TrustGuard.Environment;
using TrustGuard.Models;

namespace TrustGuard.Services
{
    public interface IApplicationService
    {
        Task<Application?> GetApplicationAsync(int id);
        Task<int> ValidateLifetime(string access_token);
        Task<bool?> RestoreApplicationAsync(int userId, int appId);
        Task<ApplicationDetailsModel> GetApplicationDetailsAsync(int id);
        Task<AccountBodyModel?> GetAccountByAppAsync(string accessToken);
        Task<bool?> RemoveApplicationAsync(bool complete, int userId, int appId);
		Task<Application?> GetApplicationByIdAsync(string? clientId, string? clientSecret);
		Task<TokenViewModel?> AuthenticateAsync(string? userId, string? clientId, string? clientSecret, bool validateLifetime = false);
        Task<ApplicationResultModel> GetApplicationsAsync(bool complete, string? userId, int? page);
        Task<TokenViewModel?> RefreshTokenAsync(string refreshToken, string accessToken, string? clientId, string? clientSecret, bool validateLifetime = false);
        Task<int> RevokeTokenAsync(string refreshToken, string accessToken, string? clientId, string? clientSecret, bool validateLifetime = false);
        Task<ApplicationResultModel> GetAppsByFilterAsync(bool complete, AppQueryFilter filter, string? userId, int? page);
        Task<bool> CreateApplicationAsync(ApplicationModel appModel);
        Task UpdateDatabase(ECParams[]? args);
    }
}
