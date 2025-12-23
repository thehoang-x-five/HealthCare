using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using HealthCare.Realtime;
using Microsoft.EntityFrameworkCore;
using HealthCare.RenderID;
using HealthCare.Services.UserInteraction;
using HealthCare.Services.Report;

namespace HealthCare.Services.PatientManagement
{
    public class AppointmentService(DataContext db, IRealtimeService realtime, INotificationService notifications, IDashboardService dashboard) : IAppointmentService
    {
        private readonly DataContext _db = db;
        private readonly IRealtimeService _realtime = realtime;
        private readonly INotificationService _notifications = notifications;
       private readonly IDashboardService _dashboard = dashboard;
        // ========= TÌM KIẾM / PHÂN TRANG LỊCH HẸN =========
        public async Task<PagedResult<AppointmentReadRequestDto>> TimKiemLichHenAsync(
            AppointmentFilterRequest filter)
        {
            await DeactivateOldAppointmentsAsync();

            var query = _db.LichHenKhams
                .AsNoTracking()
                .Include(l => l.LichTruc)
                    .ThenInclude(lt => lt.Phong)
                        .ThenInclude(p => p.KhoaChuyenMon)
                .Include(l => l.LichTruc)
                    .ThenInclude(lt => lt.Phong)
                        .ThenInclude(p => p.BacSiPhuTrach)
                .Where(l => l.CoHieuLuc); // chỉ lấy lịch hiệu lực

            if (filter.FromDate.HasValue)
            {
                var from = filter.FromDate.Value.Date;
                query = query.Where(l => l.NgayHen >= from);
            }

            if (filter.ToDate.HasValue)
            {
                var toExclusive = filter.ToDate.Value.Date.AddDays(1);
                query = query.Where(l => l.NgayHen < toExclusive);
            }

            if (!string.IsNullOrWhiteSpace(filter.MaBenhNhan))
            {
                query = query.Where(l => l.MaBenhNhan == filter.MaBenhNhan);
            }

            if (!string.IsNullOrWhiteSpace(filter.LoaiHen))
            {
                query = query.Where(l => l.LoaiHen == filter.LoaiHen);
            }

            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
            {
                query = query.Where(l => l.TrangThai == filter.TrangThai);
            }

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 50 : filter.PageSize;

            var totalItems = await query.CountAsync();

            var list = await query
                .OrderBy(l => l.NgayHen)
                .ThenBy(l => l.GioHen)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = list.Select(MapToDto).ToList();

            return new PagedResult<AppointmentReadRequestDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }


        // ========= TẠO LỊCH HẸN =========
        public async Task<AppointmentReadRequestDto> TaoLichHenAsync(
            AppointmentCreateRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.TenBenhNhan))
                throw new ArgumentException("TenBenhNhan là bắt buộc");

            if (string.IsNullOrWhiteSpace(request.SoDienThoai))
                throw new ArgumentException("SoDienThoai là bắt buộc");

            if (request.NgayHen == default)
                throw new ArgumentException("NgayHen là bắt buộc");

            if (request.GioHen == default)
                throw new ArgumentException("GioHen là bắt buộc");

            if (string.IsNullOrWhiteSpace(request.TenBacSiKham))
                throw new ArgumentException("TenBacSiKham là bắt buộc");

            if (string.IsNullOrWhiteSpace(request.KhoaKham))
                throw new ArgumentException("KhoaKham là bắt buộc");

            var date = request.NgayHen.Date;
            var gio = request.GioHen;
            // Không cho tạo lịch trong quá khứ (so với thời điểm hiện tại)
            var now = DateTime.Now;
            var requestedDateTime = date.Add(gio);

            if (requestedDateTime < now)
            {
                throw new InvalidOperationException(
                    "Không thể tạo lịch hẹn trong quá khứ. Vui lòng chọn ngày giờ lớn hơn hiện tại.");
            }
            // 1. Tìm bác sĩ theo tên + khoa
            var bacSi = await _db.NhanVienYTes
                .AsNoTracking()
                .FirstOrDefaultAsync(nv =>
                    nv.VaiTro == "bac_si"
                    && nv.HoTen == request.TenBacSiKham
                    && nv.KhoaChuyenMon.TenKhoa == request.KhoaKham); // giả định KhoaKham = TenKhoa

            if (bacSi is null)
                throw new ArgumentException("Không tìm thấy bác sĩ phù hợp với TenBacSiKham và KhoaKham");

