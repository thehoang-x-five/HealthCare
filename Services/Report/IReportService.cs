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
        /// <summary>
        /// Lấy KPI hôm nay. Nếu <paramref name="maKhoa"/> != null, chỉ trả data thuộc khoa đó.
        /// Admin / YTHC truyền null → xem toàn bộ. Clinical/CLS truyền MaKhoa của mình.
        /// </summary>
        Task<DashboardTodayDto> LayDashboardHomNayAsync(string? maKhoa = null);
    }

    public interface IReportService
    {
        // ===== BÁO CÁO TỔNG QUAN =====
        Task<ReportOverviewDto> LayBaoCaoTongQuanAsync(ReportFilterRequest filter, bool includeRevenue = true);
    }
}
