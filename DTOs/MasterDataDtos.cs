using System;
using System.Collections.Generic;

namespace HealthCare.DTOs
{
    // ===================== KHOA =====================

    public record class DepartmentDto
    {
        public string MaKhoa { get; set;} = default!;
        public string TenKhoa { get; set;} = default!;
        public string TrangThai { get; set;} = "hoat_dong";
    }

    public record class DepartmentSearchFilter
    {
        public string? Keyword { get; set;}
        public string? TrangThai { get; set;}

        public string? SortBy { get; set;}          // "TenKhoa", ...
        public string? SortDirection { get; set;} = "asc";

        public int Page { get; set;} = 1;
        public int PageSize { get; set;} = 50;
    }

    // ===================== PHÒNG =====================

    /// <summary>
    /// Raw theo Entity (CRUD nội bộ).
    /// </summary>
    public record class RoomDto
    {
        public string MaPhong { get; set;} = default!;
        public string TenPhong { get; set;} = default!;
        public string MaKhoa { get; set;} = default!;
        public string LoaiPhong { get; set;} = default!; // phong_kham_ls, phong_cls, thu_ngan...
        public int? SucChua { get; set;}
        public string? ViTri { get; set;}
        public string? Email { get; set;}
        public string? DienThoai { get; set;}
        public TimeSpan? GioMoCua { get; set;}
        public TimeSpan? GioDongCua { get; set;}
        public List<string> ThietBi { get; set;} = new();
        public string TrangThai { get; set;} = "hoat_dong"; // hoat_dong, tam_dung

        public string? MaBacSiPhuTrach { get; set;}
    }

    public record class RoomSearchFilter
    {
        public string? Keyword { get; set;}
        public string? MaKhoa { get; set;}
        public string? LoaiPhong { get; set;}
        public string? TrangThai { get; set;}
        public string? MaBacSiPhuTrach { get; set;}
        public string? SortBy { get; set;}              // "TenPhong", ...
        public string? SortDirection { get; set;} = "asc";

        public int Page { get; set;} = 1;
        public int PageSize { get; set;} = 50;
    }

    /// <summary>
    /// Card phòng (grid ngoài cùng).
    /// </summary>
    public record class RoomCardDto
    {
        public string MaPhong { get; set;} = default!;
        public string TenPhong { get; set;} = default!;
        public string? TenKhoa { get; set;}
        public string LoaiPhong { get; set;} = default!;     // phong_kham_ls, phong_cls...
        public string TrangThai { get; set;} = "hoat_dong";  // hoat_dong, tam_dung

        public string? MaBacSiPhuTrach { get; set;}
        public string? TenBacSiPhuTrach { get; set;}

        public string? DienThoai { get; set;}
        public string? Email { get; set;}

        // Năng lực ngày (hôm nay)
        public int DangCho { get; set;}
        public int DaHoanThanh { get; set;}
        public int TongHomNay { get; set;}
    }

    /// <summary>
    /// Popup chi tiết phòng.
    /// </summary>
    public record class RoomDetailDto
    {
        public string MaPhong { get; set;} = default!;
        public string TenPhong { get; set;} = default!;
        public string? TenKhoa { get; set;}
        public string LoaiPhong { get; set;} = default!;
        public string TrangThai { get; set;} = "hoat_dong";

        public string? KhuVuc { get; set;}              // map từ ViTri nếu muốn
        public string? DienThoai { get; set;}
        public string? Email { get; set;}
        public TimeSpan? GioMoCua { get; set;}
        public TimeSpan? GioDongCua { get; set;}

        public string? MaBacSiPhuTrach { get; set;}
        public string? TenBacSiPhuTrach { get; set;}

        public IReadOnlyList<string> ThietBi { get; set;} = Array.Empty<string>();

        // Năng lực ngày
        public int DangCho { get; set;}
        public int DaHoanThanh { get; set;}
        public int TongHomNay { get; set;}
        public int? SucChuaNgay { get; set;}

        // Dịch vụ tại phòng
        public IReadOnlyList<RoomServiceItemDto> DichVuTaiPhong { get; set;} = Array.Empty<RoomServiceItemDto>();
    }

    public record class RoomServiceItemDto
    {
        public string MaDichVu { get; set;} = default!;
        public string TenDichVu { get; set;} = default!;
        public string LoaiDichVu { get; set;} = default!;   // kham_lam_sang, can_lam_sang, khac
        public int ThoiGianPhut { get; set;}
        public decimal DonGia { get; set;}
    }

    /// <summary>
    /// Popup "Lịch trực — Phòng XXX".
    /// </summary>
    public record class RoomDutyWeekDto
    {
        public string MaPhong { get; set;} = default!;
        public string TenPhong { get; set;} = default!;
        public string? TenKhoa { get; set;}

        public string? MaBacSiPhuTrach { get; set;}
        public string? TenBacSiPhuTrach { get; set;}

