using TrustGuard.Environment;

namespace TrustGuard.Services
{
    public interface IApplicationService
    {
        Task UpdateDatabase(ECParams[]? args);
    }
}
