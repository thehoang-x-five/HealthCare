using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthCare.Services
{
    public class MasterDataService(DataContext db) : IMasterDataService
    {
        private readonly DataContext _db = db;

        // ========== KHOA ==========

        public async Task<IReadOnlyList<DepartmentDto>> LayDanhSachKhoaAsync()
        {
            var list = await _db.KhoaChuyenMons
                .AsNoTracking()
                .OrderBy(k => k.TenKhoa)
                .ToListAsync();

            return [.. list
                .Select(k => new DepartmentDto
                {
                    MaKhoa = k.MaKhoa,
                    TenKhoa = k.TenKhoa,
                    TrangThai = k.TrangThai
                })];
        }

        public async Task<PagedResult<DepartmentDto>> TimKiemKhoaAsync(DepartmentSearchFilter filter)
        {
            var query = _db.KhoaChuyenMons.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim();
                query = query.Where(k =>
                    k.MaKhoa.Contains(keyword) ||
                    k.TenKhoa.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
            {
                var tt = filter.TrangThai.Trim();
                query = query.Where(k => k.TrangThai == tt);
            }

            var sortBy = (filter.SortBy ?? "TenKhoa").ToLowerInvariant();
            var sortDir = (filter.SortDirection ?? "asc").ToLowerInvariant();

            query = (sortBy, sortDir) switch
            {
                ("makhoa", "desc") => query.OrderByDescending(k => k.MaKhoa),
                ("makhoa", _) => query.OrderBy(k => k.MaKhoa),
                ("tenkhoa", "desc") => query.OrderByDescending(k => k.TenKhoa),
                ("tenkhoa", _) => query.OrderBy(k => k.TenKhoa),
                _ when sortDir == "desc" => query.OrderByDescending(k => k.TenKhoa),
                _ => query.OrderBy(k => k.TenKhoa)
            };

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 50 : filter.PageSize;

            var totalItems = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = items.Select(k => new DepartmentDto
            {
                MaKhoa = k.MaKhoa,
                TenKhoa = k.TenKhoa,
                TrangThai = k.TrangThai
            }).ToList();

            return new PagedResult<DepartmentDto>
            {
                Items = dtos,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        // ========== OVERVIEW ==========
        public async Task<IReadOnlyList<DepartmentOverviewDto>> LayTongQuanKhoaAsync(
     DateTime? ngay,
     TimeSpan? gio,
     string? MaDV)
        {
            List<LichTruc> lichTrucs;

            if (!string.IsNullOrWhiteSpace(MaDV))
            {
                // ============================
                // CASE 1: Có MaDV
                // -> Dùng NGÀY + GIỜ HIỆN TẠI
                // ============================
                var now = DateTime.Now;
                var currentDate = now.Date;
                var currentTime = now.TimeOfDay;

                lichTrucs = await _db.LichTrucs
                    .AsNoTracking()
                    .Include(l => l.Phong)
                        .ThenInclude(p => p.KhoaChuyenMon)
                    .Include(l => l.Phong)
                        .ThenInclude(p => p.BacSiPhuTrach)
                    .Where(l =>
                        l.Ngay.Date == currentDate &&
                        !l.NghiTruc &&
                        l.GioBatDau <= currentTime &&
                        l.GioKetThuc > currentTime &&
                        l.Phong != null &&
                        l.Phong.DichVuYTes.Any(dv => dv.MaDichVu == MaDV))
                    .ToListAsync();
            }
            else
            {
                // ============================
                // CASE 2: Không có MaDV
                // -> Dùng NGÀY + GIỜ từ INPUT (code cũ)
                // ============================
                var date = ngay?.Date;

                lichTrucs = await _db.LichTrucs
                    .AsNoTracking()
                    .Include(l => l.Phong)
                        .ThenInclude(p => p.KhoaChuyenMon)
                    .Include(l => l.Phong)
                        .ThenInclude(p => p.BacSiPhuTrach)
                    .Where(l =>
                        l.Ngay.Date == date &&
                        !l.NghiTruc &&
                        l.GioBatDau <= gio &&
                        l.GioKetThuc > gio)
                    .ToListAsync();
            }

            if (lichTrucs.Count == 0)
                return Array.Empty<DepartmentOverviewDto>();

            // Lấy list mã khoa từ các phòng có lịch trực (đã lọc theo MaDV nếu có)
            var nhomTheoKhoa = lichTrucs
                .Where(l => l.Phong != null && l.Phong.KhoaChuyenMon != null)
                .GroupBy(l => l.Phong.MaKhoa);

            // Lấy danh sách BS đang "dang_cong_tac" để tính SoBacSiDangCongTac
            var activeDoctorIds = await _db.NhanVienYTes
                .AsNoTracking()
                .Where(nv => nv.VaiTro == "bac_si" && nv.TrangThaiCongTac == "dang_cong_tac")
                .Select(nv => nv.MaNhanVien)
                .ToListAsync();

            var activeDoctorSet = new HashSet<string>(activeDoctorIds);

            var result = new List<DepartmentOverviewDto>();

            foreach (var group in nhomTheoKhoa)
            {
                var maKhoa = group.Key;
                var firstPhong = group.First().Phong;

                if (firstPhong?.KhoaChuyenMon == null)
                    continue;

                var tenKhoa = firstPhong.KhoaChuyenMon.TenKhoa;

                // Phòng trong khoa này (distinct theo MaPhong)
                var phongDistinct = group
                    .Select(l => l.Phong!)
                    .GroupBy(p => p.MaPhong)
                    .Select(g => g.First())
                    .ToList();

                var tongSoPhong = phongDistinct.Count;

                // Bác sĩ gán cho các phòng này & đang công tác
                var bacSiIds = phongDistinct
                    .Select(p => p.MaBacSiPhuTrach)
                    .Where(id => id != null && activeDoctorSet.Contains(id))
                    .Distinct()
                    .Cast<string>()
                    .ToList();

                var dto = new DepartmentOverviewDto
                {
                    MaKhoa = maKhoa,
                    TenKhoa = tenKhoa,
                    SoPhongKham = 0,     // sau này muốn tách theo LoaiPhong thì mình chỉnh tiếp
                    SoPhongCls = 0,
                    TongSoPhong = tongSoPhong,
                    SoBacSiDangCongTac = bacSiIds.Count
                };

                result.Add(dto);
            }

            return result;
        }


        public async Task<IReadOnlyList<StaffOverviewDto>> LayTongQuanNhanSuAsync(
     string maKhoa,
     DateTime? ngay,
     TimeSpan? gio)
        {
            if (string.IsNullOrWhiteSpace(maKhoa))
                throw new ArgumentException("MaKhoa là bắt buộc", nameof(maKhoa));

            // Nếu không truyền -> dùng ngày + giờ hiện tại
            var date = (ngay?.Date) ?? DateTime.Now.Date;
            var time = gio ?? DateTime.Now.TimeOfDay;

            // Lịch trực đang hoạt động trong khoa + khung giờ
            var lichTrucs = await _db.LichTrucs
                .AsNoTracking()
                .Include(l => l.Phong)
                .Where(l => l.Ngay.Date == date
                            && !l.NghiTruc
                            && l.GioBatDau <= time   // dùng time đã chuẩn hóa
                            && l.GioKetThuc > time   // dùng time đã chuẩn hóa
                            && l.Phong.MaKhoa == maKhoa)
                .ToListAsync();

            if (lichTrucs.Count == 0)
                return Array.Empty<StaffOverviewDto>();

            // Phòng & bác sĩ phụ trách
            var phongWithDoctor = await _db.Phongs
                .AsNoTracking()
                .Where(p => p.MaKhoa == maKhoa
                            && p.MaBacSiPhuTrach != null)
                .ToListAsync();

            var phongIds = lichTrucs
                .Select(l => l.MaPhong)
                .Distinct()
                .ToHashSet();

            // Chỉ giữ các phòng có lịch trực trong khung giờ
            var phongHoatDong = phongWithDoctor
                .Where(p => phongIds.Contains(p.MaPhong))
                .ToList();

            if (phongHoatDong.Count == 0)
                return Array.Empty<StaffOverviewDto>();

            var doctorIds = phongHoatDong
                .Select(p => p.MaBacSiPhuTrach!)
                .Distinct()
                .ToList();

            var doctors = await _db.NhanVienYTes
                .AsNoTracking()
                .Where(nv => doctorIds.Contains(nv.MaNhanVien)
                             && nv.VaiTro == "bac_si")
                .ToListAsync();

            if (doctors.Count == 0)
                return Array.Empty<StaffOverviewDto>();

            // Map phòng -> bác sĩ
            var phongToDoctor = phongHoatDong
                .ToDictionary(p => p.MaPhong, p => p.MaBacSiPhuTrach!);

            // Map lịch trực -> bác sĩ
            var lichTrucToDoctor = lichTrucs
                .Where(l => phongToDoctor.ContainsKey(l.MaPhong))
                .ToDictionary(l => l.MaLichTruc, l => phongToDoctor[l.MaPhong]);

            var maLichTrucAll = lichTrucToDoctor.Keys.ToList();

            // Lịch hẹn trong ngày trên các lịch trực này
            var lichHenHomNay = await _db.LichHenKhams
                .AsNoTracking()
                .Where(h => h.CoHieuLuc
                            && h.NgayHen.Date == date
                            && h.TrangThai != "da_huy"
                            && maLichTrucAll.Contains(h.MaLichTruc))
                .ToListAsync();

            // Count theo bác sĩ
            var countByDoctor = new Dictionary<string, (int Total, int Waiting)>();

            foreach (var h in lichHenHomNay)
            {
                if (!lichTrucToDoctor.TryGetValue(h.MaLichTruc, out var doctorId))
                    continue;

                if (!countByDoctor.TryGetValue(doctorId, out var c))
                    c = (0, 0);

                c.Total++;
                if (h.TrangThai == "dang_cho")
                    c.Waiting++;

                countByDoctor[doctorId] = c;
            }

            var result = new List<StaffOverviewDto>();

            foreach (var bs in doctors)
            {
                countByDoctor.TryGetValue(bs.MaNhanVien, out var c);

                result.Add(new StaffOverviewDto
                {
                    VaiTro = "bac_si",
                    TenBS = bs.HoTen,
                    SoLichHenHomNay = c.Total,
                    SoBenhNhanDangCho = c.Waiting
                });
            }

            return result;
        }


        public async Task<IReadOnlyList<ServiceOverviewDto>> LayTongQuanDichVuAsync(
      string? maPhong,
      string? loaiDichVu)
        {
            // Base query
            var query = _db.DichVuYTes
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(maPhong))
            {
                // CASE 1: Có maPhong -> bỏ qua loaiDichVu, lọc theo phòng như hiện tại
                query = query.Where(d => d.MaPhongThucHien == maPhong
                                         /* && d.TrangThai == "hoat_dong" */);
            }
            else
            {
                // CASE 2: Không có maPhong -> lọc theo loại dịch vụ
                var loai = !string.IsNullOrWhiteSpace(loaiDichVu)
                    ? loaiDichVu
                    : "can_lam_sang"; // mặc định

                query = query.Where(d => d.LoaiDichVu == loai
                                         /* && d.TrangThai == "hoat_dong" */);
            }

            var dichVus = await query.ToListAsync();

            if (dichVus.Count == 0)
                return Array.Empty<ServiceOverviewDto>();

            var result = dichVus.Select(d => new ServiceOverviewDto
            {
                LoaiDichVu = d.LoaiDichVu,
                MaDV = d.MaDichVu,
                TenDV = d.TenDichVu
            }).ToList();

            return result;
        }

        // ========== PHÒNG ==========

        public async Task<IReadOnlyList<RoomDto>> LayDanhSachPhongAsync(
            string? maKhoa = null,
            string? loaiPhong = null)
        {
            var query = _db.Phongs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(maKhoa))
            {
                var mk = maKhoa.Trim();
                query = query.Where(p => p.MaKhoa == mk);
            }

            if (!string.IsNullOrWhiteSpace(loaiPhong))
            {
                var lp = loaiPhong.Trim();
                query = query.Where(p => p.LoaiPhong == lp);
            }

            query = query.OrderBy(p => p.TenPhong);

            var list = await query.ToListAsync();

            return list.Select(MapRoomToDto).ToList();
        }

        public async Task<PagedResult<RoomDto>> TimKiemPhongAsync(RoomSearchFilter filter)
        {
            var query = _db.Phongs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();
                query = query.Where(p =>
                    p.MaPhong.Contains(kw) ||
                    p.TenPhong.Contains(kw) ||
                    (p.ViTri != null && p.ViTri.Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(filter.MaKhoa))
            {
                var mk = filter.MaKhoa.Trim();
                query = query.Where(p => p.MaKhoa == mk);
            }

            if (!string.IsNullOrWhiteSpace(filter.LoaiPhong))
            {
                var lp = filter.LoaiPhong.Trim();
                query = query.Where(p => p.LoaiPhong == lp);
            }

            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
            {
                var tt = filter.TrangThai.Trim();
                query = query.Where(p => p.TrangThai == tt);
            }
            if (!string.IsNullOrWhiteSpace(filter.MaBacSiPhuTrach))
            {
                var mns = filter.MaBacSiPhuTrach.Trim();
                query = query.Where(p => p.MaBacSiPhuTrach == mns);
            }
            var sortBy = (filter.SortBy ?? "TenPhong").ToLowerInvariant();
            var sortDir = (filter.SortDirection ?? "asc").ToLowerInvariant();

            query = (sortBy, sortDir) switch
            {
                ("maphong", "desc") => query.OrderByDescending(p => p.MaPhong),
                ("maphong", _) => query.OrderBy(p => p.MaPhong),
                ("tenphong", "desc") => query.OrderByDescending(p => p.TenPhong),
                ("tenphong", _) => query.OrderBy(p => p.TenPhong),
                _ when sortDir == "desc" => query.OrderByDescending(p => p.TenPhong),
                _ => query.OrderBy(p => p.TenPhong)
            };

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 50 : filter.PageSize;

            var totalItems = await query.CountAsync();

            var list = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = list.Select(MapRoomToDto).ToList();

            return new PagedResult<RoomDto>
            {
                Items = dtos,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }
        // ========== PHÒNG — CARD DTO ==========

        public async Task<PagedResult<RoomCardDto>> TimKiemPhongCardAsync(RoomSearchFilter filter)
        {
            var query = _db.Phongs.AsNoTracking().AsQueryable();

            // ====== Lọc theo từ khóa / khoa / loại phòng / trạng thái ======
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();
                query = query.Where(p =>
                    p.MaPhong.Contains(kw) ||
                    p.TenPhong.Contains(kw) ||
                    (p.ViTri != null && p.ViTri.Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(filter.MaKhoa))
            {
                var mk = filter.MaKhoa.Trim();
                query = query.Where(p => p.MaKhoa == mk);
            }

            if (!string.IsNullOrWhiteSpace(filter.LoaiPhong))
            {
                var lp = filter.LoaiPhong.Trim();
                query = query.Where(p => p.LoaiPhong == lp);
            }

            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
            {
                var tt = filter.TrangThai.Trim();
                query = query.Where(p => p.TrangThai == tt);
            }

            // ====== Sắp xếp ======
            var sortBy = (filter.SortBy ?? "TenPhong").ToLowerInvariant();
            var sortDir = (filter.SortDirection ?? "asc").ToLowerInvariant();

            query = (sortBy, sortDir) switch
            {
                ("maphong", "desc") => query.OrderByDescending(p => p.MaPhong),
                ("maphong", _) => query.OrderBy(p => p.MaPhong),
                ("tenphong", "desc") => query.OrderByDescending(p => p.TenPhong),
                ("tenphong", _) => query.OrderBy(p => p.TenPhong),
                _ when sortDir == "desc" => query.OrderByDescending(p => p.TenPhong),
                _ => query.OrderBy(p => p.TenPhong)
            };

            // ====== Phân trang ======
            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 50 : filter.PageSize;

            var totalItems = await query.CountAsync();

            var list = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (list.Count == 0)
            {
                return new PagedResult<RoomCardDto>
                {
                    Items = [],
                    TotalItems = 0,
                    Page = page,
                    PageSize = pageSize
                };
            }

            // ====== Chuẩn bị dữ liệu liên quan ======
            var maPhongs = list.Select(p => p.MaPhong).Distinct().ToList();
            var maKhoas = list.Select(p => p.MaKhoa).Where(mk => mk != null).Distinct().ToList();
            var maBacSis = list
                .Select(p => p.MaBacSiPhuTrach)
                .Where(m => !string.IsNullOrEmpty(m))
                .Distinct()
                .ToList();

            // Tên khoa
            var dictKhoa = await _db.KhoaChuyenMons
                .AsNoTracking()
                .Where(k => maKhoas.Contains(k.MaKhoa))
                .ToDictionaryAsync(k => k.MaKhoa, k => k.TenKhoa);

            // Tên bác sĩ phụ trách
            var dictBacSi = await _db.NhanVienYTes
                .AsNoTracking()
                .Where(nv => maBacSis.Contains(nv.MaNhanVien))
                .ToDictionaryAsync(nv => nv.MaNhanVien, nv => nv.HoTen);

            // ====== Hàng chờ (Đang chờ khám – lấy theo Hàng đợi) ======
            // Chỉnh lại các trạng thái này cho khớp DB thực tế
            var waitingQueueStatuses = new[] { "cho_goi" };
            var cancelledQueueStatuses = new[] { "huy" };

            var hangDois = await _db.HangDois
                .AsNoTracking()
                .Where(h => maPhongs.Contains(h.MaPhong)
                    && !cancelledQueueStatuses.Contains(h.TrangThai))
                .ToListAsync();

            var queueLookup = hangDois
                .GroupBy(h => h.MaPhong)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var dangCho = g.Count(x => waitingQueueStatuses.Contains(x.TrangThai));
                        return new { DangCho = dangCho };
                    });

            // ====== Lượt khám bệnh (Đã hoàn tất – lấy theo Lượt khám bệnh) ======
            var today = DateTime.Today.Date;
            var doneVisitStatuses = new[] { "da_kham", "hoan_tat" };

            var luotKhams = await _db.LuotKhamBenhs
                .AsNoTracking()
                .Include(l => l.HangDoi)
                .Where(l =>
                    l.HangDoi != null &&
                    maPhongs.Contains(l.HangDoi.MaPhong) &&
                    l.ThoiGianBatDau.Date == today)
                .ToListAsync();

            var visitLookup = luotKhams
                .GroupBy(l => l.HangDoi.MaPhong)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var tongLuot = g.Count();
                        var daHoanThanh = g.Count(l => doneVisitStatuses.Contains(l.TrangThai));
                        return new
                        {
                            TongLuot = tongLuot,
                            DaHoanThanh = daHoanThanh
                        };
                    });

            // ====== Map sang DTO ======
            var dtos = list.Select(p =>
            {
                dictKhoa.TryGetValue(p.MaKhoa, out var tenKhoa);

                string? tenBacSi = null;
                if (!string.IsNullOrEmpty(p.MaBacSiPhuTrach) &&
                    dictBacSi.TryGetValue(p.MaBacSiPhuTrach!, out var tenBs))
                {
                    tenBacSi = tenBs;
                }

                queueLookup.TryGetValue(p.MaPhong, out var qInfo);
                visitLookup.TryGetValue(p.MaPhong, out var vInfo);

                var dangCho = qInfo?.DangCho ?? 0;
                var daHoanThanh = vInfo?.DaHoanThanh ?? 0;
                var tongHomNay = (vInfo?.TongLuot ?? 0) + dangCho;

                return new RoomCardDto
                {
                    MaPhong = p.MaPhong,
                    TenPhong = p.TenPhong,
                    TenKhoa = tenKhoa,
                    LoaiPhong = p.LoaiPhong,
                    TrangThai = p.TrangThai,

                    MaBacSiPhuTrach = p.MaBacSiPhuTrach,
                    TenBacSiPhuTrach = tenBacSi,

                    DienThoai = p.DienThoai,
                    Email = p.Email,

                    DangCho = dangCho,
                    DaHoanThanh = daHoanThanh,
                    TongHomNay = tongHomNay
                };
            }).ToList();

            return new PagedResult<RoomCardDto>
            {
                Items = dtos,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }


        // ========== PHÒNG — CHI TIẾT ==========

        public async Task<RoomDetailDto?> LayChiTietPhongAsync(string maPhong)
        {
            if (string.IsNullOrWhiteSpace(maPhong))
            {
                return null;
            }

            // Lấy thông tin phòng
            var room = await _db.Phongs
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.MaPhong == maPhong);

            if (room == null)
            {
                return null;
            }

            // Tên khoa
            var tenKhoa = await _db.KhoaChuyenMons
                .AsNoTracking()
                .Where(k => k.MaKhoa == room.MaKhoa)
                .Select(k => k.TenKhoa)
                .FirstOrDefaultAsync();

            // Bác sĩ phụ trách
            string? tenBacSi = null;
            if (!string.IsNullOrEmpty(room.MaBacSiPhuTrach))
            {
                tenBacSi = await _db.NhanVienYTes
                    .AsNoTracking()
                    .Where(nv => nv.MaNhanVien == room.MaBacSiPhuTrach)
                    .Select(nv => nv.HoTen)
                    .FirstOrDefaultAsync();
            }

            // ====== Hàng chờ của phòng này (Đang chờ khám) ======
            var waitingQueueStatuses = new[] { "cho_goi" };
            var cancelledQueueStatuses = new[] { "huy" };

            var hangDois = await _db.HangDois
                .AsNoTracking()
                .Where(h => h.MaPhong == room.MaPhong
                    && !cancelledQueueStatuses.Contains(h.TrangThai))
                .ToListAsync();

            var dangCho = hangDois.Count(h => waitingQueueStatuses.Contains(h.TrangThai));

            // ====== Lượt khám bệnh trong ngày của phòng này ======
            var today = DateTime.Today.Date;
            var doneVisitStatuses = new[] { "da_kham", "hoan_tat" };

            var luotKhams = await _db.LuotKhamBenhs
                .AsNoTracking()
                .Include(l => l.HangDoi)
                .Where(l =>
                    l.HangDoi != null &&
                    l.HangDoi.MaPhong == room.MaPhong &&
                    l.ThoiGianBatDau.Date == today)
                .ToListAsync();

            var tongLuot = luotKhams.Count;
            var daHoanThanh = luotKhams.Count(l => doneVisitStatuses.Contains(l.TrangThai));
            var tongHomNay = tongLuot + dangCho;

            // ====== Dịch vụ thực hiện tại phòng ======
            var dichVu = await _db.DichVuYTes
                .AsNoTracking()
                .Where(d => d.MaPhongThucHien == room.MaPhong)
                .Select(d => new RoomServiceItemDto
                {
                    MaDichVu = d.MaDichVu,
                    TenDichVu = d.TenDichVu,
                    LoaiDichVu = d.LoaiDichVu,
                    // Hiện entity DichVuYTe chưa có thời lượng → tạm thời 0
                    ThoiGianPhut = d.ThoiGianDuKienPhut,
                    DonGia = d.DonGia
                })
                .ToListAsync();

            // ====== Map sang DTO chi tiết ======
            var detail = new RoomDetailDto
            {
                MaPhong = room.MaPhong,
                TenPhong = room.TenPhong,
                TenKhoa = tenKhoa,
                LoaiPhong = room.LoaiPhong,
                TrangThai = room.TrangThai,

                KhuVuc = room.ViTri,
                DienThoai = room.DienThoai,
                Email = room.Email,
                GioMoCua = room.GioMoCua,
                GioDongCua = room.GioDongCua,

                MaBacSiPhuTrach = room.MaBacSiPhuTrach,
                TenBacSiPhuTrach = tenBacSi,

                ThietBi = room.ThietBi ?? new List<string>(),

                DangCho = dangCho,
                DaHoanThanh = daHoanThanh,
                TongHomNay = tongHomNay,
                SucChuaNgay = room.SucChua,

                DichVuTaiPhong = dichVu
            };

            return detail;
        }


        private static RoomDto MapRoomToDto(Phong p)
        {
            return new RoomDto
            {
                MaPhong = p.MaPhong,
                TenPhong = p.TenPhong,
                MaKhoa = p.MaKhoa,
                LoaiPhong = p.LoaiPhong,
                SucChua = p.SucChua,
                ViTri = p.ViTri,
                Email = p.Email,
                DienThoai = p.DienThoai,
                GioMoCua = p.GioMoCua,
                GioDongCua = p.GioDongCua,
                ThietBi = p.ThietBi ?? new List<string>(),
                TrangThai = p.TrangThai,
                MaBacSiPhuTrach = p.MaBacSiPhuTrach
            };
        }

        // ========== PHÒNG — LỊCH ĐIỀU DƯỠNG TUẦN ==========

        public async Task<RoomDutyWeekDto?> LayLichDieuDuongPhongTuanAsync(string maPhong, DateTime? today = null)
        {
            if (string.IsNullOrWhiteSpace(maPhong))
                throw new ArgumentNullException(nameof(maPhong));

            var room = await _db.Phongs
                .AsNoTracking()
                .Include(p => p.KhoaChuyenMon)
                .FirstOrDefaultAsync(p => p.MaPhong == maPhong);

            if (room == null) return null;

            var current = (today ?? DateTime.Today).Date;
            var startOfWeek = GetStartOfWeek(current);
            var endOfWeekExclusive = startOfWeek.AddDays(7);

            var lichTrucs = await _db.LichTrucs
                .AsNoTracking()
                .Where(l => l.MaPhong == maPhong
                            && l.Ngay >= startOfWeek
                            && l.Ngay < endOfWeekExclusive)
                .ToListAsync();

            var maYTas = lichTrucs
                .Select(l => l.MaYTaTruc)
                .Where(m => !string.IsNullOrEmpty(m))
                .Distinct()
                .ToList();

            var dictYTa = maYTas.Count == 0
                ? new Dictionary<string, string>()
                : await _db.NhanVienYTes
                    .AsNoTracking()
                    .Where(nv => maYTas.Contains(nv.MaNhanVien))
                    .ToDictionaryAsync(nv => nv.MaNhanVien, nv => nv.HoTen);

            string? tenBacSi = null;
            if (!string.IsNullOrEmpty(room.MaBacSiPhuTrach))
            {
                tenBacSi = await _db.NhanVienYTes
                    .AsNoTracking()
                    .Where(nv => nv.MaNhanVien == room.MaBacSiPhuTrach)
                    .Select(nv => nv.HoTen)
                    .FirstOrDefaultAsync();
            }

            var items = lichTrucs
                .OrderBy(l => l.Ngay)
                .ThenBy(l => l.CaTruc)
                .Select(l =>
                {
                    dictYTa.TryGetValue(l.MaYTaTruc, out var tenYTa);

                    return new RoomDutyDayDto
                    {
                        Thu = GetThuShortName(l.Ngay),
                        MaYTa = l.MaYTaTruc,
                        TenYTa = tenYTa,
                        CaTruc = l.CaTruc,
                        GioBatDau = l.GioBatDau,
                        GioKetThuc = l.GioKetThuc
                    };
                })
                .ToList();

            return new RoomDutyWeekDto
            {
                MaPhong = room.MaPhong,
                TenPhong = room.TenPhong,
                TenKhoa = room.KhoaChuyenMon?.TenKhoa,
                MaBacSiPhuTrach = room.MaBacSiPhuTrach,
                TenBacSiPhuTrach = tenBacSi,
                Today = current,
                LichDieuDuongTuan = items
            };
        }


        // ========== NHÂN SỰ ==========

        public async Task<IReadOnlyList<StaffDto>> LayDanhSachNhanSuAsync(
            string? maKhoa = null,
            string? vaiTro = null)
        {
            var query = _db.NhanVienYTes
                .AsNoTracking()
                .Include(nv => nv.KhoaChuyenMon)
                .Include(nv => nv.PhongsPhuTrach)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(maKhoa))
            {
                var mk = maKhoa.Trim();
                query = query.Where(nv => nv.MaKhoa == mk);
            }

            if (!string.IsNullOrWhiteSpace(vaiTro))
            {
                var role = vaiTro.Trim();
                query = query.Where(nv => nv.VaiTro == role);
            }

            query = query.OrderBy(nv => nv.HoTen);

            var list = await query.ToListAsync();

            return list.Select(MapStaffToDto).ToList();
        }

        public async Task<PagedResult<StaffDto>> TimKiemNhanSuAsync(StaffSearchFilter filter)
        {
            var query = _db.NhanVienYTes
                .AsNoTracking()
                .Include(nv => nv.KhoaChuyenMon)
                .Include(nv => nv.PhongsPhuTrach)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();
                query = query.Where(nv =>
                    nv.MaNhanVien.Contains(kw) ||
                    nv.HoTen.Contains(kw) ||
                    (nv.Email != null && nv.Email.Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(filter.MaKhoa))
            {
                var mk = filter.MaKhoa.Trim();
                query = query.Where(nv => nv.MaKhoa == mk);
            }

            if (!string.IsNullOrWhiteSpace(filter.VaiTro))
            {
                var role = filter.VaiTro.Trim();
                query = query.Where(nv => nv.VaiTro == role);
            }

            if (!string.IsNullOrWhiteSpace(filter.TrangThaiCongTac))
            {
                var tt = filter.TrangThaiCongTac.Trim();
                query = query.Where(nv => nv.TrangThaiCongTac == tt);
            }

            if (!string.IsNullOrWhiteSpace(filter.MaPhongPhuTrach))
            {
                var mp = filter.MaPhongPhuTrach.Trim();
                query = query.Where(nv => nv.PhongsPhuTrach != null &&
                                          nv.PhongsPhuTrach.MaPhong == mp);
            }

            var sortBy = (filter.SortBy ?? "HoTen").ToLowerInvariant();
            var sortDir = (filter.SortDirection ?? "asc").ToLowerInvariant();

            query = (sortBy, sortDir) switch
            {
                ("manhanvien", "desc") => query.OrderByDescending(nv => nv.MaNhanVien),
                ("manhanvien", _) => query.OrderBy(nv => nv.MaNhanVien),
                ("hoten", "desc") => query.OrderByDescending(nv => nv.HoTen),
                ("hoten", _) => query.OrderBy(nv => nv.HoTen),
                _ when sortDir == "desc" => query.OrderByDescending(nv => nv.HoTen),
                _ => query.OrderBy(nv => nv.HoTen)
            };

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 50 : filter.PageSize;

            var totalItems = await query.CountAsync();

            var list = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = list.Select(MapStaffToDto).ToList();

            return new PagedResult<StaffDto>
            {
                Items = dtos,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        private static StaffDto MapStaffToDto(NhanVienYTe nv)
        {
            return new StaffDto
            {
                MaNhanVien = nv.MaNhanVien,
                HoTen = nv.HoTen,
                VaiTro = nv.VaiTro,                    // bac_si / y_ta
                MaKhoa = nv.MaKhoa,
                TenKhoa = nv.KhoaChuyenMon?.TenKhoa,

                Email = nv.Email,
                DienThoai = nv.DienThoai,

                TrangThaiCongTac = nv.TrangThaiCongTac,
                SoNamKinhNghiem = nv.SoNamKinhNghiem,

                HocVi = nv.HocVi,
                ChuyenKhoa = nv.ChuyenMon,            // entity: ChuyenMon
                AnhDaiDien = nv.AnhDaiDien,

                MaPhongPhuTrach = nv.PhongsPhuTrach?.MaPhong,
                TenPhongPhuTrach = nv.PhongsPhuTrach?.TenPhong,

                LoaiYTa = nv.VaiTro == "y_ta" ? nv.LoaiYTa : null
            };
        }

        // ========== NHÂN SỰ — CARD / CHI TIẾT / LỊCH TRỰC TUẦN ==========
        public async Task<PagedResult<StaffCardDto>> TimKiemNhanSuCardAsync(StaffSearchFilter filter)
        {
            // Dùng lại filter chung
            var pagedStaff = await TimKiemNhanSuAsync(filter);
            var staffItems = pagedStaff.Items.ToList();

            if (staffItems.Count == 0)
            {
                return new PagedResult<StaffCardDto>
                {
                    Items = Array.Empty<StaffCardDto>(),
                    TotalItems = 0,
                    Page = pagedStaff.Page,
                    PageSize = pagedStaff.PageSize
                };
            }

            var today = DateTime.Today.Date;
            var startOfWeek = GetStartOfWeek(today);
            var endOfWeekExclusive = startOfWeek.AddDays(7);

            var nurseIds = staffItems.Where(s => s.VaiTro == "y_ta")
                                     .Select(s => s.MaNhanVien)
                                     .ToList();

            var doctorIds = staffItems.Where(s => s.VaiTro == "bac_si")
                                      .Select(s => s.MaNhanVien)
                                      .ToList();

            // ================== Y TÁ: ca trực tuần & phòng hôm nay ==================
            var soCaTrucTuanByYTa = new Dictionary<string, int>();
            var phongHomNayByYTa = new Dictionary<string, (string? MaPhong, string? TenPhong)>();

            if (nurseIds.Count > 0)
            {
                var lichTuanYTa = await _db.LichTrucs
                    .AsNoTracking()
                    .Where(l => nurseIds.Contains(l.MaYTaTruc)
                                && l.Ngay >= startOfWeek
                                && l.Ngay < endOfWeekExclusive
                                && !l.NghiTruc)
                    .ToListAsync();

                soCaTrucTuanByYTa = lichTuanYTa
                    .GroupBy(l => l.MaYTaTruc)
                    .ToDictionary(g => g.Key, g => g.Count());

                var lichHomNayYTa = lichTuanYTa
                    .Where(l => l.Ngay.Date == today)
                    .ToList();

                var maPhongsHomNay = lichHomNayYTa
                    .Select(l => l.MaPhong)
                    .Distinct()
                    .ToList();

                var dictPhong = maPhongsHomNay.Count == 0
                    ? new Dictionary<string, string>()
                    : await _db.Phongs
                        .AsNoTracking()
                        .Where(p => maPhongsHomNay.Contains(p.MaPhong))
                        .ToDictionaryAsync(p => p.MaPhong, p => p.TenPhong);
                phongHomNayByYTa = lichHomNayYTa
    .GroupBy(l => l.MaYTaTruc)
    .ToDictionary(
        g => g.Key,
        g => {
            var first = g.FirstOrDefault();
            return first == null
                ? (null, null)
                : (first.MaPhong, dictPhong.TryGetValue(first.MaPhong, out var tenPhong) ? tenPhong : null);
        });

            }

            // ================== BÁC SĨ: phòng phụ trách & số lịch hẹn hôm nay ==================

            // Lấy phòng mà BS phụ trách
            var phongBacSis = doctorIds.Count == 0
                ? new List<Phong>()
                : await _db.Phongs
                    .AsNoTracking()
                    .Where(p => p.MaBacSiPhuTrach != null &&
                                doctorIds.Contains(p.MaBacSiPhuTrach))
                    .ToListAsync();

            var phongByBacSi = phongBacSis
                .GroupBy(p => p.MaBacSiPhuTrach!)
                .ToDictionary(g => g.Key, g => g.First());

            // Lịch trực hôm nay của các phòng có BS phụ trách
            var maPhongBs = phongBacSis.Select(p => p.MaPhong).Distinct().ToList();

            var lichTrucPhongHomNay = maPhongBs.Count == 0
                ? new List<LichTruc>()
                : await _db.LichTrucs
                    .AsNoTracking()
                    .Where(l => maPhongBs.Contains(l.MaPhong)
                                && l.Ngay >= today
                                && l.Ngay < today.AddDays(1)
                                && !l.NghiTruc)
                    .ToListAsync();

            var maLichTrucPhongHomNay = lichTrucPhongHomNay
                .Select(l => l.MaLichTruc)
                .Distinct()
                .ToList();

            var phongByLichTruc = lichTrucPhongHomNay
                .ToDictionary(l => l.MaLichTruc, l => l.MaPhong);

            // Lịch hẹn hôm nay gắn với các lịch trực đó
            var lichHenHomNay = maLichTrucPhongHomNay.Count == 0
                ? new List<LichHenKham>()
                : await _db.LichHenKhams
                    .AsNoTracking()
                    .Where(h => h.CoHieuLuc
                                && h.TrangThai != "da_huy"
                                && h.NgayHen >= today
                                && h.NgayHen < today.AddDays(1)
                                && maLichTrucPhongHomNay.Contains(h.MaLichTruc))
                    .ToListAsync();

            var soLichHenHomNayByBacSi = new Dictionary<string, int>();

            foreach (var h in lichHenHomNay)
            {
                if (!phongByLichTruc.TryGetValue(h.MaLichTruc, out var maPhong))
                    continue;

                var phong = phongBacSis.FirstOrDefault(p => p.MaPhong == maPhong);
                if (phong?.MaBacSiPhuTrach == null) continue;

                var maBs = phong.MaBacSiPhuTrach;
                if (!soLichHenHomNayByBacSi.TryGetValue(maBs, out var count))
                    soLichHenHomNayByBacSi[maBs] = 1;
                else
                    soLichHenHomNayByBacSi[maBs] = count + 1;
            }

            // ================== MAP RA DTO ==================

            var cards = new List<StaffCardDto>();
            
            foreach (var s in staffItems)
            {
                string? maPhongHomNay = null;
                string? tenPhongHomNay = null;
                int soLichHenHomNay = 0;
                int soCaTrucTuanNay = 0;

                if (s.VaiTro == "y_ta")
                {
                    // Y tá: lấy phòng & ca trực tuần từ LichTruc
                    if (phongHomNayByYTa.TryGetValue(s.MaNhanVien, out var phongInfo))
                    {
                        maPhongHomNay = phongInfo.MaPhong;
                        tenPhongHomNay = phongInfo.TenPhong;
                    }

                    soCaTrucTuanByYTa.TryGetValue(s.MaNhanVien, out soCaTrucTuanNay);
                    soLichHenHomNay = 0; // Y tá KHÔNG dùng lịch hẹn
                }
                else if (s.VaiTro == "bac_si")
                {
                    // Bác sĩ: phòng cố định + số lịch hẹn hôm nay
                    if (phongByBacSi.TryGetValue(s.MaNhanVien, out var phong))
                    {
                        maPhongHomNay = phong.MaPhong;
                        tenPhongHomNay = phong.TenPhong;
                    }

                    soLichHenHomNayByBacSi.TryGetValue(s.MaNhanVien, out soLichHenHomNay);
                    soCaTrucTuanNay = 0; // BS không hiển thị số ca trực tuần
                }

                cards.Add(new StaffCardDto
                {

                    MaNhanVien = s.MaNhanVien,
                    HoTen = s.HoTen,
                    VaiTro = s.VaiTro,
                    HocVi = s.HocVi,
                    ChuyenKhoa = s.ChuyenKhoa,
                    MaKhoa = s.MaKhoa,
                    TenKhoa = s.TenKhoa,
                    AnhDaiDien = s.AnhDaiDien,
                    Email = s.Email,
                    DienThoai = s.DienThoai,
                    TrangThaiCongTac = s.TrangThaiCongTac,
                    LoaiYTa = s.LoaiYTa,
                    MaPhongHomNay = maPhongHomNay,
                    TenPhongHomNay = tenPhongHomNay,
                    SoLichHenHomNay = soLichHenHomNay,  // BS dùng field này
                    SoCaTrucTuanNay = soCaTrucTuanNay   // YT dùng field này
                });
            }

            return new PagedResult<StaffCardDto>
            {
                Items = cards,
                TotalItems = pagedStaff.TotalItems,
                Page = pagedStaff.Page,
                PageSize = pagedStaff.PageSize
            };
        }

        public async Task<StaffDetailDto?> LayChiTietNhanSuAsync(string maNhanVien)
        {
            if (string.IsNullOrWhiteSpace(maNhanVien))
                throw new ArgumentNullException(nameof(maNhanVien));

            var nv = await _db.NhanVienYTes
                .AsNoTracking()
                .Include(n => n.KhoaChuyenMon)
                .FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien);

            if (nv == null) return null;

            var today = DateTime.Today.Date;
            var startOfWeek = GetStartOfWeek(today);
            var endOfWeekExclusive = startOfWeek.AddDays(7);

            int? soLichHenHomNay = null;
            int? soCaTrucTuanNay = null;
            string? maPhongHomNay = null;
            string? tenPhongHomNay = null;

            if (nv.VaiTro == "y_ta")
            {
                // Y TÁ: dùng LichTruc
                var lichTuan = await _db.LichTrucs
                    .AsNoTracking()
                    .Where(l => l.MaYTaTruc == nv.MaNhanVien
                                && l.Ngay >= startOfWeek
                                && l.Ngay < endOfWeekExclusive
                                && !l.NghiTruc)
                    .ToListAsync();

                soCaTrucTuanNay = lichTuan.Count;

                var homNay = lichTuan.FirstOrDefault(l => l.Ngay.Date == today);
                if (homNay != null)
                {
                    maPhongHomNay = homNay.MaPhong;
                    tenPhongHomNay = await _db.Phongs
                        .AsNoTracking()
                        .Where(p => p.MaPhong == homNay.MaPhong)
                        .Select(p => p.TenPhong)
                        .FirstOrDefaultAsync();
                }

                soLichHenHomNay = null; // Y tá không hiển thị lịch hẹn
            }
            else if (nv.VaiTro == "bac_si")
            {
                // BÁC SĨ: phòng cố định + số lịch hẹn hôm nay
                var phongBs = await _db.Phongs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.MaBacSiPhuTrach == nv.MaNhanVien);

                if (phongBs != null)
                {
                    maPhongHomNay = phongBs.MaPhong;
                    tenPhongHomNay = phongBs.TenPhong;

                    var startOfDay = today;
                    var endOfDay = today.AddDays(1);

                    // Lịch trực (của phòng) trong ngày
                    var maLichTrucPhongHomNay = await _db.LichTrucs
                        .AsNoTracking()
                        .Where(l => l.MaPhong == phongBs.MaPhong
                                    && l.Ngay >= startOfDay
                                    && l.Ngay < endOfDay
                                    && !l.NghiTruc)
                        .Select(l => l.MaLichTruc)
                        .ToListAsync();

                    if (maLichTrucPhongHomNay.Count > 0)
                    {
                        soLichHenHomNay = await _db.LichHenKhams
                            .AsNoTracking()
                            .Where(h => h.CoHieuLuc
                                        && h.TrangThai != "da_huy"
                                        && h.NgayHen >= startOfDay
                                        && h.NgayHen < endOfDay
                                        && maLichTrucPhongHomNay.Contains(h.MaLichTruc))
                            .CountAsync();
                    }
                    else
                    {
                        soLichHenHomNay = 0;
                    }
                }
                else
                {
                    // BS không gắn phòng → mặc định không có lịch hẹn
                    soLichHenHomNay = 0;
                }

                soCaTrucTuanNay = null;   // BS không hiển thị ca trực
            }

            return new StaffDetailDto
            {
                MaNhanVien = nv.MaNhanVien,
                HoTen = nv.HoTen,
                VaiTro = nv.VaiTro,
                TenKhoa = nv.KhoaChuyenMon?.TenKhoa,
                TrangThaiCongTac = nv.TrangThaiCongTac,
                HocVi = nv.HocVi,
                ChuyenKhoa = nv.ChuyenMon,
                SoNamKinhNghiem = nv.SoNamKinhNghiem,
                Email = nv.Email,
                DienThoai = nv.DienThoai,
                LoaiYTa = nv.VaiTro == "y_ta" ? nv.LoaiYTa : null,
                SoLichHenHomNay = soLichHenHomNay,      // BS
                SoCaTrucTuanNay = soCaTrucTuanNay,      // YT
                MaPhongHoacBanHomNay = maPhongHomNay,
                TenPhongHoacBanHomNay = tenPhongHomNay,
                KyNang = Array.Empty<string>()
            };
        }

        public async Task<StaffDutyWeekDto?> LayLichTrucNhanSuTuanAsync(string maNhanVien, DateTime? today = null)
        {
            if (string.IsNullOrWhiteSpace(maNhanVien))
                throw new ArgumentNullException(nameof(maNhanVien));

            var nv = await _db.NhanVienYTes
                .AsNoTracking()
                .Include(n => n.KhoaChuyenMon)
                .FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien);

            if (nv == null) return null;

            var current = (today ?? DateTime.Today).Date;
            var startOfWeek = GetStartOfWeek(current);
            var endOfWeekExclusive = startOfWeek.AddDays(7);

            var items = new List<StaffDutyDayDto>();
            string? caHomNay = null;
            string? trangThaiHomNay = null;
            string? tenPhongHoacBanHomNay = null;

            var isNurse = nv.VaiTro == "y_ta";

            // ===== Lấy list lịch trực của nhân sự trong tuần =====
            IQueryable<LichTruc> lichQuery = _db.LichTrucs.AsNoTracking();

            if (isNurse)
            {
                // Y tá: gắn trực tiếp vào MaYTaTruc
                lichQuery = lichQuery.Where(l => l.MaYTaTruc == maNhanVien);
            }
            else
            {
                // Bác sĩ: lấy các ca trực của phòng mà BS phụ trách
                lichQuery = lichQuery
                    .Include(l => l.Phong)
                    .Where(l => l.Phong != null && l.Phong.MaBacSiPhuTrach == maNhanVien);
            }

            var lichTuan = await lichQuery
                .Where(l => l.Ngay >= startOfWeek && l.Ngay < endOfWeekExclusive)
                .ToListAsync();

            // Map MaPhong -> TenPhong để hiển thị
            var maPhongs = lichTuan.Select(l => l.MaPhong).Distinct().ToList();
            var dictPhong = maPhongs.Count == 0
                ? new Dictionary<string, string>()
                : await _db.Phongs
                    .AsNoTracking()
                    .Where(p => maPhongs.Contains(p.MaPhong))
                    .ToDictionaryAsync(p => p.MaPhong, p => p.TenPhong);

            // ===== Duyệt 7 ngày trong tuần =====
            for (int i = 0; i < 7; i++)
            {
                var date = startOfWeek.AddDays(i);
                var thu = GetThuShortName(date);

                var shiftsOfDay = lichTuan
                    .Where(l => l.Ngay.Date == date.Date)
                    .OrderBy(l => l.CaTruc)
                    .ToList();

                StaffDutyDayDto dayDto;

                if (shiftsOfDay.Count > 0)
                {
                    // Ưu tiên ca ĐANG LÀM (NghiTruc == false)
                    var workingShift = shiftsOfDay.FirstOrDefault(l => !l.NghiTruc);

                    if (workingShift != null)
                    {
                        dictPhong.TryGetValue(workingShift.MaPhong, out var tenPhong);
                        var trangThai = "lam";

                        dayDto = new StaffDutyDayDto
                        {
                            Thu = thu,
                            CaTruc = workingShift.CaTruc,
                            GioBatDau = workingShift.GioBatDau,
                            GioKetThuc = workingShift.GioKetThuc,
                            MaPhong = workingShift.MaPhong,
                            TenPhong = tenPhong,
                            TrangThaiLamViec = trangThai
                        };

                        if (date == current)
                        {
                            caHomNay = workingShift.CaTruc;
                            trangThaiHomNay = trangThai;
                            tenPhongHoacBanHomNay = tenPhong;
                        }
                    }
                    else
                    {
                        // Chỉ có bản ghi NghiTruc = true
                        dayDto = new StaffDutyDayDto
                        {
                            Thu = thu,
                            CaTruc = null,
                            GioBatDau = null,
                            GioKetThuc = null,
                            MaPhong = null,
                            TenPhong = null,
                            TrangThaiLamViec = "nghi"
                        };

                        if (date == current && trangThaiHomNay == null)
                        {
                            trangThaiHomNay = "nghi";
                        }
                    }
                }
                else
                {
                    // Không có bản ghi lịch trực trong ngày này
                    dayDto = new StaffDutyDayDto
                    {
                        Thu = thu,
                        CaTruc = null,
                        GioBatDau = null,
                        GioKetThuc = null,
                        MaPhong = null,
                        TenPhong = null,
                        TrangThaiLamViec = "nghi"
                    };

                    if (date == current && trangThaiHomNay == null)
                    {
                        trangThaiHomNay = "nghi";
                    }
                }

                items.Add(dayDto);
            }

            // Nếu tới cuối vẫn chưa set thì mặc định là nghỉ
            if (trangThaiHomNay == null)
                trangThaiHomNay = "nghi";

            return new StaffDutyWeekDto
            {
                MaNhanVien = nv.MaNhanVien,
                HoTen = nv.HoTen,
                VaiTro = nv.VaiTro,
                TenKhoa = nv.KhoaChuyenMon?.TenKhoa,
                Today = current,
                // Dùng chung cho cả BS & YT (nếu có ca thì không bị null)
                CaHomNay = caHomNay,
                TrangThaiLamViecHomNay = trangThaiHomNay,
                TenPhongHoacBanHomNay = tenPhongHoacBanHomNay,
                Items = items
            };
        }


        // ========== LỊCH TRỰC ==========

        public async Task<IReadOnlyList<DutyScheduleDto>> LayDanhSachLichTrucAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? maPhong = null,
            string? maYTaTruc = null)
        {
            var filter = new DutyScheduleSearchFilter
            {
                FromDate = fromDate,
                ToDate = toDate,
                MaPhong = maPhong,
                MaYTaTruc = maYTaTruc,
                Page = 1,
                PageSize = int.MaxValue
            };

            var paged = await TimKiemLichTrucAsync(filter);
            return paged.Items.ToList();
        }

        public async Task<PagedResult<DutyScheduleDto>> TimKiemLichTrucAsync(DutyScheduleSearchFilter filter)
        {
            var query = _db.LichTrucs.AsNoTracking().AsQueryable();

            if (filter.FromDate.HasValue)
            {
                var from = filter.FromDate.Value.Date;
                query = query.Where(l => l.Ngay >= from);
            }

            if (filter.ToDate.HasValue)
            {
                var toExclusive = filter.ToDate.Value.Date.AddDays(1);
                query = query.Where(l => l.Ngay < toExclusive);
            }

            if (!string.IsNullOrWhiteSpace(filter.MaPhong))
            {
                var mp = filter.MaPhong.Trim();
                query = query.Where(l => l.MaPhong == mp);
            }

            if (!string.IsNullOrWhiteSpace(filter.MaYTaTruc))
            {
                var myt = filter.MaYTaTruc.Trim();
                query = query.Where(l => l.MaYTaTruc == myt);
            }

            var sortBy = (filter.SortBy ?? "Ngay").ToLowerInvariant();
            var sortDir = (filter.SortDirection ?? "asc").ToLowerInvariant();

            query = (sortBy, sortDir) switch
            {
                ("ngay", "desc") => query.OrderByDescending(l => l.Ngay).ThenByDescending(l => l.CaTruc),
                ("ngay", _) => query.OrderBy(l => l.Ngay).ThenBy(l => l.CaTruc),
                ("catruc", "desc") => query.OrderByDescending(l => l.CaTruc),
                ("catruc", _) => query.OrderBy(l => l.CaTruc),
                _ when sortDir == "desc" => query.OrderByDescending(l => l.Ngay),
                _ => query.OrderBy(l => l.Ngay)
            };

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 50 : filter.PageSize;

            var totalItems = await query.CountAsync();

            var list = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = list.Select(l => new DutyScheduleDto
            {
                MaLichTruc = l.MaLichTruc,
                Ngay = l.Ngay,
                CaTruc = l.CaTruc,
                GioBatDau = l.GioBatDau,
                GioKetThuc = l.GioKetThuc,
                NghiTruc = l.NghiTruc,
                MaYTaTruc = l.MaYTaTruc,
                MaPhong = l.MaPhong
            }).ToList();

            return new PagedResult<DutyScheduleDto>
            {
                Items = dtos,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }
        // ========== LỊCH TRỰC THEO BÁC SĨ ==========

        /// <summary>
        /// Lấy danh sách lịch trực của BÁC SĨ trong khoảng ngày (suy ra từ phòng mà BS phụ trách).
        /// fromDate/toDate: nếu null => mặc định lấy 1 ngày hiện tại.
        /// </summary>
        public async Task<IReadOnlyList<DutyScheduleDto>> LayLichTrucBacSiAsync(
            string maBacSi,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            if (string.IsNullOrWhiteSpace(maBacSi))
                throw new ArgumentNullException(nameof(maBacSi));

            // Nếu không truyền → lấy ngày hôm nay
            var from = (fromDate ?? DateTime.Today).Date;
            var toExclusive = (toDate ?? from).Date.AddDays(1);

            // Lấy tất cả lịch trực mà phòng có MaBacSiPhuTrach = maBacSi
            var query = _db.LichTrucs
                .AsNoTracking()
                .Include(l => l.Phong)
                .Where(l =>
                    l.Ngay >= from &&
                    l.Ngay < toExclusive &&
                    l.Phong != null &&
                    l.Phong.MaBacSiPhuTrach == maBacSi);

            var list = await query
                .OrderBy(l => l.Ngay)
                .ThenBy(l => l.CaTruc)
                .ToListAsync();

            if (list.Count == 0)
                return Array.Empty<DutyScheduleDto>();

            return list.Select(l => new DutyScheduleDto
            {
                MaLichTruc = l.MaLichTruc,
                Ngay = l.Ngay,
                CaTruc = l.CaTruc,
                GioBatDau = l.GioBatDau,
                GioKetThuc = l.GioKetThuc,
                NghiTruc = l.NghiTruc,
                MaYTaTruc = l.MaYTaTruc,
                MaPhong = l.MaPhong
            }).ToList();
        }


        // ========== DỊCH VỤ Y TẾ ==========

        public async Task<IReadOnlyList<ServiceDto>> LayDanhSachDichVuAsync(
            string? maKhoa = null,
            string? maPhong = null,
            string? loaiDichVu = null,
            string? trangThai = null)
        {
            var filter = new ServiceSearchFilter
            {                MaKhoa = maKhoa,
                MaPhong = maPhong,
                LoaiDichVu = loaiDichVu,
                Page = 1,
                PageSize = int.MaxValue
            };

            var paged = await TimKiemDichVuAsync(filter);
            return paged.Items.ToList();
        }

        public async Task<PagedResult<ServiceDto>> TimKiemDichVuAsync(ServiceSearchFilter filter)
        {
            var query = _db.DichVuYTes
                .AsNoTracking()
                .Include(d => d.PhongThucHien)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();
                query = query.Where(d =>
                    d.MaDichVu.Contains(kw) ||
                    d.TenDichVu.Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(filter.LoaiDichVu))
            {
                var ldv = filter.LoaiDichVu.Trim();
                query = query.Where(d => d.LoaiDichVu == ldv);
            }

            if (!string.IsNullOrWhiteSpace(filter.MaPhong))
            {
                var mp = filter.MaPhong.Trim();
                query = query.Where(d => d.MaPhongThucHien == mp);
            }

            if (!string.IsNullOrWhiteSpace(filter.MaKhoa))
            {
                var mk = filter.MaKhoa.Trim();
                query = query.Where(d => d.PhongThucHien.MaKhoa == mk);
            }

            // trangThai: hiện entity DichVuYTe chưa có cột => bỏ qua

            var sortBy = (filter.SortBy ?? "TenDichVu").ToLowerInvariant();
            var sortDir = (filter.SortDirection ?? "asc").ToLowerInvariant();

            query = (sortBy, sortDir) switch
            {
                ("madichvu", "desc") => query.OrderByDescending(d => d.MaDichVu),
                ("madichvu", _) => query.OrderBy(d => d.MaDichVu),
                ("tendichvu", "desc") => query.OrderByDescending(d => d.TenDichVu),
                ("tendichvu", _) => query.OrderBy(d => d.TenDichVu),
                ("dongia", "desc") => query.OrderByDescending(d => d.DonGia),
                ("dongia", _) => query.OrderBy(d => d.DonGia),
                _ when sortDir == "desc" => query.OrderByDescending(d => d.TenDichVu),
                _ => query.OrderBy(d => d.TenDichVu)
            };

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 50 : filter.PageSize;

            var totalItems = await query.CountAsync();

            var list = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = list.Select(MapServiceToDto).ToList();

            return new PagedResult<ServiceDto>
            {
                Items = dtos,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        // ========== DỊCH VỤ – THÔNG TIN CHI TIẾT THEO MÃ ==========

        public async Task<ServiceDetailInfoDto?> LayThongTinDichVuAsync(string maDichVu)
        {
            if (string.IsNullOrWhiteSpace(maDichVu))
                throw new ArgumentException("MaDichVu là bắt buộc", nameof(maDichVu));

            // Lấy dịch vụ + phòng + khoa (qua PhongThucHien)
            var dichVu = await _db.DichVuYTes
                .AsNoTracking()
                .Include(d => d.PhongThucHien)
                    .ThenInclude(p => p.KhoaChuyenMon)
                .FirstOrDefaultAsync(d => d.MaDichVu == maDichVu);

            if (dichVu == null)
                return null; // Controller sẽ trả 404

            var phong = dichVu.PhongThucHien;

            var maKhoa = phong?.MaKhoa ?? string.Empty;
            var tenKhoa = phong?.KhoaChuyenMon?.TenKhoa ?? string.Empty;

            var maPhong = phong?.MaPhong ?? string.Empty;
            var tenPhong = phong?.TenPhong ?? string.Empty;

            // Lấy bác sĩ phụ trách phòng (mặc định dịch vụ lâm sàng sẽ có BS)
            var maBacSi = phong?.MaBacSiPhuTrach ?? string.Empty;
            var tenBacSi = string.Empty;

            if (!string.IsNullOrEmpty(maBacSi))
            {
                tenBacSi = await _db.NhanVienYTes
                    .AsNoTracking()
                    .Where(nv => nv.MaNhanVien == maBacSi)
                    .Select(nv => nv.HoTen)
                    .FirstOrDefaultAsync() ?? string.Empty;
            }

            return new ServiceDetailInfoDto
            {
                MaKhoa = maKhoa,
                TenKhoa = tenKhoa,
                MaPhong = maPhong,
                TenPhong = tenPhong,
                MaBacSi = maBacSi,
                TenBacSi = tenBacSi,
                TenDichVu = dichVu.TenDichVu,
                LoaiDichVu = dichVu.LoaiDichVu,
                DonGia = dichVu.DonGia
            };
        }

        private static DateTime GetStartOfWeek(DateTime date)
        {
            // Tuần bắt đầu từ Thứ 2
            var day = date.DayOfWeek;
            var diff = (7 + (int)day - (int)DayOfWeek.Monday) % 7;
            return date.Date.AddDays(-diff);
        }

        private static string GetThuShortName(DateTime date)
        {
            return date.DayOfWeek switch
            {
                DayOfWeek.Monday => "Mon",
                DayOfWeek.Tuesday => "Tue",
                DayOfWeek.Wednesday => "Wed",
                DayOfWeek.Thursday => "Thu",
                DayOfWeek.Friday => "Fri",
                DayOfWeek.Saturday => "Sat",
                DayOfWeek.Sunday => "Sun",
                _ => "Mon"
            };
        }

        private static ServiceDto MapServiceToDto(DichVuYTe d)
        {
            return new ServiceDto
            {
                MaDichVu = d.MaDichVu,
                TenDichVu = d.TenDichVu,
                LoaiDichVu = d.LoaiDichVu,
                MaPhong = d.MaPhongThucHien,
                MaKhoa = d.PhongThucHien?.MaKhoa,
                DonGia = d.DonGia
            };
        }
    }
}