        public DateTime Today { get; set;}
        public IReadOnlyList<RoomDutyDayDto> LichDieuDuongTuan { get; set;} = Array.Empty<RoomDutyDayDto>();
    }

    public record class RoomDutyDayDto
    {
        public string Thu { get; set;} = default!;          // "Mon", "Tue", ...
        public bool NghiTruc { get; set;}

        public string? MaYTa { get; set;}
        public string? TenYTa { get; set;}

        public string? CaTruc { get; set;}                  // "Sáng", "Chiều", ...
        public TimeSpan? GioBatDau { get; set;}
        public TimeSpan? GioKetThuc { get; set;}
    }

    // ===================== NHÂN SỰ (BS + YT) =====================

    /// <summary>
    /// Raw entity (CRUD).
    /// </summary>
    public record class StaffDto
    {
        public string MaNhanVien { get; set;} = default!;
        public string HoTen { get; set;} = default!;
        public string VaiTro { get; set;} = "bac_si";       // bac_si, y_ta
        public string? MaKhoa { get; set;}
        public string? TenKhoa { get; set;}

        public string? Email { get; set;}
        public string? DienThoai { get; set;}

        // Trạng thái công tác: dang_cong_tac, tam_nghi, nghi_viec...
        public string TrangThaiCongTac { get; set;} = "dang_cong_tac";
        public int SoNamKinhNghiem { get; set;}

        public string? HocVi { get; set;}
        public string? ChuyenKhoa { get; set;}
        public string? AnhDaiDien { get; set;}

        public string? MaPhongPhuTrach { get; set;}
        public string? TenPhongPhuTrach { get; set;}

        // Chỉ dùng cho VaiTro = "y_ta"
        public string? LoaiYTa { get; set;} // Y tá lâm sàng, Y tá hành chính...
    }

    public record class StaffSearchFilter
    {
        public string? Keyword { get; set;}
        public string? MaKhoa { get; set;}
        public string? VaiTro { get; set;}             // bac_si, y_ta
        public string? TrangThaiCongTac { get; set;}   // dang_cong_tac, tam_nghi...
        public string? MaPhongPhuTrach { get; set;}

        public string? SortBy { get; set;}             // "HoTen", ...
        public string? SortDirection { get; set;} = "asc";

        public int Page { get; set;} = 1;
        public int PageSize { get; set;} = 50;
    }

    /// <summary>
    /// Card BS / YT trên list.
    /// </summary>
    public record class StaffCardDto
    {
        public string MaNhanVien { get; set;} = default!;
        public string HoTen { get; set;} = default!;
        public string VaiTro { get; set;} = "bac_si";          // bac_si, y_ta
        public string? HocVi { get; set;}
        public string? ChuyenKhoa { get; set;}

        public string? MaKhoa { get; set;}
        public string? TenKhoa { get; set;}

        public string? AnhDaiDien { get; set;}
        public string? Email { get; set;}
        public string? DienThoai { get; set;}

        // Trạng thái công tác hiện tại
        public string TrangThaiCongTac { get; set;} = "dang_cong_tac";
        public string? LoaiYTa { get; set;}                 // "Y tá lâm sàng", ...

        // Phòng/bàn hôm nay (kèm chip "Phòng hôm nay")
        public string? MaPhongHomNay { get; set;}
        public string? TenPhongHomNay { get; set;}

        // BS: số lịch hẹn hôm nay; YT: số ca trực tuần này
        public int SoLichHenHomNay { get; set;}
        public int SoCaTrucTuanNay { get; set;}
    }

    /// <summary>
    /// Popup chi tiết BS / YT.
    /// </summary>
    public record class StaffDetailDto
    {
        // Header
        public string MaNhanVien { get; set;} = default!;
        public string HoTen { get; set;} = default!;
        public string VaiTro { get; set;} = "bac_si";     // bac_si, y_ta
        public string? TenKhoa { get; set;}

        // Trạng thái công tác: dang_cong_tac, tam_nghi, nghi_viec...
        public string TrangThaiCongTac { get; set;} = "dang_cong_tac";

        // Hành chính
        public string? HocVi { get; set;}
        public string? ChuyenKhoa { get; set;}
        public int SoNamKinhNghiem { get; set;}

        public string? Email { get; set;}
        public string? DienThoai { get; set;}

        // Chỉ dùng cho VaiTro = "y_ta"
        public string? LoaiYTa { get; set;}             // Y tá hành chính, ...

        // Thống kê nhanh
        public int? SoLichHenHomNay { get; set;}              // bác sĩ
        public int? SoCaTrucTuanNay { get; set;}              // y tá

        // Phòng / bàn trực hôm nay
        public string? MaPhongHoacBanHomNay { get; set;}
        public string? TenPhongHoacBanHomNay { get; set;}

        // Kỹ năng (chips)
        public IReadOnlyList<string> KyNang { get; set;} = Array.Empty<string>();
    }

