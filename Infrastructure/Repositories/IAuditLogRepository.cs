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
        /// Log an audit event.
        /// </summary>
        /// <param name="userId">User who performed the action</param>
        /// <param name="action">Action type (CREATE, UPDATE, DELETE)</param>
        /// <param name="resource">Resource type (e.g., "BenhNhan", "DonThuoc")</param>
        /// <param name="resourceId">Resource identifier</param>
        /// <param name="changes">Changes made (before/after values)</param>
        /// <param name="ipAddress">Client IP address</param>
        /// <param name="userAgent">Client user agent</param>
        /// <returns>True if logged successfully</returns>
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
        /// <param name="userId">Filter by user</param>
        /// <param name="resource">Filter by resource type</param>
        /// <param name="fromDate">Filter by start date</param>
        /// <param name="toDate">Filter by end date</param>
        /// <param name="limit">Maximum number of records</param>
        /// <returns>List of audit logs</returns>
        Task<List<BsonDocument>> GetLogsAsync(
            string? userId = null,
            string? resource = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int limit = 100);
    }
}