            // 2. Tìm phòng mà bác sĩ phụ trách
            var phongIds = await _db.Phongs
                .AsNoTracking()
                .Where(p => p.MaBacSiPhuTrach == bacSi.MaNhanVien)
                .Select(p => p.MaPhong)
                .ToListAsync();

            if (phongIds.Count == 0)
                throw new ArgumentException("Bác sĩ không có phòng phụ trách phù hợp");

            // 3. Tìm lịch trực của phòng trong ngày + khung giờ
            var lichTruc = await _db.LichTrucs
                .AsNoTracking()
                .Where(lt => phongIds.Contains(lt.MaPhong)
                             && lt.Ngay.Date == date
                             && !lt.NghiTruc
                             && lt.GioBatDau <= gio
                             && lt.GioKetThuc > gio)
                .OrderBy(lt => lt.GioBatDau)
                .FirstOrDefaultAsync();

            if (lichTruc is null)
                throw new InvalidOperationException("Không tìm thấy lịch trực phù hợp với ngày/giờ và bác sĩ đã chọn");


            // 5. Khởi tạo entity
            var maLichHen = GeneratorID.NewLichHenId();
            var trangThai = string.IsNullOrWhiteSpace(request.TrangThai)
                ? "dang_cho"
                : request.TrangThai.Trim().ToLowerInvariant();

            var entity = new LichHenKham
            {
                MaLichHen = maLichHen,
                CoHieuLuc = true,
                NgayHen = date,
                GioHen = gio,
                ThoiLuongPhut = 30, // duration mặc định 30 phút, FE không cần truyền
                MaBenhNhan = request.MaBenhNhan,
                LoaiHen = request.LoaiHen,
                TenBenhNhan = request.TenBenhNhan,
                SoDienThoai = (request.SoDienThoai ?? string.Empty).Trim(),
                MaLichTruc = lichTruc.MaLichTruc,
                GhiChu = request.GhiChu,
                TrangThai = trangThai
            };

            bool hasConflict = false;
            string? conflictMessage = null;

            // 6. Nếu TẠO MỚI mà trạng thái là "da_xac_nhan"
            //    → nếu bị trùng thì vẫn tạo nhưng hạ xuống "dang_cho" và bắn thông báo lỗi
            if (string.Equals(trangThai, "da_xac_nhan", StringComparison.OrdinalIgnoreCase))
            {
                var (phoneConflict, timeConflict) = await FindConflictsForConfirmedAsync(
                    entity,
                    entity.SoDienThoai,
                    checkPhone: true,
                    checkTime: true);

                if (phoneConflict != null || timeConflict != null)
                {
                    hasConflict = true;
                    // hạ trạng thái xuống "đang chờ"
                    entity.TrangThai = "dang_cho";

                    if (phoneConflict != null)
                    {
                        conflictMessage =
                            $"Lịch hẹn bị trùng số điện thoại với một lịch đã được xác nhận vào {phoneConflict.NgayHen:dd/MM/yyyy} lúc {phoneConflict.GioHen:hh\\:mm}. Đã tạo lịch mới ở trạng thái ĐANG CHỜ.";
                    }
                    else if (timeConflict != null)
                    {
                        conflictMessage =
                            $"Lịch hẹn bị trùng khung giờ với một lịch đã được xác nhận vào {timeConflict.NgayHen:dd/MM/yyyy} lúc {timeConflict.GioHen:hh\\:mm}. Đã tạo lịch mới ở trạng thái ĐANG CHỜ.";
                    }
                }
            }

            // 7. Lưu DB (luôn tạo)
            _db.LichHenKhams.Add(entity);
            await _db.SaveChangesAsync();

            // 8. Reload nav, map DTO, realtime, dashboard ...
            var saved = await _db.LichHenKhams
                .Include(l => l.LichTruc)
                    .ThenInclude(lt => lt.Phong)
                        .ThenInclude(p => p.KhoaChuyenMon)
                .Include(l => l.LichTruc)
                    .ThenInclude(lt => lt.Phong)
                        .ThenInclude(p => p.BacSiPhuTrach)
                .AsNoTracking()
                .FirstAsync(l => l.MaLichHen == entity.MaLichHen);

            var dto = MapToDto(saved);

