namespace HealthCare.DTOs
{

    /// <summary>
    /// Điểm dữ liệu theo giờ cho 4 box KPI trên Dashboard.
    /// GiaTri:
    /// - Box 1: số bệnh nhân
    /// - Box 2: số lịch hẹn
    /// - Box 3: doanh thu
    /// - Box 4: số lượt khám
    /// </summary>
    public record class TodayHourValueItemDto
    {
        /// <summary>Giờ trong ngày, 0–23.</summary>
        public int Gio { get; set;}

        /// <summary>Giá trị tại giờ đó (đếm hoặc tiền).</summary>
        public decimal GiaTri { get; set;}
    }

    /// <summary>
    /// KPI: Bệnh nhân trong ngày (ô box đầu tiên).
    /// </summary>
    public record class TodayPatientsKpiDto
    {
        public int TongSoBenhNhan { get; set;}
        public int DaXuLy { get; set;}
        public int ChoXuLy { get; set;}
        public int DaHuy { get; set;}
        public decimal TangTruongPhanTram { get; set;}

        /// <summary>Chart: giờ – số bệnh nhân.</summary>
        public IReadOnlyList<TodayHourValueItemDto> PhanBoTheoGio { get; set;}
            = Array.Empty<TodayHourValueItemDto>();
    }

    /// <summary>
    /// KPI: Lịch hẹn hôm nay (ô box thứ hai).
    /// </summary>
    public record class TodayAppointmentsKpiDto
    {
        public int TongSoLichHen { get; set;}
        public int DaXacNhan { get; set;}
        public int ChoXacNhan { get; set;}
        public int DaHuy { get; set;}
        public decimal TangTruongPhanTram { get; set;}

        /// <summary>Chart: giờ – số lịch hẹn.</summary>
        public IReadOnlyList<TodayHourValueItemDto> PhanBoTheoGio { get; set;}
            = Array.Empty<TodayHourValueItemDto>();
    }

    /// <summary>
    /// KPI: Doanh thu hôm nay (ô box thứ ba).
    /// </summary>
    public record class TodayRevenueKpiDto
    {
        public decimal TongDoanhThu { get; set;}
        public decimal DoanhThuKhamLs { get; set;}
        public decimal DoanhThuCls { get; set;}
        public decimal DoanhThuThuoc { get; set;}
        public decimal TangTruongPhanTram { get; set;}

        /// <summary>Chart: giờ – doanh thu.</summary>
        public IReadOnlyList<TodayHourValueItemDto> PhanBoTheoGio { get; set;}
            = Array.Empty<TodayHourValueItemDto>();
    }

    /// <summary>
    /// KPI: Tổng quan lượt khám LS + CLS hôm nay (ô box thứ tư).
    /// </summary>
    public record class TodayExamOverviewDto
    {
        public int TongLuotKham { get; set;} // tổng LS + CLS

        public int ChoKham { get; set;}
        public int DangKham { get; set;}
        public int DaHoanTat { get; set;}
        public int DaHuy { get; set;}

        /// <summary>% tăng/giảm tổng lượt khám so với hôm trước.</summary>
        public decimal TangTruongPhanTram { get; set;}

        /// <summary>Chart: giờ – số lượt khám.</summary>
        public IReadOnlyList<TodayHourValueItemDto> PhanBoTheoGio { get; set;}
            = Array.Empty<TodayHourValueItemDto>();
    }

    /// <summary>
    /// KPI: Dịch vụ CLS hôm nay (thay cho Lịch hẹn ở vai trò CLS/KTV).
    /// </summary>
    public record class TodayServicesKpiDto
    {
        public int TongDichVu { get; set;}
        public int HoanTat { get; set;}
        public int DangLam { get; set;}
        public int DaHuy { get; set;}
        public decimal TangTruongPhanTram { get; set;}
        public IReadOnlyList<TodayHourValueItemDto> PhanBoTheoGio { get; set;}
            = Array.Empty<TodayHourValueItemDto>();
    }

    /// <summary>
    /// Dòng dịch vụ CLS sắp thực hiện (cho Dashboard CLS/KTV).
    /// </summary>
    public record class UpcomingServiceItemDto
    {
        public string MaChiTietDV { get; set;} = default!;
        public string TenDichVu { get; set;} = default!;
        public string TenBenhNhan { get; set;} = default!;
        public string TrangThai { get; set;} = default!;
        public DateTime GioChiDinh { get; set;}
        public bool CoKetQua { get; set;}
    }

    /// <summary>
    /// Dịch vụ được thực hiện nhiều nhất hôm nay (cho Dashboard Admin/HC).
    /// </summary>
    public record class TrendingServiceItemDto
    {
        public string TenDichVu { get; set;} = default!;
        /// <summary>"ls" hoặc "cls"</summary>
        public string LoaiDichVu { get; set;} = default!;
        public int SoLuong { get; set;}
    }

    /// <summary>
    /// Dòng "Lịch hẹn sắp tới (hôm nay)".
    /// </summary>
    public record class UpcomingAppointmentDashboardItemDto
    {
        public DateTime NgayHen { get; set;}
        public TimeSpan GioHen { get; set;}

        public string TenBenhNhan { get; set;} = default!;
        public string? TenDichVuKham { get; set;}
        public string? TenKhoa { get; set;}

        public string TrangThai { get; set;} = default!;
    }

  

    /// <summary>
    /// Hoạt động gần đây (widget "Hoạt động gần đây").
    /// </summary>
    public record class DashboardActivityDto
    {
        public string MoTa { get; set;} = default!;
        public DateTime ThoiGian { get; set;}
    }

    /// <summary>
    /// DTO tổng hợp cho trang "Tổng quan" (Dashboard hôm nay).
    /// </summary>
    public record class DashboardTodayDto
    {
        public DateTime Ngay { get; set;} = DateTime.Today;

        public TodayPatientsKpiDto BenhNhanTrongNgay { get; set;} = default!;
        public TodayAppointmentsKpiDto LichHenHomNay { get; set;} = default!;
        public TodayRevenueKpiDto DoanhThuHomNay { get; set;} = default!;
        public TodayExamOverviewDto LuotKhamHomNay { get; set;} = default!;

        // === Role-specific data ===
        public TodayServicesKpiDto? DichVuHomNay { get; set;}
        public IReadOnlyList<UpcomingServiceItemDto> DichVuSapLam { get; set;}
            = Array.Empty<UpcomingServiceItemDto>();
        public IReadOnlyList<TrendingServiceItemDto> DichVuTangManh { get; set;}
            = Array.Empty<TrendingServiceItemDto>();

        // Lịch hẹn sắp tới (hôm nay)
        public IReadOnlyList<UpcomingAppointmentDashboardItemDto> LichHenSapToi { get; set;}
            = Array.Empty<UpcomingAppointmentDashboardItemDto>();

        public IReadOnlyList<DashboardActivityDto> HoatDongGanDay { get; set;}
            = Array.Empty<DashboardActivityDto>();
    }
}
