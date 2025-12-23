using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Caching.Memory;
using HealthCare.Realtime;
using HealthCare.Services.Report;

namespace HealthCare.Services.UserInteraction
{
    public class AuthService(DataContext db, IConfiguration cfg, IRealtimeService realtime, IDashboardService dashboard, IMemoryCache cache) : IAuthService
    {
        private readonly DataContext _db = db;
        private readonly IConfiguration _cfg = cfg;
        private readonly IRealtimeService _realtime = realtime;
        private readonly IDashboardService _dashboard = dashboard;
        private readonly IMemoryCache _cache = cache;

   

        // =========================================================
        // =                       LOGIN                           =
        // =========================================================
        public async Task<AuthTokenResponse> LoginAsync(AuthLoginRequest request, string? ipAddress)
        {
            var staff = await _db.NhanVienYTes
                .FirstOrDefaultAsync(s => s.TenDangNhap == request.TenDangNhap);

            if (staff is null)
                throw new UnauthorizedAccessException("Sai tài khoản hoặc mật khẩu");

            // MatKhauHash lưu hash BCrypt
            if (!BCrypt.Net.BCrypt.Verify(request.MatKhau, staff.MatKhauHash))
                throw new UnauthorizedAccessException("Sai tài khoản hoặc mật khẩu");

            var (accessToken, accessExp) = GenerateJwt(staff);
            var (refreshToken, refreshExp) = await IssueRefreshTokenAsync(staff, ipAddress);

            return BuildAuthTokenResponse(staff, accessToken, accessExp, refreshToken, refreshExp);
        }

        // =========================================================
        // =                       REFRESH                         =
        // =========================================================
        public async Task<AuthTokenResponse> RefreshAsync(string refreshToken, string? ipAddress, string? currentUserId = null)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new UnauthorizedAccessException("Refresh token không hợp lệ");

            var old = await _db.RefreshTokens
                .Include(r => r.NhanVien)
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            if (old is null || !old.IsTrangThai || old.ThoiGianHetHan <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token không hợp lệ");

            if (currentUserId != null &&
                !string.Equals(old.MaNhanVien, currentUserId, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Refresh token không thuộc về người dùng hiện tại");

            // Thu hồi token cũ + chuỗi kế thừa (nếu có)
            Revoke(old, ipAddress, cascade: true);

            var staff = old.NhanVien;

            var (newRefresh, newRefreshExp) = await IssueRefreshTokenAsync(staff, ipAddress);
            old.ReplacedToken = newRefresh;

            var (accessToken, accessExp) = GenerateJwt(staff);

            await _db.SaveChangesAsync();

            return BuildAuthTokenResponse(staff, accessToken, accessExp, newRefresh, newRefreshExp);
        }

        // =========================================================
        // =                       LOGOUT                          =
        // =========================================================
        public async Task LogoutAsync(string userId, string? refreshToken, string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var rt = await _db.RefreshTokens
                    .FirstOrDefaultAsync(r => r.Token == refreshToken &&
                                              r.MaNhanVien == userId &&
                                              r.IsTrangThai);
                if (rt != null)
                {
                    Revoke(rt, ipAddress, cascade: true);
                }
            }
            else
            {
                var tokens = await _db.RefreshTokens
                    .Where(r => r.MaNhanVien == userId &&
                                r.IsTrangThai &&
                                r.ThoiGianHetHan > DateTime.UtcNow)
                    .ToListAsync();

                foreach (var t in tokens)
                {
                    Revoke(t, ipAddress, cascade: true);
                }
            }

            await _db.SaveChangesAsync();
        }

        // =========================================================
        // =                 FORGOT PASSWORD FLOW                  =
        // =========================================================
        public async Task ForgotPasswordAsync(AuthForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TenDangNhap))
                throw new ArgumentException("Tài khoản không được để trống", nameof(request.TenDangNhap));

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Email không được để trống", nameof(request.Email));

            if (string.IsNullOrWhiteSpace(request.MatKhauMoi) || request.MatKhauMoi.Length < 6)
                throw new ArgumentException("Mật khẩu mới phải từ 6 ký tự trở lên", nameof(request.MatKhauMoi));

            var staff = await _db.NhanVienYTes
                .Include(s => s.RefreshTokens)
                .FirstOrDefaultAsync(s => s.TenDangNhap == request.TenDangNhap);

            if (staff is null)
                throw new UnauthorizedAccessException("Tài khoản không hợp lệ");

