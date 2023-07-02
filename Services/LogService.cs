using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TrustGuard.Models;
using LogLevel = TrustGuard.Models.LogLevel;

namespace TrustGuard.Services
{
    public class LogService
    {
        readonly IMongoCollection<Log> logs;

        public LogService(IOptions<TrustGuardSettings> bridgeWaterSettings)
        {
            var mongoClient = new MongoClient(bridgeWaterSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(bridgeWaterSettings.Value.DatabaseName);
            logs = mongoDatabase.GetCollection<Log>(bridgeWaterSettings.Value.CollectionName);
        }

        public async Task CreateLogAsync(string AppId, string body, LogLevel logLevel, string? webcode = null)
        {
            Log log = new Log
            {
                AppId = AppId,
                Message = body,
                CreatedAt = DateTime.UtcNow,
                LogLevel = (int)logLevel
            };

            /* warn user account if something is suspicious */
            if (!string.IsNullOrEmpty(webcode) && logLevel == LogLevel.Warning)
                log.Webcode = webcode;

            /* save log at MongoDB database */
            await logs.InsertOneAsync(log);
        }

        /* remove log by specific id */
        public async Task RemoveLogAsync(string id) => await logs.DeleteOneAsync(x => x.Id == id);
    }
}
