using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using HealthCare.Realtime;
using Microsoft.EntityFrameworkCore;

namespace HealthCare.Services.Admin
{
    public class AdminService(DataContext db, IRealtimeService realtime) : IAdminService
    {
        private readonly DataContext _db = db;
        private readonly IRealtimeService _realtime = realtime;

        private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "dang_cong_tac", "tam_nghi", "nghi_viec"
        };

        public async Task<PagedResult<AdminUserDto>> GetUsersAsync(AdminUserFilter filter)
        {
            var query = _db.NhanVienYTes
                .Include(n => n.KhoaChuyenMon)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Q))
            {
                var q = filter.Q.Trim().ToLower();
                query = query.Where(n =>
                    n.HoTen.ToLower().Contains(q) ||
                    n.TenDangNhap.ToLower().Contains(q) ||
                    n.MaNhanVien.ToLower().Contains(q));
            }

            if (!string.IsNullOrWhiteSpace(filter.VaiTro))
                query = query.Where(n => n.VaiTro == filter.VaiTro);

            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
                query = query.Where(n => n.TrangThaiCongTac == filter.TrangThai);

            if (!string.IsNullOrWhiteSpace(filter.MaKhoa))
                query = query.Where(n => n.MaKhoa == filter.MaKhoa);

            if (!string.IsNullOrWhiteSpace(filter.LoaiYTa))
            {
                var loai = filter.LoaiYTa.Trim().ToLower();
                query = query.Where(n => n.LoaiYTa != null && n.LoaiYTa.ToLower() == loai);
            }
            var total = await query.CountAsync();

            var items = await query
                .OrderBy(n => n.HoTen)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(n => MapToDto(n))
                .ToListAsync();

            return new PagedResult<AdminUserDto>
            {
                Items = items,
                TotalItems = total,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<AdminUserDto> GetUserByIdAsync(string maNhanVien)
        {
            var staff = await _db.NhanVienYTes
                .Include(n => n.KhoaChuyenMon)
                .FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien)
                ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên {maNhanVien}");

            return MapToDto(staff);
        }

        public async Task<AdminUserDto> CreateUserAsync(AdminUserCreateRequest request)
        {
            // Kiểm tra trùng tên đăng nhập
            var exists = await _db.NhanVienYTes
                .AnyAsync(n => n.TenDangNhap == request.TenDangNhap);
            if (exists)
                throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

            var id = GenerateStaffId(request.VaiTro);

            var staff = new NhanVienYTe
            {
                MaNhanVien = id,
                TenDangNhap = request.TenDangNhap,
                MatKhauHash = BCrypt.Net.BCrypt.HashPassword(request.MatKhau),
                HoTen = request.HoTen,
                VaiTro = request.VaiTro,
                ChucVu = request.ChucVu,
                LoaiYTa = request.LoaiYTa,
                Email = request.Email,
                DienThoai = request.DienThoai,
                ChuyenMon = request.ChuyenMon,
                HocVi = request.HocVi,
                SoNamKinhNghiem = request.SoNamKinhNghiem,
                MaKhoa = request.MaKhoa,
                TrangThaiCongTac = "dang_cong_tac"
            };

            _db.NhanVienYTes.Add(staff);
            await _db.SaveChangesAsync();

            // Reload với Include
            var created = await _db.NhanVienYTes
                .Include(n => n.KhoaChuyenMon)
                .FirstAsync(n => n.MaNhanVien == id);

            var dto = MapToDto(created);
            await _realtime.BroadcastStaffChangedAsync(dto);
            return dto;
        }

        public async Task<AdminUserDto> UpdateUserAsync(string maNhanVien, AdminUserUpdateRequest request)
        {
            var staff = await _db.NhanVienYTes
                .Include(n => n.KhoaChuyenMon)
                .FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien)
                ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên {maNhanVien}");

            staff.HoTen = request.HoTen;
            staff.VaiTro = request.VaiTro;
            staff.ChucVu = request.ChucVu;
            staff.LoaiYTa = request.LoaiYTa;
            staff.Email = request.Email;
            staff.DienThoai = request.DienThoai;
            staff.ChuyenMon = request.ChuyenMon;
            staff.HocVi = request.HocVi;
            staff.SoNamKinhNghiem = request.SoNamKinhNghiem;
            staff.MaKhoa = request.MaKhoa;

            await _db.SaveChangesAsync();
            var dto = MapToDto(staff);
            await _realtime.BroadcastStaffChangedAsync(dto);
            return dto;
        }

        public async Task UpdateStatusAsync(string maNhanVien, AdminStatusUpdateRequest request)
        {
            if (!ValidStatuses.Contains(request.TrangThaiCongTac))
                throw new ArgumentException($"Trạng thái không hợp lệ: {request.TrangThaiCongTac}");

            var staff = await _db.NhanVienYTes
                .FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien)
                ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên {maNhanVien}");

            staff.TrangThaiCongTac = request.TrangThaiCongTac;
            await _db.SaveChangesAsync();

            // Reload with Include for TenKhoa
            var updated = await _db.NhanVienYTes
                .Include(n => n.KhoaChuyenMon)
                .FirstAsync(n => n.MaNhanVien == maNhanVien);
            await _realtime.BroadcastStaffChangedAsync(MapToDto(updated));
        }

        // ===== KHÓA / MỞ KHÓA TÀI KHOẢN =====
        public async Task LockUnlockAsync(string maNhanVien, AdminAccountStatusRequest request)
        {
            var validAccountStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "hoat_dong", "khoa"
            };

            if (!validAccountStatuses.Contains(request.TrangThai))
                throw new ArgumentException($"Trạng thái tài khoản không hợp lệ: {request.TrangThai}. Chỉ cho phép: hoat_dong, khoa");

            var staff = await _db.NhanVienYTes
                .FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien)
                ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên {maNhanVien}");

            staff.TrangThaiTaiKhoan = request.TrangThai;
            await _db.SaveChangesAsync();

            var updated = await _db.NhanVienYTes
                .Include(n => n.KhoaChuyenMon)
                .FirstAsync(n => n.MaNhanVien == maNhanVien);
            await _realtime.BroadcastStaffChangedAsync(MapToDto(updated));
        }

        public async Task ResetPasswordAsync(string maNhanVien, AdminResetPasswordRequest request)
        {
            var staff = await _db.NhanVienYTes
                .FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien)
                ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên {maNhanVien}");

            staff.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(request.MatKhauMoi);
            await _db.SaveChangesAsync();
        }

        // =============== Helpers ===============

        private static AdminUserDto MapToDto(NhanVienYTe n) => new()
        {
            MaNhanVien = n.MaNhanVien,
            TenDangNhap = n.TenDangNhap,
            HoTen = n.HoTen,
            VaiTro = n.VaiTro,
            ChucVu = n.ChucVu,
            LoaiYTa = n.LoaiYTa,
            Email = n.Email,
            DienThoai = n.DienThoai,
            ChuyenMon = n.ChuyenMon,
            HocVi = n.HocVi,
            SoNamKinhNghiem = n.SoNamKinhNghiem,
            MaKhoa = n.MaKhoa,
            TenKhoa = n.KhoaChuyenMon?.TenKhoa,
            TrangThaiCongTac = n.TrangThaiCongTac,
            TrangThaiTaiKhoan = n.TrangThaiTaiKhoan,
            AnhDaiDien = n.AnhDaiDien
        };

        private string GenerateStaffId(string vaiTro)
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
    }
}
