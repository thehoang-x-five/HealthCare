using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HealthCare.Infrastructure.Repositories
{
    /// <summary>
    /// MongoDB repository for audit logging with TTL index (365 days retention).
    /// </summary>
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly IMongoDbContext _mongoContext;
        private readonly ILogger<AuditLogRepository> _logger;
        private const string CollectionName = "audit_logs";
        private bool _indexCreated = false;

        public AuditLogRepository(IMongoDbContext mongoContext, ILogger<AuditLogRepository> logger)
        {
            _mongoContext = mongoContext;
            _logger = logger;
        }

        public async Task<bool> LogAsync(
            string userId,
            string action,
            string resource,
            string resourceId,
            BsonDocument? changes = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            if (!_mongoContext.IsConnected)
            {
                _logger.LogWarning("MongoDB unavailable - audit log not recorded");
                return false;
            }

            try
            {
                var collection = _mongoContext.GetCollection<BsonDocument>(CollectionName);

                // Ensure TTL index exists (365 days retention)
                await EnsureTTLIndexAsync(collection);

                var document = new BsonDocument
                {
                    { "user_id", userId },
                    { "action", action },
                    { "resource", resource },
                    { "resource_id", resourceId },
                    { "changes", (BsonValue)(changes ?? (BsonValue)BsonNull.Value) },
                    { "ip_address", (BsonValue)(ipAddress != null ? (BsonValue)ipAddress : BsonNull.Value) },
                    { "user_agent", (BsonValue)(userAgent != null ? (BsonValue)userAgent : BsonNull.Value) },
                    { "timestamp", DateTime.UtcNow },
                    { "created_at", DateTime.UtcNow } // For TTL index
                };

                await collection.InsertOneAsync(document);

                _logger.LogInformation(
                    "Audit log recorded: User={UserId}, Action={Action}, Resource={Resource}, ResourceId={ResourceId}",
                    userId, action, resource, resourceId);

                return true;
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Failed to record audit log");
                return false;
            }
        }

        public async Task<List<BsonDocument>> GetLogsAsync(
            string? userId = null,
            string? resource = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int limit = 100)
        {
            if (!_mongoContext.IsConnected)
            {
                _logger.LogWarning("MongoDB unavailable - cannot retrieve audit logs");
                return new List<BsonDocument>();
            }

            try
            {
                var collection = _mongoContext.GetCollection<BsonDocument>(CollectionName);

                var filterBuilder = Builders<BsonDocument>.Filter;
                var filter = filterBuilder.Empty;

                if (!string.IsNullOrWhiteSpace(userId))
                    filter &= filterBuilder.Eq("user_id", userId);

                if (!string.IsNullOrWhiteSpace(resource))
                    filter &= filterBuilder.Eq("resource", resource);

                if (fromDate.HasValue)
                    filter &= filterBuilder.Gte("timestamp", fromDate.Value);

                if (toDate.HasValue)
                    filter &= filterBuilder.Lte("timestamp", toDate.Value);

                var results = await collection
                    .Find(filter)
                    .Sort(Builders<BsonDocument>.Sort.Descending("timestamp"))
                    .Limit(limit)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} audit logs", results.Count);

                return results;
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Failed to retrieve audit logs");
                return new List<BsonDocument>();
            }
        }

        /// <summary>
        /// Ensure TTL index exists for automatic deletion after 365 days.
        /// </summary>
        private async Task EnsureTTLIndexAsync(IMongoCollection<BsonDocument> collection)
        {
            if (_indexCreated) return;

            try
            {
                var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("created_at");
                var indexOptions = new CreateIndexOptions
                {
                    ExpireAfter = TimeSpan.FromDays(365),
                    Name = "ttl_365_days"
                };

                var indexModel = new CreateIndexModel<BsonDocument>(indexKeys, indexOptions);
                await collection.Indexes.CreateOneAsync(indexModel);

                _indexCreated = true;
                _logger.LogInformation("TTL index created for audit_logs collection (365 days retention)");
            }
            catch (MongoException ex)
            {
                _logger.LogWarning(ex, "Failed to create TTL index (may already exist)");
                _indexCreated = true; // Don't retry
            }
        }
    }
}
