using System;
using System.Threading.Tasks;
using HealthCare.DTOs;

namespace HealthCare.Services.Report
{
    /// <summary>
    /// Report Service Module - Quản lý báo cáo, thống kê, dashboard
    /// Aggregate data from other services via HTTP APIs
    /// </summary>
    
    public interface IDashboardService
    {
        // ===== DASHBOARD HÔM NAY =====
        Task<DashboardTodayDto> LayDashboardHomNayAsync();
    }

    public interface IReportService
    {
        // ===== BÁO CÁO TỔNG QUAN =====
        Task<ReportOverviewDto> LayBaoCaoTongQuanAsync(ReportFilterRequest filter);
    }
}
