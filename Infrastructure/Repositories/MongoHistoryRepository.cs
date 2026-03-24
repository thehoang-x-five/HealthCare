using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthCare.Infrastructure.Repositories
{
    /// <summary>
    /// MongoDB repository implementation for medical event history.
    /// Uses flat document pattern with graceful degradation when MongoDB unavailable.
    /// </summary>
    public class MongoHistoryRepository : IMongoHistoryRepository
    {
        private readonly IMongoDbContext _mongoContext;
        private readonly ILogger<MongoHistoryRepository> _logger;
        private const string CollectionName = "medical_history";

        public MongoHistoryRepository(IMongoDbContext mongoContext, ILogger<MongoHistoryRepository> logger)
        {
            _mongoContext = mongoContext;
            _logger = logger;
        }

        public async Task<bool> LogEventAsync(string maBenhNhan, string eventType, BsonDocument payload, string? maNhanSu = null)
        {
            if (!_mongoContext.IsConnected)
            {
                _logger.LogWarning(
                    "MongoDB unavailable - medical event not logged: Patient={MaBenhNhan}, EventType={EventType}",
                    maBenhNhan, eventType);
                return false;
            }

            try
            {
                var collection = _mongoContext.GetCollection<BsonDocument>(CollectionName);

                var document = new BsonDocument
                {
                    { "ma_benh_nhan", maBenhNhan },
                    { "event_type", eventType },
                    { "timestamp", DateTime.UtcNow },
                    { "ma_nhan_su", maNhanSu ?? BsonNull.Value },
                    { "payload", payload }
                };

                await collection.InsertOneAsync(document);

                _logger.LogInformation(
                    "Medical event logged: Patient={MaBenhNhan}, EventType={EventType}",
                    maBenhNhan, eventType);

                return true;
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex,
                    "Failed to log medical event: Patient={MaBenhNhan}, EventType={EventType}",
                    maBenhNhan, eventType);
                return false;
            }
        }

        public async Task<List<BsonDocument>> GetPatientHistoryAsync(
            string maBenhNhan,
            string? eventType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int limit = 100)
        {
            if (!_mongoContext.IsConnected)
            {
                _logger.LogWarning(
                    "MongoDB unavailable - cannot retrieve history for Patient={MaBenhNhan}",
                    maBenhNhan);
                return new List<BsonDocument>();
            }

            try
            {
                var collection = _mongoContext.GetCollection<BsonDocument>(CollectionName);

                // Build filter
                var filterBuilder = Builders<BsonDocument>.Filter;
                var filter = filterBuilder.Eq("ma_benh_nhan", maBenhNhan);

                if (!string.IsNullOrWhiteSpace(eventType))
                {
                    filter &= filterBuilder.Eq("event_type", eventType);
                }

                if (fromDate.HasValue)
                {
                    filter &= filterBuilder.Gte("timestamp", fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    filter &= filterBuilder.Lte("timestamp", toDate.Value);
                }

                // Query with sorting and limit
                var results = await collection
                    .Find(filter)
                    .Sort(Builders<BsonDocument>.Sort.Descending("timestamp"))
                    .Limit(limit)
                    .ToListAsync();

                _logger.LogInformation(
                    "Retrieved {Count} medical events for Patient={MaBenhNhan}",
                    results.Count, maBenhNhan);

                return results;
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex,
                    "Failed to retrieve medical history for Patient={MaBenhNhan}",
                    maBenhNhan);
                return new List<BsonDocument>();
            }
        }
    }
}