    /// <summary>
    /// Popup "Lịch trực & bàn trực" cho BS / YT.
    /// </summary>
    public record class StaffDutyWeekDto
    {
        public string MaNhanVien { get; set;} = default!;
        public string HoTen { get; set;} = default!;
        public string VaiTro { get; set;} = "bac_si";    // bac_si, y_ta
        public string? TenKhoa { get; set;}

        public DateTime Today { get; set;}

        // Hôm nay (header)
        public string? CaHomNay { get; set;}                    // YT: "Sáng", "Chiều"
        public string? TrangThaiLamViecHomNay { get; set;}      // lam, nghi
        public string? TenPhongHoacBanHomNay { get; set;}

        public IReadOnlyList<StaffDutyDayDto> Items { get; set;} = Array.Empty<StaffDutyDayDto>();
    }

    /// <summary>
    /// 1 dòng trong bảng lịch trực tuần.
    /// Với BS: dùng Thu + TrangThaiLamViec.
    /// Với YT: dùng Thu + CaTruc + TenPhong.
    /// </summary>
    public record class StaffDutyDayDto
    {
        public string Thu { get; set;} = default!;       // "Mon", "Tue", ...

        // YT: thông tin ca + phòng/bàn
        public string? CaTruc { get; set;}
        public TimeSpan? GioBatDau { get; set;}
        public TimeSpan? GioKetThuc { get; set;}
        public string? MaPhong { get; set;}
        public string? TenPhong { get; set;}

        // BS + YT: trạng thái làm việc trong ngày
        public string TrangThaiLamViec { get; set;} = "nghi"; // lam, nghi
    }

    // ===================== LỊCH TRỰC RAW =====================

    public record class DutyScheduleDto
    {
        public string MaLichTruc { get; set;} = default!;
        public DateTime Ngay { get; set;}
        public string CaTruc { get; set;} = default!;
        public TimeSpan GioBatDau { get; set;}
        public TimeSpan GioKetThuc { get; set;}
        public bool NghiTruc { get; set;}

        public string MaYTaTruc { get; set;} = default!;
        public string MaPhong { get; set;} = default!;
    }

    public record class DutyScheduleSearchFilter
    {
        public DateTime? FromDate { get; set;}
        public DateTime? ToDate { get; set;}
        public string? MaPhong { get; set;}
        public string? MaYTaTruc { get; set;}

        public string? SortBy { get; set;}              // "Ngay", "CaTruc"...
        public string? SortDirection { get; set;} = "asc";

        public int Page { get; set;} = 1;
        public int PageSize { get; set;} = 50;
    }

    // ===================== DỊCH VỤ Y TẾ =====================

    public record class ServiceDto
    {
        public string MaDichVu { get; set;} = default!;
        public string TenDichVu { get; set;} = default!;
        public string LoaiDichVu { get; set;} = default!; // kham_lam_sang, can_lam_sang, khac...
        public string? MaKhoa { get; set;}
        public string? MaPhong { get; set;}
        public decimal DonGia { get; set;}
    }

    public record class ServiceSearchFilter
    {
        public string? Keyword { get; set;}
        public string? LoaiDichVu { get; set;}
        public string? MaKhoa { get; set;}
        public string? MaPhong { get; set;}

        public string? SortBy { get; set;}              // "TenDichVu", "DonGia"...
        public string? SortDirection { get; set;} = "asc";

        public int Page { get; set;} = 1;
        public int PageSize { get; set;} = 50;
    }

    // ===================== OVERVIEW =====================

    public record class DepartmentOverviewDto
    {
        public string MaKhoa { get; set;} = default!;
        public string TenKhoa { get; set;} = default!;

        public int SoPhongKham { get; set;}
        public int SoPhongCls { get; set;}
        public int TongSoPhong { get; set;}


        public int SoBacSiDangCongTac { get; set;}
    }

    public record class StaffOverviewDto
    {
        public string VaiTro { get; set;} = "bac_si";  
        public int SoBenhNhanDangCho { get; set;}
        public int SoLichHenHomNay { get; set;}
        public string TenBS { get; set;} = default!;
    }

    public record class ServiceOverviewDto
    {
        public string LoaiDichVu { get; set;} = default!;   // kham_lam_sang, can_lam_sang, 
        public string MaDV { get; set;} = default!;
         public string TenDV { get; set;} = default!;
    }
    public record class ServiceDetailInfoDto
    {
        public string MaKhoa { get; set;} = default!;
        public string TenKhoa { get; set;} = default!;

        public string MaPhong { get; set;} = default!;
        public string TenPhong { get; set;} = default!;

        public string MaBacSi { get; set;} = default!;
        public string TenBacSi { get; set;} = default!;

        public string TenDichVu { get; set;} = default!;
        public string LoaiDichVu { get; set;} = default!;   // kham_lam_sang, can_lam_sang, ...

        public decimal DonGia { get; set;}
    }

}
