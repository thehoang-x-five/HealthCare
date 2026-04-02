using System;
using System.Threading.Tasks;
using HealthCare.DTOs;

namespace HealthCare.Services.Report
{
    /// <summary>
    /// Service interface for MongoDB analytics using Aggregation Framework.
    /// Provides statistical analysis of medical data.
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// Get statistics of abnormal test results frequency.
        /// </summary>
        Task<AbnormalStatsDto> GetAbnormalStatsAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Get disease trends based on ICD-10 codes.
        /// </summary>
        Task<DiseaseTrendsDto> GetDiseaseTrendsAsync(DateTime? fromDate = null, DateTime? toDate = null, int topN = 10);

        /// <summary>
        /// Get top consumed medications.
        /// </summary>
        Task<PopularDrugsDto> GetPopularDrugsAsync(DateTime? fromDate = null, DateTime? toDate = null, int topN = 10);
    }
}
