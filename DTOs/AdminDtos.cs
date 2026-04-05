namespace HealthCare.DTOs
{
    // ===== Admin User Management DTOs =====

    public class AdminUserDto
    {
        public string MaUser { get; set; } = default!;
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
        public string? MaKhoa { get; set; }
        public string? TenKhoa { get; set; }
        public string TrangThaiCongTac { get; set; } = default!;
        public string TrangThaiTaiKhoan { get; set; } = default!;
        public string? AnhDaiDien { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? LanDangNhapCuoi { get; set; }
    }

    public class AdminUserCreateRequest
    {
        public string TenDangNhap { get; set; } = default!;
        public string MatKhau { get; set; } = default!;
        public string HoTen { get; set; } = default!;
        public string VaiTro { get; set; } = "bac_si";
        public string? LoaiYTa { get; set; }
        public string? Email { get; set; }
        public string? DienThoai { get; set; }
        public string? ChuyenMon { get; set; }
        public string? HocVi { get; set; }
        public int SoNamKinhNghiem { get; set; }
        public string MaKhoa { get; set; } = default!;
        public string? AnhDaiDien { get; set; }
        public string? MoTa { get; set; }
    }

    public class AdminUserUpdateRequest
    {
        public string HoTen { get; set; } = default!;
        public string VaiTro { get; set; } = default!;
        public string? LoaiYTa { get; set; }
        public string? Email { get; set; }
        public string? DienThoai { get; set; }
        public string? ChuyenMon { get; set; }
        public string? HocVi { get; set; }
        public int SoNamKinhNghiem { get; set; }
        public string MaKhoa { get; set; } = default!;
        public string? AnhDaiDien { get; set; }
        public string? MoTa { get; set; }
    }

    public class AdminUserFilter
    {
        public string? Q { get; set; }
        public string? VaiTro { get; set; }
        public string? TrangThai { get; set; }
        public string? MaKhoa { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AdminResetPasswordResponse
    {
        public string NewPassword { get; set; } = default!;
        public string Message { get; set; } = default!;
    }
}
