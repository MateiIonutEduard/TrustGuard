using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TrustGuard.Models;

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
    }
}
