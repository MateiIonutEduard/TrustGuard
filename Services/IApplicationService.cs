﻿using TrustGuard.Data;
using TrustGuard.Environment;
using TrustGuard.Models;

namespace TrustGuard.Services
{
    public interface IApplicationService
    {
        Task<Application?> GetApplicationAsync(int id);
        Task<bool?> RestoreApplicationAsync(int userId, int appId);
        Task<bool?> RemoveApplicationAsync(bool complete, int userId, int appId);
		Task<Application?> GetApplicationByIdAsync(string? clientId, string? clientSecret);
		Task<TokenViewModel?> AuthenticateAsync(string? userId, string? clientId, string? clientSecret);
        Task<ApplicationResultModel> GetApplicationsAsync(bool complete, string? userId, int? page);
        Task<TokenViewModel?> RefreshTokenAsync(string refreshToken, string accessToken, string? clientId, string? clientSecret);
        Task<int> RevokeTokenAsync(string refreshToken, string accessToken, string? clientId, string? clientSecret);
        Task<ApplicationResultModel> GetAppsByFilterAsync(bool complete, AppQueryFilter filter, string? userId, int? page);
        Task<bool> CreateApplicationAsync(ApplicationModel appModel);
        Task UpdateDatabase(ECParams[]? args);
    }
}
