using System;
using System.Collections.Generic;
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
        /// <param name="fromDate">Start date for analysis</param>
        /// <param name="toDate">End date for analysis</param>
        /// <returns>Abnormal test statistics</returns>
        Task<AbnormalStatsDto> GetAbnormalStatsAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Get disease trends based on ICD-10 codes.
        /// </summary>
        /// <param name="fromDate">Start date for analysis</param>
        /// <param name="toDate">End date for analysis</param>
        /// <param name="topN">Number of top diseases to return (default 10)</param>
        /// <returns>Disease trend statistics</returns>
        Task<DiseaseTrendsDto> GetDiseaseTrendsAsync(DateTime? fromDate = null, DateTime? toDate = null, int topN = 10);

        /// <summary>
        /// Get top consumed medications.
        /// </summary>
        /// <param name="fromDate">Start date for analysis</param>
        /// <param name="toDate">End date for analysis</param>
        /// <param name="topN">Number of top drugs to return (default 10)</param>
        /// <returns>Popular drugs statistics</returns>
        Task<PopularDrugsDto> GetPopularDrugsAsync(DateTime? fromDate = null, DateTime? toDate = null, int topN = 10);
    }
}
