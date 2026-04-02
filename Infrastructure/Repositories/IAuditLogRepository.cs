using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace HealthCare.Infrastructure.Repositories
{
    /// <summary>
    /// Repository interface for audit logging.
    /// Logs all mutation operations (POST/PUT/DELETE) for compliance and security.
    /// </summary>
    public interface IAuditLogRepository
    {
        /// <summary>
        /// Log an audit event to MongoDB audit_logs collection.
        /// </summary>
        Task<bool> LogAsync(
            string userId,
            string action,
            string resource,
            string resourceId,
            BsonDocument? changes = null,
            string? ipAddress = null,
            string? userAgent = null);

        /// <summary>
        /// Get audit logs with filtering.
        /// </summary>
        Task<List<BsonDocument>> GetLogsAsync(
            string? userId = null,
            string? resource = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int limit = 100);
    }
}
