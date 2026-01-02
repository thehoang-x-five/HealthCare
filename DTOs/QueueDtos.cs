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

        // 🔥 Info phòng/khoa/BN
        public string? TenBenhNhan { get; set; }
        public string? TenPhong { get; set; }
        public string? MaKhoa { get; set; }
        public string? TenKhoa { get; set; }
        public string? LoaiPhong { get; set; }

        // 🔥 NEW: Bác sĩ khám (chỉ cho hàng đợi khám LS)
        public string? MaBacSiKham { get; set; }
        public string? TenBacSiKham { get; set; }

        // 🔥 Summary + full phiếu
        public QueueClinicalExamInfoDto? PhieuKhamLs { get; set; }
        public QueueClsExamInfoDto? PhieuKhamCls { get; set; }
        public ClinicalExamDto? PhieuKhamLsFull { get; set; }
        public ClsOrderDto? PhieuKhamClsFull { get; set; }
        public ClsItemDto? PhieuKhamClsItem { get; set; }
    }


    /// <summary>
    /// Thông tin tóm tắt phiếu khám lâm sàng gắn với một hàng đợi.
    /// Dùng cho queue LoaiHangDoi = "kham_ls".
    /// </summary>
    public record class QueueClinicalExamInfoDto
    {
        public string MaPhieuKham { get; set; } = default!;
        public string MaBenhNhan { get; set; } = default!;
        public string? TenBenhNhan { get; set; }
        public string? TenDichVuKham { get; set; }
        public string HinhThucTiepNhan { get; set; } = default!; // walkin, appointment, service_return
        public string TrangThai { get; set; } = default!;        // da_lap, dang_kham, da_hoan_tat, ...
        public DateTime NgayLap { get; set; }
        public TimeSpan GioLap { get; set; }
    }

    /// <summary>
    /// Thông tin tóm tắt phiếu khám CLS gắn với một hàng đợi.
    /// gắn qua ChiTietDichVu -> PhieuKhamCanLamSang.
    /// </summary>
    public record class QueueClsExamInfoDto
    {
        public string MaPhieuKhamCls { get; set; } = default!;
        public string MaPhieuKhamLs { get; set; } = default!;
        public DateTime NgayGioLap { get; set; }
        public bool AutoPublishEnabled { get; set; }
        public string TrangThai { get; set; } = default!;        // da_lap, dang_thuc_hien, da_hoan_tat...
        public string? TenDichVuCls { get; set; }                // tên dịch vụ CLS của chi tiết DV
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
        public string? Nguon { get; set; }  // appointment, walkin, service_return...
        public string? Keyword { get; set; }  // Tìm kiếm trong tên BN, mã BN, tên BS, tên khoa, SĐT
        public DateTime? FromTime { get; set; }
        public DateTime? ToTime { get; set; }

        /// <summary>
        /// Trường sort: "DoUuTien", "ThoiGianCheckin", "ThoiGianLichHen"...
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// "asc" hoặc "desc", mặc định "asc".
        /// </summary>
        public string? SortDirection { get; set; } = "asc";

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50; // ✅ Chuẩn hóa: 50 items mặc định
    }
}
