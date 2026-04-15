namespace HealthCare.DTOs
{
    public record class QueueItemDto
    {
        public string MaHangDoi { get; set; } = default!;
        public string MaBenhNhan { get; set; } = default!;
        public string MaPhong { get; set; } = default!;
        public string LoaiHangDoi { get; set; } = default!;
        public string? Nguon { get; set; }
        public string? Nhan { get; set; }
        public bool CapCuu { get; set; } = false;
        public string? PhanLoaiDen { get; set; }
        public DateTime ThoiGianCheckin { get; set; }
        public DateTime? ThoiGianLichHen { get; set; }
        public int DoUuTien { get; set; }
        public string TrangThai { get; set; } = default!;
        public string? MaPhieuKham { get; set; }
        public string? MaChiTietDv { get; set; }
        public string? MaLuotKham { get; set; }
        public string? TrangThaiLuot { get; set; }

        public string? TenBenhNhan { get; set; }
        public string? TenPhong { get; set; }
        public string? MaKhoa { get; set; }
        public string? TenKhoa { get; set; }
        public string? LoaiPhong { get; set; }

        public string? MaBacSiKham { get; set; }
        public string? TenBacSiKham { get; set; }
        public string? MaNhanSuThucHien { get; set; }
        public string? TenNhanSuThucHien { get; set; }
        public string? MaKyThuatVienThucHien { get; set; }
        public string? TenKyThuatVienThucHien { get; set; }
        public DateTime? ThoiGianBatDauLuot { get; set; }
        public DateTime? ThoiGianKetThucLuot { get; set; }

        public QueueClinicalExamInfoDto? PhieuKhamLs { get; set; }
        public QueueClsExamInfoDto? PhieuKhamCls { get; set; }
        public ClinicalExamDto? PhieuKhamLsFull { get; set; }
        public ClsOrderDto? PhieuKhamClsFull { get; set; }
        public ClsItemDto? PhieuKhamClsItem { get; set; }

        public bool HasPendingCls { get; set; } = false;
    }

    public record class QueueClinicalExamInfoDto
    {
        public string MaPhieuKham { get; set; } = default!;
        public string MaBenhNhan { get; set; } = default!;
        public string? TenBenhNhan { get; set; }
        public string? TenDichVuKham { get; set; }
        public string HinhThucTiepNhan { get; set; } = default!;
        public string TrangThai { get; set; } = default!;
        public DateTime NgayLap { get; set; }
        public TimeSpan GioLap { get; set; }
    }

    public record class QueueClsExamInfoDto
    {
        public string MaPhieuKhamCls { get; set; } = default!;
        public string MaPhieuKhamLs { get; set; } = default!;
        public DateTime NgayGioLap { get; set; }
        public bool AutoPublishEnabled { get; set; }
        public string TrangThai { get; set; } = default!;
        public string? TenDichVuCls { get; set; }
        public string? MaNhanSuThucHien { get; set; }
        public string? TenNhanSuThucHien { get; set; }
        public string? MaKyThuatVienThucHien { get; set; }
        public string? TenKyThuatVienThucHien { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
    }

    public record class QueueEnqueueRequest
    {
        public string MaBenhNhan { get; set; } = default!;
        public string MaPhong { get; set; } = default!;
        public string LoaiHangDoi { get; set; } = default!;
        public string? Nguon { get; set; }
        public string? Nhan { get; set; }
        public bool CapCuu { get; set; } = false;
        public int DoUuTien { get; set; } = 0;
        public DateTime? ThoiGianLichHen { get; set; }
        public string? MaPhieuKham { get; set; }
        public string? MaChiTietDv { get; set; }
        public string? PhanLoaiDen { get; set; }
    }

    public record class QueueStatusUpdateRequest
    {
        public string TrangThai { get; set; } = default!;
    }

    public record class QueueSearchFilter
    {
        public string? MaPhong { get; set; }
        public string? Vaitro { get; set; }
        public string? MaNhanSu { get; set; }
        public string? LoaiHangDoi { get; set; }
        public string? TrangThai { get; set; }
        public string? Nguon { get; set; }
        public string? Keyword { get; set; }
        public DateTime? FromTime { get; set; }
        public DateTime? ToTime { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; } = "asc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
