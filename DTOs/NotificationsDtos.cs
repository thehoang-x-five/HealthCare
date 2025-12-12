
using System.Collections.Generic;

namespace HealthCare.DTOs
{
    /// <summary>
    /// Thông tin 1 bản ghi người nhận thông báo (bang thong_bao_nguoi_nhan).
    /// </summary>
    public record class NotificationRecipientDto
    {
        public long MaTbNguoiNhan { get; set;}
        public string MaThongBao { get; set;} = default!;
        public string LoaiNguoiNhan { get; set;} = default!;   // benh_nhan, nhan_vien_y_te
        public string MaNguoiNhan { get; set;} = default!;     // MaBenhNhan / MaNhanVien

        public bool DaDoc { get; set;}
        public DateTime? ThoiGianDoc { get; set;}
    }

    /// <summary>
    /// DTO trả về cho FE (kết hợp thông báo + trạng thái người nhận).
    /// </summary>
    public record class NotificationDto
    {
        public string MaThongBao { get; set;} = default!;
        public string LoaiThongBao { get; set;} = default!;    // system, cls, billing...
        public string TieuDe { get; set;} = default!;
        public string NoiDung { get; set;} = default!;
        public string MucDoUuTien { get; set;} = "normal";     // normal, high...

        /// <summary>
        /// Trạng thái thông báo:
        /// - cho_gui
        /// - da_gui
        /// - da_doc (đã có ít nhất 1 người nhận đọc)
        /// </summary>
        public string TrangThai { get; set;} = default!;       // cho_gui, da_gui, da_doc

        public DateTime ThoiGianTao { get; set;}
        public DateTime? ThoiGianGui { get; set;}

        // Thông tin theo NGƯỜI NHẬN (hộp thư).
        public long? MaTbNguoiNhan { get; set;}
        public string LoaiNguoiNhan { get; set;} = default!;
        public string MaNguoiNhan { get; set;} = default!;
        public bool DaDoc { get; set;}
        public DateTime? ThoiGianDoc { get; set;}
    }

    /// <summary>
    /// Dùng khi tạo mới: 1 người nhận thông báo.
    /// </summary>
    public record class NotificationRecipientCreateRequest
    {
        public string LoaiNguoiNhan { get; set;} = default!;   // benh_nhan, nhan_vien_y_te
        public string MaNguoiNhan { get; set;} = default!;
    }

    /// <summary>
    /// Request tạo thông báo hệ thống + danh sách người nhận.
    /// </summary>
    public record class NotificationCreateRequest
    {
        public string LoaiThongBao { get; set;} = default!;    // system, cls, billing...
        public string TieuDe { get; set;} = default!;
        public string NoiDung { get; set;} = default!;
        public string MucDoUuTien { get; set;} = "normal";

        /// <summary>
        /// Tham chiếu nghiệp vụ (vd: ma_phieu_kham, ma_phieu_cls...).
        /// </summary>
        public string? NguonLienQuan { get; set;}
        public string? MaDoiTuongLienQuan { get; set;}

        public IReadOnlyList<NotificationRecipientCreateRequest> NguoiNhan { get; set;}
            = Array.Empty<NotificationRecipientCreateRequest>();
    }

    /// <summary>
    /// Filter cho hộp thư người nhận (BN / NVYT).
    /// </summary>
    public record class NotificationFilterRequest
    {
        public string LoaiNguoiNhan { get; set;} = default!;   // benh_nhan, nhan_vien_y_te
        public string MaNguoiNhan { get; set;} = default!;

        /// <summary>
        /// Chỉ lấy chưa đọc (true) hay tất cả (false/null).
        /// </summary>
        public bool? OnlyUnread { get; set;} = true;

        /// <summary>
        /// Trạng thái thông báo: cho_gui, da_gui, da_doc (optional filter).
        /// </summary>
        public string? TrangThai { get; set;}

        public DateTime? FromTime { get; set;}
        public DateTime? ToTime { get; set;}
        public int Page { get; set;} = 1;
        public int PageSize { get; set;} = 50;
    }

    /// <summary>
    /// Filter dùng cho màn quản trị / log xem toàn bộ thông báo.
    /// </summary>
    public record class NotificationSearchFilter
    {
        public string? LoaiThongBao { get; set;}
        public string? MucDoUuTien { get; set;}
        public string? TrangThai { get; set;}   // cho_gui, da_gui, da_doc
        public string? Keyword { get; set;}     // tìm trong tiêu đề / nội dung

        public DateTime? FromTime { get; set;}
        public DateTime? ToTime { get; set;}

        public int Page { get; set;} = 1;
        public int PageSize { get; set;} = 50;
    }

    /// <summary>
    /// Cập nhật trạng thái thông báo (trên bảng thong_bao_he_thong).
    /// </summary>
    public record class NotificationStatusUpdateRequest
    {
        /// <summary>
        /// cho_gui | da_gui | da_doc
        /// </summary>
        public string TrangThai { get; set;} = default!;
    }
}