            // bind OTP với email tài khoản
            EnsureOtpVerified(request.OtpIntentId, staff.Email ?? string.Empty);

            // (tuỳ bạn: có thể verify request.Email == staff.Email thêm 1 lớp nữa)
            if (!string.Equals(staff.Email?.Trim(), request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Email không khớp với tài khoản.");

            // Cập nhật hash mật khẩu mới (BCrypt)
            staff.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(request.MatKhauMoi);

            // Thu hồi tất cả refresh token đang còn hiệu lực của nhân viên này
            var activeTokens = await _db.RefreshTokens
                .Where(r => r.MaNhanVien == staff.MaNhanVien &&
                            r.IsTrangThai &&
                            r.ThoiGianHetHan > DateTime.UtcNow)
                .ToListAsync();

            foreach (var t in activeTokens)
            {
                Revoke(t, ipAddress: null, cascade: true);
            }

            await _db.SaveChangesAsync();

        }
        // =========================================================
        // =                 CHANGE PASSWORD FLOW                  =
        // =========================================================
        public async Task ChangePasswordAsync(
            string userId,
            AuthChangePasswordRequest request,
            string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("Không xác định được người dùng hiện tại.");

            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                throw new ArgumentException("Mật khẩu hiện tại không được để trống.", nameof(request.CurrentPassword));

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                throw new ArgumentException("Mật khẩu mới phải từ 6 ký tự trở lên.", nameof(request.NewPassword));

            if (request.NewPassword != request.ConfirmPassword)
                throw new ArgumentException("Xác nhận mật khẩu mới không khớp.", nameof(request.ConfirmPassword));

            // BẮT BUỘC OTP
            
            var staff = await _db.NhanVienYTes
                .Include(s => s.RefreshTokens)
                .FirstOrDefaultAsync(s => s.MaNhanVien == userId);

            if (staff is null)
                throw new UnauthorizedAccessException("Tài khoản không tồn tại.");
            EnsureOtpVerified(request.OtpIntentId, staff.Email ?? string.Empty);

            // Kiểm tra mật khẩu hiện tại
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, staff.MatKhauHash))
                throw new UnauthorizedAccessException("Mật khẩu hiện tại không đúng.");

            // Cập nhật mật khẩu mới (BCrypt)
            staff.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // Thu hồi tất cả refresh token đang còn hiệu lực
            var activeTokens = await _db.RefreshTokens
                .Where(r => r.MaNhanVien == staff.MaNhanVien &&
                            r.IsTrangThai &&
                            r.ThoiGianHetHan > DateTime.UtcNow)
                .ToListAsync();

            foreach (var t in activeTokens)
            {
                Revoke(t, ipAddress, cascade: true);
            }

            await _db.SaveChangesAsync();



        }
        // ================== Helper: revoke token ==================

        private void Revoke(RefreshToken token, string? ipAddress, bool cascade)
        {
            token.IsTrangThai = false;
            token.ThoiGianThuHoi = DateTime.UtcNow;
            token.RevokedIp = ipAddress;

            if (cascade && !string.IsNullOrEmpty(token.ReplacedToken))
            {
                var successor = _db.RefreshTokens
                    .SingleOrDefault(r => r.Token == token.ReplacedToken);
                if (successor != null && successor.IsTrangThai)
                {
                    Revoke(successor, ipAddress, cascade: true);
                }
            }
        }

        private async Task<(string token, DateTime expUtc)> IssueRefreshTokenAsync(NhanVienYTe staff, string? ipAddress)
        {
            var days = int.Parse(_cfg["Jwt:RefreshTokenDays"] ?? "14");
            var now = DateTime.UtcNow;
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var rt = new RefreshToken
            {
                Id = Guid.NewGuid().ToString("N"),
                MaNhanVien = staff.MaNhanVien,
                Token = token,
                ThoiGianTao = now,
                ThoiGianHetHan = now.AddDays(days),
                CreatedIp = ipAddress,
                IsTrangThai = true,
                ThoiGianThuHoi = null,
                RevokedIp = null,
                ReplacedToken = null
            };

            _db.RefreshTokens.Add(rt);
            await _db.SaveChangesAsync();

            return (rt.Token, rt.ThoiGianHetHan);
        }

