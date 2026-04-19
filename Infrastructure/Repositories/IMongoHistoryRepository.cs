using MongoDB.Bson;
using System.Threading.Tasks;

namespace HealthCare.Infrastructure.Repositories
{
    /// <summary>
    /// Repository interface for logging medical events to MongoDB.
    /// Implements flat document pattern - one event per document for optimal query performance.
    /// </summary>
    public interface IMongoHistoryRepository
    {
        /// <summary>
        /// Log a medical event to MongoDB history collection.
        /// </summary>
        /// <param name="maBenhNhan">Patient identifier</param>
        /// <param name="eventType">Event type, e.g. kham_lam_sang, xet_nghiem, chan_doan_hinh_anh, cls_order_created, cls_service_completed, tong_hop_cls, don_thuoc, thanh_toan</param>
        /// <param name="payload">Event-specific data as BsonDocument</param>
        /// <param name="maNhanSu">Healthcare staff identifier who performed the action</param>
        /// <returns>True if logged successfully, false if MongoDB unavailable</returns>
        Task<bool> LogEventAsync(string maBenhNhan, string eventType, BsonDocument payload, string? maNhanSu = null);

        /// <summary>
        /// Retrieve medical history for a patient with optional filtering.
        /// </summary>
        /// <param name="maBenhNhan">Patient identifier</param>
        /// <param name="eventType">Optional filter by event type</param>
        /// <param name="fromDate">Optional filter by start date</param>
        /// <param name="toDate">Optional filter by end date</param>
        /// <param name="limit">Maximum number of records to return (default 100)</param>
        /// <returns>List of medical events ordered by timestamp descending</returns>
        Task<List<BsonDocument>> GetPatientHistoryAsync(
            string maBenhNhan,
            string? eventType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int limit = 100);
    }
}
