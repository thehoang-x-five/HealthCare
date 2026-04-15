using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthCare.Services.MasterData
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

            return [.. list.Select(MapDepartmentToDto)];
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

            var dtos = items.Select(MapDepartmentToDto).ToList();

            return new PagedResult<DepartmentDto>
            {
                Items = dtos,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<DepartmentDto> TaoKhoaAsync(DepartmentUpsertRequest request)
        {
            ValidateDepartmentRequest(request);

            var maKhoa = request.MaKhoa.Trim();
            var exists = await _db.KhoaChuyenMons.AnyAsync(k => k.MaKhoa == maKhoa);
            if (exists)
                throw new InvalidOperationException($"Khoa '{maKhoa}' đã tồn tại.");

            var entity = new KhoaChuyenMon
            {
                MaKhoa = maKhoa,
                TenKhoa = request.TenKhoa.Trim(),
                TrangThai = string.IsNullOrWhiteSpace(request.TrangThai) ? "hoat_dong" : request.TrangThai.Trim(),
                MoTa = NormalizeOptional(request.MoTa),
                DienThoai = NormalizeOptional(request.DienThoai),
                Email = NormalizeOptional(request.Email),
                DiaDiem = NormalizeOptional(request.DiaDiem),
                GhiChu = NormalizeOptional(request.GhiChu),
            };

            _db.KhoaChuyenMons.Add(entity);
            await _db.SaveChangesAsync();

            return MapDepartmentToDto(entity);
        }

        public async Task<DepartmentDto> CapNhatKhoaAsync(string maKhoa, DepartmentUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(maKhoa))
                throw new ArgumentException("MaKhoa là bắt buộc.", nameof(maKhoa));

            ValidateDepartmentRequest(request, requireCode: false);

            var entity = await _db.KhoaChuyenMons.FirstOrDefaultAsync(k => k.MaKhoa == maKhoa);
            if (entity == null)
                throw new KeyNotFoundException($"Không tìm thấy khoa '{maKhoa}'.");

            entity.TenKhoa = request.TenKhoa.Trim();
            entity.TrangThai = string.IsNullOrWhiteSpace(request.TrangThai) ? entity.TrangThai : request.TrangThai.Trim();
            entity.MoTa = NormalizeOptional(request.MoTa);
            entity.DienThoai = NormalizeOptional(request.DienThoai);
            entity.Email = NormalizeOptional(request.Email);
            entity.DiaDiem = NormalizeOptional(request.DiaDiem);
            entity.GhiChu = NormalizeOptional(request.GhiChu);

            await _db.SaveChangesAsync();
            return MapDepartmentToDto(entity);
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
            var query = _db.Phongs
                .AsNoTracking()
                .Include(p => p.BacSiPhuTrach)
                .Include(p => p.KTVPhuTrach)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(maKhoa))
            {
                var mk = maKhoa.Trim();
                query = query.Where(p => p.MaKhoa == mk);
            }

            if (!string.IsNullOrWhiteSpace(loaiPhong))
            {
                query = ApplyRoomTypeFilter(query, loaiPhong);
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
                query = ApplyRoomTypeFilter(query, filter.LoaiPhong);
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
            if (!string.IsNullOrWhiteSpace(filter.MaKTVPhuTrach))
            {
                var mns = filter.MaKTVPhuTrach.Trim();
                query = query.Where(p => p.MaKTVPhuTrach == mns);
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
                query = ApplyRoomTypeFilter(query, filter.LoaiPhong);
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
            var maKtvs = list
                .Select(p => p.MaKTVPhuTrach)
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
            var dictKtv = await _db.NhanVienYTes
                .AsNoTracking()
                .Where(nv => maKtvs.Contains(nv.MaNhanVien))
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

                string? tenKtv = null;
                if (!string.IsNullOrEmpty(p.MaKTVPhuTrach) &&
                    dictKtv.TryGetValue(p.MaKTVPhuTrach!, out var tenKt))
                {
                    tenKtv = tenKt;
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
                    LoaiPhong = NormalizeRoomTypeCode(p.LoaiPhong),
                    TrangThai = p.TrangThai,

                    MaBacSiPhuTrach = p.MaBacSiPhuTrach,
                    TenBacSiPhuTrach = tenBacSi,
                    MaKTVPhuTrach = p.MaKTVPhuTrach,
                    TenKTVPhuTrach = tenKtv,

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
            string? tenKtv = null;
            if (!string.IsNullOrEmpty(room.MaKTVPhuTrach))
            {
                tenKtv = await _db.NhanVienYTes
                    .AsNoTracking()
                    .Where(nv => nv.MaNhanVien == room.MaKTVPhuTrach)
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
                LoaiPhong = NormalizeRoomTypeCode(room.LoaiPhong),
                TrangThai = room.TrangThai,

                KhuVuc = room.ViTri,
                DienThoai = room.DienThoai,
                Email = room.Email,
                GioMoCua = room.GioMoCua,
                GioDongCua = room.GioDongCua,

                MaBacSiPhuTrach = room.MaBacSiPhuTrach,
                TenBacSiPhuTrach = tenBacSi,
                MaKTVPhuTrach = room.MaKTVPhuTrach,
                TenKTVPhuTrach = tenKtv,

                ThietBi = room.ThietBi ?? new List<string>(),

                DangCho = dangCho,
                DaHoanThanh = daHoanThanh,
                TongHomNay = tongHomNay,
                SucChuaNgay = room.SucChua,

                DichVuTaiPhong = dichVu
            };

            return detail;
        }

        public async Task<RoomDto> TaoPhongAsync(RoomUpsertRequest request)
        {
            ValidateRoomRequest(request);

            var maPhong = request.MaPhong.Trim();
            var exists = await _db.Phongs.AnyAsync(p => p.MaPhong == maPhong);
            if (exists)
                throw new InvalidOperationException($"Phòng '{maPhong}' đã tồn tại.");

            await ValidateDepartmentExistsAsync(request.MaKhoa);
            ValidateRoomRoleAssignmentsAsync(request);
            await ValidateDoctorAssignmentAsync(request.MaBacSiPhuTrach, maPhong: null);
            await ValidateTechnicianAssignmentAsync(request.MaKTVPhuTrach, maPhong: null);

            var entity = new Phong
            {
                MaPhong = maPhong,
                TenPhong = request.TenPhong.Trim(),
                MaKhoa = request.MaKhoa.Trim(),
                LoaiPhong = NormalizeRoomTypeCode(request.LoaiPhong),
                SucChua = request.SucChua,
                ViTri = NormalizeOptional(request.ViTri),
                Email = NormalizeOptional(request.Email),
                DienThoai = NormalizeOptional(request.DienThoai),
                GioMoCua = request.GioMoCua,
                GioDongCua = request.GioDongCua,
                ThietBi = request.ThietBi?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? new List<string>(),
                TrangThai = string.IsNullOrWhiteSpace(request.TrangThai) ? "hoat_dong" : request.TrangThai.Trim(),
                MaBacSiPhuTrach = NormalizeOptional(request.MaBacSiPhuTrach),
                MaKTVPhuTrach = NormalizeOptional(request.MaKTVPhuTrach)
            };

            _db.Phongs.Add(entity);
            await _db.SaveChangesAsync();

            return MapRoomToDto(entity);
        }

        public async Task<RoomDto> CapNhatPhongAsync(string maPhong, RoomUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(maPhong))
                throw new ArgumentException("MaPhong là bắt buộc.", nameof(maPhong));

            ValidateRoomRequest(request, requireCode: false);

            var entity = await _db.Phongs.FirstOrDefaultAsync(p => p.MaPhong == maPhong);
            if (entity == null)
                throw new KeyNotFoundException($"Không tìm thấy phòng '{maPhong}'.");

            await ValidateDepartmentExistsAsync(request.MaKhoa);
            ValidateRoomRoleAssignmentsAsync(request);
            await ValidateDoctorAssignmentAsync(request.MaBacSiPhuTrach, maPhong);
            await ValidateTechnicianAssignmentAsync(request.MaKTVPhuTrach, maPhong);

            entity.TenPhong = request.TenPhong.Trim();
            entity.MaKhoa = request.MaKhoa.Trim();
            entity.LoaiPhong = NormalizeRoomTypeCode(request.LoaiPhong);
            entity.SucChua = request.SucChua;
            entity.ViTri = NormalizeOptional(request.ViTri);
            entity.Email = NormalizeOptional(request.Email);
            entity.DienThoai = NormalizeOptional(request.DienThoai);
            entity.GioMoCua = request.GioMoCua;
            entity.GioDongCua = request.GioDongCua;
            entity.ThietBi = request.ThietBi?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? new List<string>();
            entity.TrangThai = string.IsNullOrWhiteSpace(request.TrangThai) ? entity.TrangThai : request.TrangThai.Trim();
            entity.MaBacSiPhuTrach = NormalizeOptional(request.MaBacSiPhuTrach);
            entity.MaKTVPhuTrach = NormalizeOptional(request.MaKTVPhuTrach);

            await _db.SaveChangesAsync();

            return MapRoomToDto(entity);
        }


        private static RoomDto MapRoomToDto(Phong p)
        {
            return new RoomDto
            {
                MaPhong = p.MaPhong,
                TenPhong = p.TenPhong,
                MaKhoa = p.MaKhoa,
                LoaiPhong = NormalizeRoomTypeCode(p.LoaiPhong),
                SucChua = p.SucChua,
                ViTri = p.ViTri,
                Email = p.Email,
                DienThoai = p.DienThoai,
                GioMoCua = p.GioMoCua,
                GioDongCua = p.GioDongCua,
                ThietBi = p.ThietBi ?? new List<string>(),
                TrangThai = p.TrangThai,
                MaBacSiPhuTrach = p.MaBacSiPhuTrach,
                TenBacSiPhuTrach = p.BacSiPhuTrach?.HoTen,
                MaKTVPhuTrach = p.MaKTVPhuTrach,
                TenKTVPhuTrach = p.KTVPhuTrach?.HoTen
            };
        }

        private static DepartmentDto MapDepartmentToDto(KhoaChuyenMon k)
        {
            return new DepartmentDto
            {
                MaKhoa = k.MaKhoa,
                TenKhoa = k.TenKhoa,
                TrangThai = k.TrangThai,
                MoTa = k.MoTa,
                DienThoai = k.DienThoai,
                Email = k.Email,
                DiaDiem = k.DiaDiem,
                GhiChu = k.GhiChu
            };
        }

        private static void ValidateDepartmentRequest(DepartmentUpsertRequest request, bool requireCode = true)
        {
            if (request == null)
                throw new ArgumentException("Dữ liệu khoa không hợp lệ.");

            if (requireCode && string.IsNullOrWhiteSpace(request.MaKhoa))
                throw new ArgumentException("MaKhoa là bắt buộc.");

            if (string.IsNullOrWhiteSpace(request.TenKhoa))
                throw new ArgumentException("TenKhoa là bắt buộc.");
        }

        private static void ValidateRoomRequest(RoomUpsertRequest request, bool requireCode = true)
        {
            if (request == null)
                throw new ArgumentException("Dữ liệu phòng không hợp lệ.");

            if (requireCode && string.IsNullOrWhiteSpace(request.MaPhong))
                throw new ArgumentException("MaPhong là bắt buộc.");

            if (string.IsNullOrWhiteSpace(request.TenPhong))
                throw new ArgumentException("TenPhong là bắt buộc.");

            if (string.IsNullOrWhiteSpace(request.MaKhoa))
                throw new ArgumentException("MaKhoa là bắt buộc.");

            if (string.IsNullOrWhiteSpace(request.LoaiPhong))
                throw new ArgumentException("LoaiPhong là bắt buộc.");
        }

        private static void ValidateServiceRequest(ServiceUpsertRequest request, bool requireCode = true)
        {
            if (request == null)
                throw new ArgumentException("Dữ liệu dịch vụ không hợp lệ.");

            if (requireCode && string.IsNullOrWhiteSpace(request.MaDichVu))
                throw new ArgumentException("MaDichVu là bắt buộc.");

            if (string.IsNullOrWhiteSpace(request.TenDichVu))
                throw new ArgumentException("TenDichVu là bắt buộc.");

            if (string.IsNullOrWhiteSpace(request.LoaiDichVu))
                throw new ArgumentException("LoaiDichVu là bắt buộc.");

            if (string.IsNullOrWhiteSpace(request.MaPhong))
                throw new ArgumentException("MaPhong là bắt buộc.");

            if (request.DonGia < 0)
                throw new ArgumentException("DonGia không được âm.");

            if (request.ThoiGianDuKienPhut < 0)
                throw new ArgumentException("ThoiGianDuKienPhut không được âm.");
        }

        private async Task ValidateDepartmentExistsAsync(string? maKhoa)
        {
            if (string.IsNullOrWhiteSpace(maKhoa))
                throw new ArgumentException("MaKhoa là bắt buộc.");

            var normalized = maKhoa.Trim();
            var exists = await _db.KhoaChuyenMons.AnyAsync(k => k.MaKhoa == normalized);
            if (!exists)
                throw new ArgumentException($"Không tìm thấy khoa '{normalized}'.");
        }

        private async Task ValidateRoomExistsAsync(string? maPhong)
        {
            if (string.IsNullOrWhiteSpace(maPhong))
                throw new ArgumentException("MaPhong là bắt buộc.");

            var normalized = maPhong.Trim();
            var exists = await _db.Phongs.AnyAsync(p => p.MaPhong == normalized);
            if (!exists)
                throw new ArgumentException($"Không tìm thấy phòng '{normalized}'.");
        }

        private async Task ValidateServiceRoomCompatibilityAsync(string? loaiDichVu, string? maPhong)
        {
            var normalizedRoomId = NormalizeOptional(maPhong);
            if (normalizedRoomId == null)
                throw new ArgumentException("MaPhong là bắt buộc.");

            var room = await _db.Phongs
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.MaPhong == normalizedRoomId);

            if (room == null)
                throw new ArgumentException($"Không tìm thấy phòng '{normalizedRoomId}'.");

            var serviceType = (loaiDichVu ?? string.Empty).Trim().ToLowerInvariant();
            var roomType = NormalizeRoomTypeCode(room.LoaiPhong);

            if (serviceType == "kham_lam_sang" && !IsClinicalRoomType(roomType))
                throw new ArgumentException("Dịch vụ khám lâm sàng chỉ được gán cho phòng khám LS.");

            if (serviceType == "can_lam_sang" && !IsClsRoomType(roomType))
                throw new ArgumentException("Dịch vụ cận lâm sàng chỉ được gán cho phòng CLS / dịch vụ.");

            if (serviceType == "khac" && string.Equals(roomType, "thu_ngan", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Dịch vụ không được gán cho phòng thu ngân.");
        }

        private async Task ValidateDoctorAssignmentAsync(string? maBacSiPhuTrach, string? maPhong)
        {
            var doctorId = NormalizeOptional(maBacSiPhuTrach);
            if (doctorId == null)
                return;

            var doctor = await _db.NhanVienYTes
                .AsNoTracking()
                .FirstOrDefaultAsync(nv => nv.MaNhanVien == doctorId);

            if (doctor == null)
                throw new ArgumentException($"Không tìm thấy bác sĩ '{doctorId}'.");

            if (doctor.VaiTro != "bac_si")
                throw new ArgumentException("MaBacSiPhuTrach phải là nhân sự có vai trò bác sĩ.");

            var assignedElsewhere = await _db.Phongs
                .AsNoTracking()
                .AnyAsync(p => p.MaBacSiPhuTrach == doctorId && p.MaPhong != maPhong);

            if (assignedElsewhere)
                throw new ArgumentException($"Bác sĩ '{doctorId}' đã được gán cho phòng khác.");
        }

        private async Task ValidateTechnicianAssignmentAsync(string? maKtvPhuTrach, string? maPhong)
        {
            var technicianId = NormalizeOptional(maKtvPhuTrach);
            if (technicianId == null)
                return;

            var technician = await _db.NhanVienYTes
                .AsNoTracking()
                .FirstOrDefaultAsync(nv => nv.MaNhanVien == technicianId);

            if (technician == null)
                throw new ArgumentException($"Không tìm thấy kỹ thuật viên '{technicianId}'.");

            if (!string.Equals(technician.VaiTro, "ky_thuat_vien", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("MaKTVPhuTrach phải là nhân sự có vai trò kỹ thuật viên.");

            var assignedElsewhere = await _db.Phongs
                .AsNoTracking()
                .AnyAsync(p => p.MaKTVPhuTrach == technicianId && p.MaPhong != maPhong);

            if (assignedElsewhere)
                throw new ArgumentException($"Kỹ thuật viên '{technicianId}' đã được gán cho phòng CLS khác.");
        }

        private static string NormalizeRoomTypeCode(string? loaiPhong)
        {
            var value = (loaiPhong ?? string.Empty).Trim().ToLowerInvariant();

            return value switch
            {
                "phong_kham" or "phong_kham_ls" => "phong_kham_ls",
                "phong_dich_vu" or "phong_cls" => "phong_cls",
                _ => value
            };
        }

        private static string NormalizeNurseTypeCode(string? loaiYTa)
        {
            var value = (loaiYTa ?? string.Empty).Trim().ToLowerInvariant();

            return value switch
            {
                "ls" or "lam_sang" or "phong_kham" or "y_ta_lam_sang" => "lam_sang",
                "cls" or "can_lam_sang" or "y_ta_can_lam_sang" => "can_lam_sang",
                "hanhchinh" or "hanh_chinh" or "hc" or "y_ta_hanh_chinh" => "hanh_chinh",
                _ => value
            };
        }

        private static void ValidateRoomRoleAssignmentsAsync(RoomUpsertRequest request)
        {
            var loaiPhong = NormalizeRoomTypeCode(request.LoaiPhong);
            var maBacSi = NormalizeOptional(request.MaBacSiPhuTrach);
            var maKtv = NormalizeOptional(request.MaKTVPhuTrach);

            if (IsClinicalRoomType(loaiPhong))
            {
                if (maBacSi == null)
                    throw new ArgumentException("Phòng khám LS phải có MaBacSiPhuTrach.");
                if (maKtv != null)
                    throw new ArgumentException("Phòng khám LS không được gán MaKTVPhuTrach.");
                return;
            }

            if (IsClsRoomType(loaiPhong))
            {
                if (maKtv == null)
                    throw new ArgumentException("Phòng CLS phải có MaKTVPhuTrach.");
                if (maBacSi != null)
                    throw new ArgumentException("Phòng CLS không được gán MaBacSiPhuTrach.");
                return;
            }

            if (maBacSi != null || maKtv != null)
                throw new ArgumentException("Loại phòng hiện tại không hỗ trợ gán bác sĩ hoặc kỹ thuật viên phụ trách.");
        }

        private static bool IsClinicalRoomType(string? loaiPhong)
        {
            var value = NormalizeRoomTypeCode(loaiPhong);
            return value is "phong_kham" or "phong_kham_ls";
        }

        private static bool IsClsRoomType(string? loaiPhong)
        {
            var value = NormalizeRoomTypeCode(loaiPhong);
            return value is "phong_dich_vu" or "phong_cls";
        }

        private static bool CanAssignDutyStaffToRoom(Phong room, NhanVienYTe staff)
        {
            if (staff.TrangThaiCongTac != "dang_cong_tac" || staff.TrangThaiTaiKhoan != "hoat_dong")
                return false;

            var roomType = NormalizeRoomTypeCode(room.LoaiPhong);

            if (IsClsRoomType(roomType))
            {
                return staff.VaiTro == "ky_thuat_vien"
                    || (staff.VaiTro == "y_ta" && NormalizeNurseTypeCode(staff.LoaiYTa) == "can_lam_sang");
            }

            if (IsClinicalRoomType(roomType))
            {
                return (staff.VaiTro == "bac_si" && room.MaBacSiPhuTrach == staff.MaNhanVien)
                    || (staff.VaiTro == "y_ta" && NormalizeNurseTypeCode(staff.LoaiYTa) == "lam_sang");
            }

            return false;
        }

        private static string GetRoomDutyRoleHint(Phong room)
        {
            var roomType = NormalizeRoomTypeCode(room.LoaiPhong);

            if (IsClsRoomType(roomType))
                return "Phòng CLS chỉ cho phép kỹ thuật viên hoặc y tá cận lâm sàng trực.";

            if (IsClinicalRoomType(roomType))
                return "Phòng khám LS chỉ cho phép bác sĩ phụ trách hoặc y tá lâm sàng trực.";

            return "Loại phòng hiện tại không hỗ trợ phân công lịch trực.";
        }

        private static IQueryable<Phong> ApplyRoomTypeFilter(IQueryable<Phong> query, string? loaiPhong)
        {
            var value = NormalizeRoomTypeCode(loaiPhong);
            if (string.IsNullOrWhiteSpace(value))
                return query;

            if (value is "ls" or "clinical" or "phong_kham" or "phong_kham_ls")
                return query.Where(p => p.LoaiPhong == "phong_kham" || p.LoaiPhong == "phong_kham_ls");

            if (value is "cls" or "can_lam_sang" or "paraclinical" or "phong_cls" or "phong_dich_vu")
                return query.Where(p => p.LoaiPhong == "phong_cls" || p.LoaiPhong == "phong_dich_vu");

            return query.Where(p => p.LoaiPhong == value);
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
            string? tenKtv = null;
            if (!string.IsNullOrEmpty(room.MaKTVPhuTrach))
            {
                tenKtv = await _db.NhanVienYTes
                    .AsNoTracking()
                    .Where(nv => nv.MaNhanVien == room.MaKTVPhuTrach)
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
                        NghiTruc = l.NghiTruc,
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
                MaKTVPhuTrach = room.MaKTVPhuTrach,
                TenKTVPhuTrach = tenKtv,
                Today = current,
                LichDieuDuongTuan = items
            };
        }

        public async Task<RoomDutyWeekDto> CapNhatLichDieuDuongPhongTuanAsync(
            string maPhong,
            RoomDutyWeekUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(maPhong))
                throw new ArgumentException("MaPhong là bắt buộc.", nameof(maPhong));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var room = await _db.Phongs
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.MaPhong == maPhong);

            if (room == null)
                throw new KeyNotFoundException($"Không tìm thấy phòng '{maPhong}'.");

            var weekStart = GetStartOfWeek(request.WeekStartDate.Date);
            var weekEndExclusive = weekStart.AddDays(7);
            var supportedShifts = new[] { "Sáng", "Chiều", "Tối" };

            var requestedItems = (request.Items ?? Array.Empty<RoomDutyWeekUpsertItem>())
                .Where(item => item != null
                    && !item.NghiTruc
                    && !string.IsNullOrWhiteSpace(item.CaTruc)
                    && !string.IsNullOrWhiteSpace(item.MaNhanVienTruc))
                .Select(item => new
                {
                    Ngay = item.Ngay.Date,
                    CaTruc = NormalizeDutyShift(item.CaTruc),
                    MaNhanVienTruc = item.MaNhanVienTruc!.Trim()
                })
                .Where(item => supportedShifts.Contains(item.CaTruc))
                .GroupBy(item => $"{item.Ngay:yyyy-MM-dd}|{item.CaTruc}")
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(item => item.MaNhanVienTruc)
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList());

            var requestedStaffIds = requestedItems.Values
                .SelectMany(list => list)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (requestedStaffIds.Count > 0)
            {
                var validStaff = await _db.NhanVienYTes
                    .AsNoTracking()
                    .Where(nv => requestedStaffIds.Contains(nv.MaNhanVien))
                    .ToListAsync();

                var validMap = validStaff.ToDictionary(nv => nv.MaNhanVien, nv => nv);
                var invalidStaffIds = requestedStaffIds
                    .Where(id => !validMap.ContainsKey(id))
                    .ToList();

                if (invalidStaffIds.Count > 0)
                    throw new InvalidOperationException($"Không tìm thấy nhân sự: {string.Join(", ", invalidStaffIds)}.");

                var inactiveAssignments = validStaff
                    .Where(nv =>
                        nv.TrangThaiCongTac != "dang_cong_tac" ||
                        nv.TrangThaiTaiKhoan != "hoat_dong")
                    .Select(nv => $"{nv.MaNhanVien} ({nv.HoTen})")
                    .ToList();

                if (inactiveAssignments.Count > 0)
                    throw new InvalidOperationException(
                        $"Chỉ có thể phân công nhân sự đang hoạt động. Nhân sự không hợp lệ: {string.Join(", ", inactiveAssignments)}.");

                var invalidAssignments = validStaff
                    .Where(nv => !CanAssignDutyStaffToRoom(room, nv))
                    .Select(nv => $"{nv.MaNhanVien} ({nv.HoTen})")
                    .ToList();

                if (invalidAssignments.Count > 0)
                    throw new InvalidOperationException(
                        $"{GetRoomDutyRoleHint(room)} Nhân sự không hợp lệ: {string.Join(", ", invalidAssignments)}.");
            }

            var existingSchedules = await _db.LichTrucs
                .Where(l => l.MaPhong == maPhong && l.Ngay >= weekStart && l.Ngay < weekEndExclusive)
                .OrderBy(l => l.Ngay)
                .ThenBy(l => l.CaTruc)
                .ToListAsync();

            var existingIds = existingSchedules.Select(item => item.MaLichTruc).ToList();
            var schedulesWithAppointments = existingIds.Count == 0
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : (await _db.LichHenKhams
                    .AsNoTracking()
                    .Where(h => existingIds.Contains(h.MaLichTruc))
                    .Select(h => h.MaLichTruc)
                    .Distinct()
                    .ToListAsync())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var existingBySlot = existingSchedules
                .GroupBy(l => $"{l.Ngay.Date:yyyy-MM-dd}|{NormalizeDutyShift(l.CaTruc)}")
                .ToDictionary(group => group.Key, group => group.ToList());

            for (var dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                var date = weekStart.AddDays(dayOffset).Date;

                foreach (var shift in supportedShifts)
                {
                    var slotKey = $"{date:yyyy-MM-dd}|{shift}";
                    requestedItems.TryGetValue(slotKey, out var requestedStaffOfSlot);
                    requestedStaffOfSlot ??= new List<string>();

                    existingBySlot.TryGetValue(slotKey, out var existingRowsOfSlot);
                    existingRowsOfSlot ??= new List<LichTruc>();

                    var rowsWithAppointments = existingRowsOfSlot
                        .Where(row => schedulesWithAppointments.Contains(row.MaLichTruc))
                        .ToList();

                    if (requestedStaffOfSlot.Count == 0)
                    {
                        if (rowsWithAppointments.Count > 0)
                        {
                            throw new InvalidOperationException(
                                $"Không thể xóa ca {shift} ngày {date:dd/MM/yyyy} vì đã có lịch hẹn.");
                        }

                        foreach (var row in existingRowsOfSlot)
                        {
                            _db.LichTrucs.Remove(row);
                        }

                        continue;
                    }

                    if (rowsWithAppointments.Count > requestedStaffOfSlot.Count)
                    {
                        throw new InvalidOperationException(
                            $"Ca {shift} ngày {date:dd/MM/yyyy} đang có nhiều lịch trực phát sinh lịch hẹn hơn số nhân sự bạn giữ lại.");
                    }

                    var reusableRows = rowsWithAppointments
                        .Concat(existingRowsOfSlot.Where(row => !schedulesWithAppointments.Contains(row.MaLichTruc)))
                        .ToList();

                    while (reusableRows.Count < requestedStaffOfSlot.Count)
                    {
                        var createdRow = new LichTruc
                        {
                            MaLichTruc = GenerateDutyScheduleId(),
                            Ngay = date,
                            CaTruc = shift,
                            MaPhong = maPhong,
                            MaYTaTruc = requestedStaffOfSlot[reusableRows.Count],
                            NghiTruc = false
                        };

                        _db.LichTrucs.Add(createdRow);
                        reusableRows.Add(createdRow);
                    }

                    var (gioBatDau, gioKetThuc) = ResolveDutyShiftHours(shift);

                    for (var index = 0; index < requestedStaffOfSlot.Count; index++)
                    {
                        var row = reusableRows[index];
                        row.Ngay = date;
                        row.CaTruc = shift;
                        row.GioBatDau = gioBatDau;
                        row.GioKetThuc = gioKetThuc;
                        row.NghiTruc = false;
                        row.MaPhong = maPhong;
                        row.MaYTaTruc = requestedStaffOfSlot[index];
                    }

                    foreach (var extraRow in reusableRows.Skip(requestedStaffOfSlot.Count))
                    {
                        if (schedulesWithAppointments.Contains(extraRow.MaLichTruc))
                            continue;

                        _db.LichTrucs.Remove(extraRow);
                    }
                }
            }

            await _db.SaveChangesAsync();
            return (await LayLichDieuDuongPhongTuanAsync(maPhong, weekStart))!;
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
                .Include(nv => nv.PhongClsPhuTrach)
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
                .Include(nv => nv.PhongClsPhuTrach)
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

            if (!string.IsNullOrWhiteSpace(filter.LoaiYTa))
            {
                var loai = filter.LoaiYTa.Trim().ToLower();
                query = query.Where(nv => nv.LoaiYTa != null && nv.LoaiYTa.ToLower() == loai);
            }

            if (!string.IsNullOrWhiteSpace(filter.MaPhongPhuTrach))
            {
                var mp = filter.MaPhongPhuTrach.Trim();
                query = query.Where(nv =>
                    (nv.PhongsPhuTrach != null && nv.PhongsPhuTrach.MaPhong == mp) ||
                    (nv.PhongClsPhuTrach != null && nv.PhongClsPhuTrach.MaPhong == mp));
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

                MaPhongPhuTrach = nv.VaiTro == "ky_thuat_vien"
                    ? nv.PhongClsPhuTrach?.MaPhong
                    : nv.PhongsPhuTrach?.MaPhong,
                TenPhongPhuTrach = nv.VaiTro == "ky_thuat_vien"
                    ? nv.PhongClsPhuTrach?.TenPhong
                    : nv.PhongsPhuTrach?.TenPhong,

                LoaiYTa = nv.VaiTro == "y_ta" ? nv.LoaiYTa : null
            };
        }

        // ========== NHÂN SỰ — CARD / CHI TIẾT / LỊCH TRỰC TUẦN ==========
        private static IReadOnlyList<string> BuildStaffSkills(NhanVienYTe nv)
        {
            var skills = new List<string>();

            if (!string.IsNullOrWhiteSpace(nv.ChuyenMon))
                skills.Add(nv.ChuyenMon.Trim());

            if (nv.VaiTro == "ky_thuat_vien")
            {
                skills.Add("Vận hành CLS");
                skills.Add("Xử lý kết quả");
            }
            else if (nv.VaiTro == "y_ta")
            {
                var nurseType = NormalizeNurseTypeCode(nv.LoaiYTa);
                if (nurseType == "lam_sang")
                    skills.Add("Hỗ trợ khám bệnh");
                else if (nurseType == "can_lam_sang")
                    skills.Add("Điều phối CLS");
                else if (nurseType == "hanh_chinh")
                    skills.Add("Tiếp nhận bệnh nhân");
            }

            return skills
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<PagedResult<StaffCardDto>> TimKiemNhanSuCardAsync(StaffSearchFilter filter)
        {
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

            var nurseIds = staffItems
                .Where(s => s.VaiTro == "y_ta")
                .Select(s => s.MaNhanVien)
                .ToList();
            var technicianIds = staffItems
                .Where(s => s.VaiTro == "ky_thuat_vien")
                .Select(s => s.MaNhanVien)
                .ToList();
            var dutyStaffIds = nurseIds
                .Concat(technicianIds)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var doctorIds = staffItems
                .Where(s => s.VaiTro == "bac_si")
                .Select(s => s.MaNhanVien)
                .ToList();

            var soCaTrucTuanByDutyStaff = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var phongHomNayByDutyStaff = new Dictionary<string, (string? MaPhong, string? TenPhong)>(StringComparer.OrdinalIgnoreCase);
            if (dutyStaffIds.Count > 0)
            {
                var lichTuanDutyStaff = await _db.LichTrucs
                    .AsNoTracking()
                    .Where(l => dutyStaffIds.Contains(l.MaYTaTruc)
                        && l.Ngay >= startOfWeek
                        && l.Ngay < endOfWeekExclusive
                        && !l.NghiTruc)
                    .ToListAsync();

                soCaTrucTuanByDutyStaff = lichTuanDutyStaff
                    .GroupBy(l => l.MaYTaTruc)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

                var lichHomNayDutyStaff = lichTuanDutyStaff
                    .Where(l => l.Ngay.Date == today)
                    .ToList();

                var maPhongsHomNay = lichHomNayDutyStaff
                    .Select(l => l.MaPhong)
                    .Distinct()
                    .ToList();

                var dictPhongHomNay = maPhongsHomNay.Count == 0
                    ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    : await _db.Phongs
                        .AsNoTracking()
                        .Where(p => maPhongsHomNay.Contains(p.MaPhong))
                        .ToDictionaryAsync(p => p.MaPhong, p => p.TenPhong);

                phongHomNayByDutyStaff = lichHomNayDutyStaff
                    .GroupBy(l => l.MaYTaTruc)
                    .ToDictionary(
                        g => g.Key,
                        g =>
                        {
                            var first = g.OrderBy(x => x.CaTruc).FirstOrDefault();
                            return first == null
                                ? (null, null)
                                : (first.MaPhong, dictPhongHomNay.TryGetValue(first.MaPhong, out var tenPhong) ? tenPhong : null);
                        },
                        StringComparer.OrdinalIgnoreCase);
            }

            var soCaLamClsHomNayByKtv = technicianIds.Count == 0
                ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                : (await _db.LuotKhamBenhs
                    .AsNoTracking()
                    .Where(v => v.MaNhanSuThucHien != null
                        && technicianIds.Contains(v.MaNhanSuThucHien)
                        && v.LoaiLuot == "can_lam_sang"
                        && v.ThoiGianBatDau >= today
                        && v.ThoiGianBatDau < today.AddDays(1)
                        && v.TrangThai != "da_huy")
                    .GroupBy(v => v.MaNhanSuThucHien!)
                    .Select(g => new { MaNhanVien = g.Key, Count = g.Count() })
                    .ToListAsync())
                    .ToDictionary(x => x.MaNhanVien, x => x.Count, StringComparer.OrdinalIgnoreCase);

            var phongBacSis = doctorIds.Count == 0
                ? new List<Phong>()
                : await _db.Phongs
                    .AsNoTracking()
                    .Where(p => p.MaBacSiPhuTrach != null && doctorIds.Contains(p.MaBacSiPhuTrach))
                    .ToListAsync();

            var phongByBacSi = phongBacSis
                .GroupBy(p => p.MaBacSiPhuTrach!)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

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

            var maLichTrucPhongHomNay = lichTrucPhongHomNay.Select(l => l.MaLichTruc).Distinct().ToList();
            var phongByLichTruc = lichTrucPhongHomNay.ToDictionary(l => l.MaLichTruc, l => l.MaPhong, StringComparer.OrdinalIgnoreCase);
            var phongByMaPhong = phongBacSis.ToDictionary(p => p.MaPhong, p => p, StringComparer.OrdinalIgnoreCase);

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

            var soLichHenHomNayByBacSi = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var lichHen in lichHenHomNay)
            {
                if (!phongByLichTruc.TryGetValue(lichHen.MaLichTruc, out var maPhong) ||
                    !phongByMaPhong.TryGetValue(maPhong, out var phong) ||
                    string.IsNullOrWhiteSpace(phong.MaBacSiPhuTrach))
                {
                    continue;
                }

                soLichHenHomNayByBacSi.TryGetValue(phong.MaBacSiPhuTrach, out var count);
                soLichHenHomNayByBacSi[phong.MaBacSiPhuTrach] = count + 1;
            }

            var cards = new List<StaffCardDto>();
            foreach (var s in staffItems)
            {
                string? maPhongHomNay = null;
                string? tenPhongHomNay = null;
                int soLichHenHomNay = 0;
                int soCaTrucTuanNay = 0;
                int soCaLamClsHomNay = 0;

                if (s.VaiTro == "y_ta")
                {
                    if (phongHomNayByDutyStaff.TryGetValue(s.MaNhanVien, out var phongInfo))
                    {
                        maPhongHomNay = phongInfo.MaPhong;
                        tenPhongHomNay = phongInfo.TenPhong;
                    }

                    soCaTrucTuanByDutyStaff.TryGetValue(s.MaNhanVien, out soCaTrucTuanNay);
                }
                else if (s.VaiTro == "bac_si")
                {
                    if (phongByBacSi.TryGetValue(s.MaNhanVien, out var phong))
                    {
                        maPhongHomNay = phong.MaPhong;
                        tenPhongHomNay = phong.TenPhong;
                    }

                    soLichHenHomNayByBacSi.TryGetValue(s.MaNhanVien, out soLichHenHomNay);
                }
                else if (s.VaiTro == "ky_thuat_vien")
                {
                    if (phongHomNayByDutyStaff.TryGetValue(s.MaNhanVien, out var phongInfo))
                    {
                        maPhongHomNay = phongInfo.MaPhong;
                        tenPhongHomNay = phongInfo.TenPhong;
                    }
                    else
                    {
                        maPhongHomNay = s.MaPhongPhuTrach;
                        tenPhongHomNay = s.TenPhongPhuTrach;
                    }

                    soCaTrucTuanByDutyStaff.TryGetValue(s.MaNhanVien, out soCaTrucTuanNay);
                    soCaLamClsHomNayByKtv.TryGetValue(s.MaNhanVien, out soCaLamClsHomNay);
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
                    MaPhongPhuTrach = s.MaPhongPhuTrach,
                    TenPhongPhuTrach = s.TenPhongPhuTrach,
                    SoLichHenHomNay = soLichHenHomNay,
                    SoCaTrucTuanNay = soCaTrucTuanNay,
                    SoCaLamClsHomNay = soCaLamClsHomNay
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
            int? soCaLamClsHomNay = null;
            string? maPhongHomNay = null;
            string? tenPhongHomNay = null;
            string? maPhongPhuTrach = null;
            string? tenPhongPhuTrach = null;

            if (nv.VaiTro == "y_ta")
            {
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
                var phongBs = await _db.Phongs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.MaBacSiPhuTrach == nv.MaNhanVien);

                if (phongBs != null)
                {
                    maPhongPhuTrach = phongBs.MaPhong;
                    tenPhongPhuTrach = phongBs.TenPhong;
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
            else if (nv.VaiTro == "ky_thuat_vien")
            {
                var lichTuan = await _db.LichTrucs
                    .AsNoTracking()
                    .Where(l => l.MaYTaTruc == nv.MaNhanVien
                        && l.Ngay >= startOfWeek
                        && l.Ngay < endOfWeekExclusive
                        && !l.NghiTruc)
                    .ToListAsync();

                soCaTrucTuanNay = lichTuan.Count;

                var phongCls = await _db.Phongs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.MaKTVPhuTrach == nv.MaNhanVien);

                if (phongCls != null)
                {
                    maPhongPhuTrach = phongCls.MaPhong;
                    tenPhongPhuTrach = phongCls.TenPhong;
                }

                var homNay = lichTuan
                    .Where(l => l.Ngay.Date == today)
                    .OrderBy(l => l.CaTruc)
                    .FirstOrDefault();

                if (homNay != null)
                {
                    maPhongHomNay = homNay.MaPhong;
                    tenPhongHomNay = await _db.Phongs
                        .AsNoTracking()
                        .Where(p => p.MaPhong == homNay.MaPhong)
                        .Select(p => p.TenPhong)
                        .FirstOrDefaultAsync();
                }
                else
                {
                    maPhongHomNay = maPhongPhuTrach;
                    tenPhongHomNay = tenPhongPhuTrach;
                }

                soCaLamClsHomNay = await _db.LuotKhamBenhs
                    .AsNoTracking()
                    .Where(v => v.MaNhanSuThucHien == nv.MaNhanVien
                        && v.LoaiLuot == "can_lam_sang"
                        && v.ThoiGianBatDau >= today
                        && v.ThoiGianBatDau < today.AddDays(1)
                        && v.TrangThai != "da_huy")
                    .CountAsync();
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
                SoCaLamClsHomNay = soCaLamClsHomNay,
                MaPhongHoacBanHomNay = maPhongHomNay,
                TenPhongHoacBanHomNay = tenPhongHomNay,
                MaPhongPhuTrach = maPhongPhuTrach,
                TenPhongPhuTrach = tenPhongPhuTrach,
                KyNang = BuildStaffSkills(nv)
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

            var isDoctor = nv.VaiTro == "bac_si";
            var isDirectDutyStaff = isDoctor || nv.VaiTro == "y_ta" || nv.VaiTro == "ky_thuat_vien";

            // ===== L?y list l?ch tr?c c?a nh?n s? trong tu?n =====
            List<LichTruc> lichTuan;

            if (isDoctor || nv.VaiTro == "ky_thuat_vien")
            {
                var directSchedules = await _db.LichTrucs
                    .AsNoTracking()
                    .Where(l => l.MaYTaTruc == maNhanVien
                                && l.Ngay >= startOfWeek
                                && l.Ngay < endOfWeekExclusive)
                    .ToListAsync();

                if (directSchedules.Count > 0)
                {
                    lichTuan = directSchedules;
                }
                else
                {
                    lichTuan = await _db.LichTrucs
                        .AsNoTracking()
                        .Include(l => l.Phong)
                        .Where(l => l.Ngay >= startOfWeek
                                    && l.Ngay < endOfWeekExclusive
                                    && l.Phong != null
                                    && (isDoctor ? l.Phong.MaBacSiPhuTrach == maNhanVien : l.Phong.MaKTVPhuTrach == maNhanVien))
                        .ToListAsync();
                }
            }
            else
            {
                IQueryable<LichTruc> lichQuery = _db.LichTrucs.AsNoTracking();

                if (isDirectDutyStaff)
                {
                    lichQuery = lichQuery.Where(l => l.MaYTaTruc == maNhanVien);
                }
                else
                {
                    lichQuery = lichQuery
                        .Include(l => l.Phong)
                        .Where(l => l.Phong != null && l.Phong.MaBacSiPhuTrach == maNhanVien);
                }

                lichTuan = await lichQuery
                    .Where(l => l.Ngay >= startOfWeek && l.Ngay < endOfWeekExclusive)
                    .ToListAsync();
            }

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

            if (string.IsNullOrWhiteSpace(tenPhongHoacBanHomNay) && nv.VaiTro == "ky_thuat_vien")
            {
                tenPhongHoacBanHomNay = await _db.Phongs
                    .AsNoTracking()
                    .Where(p => p.MaKTVPhuTrach == nv.MaNhanVien)
                    .Select(p => p.TenPhong)
                    .FirstOrDefaultAsync();
            }

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

        public async Task<StaffDutyWeekDto> CapNhatLichTrucNhanSuTuanAsync(
            string maNhanVien,
            StaffDutyWeekUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(maNhanVien))
                throw new ArgumentException("MaNhanVien là bắt buộc.", nameof(maNhanVien));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var nhanSu = await _db.NhanVienYTes
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien);

            if (nhanSu == null)
                throw new KeyNotFoundException($"Không tìm thấy nhân sự '{maNhanVien}'.");

            var vaiTro = nhanSu.VaiTro?.Trim().ToLowerInvariant();
            if (vaiTro != "y_ta" && vaiTro != "ky_thuat_vien" && vaiTro != "bac_si")
                throw new InvalidOperationException("Chỉ hỗ trợ quản lý lịch làm cho bác sĩ, y tá và kỹ thuật viên.");
            var isTechnician = vaiTro == "ky_thuat_vien";
            var isDoctor = vaiTro == "bac_si";
            var hasFixedRoom = isTechnician || isDoctor;

            string? fixedRoomId = null;
            if (isTechnician)
            {
                fixedRoomId = await _db.Phongs
                    .AsNoTracking()
                    .Where(p => p.MaKTVPhuTrach == maNhanVien)
                    .Select(p => p.MaPhong)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(fixedRoomId))
                    throw new InvalidOperationException("KTV phải được gán phòng CLS cố định trước khi xếp lịch làm.");
            }
            else if (isDoctor)
            {
                fixedRoomId = await _db.Phongs
                    .AsNoTracking()
                    .Where(p => p.MaBacSiPhuTrach == maNhanVien)
                    .Select(p => p.MaPhong)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(fixedRoomId))
                    throw new InvalidOperationException("Bác sĩ phải được gán phòng khám cố định trước khi xếp lịch làm.");
            }

            var weekStart = GetStartOfWeek(request.WeekStartDate.Date);
            var weekEndExclusive = weekStart.AddDays(7);

            var requestedItems = (request.Items ?? Array.Empty<StaffDutyWeekUpsertItem>())
                .Where(x => x != null)
                .GroupBy(x => x.Ngay.Date)
                .ToDictionary(g => g.Key, g => g.Last());

            var requestedRooms = requestedItems.Values
                .Where(x => !x.NghiTruc && !string.IsNullOrWhiteSpace(x.CaTruc))
                .Select(x => hasFixedRoom
                    ? fixedRoomId!
                    : NormalizeOptional(x.MaPhong))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (requestedRooms.Count > 0)
            {
                var roomEntities = await _db.Phongs
                    .AsNoTracking()
                    .Where(p => requestedRooms.Contains(p.MaPhong))
                    .ToListAsync();

                var invalidRooms = requestedRooms
                    .Except(roomEntities.Select(p => p.MaPhong), StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (invalidRooms.Count > 0)
                    throw new InvalidOperationException($"Không tìm thấy phòng: {string.Join(", ", invalidRooms)}.");
            }

            if (requestedRooms.Count > 0)
            {
                foreach (var room in await _db.Phongs
                    .AsNoTracking()
                    .Where(p => requestedRooms.Contains(p.MaPhong))
                    .ToListAsync())
                {
                    if (!CanAssignDutyStaffToRoom(room, nhanSu))
                        throw new InvalidOperationException(GetRoomDutyRoleHint(room));
                }
            }

            var existingSchedules = await _db.LichTrucs
                .Where(l => l.MaYTaTruc == maNhanVien && l.Ngay >= weekStart && l.Ngay < weekEndExclusive)
                .OrderBy(l => l.Ngay)
                .ThenBy(l => l.CaTruc)
                .ToListAsync();

            var existingIds = existingSchedules.Select(x => x.MaLichTruc).ToList();
            var schedulesWithAppointments = existingIds.Count == 0
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : (await _db.LichHenKhams
                    .AsNoTracking()
                    .Where(h => existingIds.Contains(h.MaLichTruc))
                    .Select(h => h.MaLichTruc)
                    .Distinct()
                    .ToListAsync())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var existingByDate = existingSchedules
                .GroupBy(l => l.Ngay.Date)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.CaTruc).ToList());

            for (var i = 0; i < 7; i++)
            {
                var date = weekStart.AddDays(i).Date;
                requestedItems.TryGetValue(date, out var requested);
                existingByDate.TryGetValue(date, out var rowsOfDay);
                rowsOfDay ??= new List<LichTruc>();

                var keeper = rowsOfDay.FirstOrDefault();
                var extraRows = rowsOfDay.Skip(1).ToList();

                foreach (var extra in extraRows)
                {
                    if (schedulesWithAppointments.Contains(extra.MaLichTruc))
                    {
                        extra.NghiTruc = true;
                    }
                    else
                    {
                        _db.LichTrucs.Remove(extra);
                    }
                }

                var isWorkingDay =
                    requested != null &&
                    !requested.NghiTruc &&
                    !string.IsNullOrWhiteSpace(requested.CaTruc) &&
                    (hasFixedRoom || !string.IsNullOrWhiteSpace(requested.MaPhong));

                if (!isWorkingDay)
                {
                    if (keeper != null)
                    {
                        if (schedulesWithAppointments.Contains(keeper.MaLichTruc))
                        {
                            keeper.NghiTruc = true;
                        }
                        else
                        {
                            _db.LichTrucs.Remove(keeper);
                        }
                    }

                    continue;
                }

                var shift = NormalizeDutyShift(requested!.CaTruc!);
                var (gioBatDau, gioKetThuc) = ResolveDutyShiftHours(shift);
                var maPhong = hasFixedRoom
                    ? fixedRoomId!
                    : requested.MaPhong!.Trim();

                if (keeper == null)
                {
                    keeper = new LichTruc
                    {
                        MaLichTruc = GenerateDutyScheduleId(),
                        Ngay = date,
                        CaTruc = shift,
                        GioBatDau = gioBatDau,
                        GioKetThuc = gioKetThuc,
                        NghiTruc = false,
                        MaYTaTruc = maNhanVien,
                        MaPhong = maPhong
                    };
                    _db.LichTrucs.Add(keeper);
                }
                else
                {
                    keeper.Ngay = date;
                    keeper.CaTruc = shift;
                    keeper.GioBatDau = gioBatDau;
                    keeper.GioKetThuc = gioKetThuc;
                    keeper.NghiTruc = false;
                    keeper.MaPhong = maPhong;
                }
            }

            await _db.SaveChangesAsync();
            return (await LayLichTrucNhanSuTuanAsync(maNhanVien, weekStart))!;
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
            var maKtv = phong?.MaKTVPhuTrach ?? string.Empty;
            var tenKtv = string.Empty;

            if (!string.IsNullOrEmpty(maBacSi))
            {
                tenBacSi = await _db.NhanVienYTes
                    .AsNoTracking()
                    .Where(nv => nv.MaNhanVien == maBacSi)
                    .Select(nv => nv.HoTen)
                    .FirstOrDefaultAsync() ?? string.Empty;
            }
            if (!string.IsNullOrEmpty(maKtv))
            {
                tenKtv = await _db.NhanVienYTes
                    .AsNoTracking()
                    .Where(nv => nv.MaNhanVien == maKtv)
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
                MaKTVPhuTrach = maKtv,
                TenKTVPhuTrach = tenKtv,
                TenDichVu = dichVu.TenDichVu,
                LoaiDichVu = dichVu.LoaiDichVu,
                DonGia = dichVu.DonGia
            };
        }

        public async Task<ServiceDto> TaoDichVuAsync(ServiceUpsertRequest request)
        {
            ValidateServiceRequest(request);

            var maDichVu = request.MaDichVu.Trim();
            var exists = await _db.DichVuYTes.AnyAsync(d => d.MaDichVu == maDichVu);
            if (exists)
                throw new InvalidOperationException($"Dịch vụ '{maDichVu}' đã tồn tại.");

            await ValidateRoomExistsAsync(request.MaPhong);
            await ValidateServiceRoomCompatibilityAsync(request.LoaiDichVu, request.MaPhong);

            var entity = new DichVuYTe
            {
                MaDichVu = maDichVu,
                TenDichVu = request.TenDichVu.Trim(),
                LoaiDichVu = request.LoaiDichVu.Trim(),
                MaPhongThucHien = request.MaPhong.Trim(),
                DonGia = request.DonGia,
                ThoiGianDuKienPhut = request.ThoiGianDuKienPhut
            };

            _db.DichVuYTes.Add(entity);
            await _db.SaveChangesAsync();

            await _db.Entry(entity).Reference(d => d.PhongThucHien).LoadAsync();
            return MapServiceToDto(entity);
        }

        public async Task<ServiceDto> CapNhatDichVuAsync(string maDichVu, ServiceUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(maDichVu))
                throw new ArgumentException("MaDichVu là bắt buộc.", nameof(maDichVu));

            ValidateServiceRequest(request, requireCode: false);

            var entity = await _db.DichVuYTes
                .Include(d => d.PhongThucHien)
                .FirstOrDefaultAsync(d => d.MaDichVu == maDichVu);

            if (entity == null)
                throw new KeyNotFoundException($"Không tìm thấy dịch vụ '{maDichVu}'.");

            await ValidateRoomExistsAsync(request.MaPhong);
            await ValidateServiceRoomCompatibilityAsync(request.LoaiDichVu, request.MaPhong);

            entity.TenDichVu = request.TenDichVu.Trim();
            entity.LoaiDichVu = request.LoaiDichVu.Trim();
            entity.MaPhongThucHien = request.MaPhong.Trim();
            entity.DonGia = request.DonGia;
            entity.ThoiGianDuKienPhut = request.ThoiGianDuKienPhut;

            await _db.SaveChangesAsync();
            await _db.Entry(entity).Reference(d => d.PhongThucHien).LoadAsync();

            return MapServiceToDto(entity);
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

        private static string NormalizeDutyShift(string shift)
        {
            var raw = shift?.Trim() ?? string.Empty;
            var lower = raw.ToLowerInvariant();

            return lower switch
            {
                "sang" or "sáng" => "Sáng",
                "chieu" or "chiều" => "Chiều",
                "toi" or "tối" => "Tối",
                _ => raw
            };
        }

        private static (TimeSpan GioBatDau, TimeSpan GioKetThuc) ResolveDutyShiftHours(string shift)
        {
            var normalized = NormalizeDutyShift(shift);

            return normalized switch
            {
                "Sáng" => (new TimeSpan(7, 30, 0), new TimeSpan(11, 30, 0)),
                "Chiều" => (new TimeSpan(13, 0, 0), new TimeSpan(17, 0, 0)),
                "Tối" => (new TimeSpan(17, 0, 0), new TimeSpan(21, 0, 0)),
                _ => throw new InvalidOperationException($"Ca trực không hợp lệ: {shift}.")
            };
        }

        private static string GenerateDutyScheduleId()
        {
            return $"LT{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(100, 999)}";
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
                DonGia = d.DonGia,
                ThoiGianDuKienPhut = d.ThoiGianDuKienPhut
            };
        }
    }
}
