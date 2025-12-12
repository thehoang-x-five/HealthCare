using System.Collections.Generic;

namespace HealthCare.DTOs
{
    // ====================================
    // 2. DTO dùng cho trang "Báo cáo"
    // ====================================

    /// <summary>
    /// Filter dùng chung cho các báo cáo / biểu đồ (trang "Báo cáo").
    /// </summary>
    public record class ReportFilterRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? GroupBy { get; set; } = "day"; // day, week, month
    }

    /// <summary>
    /// Điểm dữ liệu (Ngày + Giá trị) cho 4 ô KPI trên trang Báo cáo.
    /// - Doanh thu: Giá trị = số tiền
    /// - BN mới: Giá trị = số BN
    /// - Tái khám: Giá trị = số lượt
    /// - Tỷ lệ hủy: Giá trị = %
    /// </summary>
    public record class ReportDateValueItemDto
    {
        /// <summary>Ngày của điểm dữ liệu (đầu kỳ nếu groupBy != day).</summary>
        public DateTime Ngay { get; set; }

        /// <summary>Giá trị tại ngày đó (đếm hoặc tiền hoặc %).</summary>
        public decimal GiaTri { get; set; }
    }

    /// <summary>Ô KPI 1: Doanh thu.</summary>
    public record class ReportRevenueKpiDto
    {
        public decimal TongDoanhThu { get; set; }
        public decimal DoanhThuChangePercent { get; set; }

        /// <summary>Series: Ngày – Doanh thu (dùng cho mini-chart như screenshot).</summary>
        public IReadOnlyList<ReportDateValueItemDto> PhanBoTheoNgay { get; set; }
            = Array.Empty<ReportDateValueItemDto>();
    }

    /// <summary>Ô KPI 2: Bệnh nhân mới.</summary>
    public record class ReportNewPatientsKpiDto
    {
        public int TongBenhNhanMoi { get; set; }
        public decimal BenhNhanMoiChangePercent { get; set; }

        /// <summary>Series: Ngày – Số BN mới.</summary>
        public IReadOnlyList<ReportDateValueItemDto> PhanBoTheoNgay { get; set; }
            = Array.Empty<ReportDateValueItemDto>();
    }

    /// <summary>Ô KPI 3: Tái khám.</summary>
    public record class ReportRevisitKpiDto
    {
        public int TongTaiKham { get; set; }
        public decimal TaiKhamChangePercent { get; set; }

        /// <summary>Series: Ngày – Số lượt tái khám.</summary>
        public IReadOnlyList<ReportDateValueItemDto> PhanBoTheoNgay { get; set; }
            = Array.Empty<ReportDateValueItemDto>();
    }

    /// <summary>Ô KPI 4: Tỷ lệ hủy lịch hẹn.</summary>
    public record class ReportCancelRateKpiDto
    {
        /// <summary>Tỷ lệ hủy chung trong kỳ (%).</summary>
        public decimal TyLeHuy { get; set; }

        /// <summary>% thay đổi so với kỳ trước.</summary>
        public decimal TyLeHuyChangePercent { get; set; }

        /// <summary>Series: Ngày – Tỷ lệ hủy (%).</summary>
        public IReadOnlyList<ReportDateValueItemDto> PhanBoTheoNgay { get; set; }
            = Array.Empty<ReportDateValueItemDto>();
    }

    /// <summary>
    /// Một dòng trong "Biểu đồ tổng quan" + "Bảng số liệu".
    /// </summary>
    public record class ReportOverviewItemDto
    {
        public DateTime Ngay { get; set; }       // nếu groupBy != day: là ngày đầu kỳ
        public decimal DoanhThu { get; set; }
        public int BenhNhanMoi { get; set; }
        public int TaiKham { get; set; }
        public decimal TyLeHuy { get; set; }     // %
    }

    /// <summary>
    /// DTO tổng hợp cho tab "Tổng quan" của trang Báo cáo.
    /// FE dùng 4 ô KPI + Items để vẽ chart & table.
    /// </summary>
    public record class ReportOverviewDto
    {
        public ReportRevenueKpiDto DoanhThu { get; set; } = default!;
        public ReportNewPatientsKpiDto BenhNhanMoi { get; set; } = default!;
        public ReportRevisitKpiDto TaiKham { get; set; } = default!;
        public ReportCancelRateKpiDto TyLeHuy { get; set; } = default!;

        public IReadOnlyList<ReportOverviewItemDto> Items { get; set; }
            = Array.Empty<ReportOverviewItemDto>();
    }
}
