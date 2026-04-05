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
            var userAccount = await _db.UserAccounts
                .Include(u => u.NhanVienYTe)
                .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap);

            if (userAccount is null)
                throw new UnauthorizedAccessException("Sai tài khoản hoặc mật khẩu");

            if (userAccount.TrangThaiTaiKhoan != "hoat_dong")
                throw new UnauthorizedAccessException("Tài khoản đã bị khóa hoặc tạm ngưng");

            if (!BCrypt.Net.BCrypt.Verify(request.MatKhau, userAccount.MatKhauHash))
                throw new UnauthorizedAccessException("Sai tài khoản hoặc mật khẩu");

            userAccount.LanDangNhapCuoi = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var (accessToken, accessExp) = GenerateJwt(userAccount);
            var (refreshToken, refreshExp) = await IssueRefreshTokenAsync(userAccount, ipAddress);

            return BuildAuthTokenResponse(userAccount, accessToken, accessExp, refreshToken, refreshExp);
        }

        // =========================================================
        // =                       REFRESH                         =
        // =========================================================
        public async Task<AuthTokenResponse> RefreshAsync(string refreshToken, string? ipAddress, string? currentUserId = null)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new UnauthorizedAccessException("Refresh token không hợp lệ");

            var old = await _db.RefreshTokens
                .Include(r => r.UserAccount)
                    .ThenInclude(u => u.NhanVienYTe)
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            if (old is null || !old.IsTrangThai || old.ThoiGianHetHan <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token không hợp lệ");

            if (currentUserId != null &&
                !string.Equals(old.MaUser, currentUserId, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Refresh token không thuộc về người dùng hiện tại");

            Revoke(old, ipAddress, cascade: true);

            var userAccount = old.UserAccount;

            var (newRefresh, newRefreshExp) = await IssueRefreshTokenAsync(userAccount, ipAddress);
            old.ReplacedToken = newRefresh;

            var (accessToken, accessExp) = GenerateJwt(userAccount);

            await _db.SaveChangesAsync();

            return BuildAuthTokenResponse(userAccount, accessToken, accessExp, newRefresh, newRefreshExp);
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
                                              r.MaUser == userId &&
                                              r.IsTrangThai);
                if (rt != null)
                {
                    Revoke(rt, ipAddress, cascade: true);
                }
            }
            else
            {
                var tokens = await _db.RefreshTokens
                    .Where(r => r.MaUser == userId &&
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

            var userAccount = await _db.UserAccounts
                .Include(u => u.NhanVienYTe)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap);

            if (userAccount is null)
                throw new UnauthorizedAccessException("Tài khoản không hợp lệ");

            var staffEmail = userAccount.NhanVienYTe?.Email ?? string.Empty;
            EnsureOtpVerified(request.OtpIntentId, staffEmail);

            if (!string.Equals(staffEmail.Trim(), request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Email không khớp với tài khoản.");

            userAccount.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(request.MatKhauMoi);
            userAccount.NgayCapNhat = DateTime.UtcNow;

            var activeTokens = await _db.RefreshTokens
                .Where(r => r.MaUser == userAccount.MaUser &&
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

            var userAccount = await _db.UserAccounts
                .Include(u => u.NhanVienYTe)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.MaUser == userId);

            if (userAccount is null)
                throw new UnauthorizedAccessException("Tài khoản không tồn tại.");

            var staffEmail = userAccount.NhanVienYTe?.Email ?? string.Empty;
            EnsureOtpVerified(request.OtpIntentId, staffEmail);

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, userAccount.MatKhauHash))
                throw new UnauthorizedAccessException("Mật khẩu hiện tại không đúng.");

            userAccount.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            userAccount.NgayCapNhat = DateTime.UtcNow;

            var activeTokens = await _db.RefreshTokens
                .Where(r => r.MaUser == userAccount.MaUser &&
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

        private async Task<(string token, DateTime expUtc)> IssueRefreshTokenAsync(UserAccount userAccount, string? ipAddress)
        {
            var days = int.Parse(_cfg["Jwt:RefreshTokenDays"] ?? "14");
            var now = DateTime.UtcNow;
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var rt = new RefreshToken
            {
                Id = Guid.NewGuid().ToString("N"),
                MaUser = userAccount.MaUser,
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

        private (string token, DateTime expUtc) GenerateJwt(UserAccount userAccount)
        {
            var jwtSection = _cfg.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var minutes = int.Parse(jwtSection["AccessTokenMinutes"] ?? "30");
            var exp = now.AddMinutes(minutes);

            var staff = userAccount.NhanVienYTe;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userAccount.MaUser),
                new Claim(JwtRegisteredClaimNames.UniqueName, userAccount.TenDangNhap),
                new Claim(ClaimTypes.NameIdentifier, userAccount.MaUser),
                new Claim("ma_user", userAccount.MaUser),
                new Claim("ma_nhan_vien", staff?.MaNhanVien ?? string.Empty),
                new Claim(ClaimTypes.Name, staff?.HoTen ?? string.Empty),
                new Claim(ClaimTypes.Role, userAccount.VaiTro),
                new Claim("vai_tro", userAccount.VaiTro),
                new Claim("loai_y_ta", userAccount.LoaiYTa ?? string.Empty),
                new Claim("ma_khoa", staff?.MaKhoa ?? string.Empty),
                new Claim("ho_ten", staff?.HoTen ?? string.Empty),
                new Claim("trang_thai_tai_khoan", userAccount.TrangThaiTaiKhoan),
                new Claim("trang_thai_cong_tac", staff?.TrangThaiCongTac ?? string.Empty),
                new Claim("LoaiNguoiNhan", "nhan_vien_y_te"),
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

        private static StaffAuthInfoDto MapStaffAuthInfo(UserAccount userAccount)
        {
            var staff = userAccount.NhanVienYTe;
            return new StaffAuthInfoDto
            {
                MaNhanVien = staff?.MaNhanVien ?? string.Empty,
                TenDangNhap = userAccount.TenDangNhap,
                HoTen = staff?.HoTen ?? string.Empty,
                VaiTro = userAccount.VaiTro,
                ChucVu = userAccount.VaiTro,
                LoaiYTa = userAccount.LoaiYTa,
                MaKhoa = staff?.MaKhoa ?? string.Empty,
                Email = staff?.Email,
                DienThoai = staff?.DienThoai,
                TrangThaiCongTac = staff?.TrangThaiCongTac ?? string.Empty,
                AnhDaiDien = staff?.AnhDaiDien,
                SoNamKinhNghiem = staff?.SoNamKinhNghiem ?? 0,
                ChuyenMon = staff?.ChuyenMon,
                HocVi = staff?.HocVi,
                MoTa = staff?.MoTa
            };
        }

        private static AuthTokenResponse BuildAuthTokenResponse(
            UserAccount userAccount,
            string accessToken,
            DateTime accessExp,
            string refreshToken,
            DateTime refreshExp)
        {
            var nhanVien = MapStaffAuthInfo(userAccount);
            var staff = userAccount.NhanVienYTe;

            return new AuthTokenResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessExp,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshExp,

                MaUser = userAccount.MaUser,
                MaNhanVien = staff?.MaNhanVien ?? string.Empty,
                HoTen = staff?.HoTen ?? string.Empty,
                VaiTro = userAccount.VaiTro,
                ChucVu = userAccount.VaiTro,
                MaKhoa = staff?.MaKhoa ?? string.Empty,

                TenDangNhap = userAccount.TenDangNhap,
                AnhDaiDien = staff?.AnhDaiDien,
                Email = staff?.Email,
                DienThoai = staff?.DienThoai,
                TrangThaiCongTac = staff?.TrangThaiCongTac ?? string.Empty,
                LoaiYTa = userAccount.LoaiYTa,
                SoNamKinhNghiem = staff?.SoNamKinhNghiem ?? 0,
                ChuyenMon = staff?.ChuyenMon,
                HocVi = staff?.HocVi,
                MoTa = staff?.MoTa,

                NhanVien = nhanVien
            };
        }

    }
}