        private (string token, DateTime expUtc) GenerateJwt(NhanVienYTe staff)
        {
            var jwtSection = _cfg.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var minutes = int.Parse(jwtSection["AccessTokenMinutes"] ?? "30");
            var exp = now.AddMinutes(minutes);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, staff.MaNhanVien),
                new Claim(JwtRegisteredClaimNames.UniqueName, staff.TenDangNhap ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, staff.MaNhanVien),
                new Claim(ClaimTypes.Name, staff.HoTen ?? string.Empty),
             // Vai trò trong hệ thống (bac_si, y_ta, admin, ...)
                new Claim(ClaimTypes.Role, staff.VaiTro ?? "bac_si"),
                new Claim("VaiTro", staff.VaiTro ?? "bac_si"),
                
                // Chức vụ chi tiết (bac_si, y_ta_hanh_chinh, y_ta_phong_kham, ky_thuat_vien, admin)
                new Claim("ChucVu", staff.ChucVu ?? "bac_si"),

                // Loại người nhận notification: NVYT
                new Claim("LoaiNguoiNhan", "nhan_vien_y_te"),
                new Claim("ma_khoa", staff.MaKhoa ?? string.Empty),
                new Claim("loai_y_ta", staff.LoaiYTa ?? string.Empty),
                new Claim("trang_thai_cong_tac", staff.TrangThaiCongTac ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                notBefore: now,
                expires: exp,
                signingCredentials: creds);

            var encoded = new JwtSecurityTokenHandler().WriteToken(token);
            return (encoded, exp);
        }
    

            
        private void EnsureOtpVerified(string otpIntentId, string expectedEmail)
        {
            if (string.IsNullOrWhiteSpace(otpIntentId))
                throw new ArgumentException("Thiếu thông tin OTP.", nameof(otpIntentId));
            if (string.IsNullOrWhiteSpace(expectedEmail))
                throw new ArgumentException("Không xác định được email tài khoản.");

            var verifiedKey = $"otp:verified:intent:{otpIntentId}";

            if (!_cache.TryGetValue<string>(verifiedKey, out var verifiedEmail) ||
                string.IsNullOrWhiteSpace(verifiedEmail))
            {
                throw new UnauthorizedAccessException("OTP chưa được xác thực hoặc đã hết hạn.");
            }

            if (!string.Equals(verifiedEmail.Trim(), expectedEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("OTP không hợp lệ cho tài khoản này.");
            }

            // Mỗi intent chỉ dùng 1 lần
            _cache.Remove(verifiedKey);
        }

        // ================== Helper: map DTO ==================

        private static StaffAuthInfoDto MapStaffAuthInfo(NhanVienYTe staff)
        {
            return new StaffAuthInfoDto
            {
                MaNhanVien = staff.MaNhanVien,
                TenDangNhap = staff.TenDangNhap,
                HoTen = staff.HoTen,
                VaiTro = staff.VaiTro,
                ChucVu = staff.ChucVu,
                LoaiYTa = staff.LoaiYTa,
                MaKhoa = staff.MaKhoa,
                Email = staff.Email,
                DienThoai = staff.DienThoai,
                TrangThaiCongTac = staff.TrangThaiCongTac,
                AnhDaiDien = staff.AnhDaiDien,
                SoNamKinhNghiem = staff.SoNamKinhNghiem,
                ChuyenMon = staff.ChuyenMon,
                HocVi = staff.HocVi,
                MoTa = staff.MoTa
            };
        }

        private static AuthTokenResponse BuildAuthTokenResponse(
            NhanVienYTe staff,
            string accessToken,
            DateTime accessExp,
            string refreshToken,
            DateTime refreshExp)
        {
            var nhanVien = MapStaffAuthInfo(staff);

            return new AuthTokenResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessExp,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshExp,

                MaNhanVien = staff.MaNhanVien,
                HoTen = staff.HoTen,
                VaiTro = staff.VaiTro,
                ChucVu = staff.ChucVu,
                MaKhoa = staff.MaKhoa,

                TenDangNhap = staff.TenDangNhap,
                AnhDaiDien = staff.AnhDaiDien,
                Email = staff.Email,
                DienThoai = staff.DienThoai,
                TrangThaiCongTac = staff.TrangThaiCongTac,
                LoaiYTa = staff.LoaiYTa,
                SoNamKinhNghiem = staff.SoNamKinhNghiem,
                ChuyenMon = staff.ChuyenMon,
                HocVi = staff.HocVi,
                MoTa = staff.MoTa,

                NhanVien = nhanVien
            };
        }

    }
}
