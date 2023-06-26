﻿using TrustGuard.Data;
using TrustGuard.Environment;
using TrustGuard.Models;

namespace TrustGuard.Services
{
    public interface IApplicationService
    {
        Task<Application?> GetApplicationAsync(int id);
        Task<bool?> RemoveApplicationAsync(int userId, int appId);
        Task<ApplicationResultModel> GetApplicationsAsync(string? userId, int? page);
        Task<bool> CreateProductAsync(ApplicationModel appModel);
        Task UpdateDatabase(ECParams[]? args);
    }
}
