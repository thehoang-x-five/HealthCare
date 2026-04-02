using System;
using System.Threading.Tasks;
using HealthCare.DTOs;

namespace HealthCare.Services.Report
{
    public interface IAnalyticsService
    {
        Task<AbnormalStatsDto> GetAbnormalStatsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<DiseaseTrendsDto> GetDiseaseTrendsAsync(DateTime? fromDate = null, DateTime? toDate = null, int topN = 10);
        Task<PopularDrugsDto> GetPopularDrugsAsync(DateTime? fromDate = null, DateTime? toDate = null, int topN = 10);
    }
}
