// AuthDtos.cs :contentReference[oaicite:2]{index=2} :contentReference[oaicite:3]{index=3}
using System;

namespace HealthCare.DTOs
{
    public record class AuthLoginRequest
    {
        public string TenDangNhap { get; set;} = default!;
        public string MatKhau { get; set;} = default!;
    }

    public record class AuthLogoutRequest
    {
        /// <summary>
        /// Nếu null: đăng xuất tất cả phiên đăng nhập còn hiệu lực của user hiện tại.
        /// Nếu có giá trị: chỉ thu hồi refresh token cụ thể.
        /// </summary>
        public string? RefreshToken { get; set;}
    }

    /// <summary>
    /// Thông tin nhân viên y tế trả về kèm token (bám NhanVienYTe). :contentReference[oaicite:4]{index=4}
    /// </summary>
    public record class StaffAuthInfoDto
    {
        public string MaNhanVien { get; set;} = default!;
        public string TenDangNhap { get; set;} = default!;

        public string HoTen { get; set;} = default!;
        public string VaiTro { get; set;} = "bac_si";   // bac_si, y_ta, admin...
        public string ChucVu { get; set;} = "bac_si";   // bac_si, y_ta_hanh_chinh, y_ta_phong_kham, ky_thuat_vien, admin

        public string? LoaiYTa { get; set;}            // hanhchinh, ls, cls
        public string MaKhoa { get; set;} = default!;
        public string? Email { get; set;}
        public string? DienThoai { get; set;}

        public string TrangThaiCongTac { get; set;} = "dang_cong_tac";
        public string? AnhDaiDien { get; set;}

        public int SoNamKinhNghiem { get; set;}
        public string? ChuyenMon { get; set;}
        public string? HocVi { get; set;}
        public string? MoTa { get; set;}
    }

    public record class AuthTokenResponse
    {
        public string AccessToken { get; set;} = default!;
        public DateTime AccessTokenExpiresAt { get; set;}

        public string RefreshToken { get; set;} = default!;
        public DateTime RefreshTokenExpiresAt { get; set;}

        // Thông tin nhân viên (flatten + object)
        public string MaNhanVien { get; set;} = default!;
        public string HoTen { get; set;} = default!;
        public string VaiTro { get; set;} = default!;
        public string ChucVu { get; set;} = default!;
        public string? MaKhoa { get; set;}

        public string TenDangNhap { get; set;} = default!;
        public string? AnhDaiDien { get; set;}
        public string? Email { get; set;}
        public string? DienThoai { get; set;}
        public string TrangThaiCongTac { get; set;} = "dang_cong_tac";
        public string? LoaiYTa { get; set;}
        public int SoNamKinhNghiem { get; set;}
        public string? ChuyenMon { get; set;}
        public string? HocVi { get; set;}
        public string? MoTa { get; set;}

        public StaffAuthInfoDto? NhanVien { get; set;}
    }

    /// <summary>
    /// Request đặt lại mật khẩu (flow "quên mật khẩu").
    /// Có thể bổ sung xác thực (email/OTP) sau này.
    /// </summary>
    public record class AuthForgotPasswordRequest
    {
        public string TenDangNhap { get; set;} = default!;
        public string MatKhauMoi { get; set;} = default!;
        public string Email { get; set;} = default!;

        /// <summary>IntentId đã verify OTP qua /api/otp/verify.</summary>
        public string OtpIntentId { get; set;} = default!;
    }

    public record class AuthChangePasswordRequest
    {
        public string CurrentPassword { get; set;} = default!;
        public string NewPassword { get; set;} = default!;
        public string ConfirmPassword { get; set;} = default!;

        /// <summary>IntentId đã verify OTP qua /api/otp/verify.</summary>
        public string OtpIntentId { get; set;} = default!;
    }
}