            await _realtime.BroadcastAppointmentChangedAsync(dto);
            await TaoThongBaoLichHenChoBacSiAsync(dto, "created");
            var dashboard = await _dashboard.LayDashboardHomNayAsync();
            await _realtime.BroadcastDashboardTodayAsync(dashboard);
            //await _realtime.BroadcastTodayAppointmentsKpiAsync(dashboard.LichHenHomNay);
            //await _realtime.BroadcastUpcomingAppointmentsAsync(dashboard.LichHenSapToi);
            //await _realtime.BroadcastRecentActivitiesAsync(dashboard.HoatDongGanDay);

            // 9. Nếu có trùng → ném lỗi để FE catch và toast, nhưng lịch vẫn đã được tạo
            if (hasConflict)
            {
                throw new InvalidOperationException(
                    conflictMessage ??
                    "Lịch hẹn bị trùng thông tin với một lịch đã xác nhận. Đã tạo lịch mới ở trạng thái ĐANG CHỜ.");
            }

            return dto;
        }



        // ========= LẤY CHI TIẾT LỊCH HẸN =========
        public async Task<AppointmentReadRequestDto> LayLichHenAsync(string maLichHen)
        {
            var entity = await _db.LichHenKhams
                .Include(l => l.LichTruc)
                    .ThenInclude(lt => lt.Phong)
                        .ThenInclude(p => p.KhoaChuyenMon)
                .Include(l => l.LichTruc)
                    .ThenInclude(lt => lt.Phong)
                        .ThenInclude(p => p.BacSiPhuTrach)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.MaLichHen == maLichHen);

            if (entity is null)
                throw new KeyNotFoundException($"Không tìm thấy lịch hẹn {maLichHen}");

            return MapToDto(entity);
        }

        // ========= CẬP NHẬT TRẠNG THÁI LỊCH HẸN =========
        public async Task<AppointmentReadRequestDto?> CapNhatTrangThaiLichHenAsync(
    string maLichHen,
    AppointmentStatusUpdateRequest request)
        {
            var entity = await _db.LichHenKhams
                .Include(l => l.LichTruc)
                    .ThenInclude(lt => lt.Phong)
                        .ThenInclude(p => p.KhoaChuyenMon)
                .Include(l => l.LichTruc)
                    .ThenInclude(lt => lt.Phong)
                        .ThenInclude(p => p.BacSiPhuTrach)
                .FirstOrDefaultAsync(l => l.MaLichHen == maLichHen);

            if (entity is null)
                return null;

            // 🚫 Nếu lịch đã check-in rồi thì không cho cập nhật trạng thái nữa
            if (string.Equals(entity.TrangThai, "da_checkin", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Lịch hẹn đã được check-in, không thể cập nhật trạng thái nữa.");
            }

            var targetStatus = request.TrangThai;

            // ✳️ Chỉ check trùng khi TRẠNG THÁI INPUT là "da_xac_nhan"
            if (string.Equals(targetStatus, "da_xac_nhan", StringComparison.OrdinalIgnoreCase))
            {
                await EnsureNoPhoneOrTimeDuplicateForConfirmedAsync(entity, targetStatus);
            }

            entity.TrangThai = targetStatus;
            await _db.SaveChangesAsync();

            var dto = MapToDto(entity);

            try
            {
                await _realtime.BroadcastAppointmentChangedAsync(dto);
                await TaoThongBaoLichHenChoBacSiAsync(dto, "status_changed");
                var dashboard = await _dashboard.LayDashboardHomNayAsync();
                await _realtime.BroadcastDashboardTodayAsync(dashboard);
                //await _realtime.BroadcastTodayAppointmentsKpiAsync(dashboard.LichHenHomNay);
                //await _realtime.BroadcastUpcomingAppointmentsAsync(dashboard.LichHenSapToi);
                //await _realtime.BroadcastRecentActivitiesAsync(dashboard.HoatDongGanDay);
            }
            catch
            {
                // bỏ qua lỗi realtime
            }

            return dto;
        }




        // ========= CẬP NHẬT THÔNG TIN LỊCH HẸN =========
        public async Task<AppointmentReadRequestDto?> CapNhatLichHenAsync(
    string maLichHen,
    AppointmentUpdateRequest request)
        {
            var entity = await _db.LichHenKhams
                .Include(l => l.LichTruc)
                    .ThenInclude(lt => lt.Phong)
                        .ThenInclude(p => p.KhoaChuyenMon)
                .Include(l => l.LichTruc)
                    .ThenInclude(lt => lt.Phong)
                        .ThenInclude(p => p.BacSiPhuTrach)
                .FirstOrDefaultAsync(l => l.MaLichHen == maLichHen);

            if (entity is null)
                return null;

            // Ngày/giờ sau cập nhật (chưa SaveChanges)
            var nextDate = request.NgayHen.HasValue
                ? request.NgayHen.Value.Date
                : entity.NgayHen;

            var nextTime = request.GioHen.HasValue
                ? request.GioHen.Value
                : entity.GioHen;

            // Không cho cập nhật lịch về thời điểm trong quá khứ
            var now = DateTime.Now;
            var nowDate = now.Date;
            var nowTime = now.TimeOfDay;

            if (nextDate < nowDate || (nextDate == nowDate && nextTime < nowTime))
            {
                throw new InvalidOperationException(
                    "Không thể cập nhật lịch hẹn về thời điểm trong quá khứ. Vui lòng chọn ngày giờ lớn hơn hiện tại.");
            }
            var targetStatus = entity.TrangThai;

            // Nếu lịch hiện tại ĐÃ XÁC NHẬN → đổi giờ/ngày chỉ cần check trùng GIỜ (không check SĐT)
            if (string.Equals(targetStatus, "da_xac_nhan", StringComparison.OrdinalIgnoreCase))
            {
                var temp = new LichHenKham
                {
                    MaLichHen = entity.MaLichHen,
                    SoDienThoai = entity.SoDienThoai,
                    NgayHen = nextDate,
                    GioHen = nextTime,
                    ThoiLuongPhut = entity.ThoiLuongPhut > 0 ? entity.ThoiLuongPhut : 30, // default 30'
                    CoHieuLuc = entity.CoHieuLuc,
                    MaLichTruc = entity.MaLichTruc,
                    TrangThai = targetStatus
                };

                var (_, timeConflict) = await FindConflictsForConfirmedAsync(
                    temp,
                    phoneOverride: null,
                    checkPhone: false,   // ❌ KHÔNG check trùng SĐT
                    checkTime: true);    // ✅ chỉ check trùng giờ

                if (timeConflict != null)
                {
                    throw new InvalidOperationException(
                        $"Không thể cập nhật lịch hẹn: giờ hẹn {temp.GioHen:hh\\:mm} đã trùng với một lịch hẹn được xác nhận khác vào {timeConflict.NgayHen:dd/MM/yyyy}.");
                }
            }

            // Thực sự cập nhật entity
            entity.NgayHen = nextDate;

            if (request.GioHen.HasValue)
                entity.GioHen = request.GioHen.Value;

            if (!string.IsNullOrWhiteSpace(request.GhiChu))
                entity.GhiChu = request.GhiChu;

            await _db.SaveChangesAsync();

            var dto = MapToDto(entity);

            try
            {
                await _realtime.BroadcastAppointmentChangedAsync(dto);
                await TaoThongBaoLichHenChoBacSiAsync(dto, "updated");
                var dashboard = await _dashboard.LayDashboardHomNayAsync();
                await _realtime.BroadcastDashboardTodayAsync(dashboard);
                //await _realtime.BroadcastTodayAppointmentsKpiAsync(dashboard.LichHenHomNay);
                //await _realtime.BroadcastUpcomingAppointmentsAsync(dashboard.LichHenSapToi);
                //await _realtime.BroadcastRecentActivitiesAsync(dashboard.HoatDongGanDay);
            }
            catch
            {
                // ignore realtime error
            }

            return dto;
        }




        // ========= MAP ENTITY -> DTO =========
        private static AppointmentReadRequestDto MapToDto(LichHenKham l)
        {
            var phong = l.LichTruc?.Phong;
            var khoa = phong?.KhoaChuyenMon;
            var bacSi = phong?.BacSiPhuTrach;

            return new AppointmentReadRequestDto
            {
                MaLichHen = l.MaLichHen,
                NgayHen = l.NgayHen,
                GioHen = l.GioHen,
                MaBenhNhan = l.MaBenhNhan,
                LoaiHen = l.LoaiHen,
                TenBenhNhan = l.TenBenhNhan,
                SoDienThoai = l.SoDienThoai,
                MaBacSiKham = bacSi?.MaNhanVien ?? string.Empty,
                TenBacSiKham = bacSi?.HoTen ?? string.Empty,
                KhoaKham = khoa?.TenKhoa ?? string.Empty,
                MaLichTruc = l.MaLichTruc,
                GhiChu = l.GhiChu,
                TrangThai = l.TrangThai
            };
        }

        private static string NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
            return new string(phone.Where(char.IsDigit).ToArray());
        }

        /// <summary>
        /// Tìm conflict cho lịch đã xác nhận:
        ///   - trùng SĐT (tuỳ chọn)
        ///   - trùng giờ (tuỳ chọn, overlap theo ThoiLuongPhut - mặc định 30 phút)
        /// </summary>
        private async Task<(LichHenKham? phoneConflict, LichHenKham? timeConflict)>
            FindConflictsForConfirmedAsync(
                LichHenKham entity,
                string? phoneOverride,
                bool checkPhone,
                bool checkTime)
        {
            var phoneNorm = NormalizePhone(phoneOverride ?? entity.SoDienThoai);
            var hasPhoneToCheck = checkPhone && !string.IsNullOrEmpty(phoneNorm);
            var hasTimeToCheck = checkTime && entity.NgayHen != default && entity.GioHen != default;

            if (!hasPhoneToCheck && !hasTimeToCheck)
                return (null, null);

            var today = DateTime.Today;
            var now = DateTime.Now.TimeOfDay;

            var candidates = await _db.LichHenKhams
                .AsNoTracking()
                .Where(l =>
                    l.CoHieuLuc &&
                    l.MaLichHen != entity.MaLichHen &&      // bỏ qua chính nó
                    l.TrangThai == "da_xac_nhan" &&         // chỉ lịch ĐÃ XÁC NHẬN
                    (l.NgayHen > today ||
                     (l.NgayHen == today && l.GioHen >= now)))
                .ToListAsync();

            LichHenKham? phoneConflict = null;
            if (hasPhoneToCheck)
            {
                phoneConflict = candidates
                    .FirstOrDefault(l => NormalizePhone(l.SoDienThoai) == phoneNorm);
            }

            LichHenKham? timeConflict = null;
            if (hasTimeToCheck)
            {
                var startB = entity.GioHen;
                var durB = entity.ThoiLuongPhut > 0 ? entity.ThoiLuongPhut : 30;
                var endB = startB + TimeSpan.FromMinutes(durB);

                timeConflict = candidates.FirstOrDefault(l =>
                {
                    if (l.NgayHen.Date != entity.NgayHen.Date)
                        return false;

                    var startA = l.GioHen;
                    var durA = l.ThoiLuongPhut > 0 ? l.ThoiLuongPhut : 30;
                    var endA = startA + TimeSpan.FromMinutes(durA);

                    return startA < endB && startB < endA; // overlap
                });
            }

            return (phoneConflict, timeConflict);
        }
        /// <summary>
        /// Kiểm tra conflict cho lịch với trạng thái mục tiêu "da_xac_nhan".
        /// Conflict nếu:
        ///  - Trùng số điện thoại HOẶC
        ///  - Trùng giờ hẹn (overlap khung 30 phút mặc định, hoặc ThoiLuongPhut nếu có)
        /// So sánh với:
        ///  - Chỉ lịch CoHieuLuc = true
        ///  - TrangThai = "da_xac_nhan"
        ///  - Từ thời điểm hiện tại trở đi
        ///  - Bỏ qua chính lịch hiện tại (MaLichHen)
        /// </summary>
        private async Task EnsureNoPhoneOrTimeDuplicateForConfirmedAsync(
            LichHenKham entity,
            string targetStatus,
            string? phoneOverride = null)
        {
            // ❗ Chỉ check nếu TRẠNG THÁI MỤC TIÊU là "da_xac_nhan"
            if (!string.Equals(targetStatus, "da_xac_nhan", StringComparison.OrdinalIgnoreCase))
                return;

            var (phoneConflict, timeConflict) = await FindConflictsForConfirmedAsync(
                entity,
                phoneOverride,
                checkPhone: true,
                checkTime: true);

            if (phoneConflict is null && timeConflict is null)
                return;

            if (phoneConflict != null)
            {
                throw new InvalidOperationException(
                    $"Không thể xác nhận lịch hẹn: số điện thoại {entity.SoDienThoai} đã có lịch hẹn được xác nhận vào {phoneConflict.NgayHen:dd/MM/yyyy} lúc {phoneConflict.GioHen:hh\\:mm}.");
            }

            if (timeConflict != null)
            {
                throw new InvalidOperationException(
                    $"Không thể xác nhận lịch hẹn: giờ hẹn {entity.GioHen:hh\\:mm} đã trùng với một lịch hẹn được xác nhận khác vào {timeConflict.NgayHen:dd/MM/yyyy}.");
            }
        }



        private async Task DeactivateOldAppointmentsAsync()
        {
            var today = DateTime.Today;
            var now = DateTime.Now.TimeOfDay;

            // 1. Giữ nguyên: các lịch trước hôm nay -> hết hiệu lực (CoHieuLuc = false)
            await _db.LichHenKhams
                .Where(l => l.CoHieuLuc && l.NgayHen < today)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(l => l.CoHieuLuc, false)
                );

            // 2. Lịch hôm nay, trạng thái "đang chờ" và đã quá giờ hẹn -> hủy luôn
            await _db.LichHenKhams
                .Where(l =>
                    l.CoHieuLuc &&
                    l.NgayHen == today &&
                    l.TrangThai == "dang_cho" &&
                    l.GioHen < now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(l => l.TrangThai, "da_huy")
                );

            // 3. Lịch hôm nay, trạng thái "đã xác nhận":
            //    chỉ hủy nếu đã trễ hơn giờ hẹn 30 phút
            var cutoffForConfirmed = now - TimeSpan.FromMinutes(30);

            if (cutoffForConfirmed > TimeSpan.Zero)
            {
                await _db.LichHenKhams
                    .Where(l =>
                        l.CoHieuLuc &&
                        l.NgayHen == today &&
                        l.TrangThai == "da_xac_nhan" &&
                        l.GioHen < cutoffForConfirmed)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(l => l.TrangThai, "da_huy")
                    );
            }
        }

        private async Task TaoThongBaoLichHenChoBacSiAsync(
    AppointmentReadRequestDto lichHen,
    string action // "created" | "status_changed" | "updated"
)
        {
            if (lichHen == null) return;
            if (string.IsNullOrWhiteSpace(lichHen.MaBacSiKham)) return;

            // Build tiêu đề + nội dung theo loại hành động
            string title;
            string body;
            var trangThaiText = lichHen.TrangThai.ToString() switch
            {
                "dang_cho" => "Đang chờ",
                "da_xac_nhan" => "Đã xác nhận",
                _ => ""
            };

            switch (action)
            {
                case "created":
                    title = "Lịch hẹn mới";
                    body =
                        $"Bạn có lịch hẹn mới với bệnh nhân {lichHen.TenBenhNhan} " +
                        // ❗ GioHen là TimeSpan -> dùng hh:mm (24h), KHÔNG có 'tt'
                        $"vào {lichHen.NgayHen:dd/MM/yyyy} lúc {lichHen.GioHen:hh\\:mm}.";
                    break;

                case "status_changed":
                    title = "Trạng thái lịch hẹn thay đổi";
                    body =
                        $"Lịch hẹn với bệnh nhân {lichHen.TenBenhNhan} " +
                        $"vào {lichHen.NgayHen:dd/MM/yyyy} {lichHen.GioHen:hh\\:mm} " +
                        $"đã được cập nhật trạng thái: {trangThaiText}.";
                    break;

                default:
                    title = "Lịch hẹn được cập nhật";
                    body =
                        $"Lịch hẹn với bệnh nhân {lichHen.TenBenhNhan} " +
                        $"vào {lichHen.NgayHen:dd/MM/yyyy} {lichHen.GioHen:hh\\:mm} đã được chỉnh sửa.";
                    break;
            }

            // Build request dùng đúng cấu trúc NotificationService đang xử lý
            var request = new NotificationCreateRequest
            {
                LoaiThongBao = "lich_hen",
                TieuDe = title,
                NoiDung = body,
                MucDoUuTien = "normal",

                // Map sang NguonLienQuan + MaDoiTuongLienQuan nếu anh muốn
                NguonLienQuan = "lich_hen",
                MaDoiTuongLienQuan = lichHen.MaLichHen,

                // 1 người nhận: bác sĩ (nhân viên y tế)
                NguoiNhan = new List<NotificationRecipientCreateRequest>
        {
            new NotificationRecipientCreateRequest
            {
                LoaiNguoiNhan = "bac_si",
                MaNguoiNhan = lichHen.MaBacSiKham
            }
        }
            };

            // Ghi vào DB qua NotificationService
            await _notifications.TaoThongBaoAsync(request);
        }

    }
}
