using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using HealthCare.Realtime;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace HealthCare.Services.Admin
{
    public class AdminService(DataContext db, IRealtimeService realtime) : IAdminService
    {
        private readonly DataContext _db = db;
        private readonly IRealtimeService _realtime = realtime;

        public async Task<PagedResult<AdminUserDto>> GetUsersAsync(AdminUserFilter filter)
        {
            var query = _db.UserAccounts
                .Include(u => u.NhanVienYTe)
                    .ThenInclude(nv => nv!.KhoaChuyenMon)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Q))
            {
                var q = filter.Q.Trim().ToLower();
                query = query.Where(u =>
                    u.TenDangNhap.ToLower().Contains(q) ||
                    u.NhanVienYTe!.HoTen.ToLower().Contains(q) ||
                    u.MaUser.ToLower().Contains(q) ||
                    u.MaNhanVien!.ToLower().Contains(q));
            }

            if (!string.IsNullOrWhiteSpace(filter.VaiTro))
                query = query.Where(u => u.VaiTro == filter.VaiTro);

            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
                query = query.Where(u => u.TrangThaiTaiKhoan == filter.TrangThai);

            if (!string.IsNullOrWhiteSpace(filter.MaKhoa))
                query = query.Where(u => u.NhanVienYTe!.MaKhoa == filter.MaKhoa);

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.NgayTao)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(u => MapToDto(u))
                .ToListAsync();

            return new PagedResult<AdminUserDto>
            {
                Items = items,
                TotalItems = total,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<AdminUserDto> GetUserByIdAsync(string maUser)
        {
            var userAccount = await _db.UserAccounts
                .Include(u => u.NhanVienYTe)
                    .ThenInclude(nv => nv!.KhoaChuyenMon)
                .FirstOrDefaultAsync(u => u.MaUser == maUser)
                ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản {maUser}");

            return MapToDto(userAccount);
        }

        public async Task<AdminUserDto> CreateUserAsync(AdminUserCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TenDangNhap))
                throw new ArgumentException("Tên đăng nhập không được để trống");

            if (string.IsNullOrWhiteSpace(request.MatKhau) || request.MatKhau.Length < 6)
                throw new ArgumentException("Mật khẩu phải từ 6 ký tự trở lên");

            var existingUser = await _db.UserAccounts
                .AnyAsync(u => u.TenDangNhap == request.TenDangNhap);
            if (existingUser)
                throw new InvalidOperationException("Tên đăng nhập đã tồn tại");

            if (request.VaiTro == "y_ta" && string.IsNullOrWhiteSpace(request.LoaiYTa))
                throw new ArgumentException("Y tá phải có loại y tá (hanhchinh, ls, cls)");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var maNhanVien = GenerateStaffId(request.VaiTro);
                var maUser = $"USR_{maNhanVien}";

                var nhanVien = new NhanVienYTe
                {
                    MaNhanVien = maNhanVien,
                    HoTen = request.HoTen,
                    Email = request.Email,
                    DienThoai = request.DienThoai,
                    ChuyenMon = request.ChuyenMon,
                    HocVi = request.HocVi,
                    SoNamKinhNghiem = request.SoNamKinhNghiem,
                    MaKhoa = request.MaKhoa,
                    TrangThaiCongTac = "dang_cong_tac",
                    AnhDaiDien = request.AnhDaiDien,
                    MoTa = request.MoTa
                };

                _db.NhanVienYTes.Add(nhanVien);
                await _db.SaveChangesAsync();

                var userAccount = new UserAccount
                {
                    MaUser = maUser,
                    TenDangNhap = request.TenDangNhap,
                    MatKhauHash = BCrypt.Net.BCrypt.HashPassword(request.MatKhau, workFactor: 12),
                    VaiTro = request.VaiTro,
                    LoaiYTa = request.LoaiYTa,
                    TrangThaiTaiKhoan = "hoat_dong",
                    NgayTao = DateTime.UtcNow,
                    NgayCapNhat = DateTime.UtcNow,
                    MaNhanVien = maNhanVien
                };

                _db.UserAccounts.Add(userAccount);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                var created = await _db.UserAccounts
                    .Include(u => u.NhanVienYTe)
                        .ThenInclude(nv => nv!.KhoaChuyenMon)
                    .FirstAsync(u => u.MaUser == maUser);

                var dto = MapToDto(created);
                await _realtime.BroadcastStaffChangedAsync(dto);
                return dto;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<AdminUserDto> UpdateUserAsync(string maUser, AdminUserUpdateRequest request)
        {
            var userAccount = await _db.UserAccounts
                .Include(u => u.NhanVienYTe)
                    .ThenInclude(nv => nv!.KhoaChuyenMon)
                .FirstOrDefaultAsync(u => u.MaUser == maUser)
                ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản {maUser}");

            if (userAccount.NhanVienYTe == null)
                throw new InvalidOperationException("Tài khoản không có thông tin nhân viên");

            if (request.VaiTro == "y_ta" && string.IsNullOrWhiteSpace(request.LoaiYTa))
                throw new ArgumentException("Y tá phải có loại y tá (hanhchinh, ls, cls)");

            userAccount.VaiTro = request.VaiTro;
            userAccount.LoaiYTa = request.LoaiYTa;
            userAccount.NgayCapNhat = DateTime.UtcNow;

            userAccount.NhanVienYTe.HoTen = request.HoTen;
            userAccount.NhanVienYTe.Email = request.Email;
            userAccount.NhanVienYTe.DienThoai = request.DienThoai;
            userAccount.NhanVienYTe.ChuyenMon = request.ChuyenMon;
            userAccount.NhanVienYTe.HocVi = request.HocVi;
            userAccount.NhanVienYTe.SoNamKinhNghiem = request.SoNamKinhNghiem;
            userAccount.NhanVienYTe.MaKhoa = request.MaKhoa;
            userAccount.NhanVienYTe.AnhDaiDien = request.AnhDaiDien;
            userAccount.NhanVienYTe.MoTa = request.MoTa;

            await _db.SaveChangesAsync();

            var dto = MapToDto(userAccount);
            await _realtime.BroadcastStaffChangedAsync(dto);
            return dto;
        }

        public async Task LockAccountAsync(string maUser, string adminUserId)
        {
            var adminCount = await _db.UserAccounts
                .CountAsync(u => u.VaiTro == "admin" && u.TrangThaiTaiKhoan == "hoat_dong");

            if (adminCount <= 1)
            {
                var userToLock = await _db.UserAccounts
                    .FirstOrDefaultAsync(u => u.MaUser == maUser);
                if (userToLock?.VaiTro == "admin")
                    throw new InvalidOperationException("Không thể khóa tài khoản admin cuối cùng");
            }

            var userAccount = await _db.UserAccounts
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.MaUser == maUser)
                ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản {maUser}");

            userAccount.TrangThaiTaiKhoan = "khoa";
            userAccount.NgayCapNhat = DateTime.UtcNow;

            var activeTokens = userAccount.RefreshTokens
                .Where(r => r.IsTrangThai && r.ThoiGianHetHan > DateTime.UtcNow)
                .ToList();

            foreach (var token in activeTokens)
            {
                token.IsTrangThai = false;
                token.ThoiGianThuHoi = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }

        public async Task UnlockAccountAsync(string maUser)
        {
            var userAccount = await _db.UserAccounts
                .FirstOrDefaultAsync(u => u.MaUser == maUser)
                ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản {maUser}");

            if (userAccount.TrangThaiTaiKhoan != "khoa")
                throw new InvalidOperationException("Tài khoản không ở trạng thái khóa");

            userAccount.TrangThaiTaiKhoan = "hoat_dong";
            userAccount.NgayCapNhat = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        public async Task<string> ResetPasswordAsync(string maUser)
        {
            var userAccount = await _db.UserAccounts
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.MaUser == maUser)
                ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản {maUser}");

            var newPassword = GenerateRandomPassword();

            userAccount.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
            userAccount.NgayCapNhat = DateTime.UtcNow;

            var activeTokens = userAccount.RefreshTokens
                .Where(r => r.IsTrangThai && r.ThoiGianHetHan > DateTime.UtcNow)
                .ToList();

            foreach (var token in activeTokens)
            {
                token.IsTrangThai = false;
                token.ThoiGianThuHoi = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            return newPassword;
        }

        private static AdminUserDto MapToDto(UserAccount u) => new()
        {
            MaUser = u.MaUser,
            MaNhanVien = u.MaNhanVien ?? string.Empty,
            TenDangNhap = u.TenDangNhap,
            HoTen = u.NhanVienYTe?.HoTen ?? string.Empty,
            VaiTro = u.VaiTro,
            ChucVu = u.VaiTro,
            LoaiYTa = u.LoaiYTa,
            Email = u.NhanVienYTe?.Email,
            DienThoai = u.NhanVienYTe?.DienThoai,
            ChuyenMon = u.NhanVienYTe?.ChuyenMon,
            HocVi = u.NhanVienYTe?.HocVi,
            SoNamKinhNghiem = u.NhanVienYTe?.SoNamKinhNghiem ?? 0,
            MaKhoa = u.NhanVienYTe?.MaKhoa,
            TenKhoa = u.NhanVienYTe?.KhoaChuyenMon?.TenKhoa,
            TrangThaiCongTac = u.NhanVienYTe?.TrangThaiCongTac ?? string.Empty,
            TrangThaiTaiKhoan = u.TrangThaiTaiKhoan,
            AnhDaiDien = u.NhanVienYTe?.AnhDaiDien,
            NgayTao = u.NgayTao,
            LanDangNhapCuoi = u.LanDangNhapCuoi
        };

        private static string GenerateStaffId(string vaiTro)
        {
            var prefix = vaiTro switch
            {
                "bac_si" => "BS",
                "y_ta" => "YT",
                "ky_thuat_vien" => "KTV",
                "admin" => "AD",
                _ => "NV"
            };
            return $"{prefix}{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(100, 999)}";
        }

        private static string GenerateRandomPassword()
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";
            const string all = uppercase + lowercase + digits + special;

            var password = new StringBuilder();
            password.Append(uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)]);
            password.Append(lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)]);
            password.Append(digits[RandomNumberGenerator.GetInt32(digits.Length)]);
            password.Append(special[RandomNumberGenerator.GetInt32(special.Length)]);

            for (int i = 4; i < 12; i++)
            {
                password.Append(all[RandomNumberGenerator.GetInt32(all.Length)]);
            }

            var chars = password.ToString().ToCharArray();
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars);
        }
    }
}
