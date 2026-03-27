namespace HealthCare.DTOs
{
    // ===== Admin User Management DTOs =====

    /// <summary>DTO trả về thông tin nhân viên cho Admin UI.</summary>
    public class AdminUserDto
    {
        public string MaNhanVien { get; set; } = default!;
        public string TenDangNhap { get; set; } = default!;
        public string HoTen { get; set; } = default!;
        public string VaiTro { get; set; } = default!;
        public string ChucVu { get; set; } = default!;
        public string? LoaiYTa { get; set; }
        public string? Email { get; set; }
        public string? DienThoai { get; set; }
        public string? ChuyenMon { get; set; }
        public string? HocVi { get; set; }
        public int SoNamKinhNghiem { get; set; }
        public string MaKhoa { get; set; } = default!;
        public string? TenKhoa { get; set; }
        public string TrangThaiCongTac { get; set; } = default!;
        public string? AnhDaiDien { get; set; }
    }

    /// <summary>Request tạo nhân viên mới.</summary>
    public class AdminUserCreateRequest
    {
        public string TenDangNhap { get; set; } = default!;
        public string MatKhau { get; set; } = default!;
        public string HoTen { get; set; } = default!;
        public string VaiTro { get; set; } = "bac_si";
        public string ChucVu { get; set; } = "bac_si";
        public string? LoaiYTa { get; set; }
        public string? Email { get; set; }
        public string? DienThoai { get; set; }
        public string? ChuyenMon { get; set; }
        public string? HocVi { get; set; }
        public int SoNamKinhNghiem { get; set; }
        public string MaKhoa { get; set; } = default!;
    }

    /// <summary>Request cập nhật thông tin nhân viên.</summary>
    public class AdminUserUpdateRequest
    {
        public string HoTen { get; set; } = default!;
        public string VaiTro { get; set; } = default!;
        public string ChucVu { get; set; } = default!;
        public string? LoaiYTa { get; set; }
        public string? Email { get; set; }
        public string? DienThoai { get; set; }
        public string? ChuyenMon { get; set; }
        public string? HocVi { get; set; }
        public int SoNamKinhNghiem { get; set; }
        public string MaKhoa { get; set; } = default!;
    }

    /// <summary>Request thay đổi trạng thái công tác (khóa/mở).</summary>
    public class AdminStatusUpdateRequest
    {
        public string TrangThaiCongTac { get; set; } = default!; // dang_cong_tac | tam_nghi | nghi_viec
    }

    /// <summary>Admin reset mật khẩu cho nhân viên.</summary>
    public class AdminResetPasswordRequest
    {
        public string MatKhauMoi { get; set; } = default!;
    }

    /// <summary>Filter params cho GET list nhân viên.</summary>
    public class AdminUserFilter
    {
        public string? Q { get; set; }
        public string? VaiTro { get; set; }
        public string? TrangThai { get; set; }
        public string? MaKhoa { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
