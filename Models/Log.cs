using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TrustGuard.Models
{
    public class Log
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public int LogLevel { get; set; }
    }
}
