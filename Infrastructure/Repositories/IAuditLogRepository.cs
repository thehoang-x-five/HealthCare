using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace HealthCare.Infrastructure.Repositories
{
    public interface IAuditLogRepository
    {
        Task<bool> LogAsync(
            string userId,
            string action,
            string resource,
            string resourceId,
            BsonDocument? changes = null,
            string? ipAddress = null,
            string? userAgent = null);

        Task<List<BsonDocument>> GetLogsAsync(
            string? userId = null,
            string? resource = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int limit = 100);
    }
}
