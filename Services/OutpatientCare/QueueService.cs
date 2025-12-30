using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using HealthCare.RenderID;
using HealthCare.Realtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;
using HealthCare.Services.PatientManagement;

namespace HealthCare.Services.OutpatientCare
{
    public class QueueService(DataContext db, IRealtimeService realtime, IPatientService patients, IServiceProvider provider) : IQueueService
    {
        private readonly DataContext _db = db;
        private readonly IRealtimeService _realtime = realtime;
        private readonly IPatientService _patients = patients;
        private readonly IServiceProvider _provider = provider;

        private async Task<QueueItemDto> BroadcastAndReturnAsync(HangDoi entity)
        {
            var dto = await MapToDtoAsync(entity);
            var roomItems = await LayHangDoiTheoPhongAsync(dto.MaPhong);
            await _realtime.BroadcastQueueByRoomAsync(dto.MaPhong, roomItems);
            await _realtime.BroadcastQueueItemChangedAsync(dto);
            return dto;
        }

        private void ApplyQueueUpdateFields(
            HangDoi entity,
            QueueEnqueueRequest request,
            bool isClsQueue,
            int doUuTien,
            string? phanLoaiDen,
            DateTime now)
        {
            entity.MaBenhNhan = request.MaBenhNhan;
            entity.MaPhong = request.MaPhong;
            entity.LoaiHangDoi = request.LoaiHangDoi;
            entity.Nguon = isClsQueue ? null : request.Nguon;
            entity.Nhan = isClsQueue ? null : request.Nhan;
            entity.CapCuu = request.CapCuu;
            entity.PhanLoaiDen = isClsQueue ? null : phanLoaiDen;
            entity.ThoiGianLichHen = isClsQueue ? null : request.ThoiGianLichHen;
            entity.DoUuTien = doUuTien;
            entity.MaPhieuKham = request.MaPhieuKham;
            entity.MaChiTietDv = request.MaChiTietDv;
            entity.ThoiGianCheckin = now;
        }

        // ====== Helper: phân loại đến theo lịch hẹn ======
        private static string? TinhPhanLoaiDen(DateTime now, DateTime? lichHen)
        {
            if (!lichHen.HasValue) return null;

            var diff = now - lichHen.Value; // >0: đến trễ

            if (diff.TotalMinutes > 30)
                return "den_muon";
            if (diff.TotalMinutes < -15)
                return "den_som";

            return "dung_gio";
        }

        /// <summary>
        /// BE tự tính độ ưu tiên:
        /// - CapCuu           -> group 0
        /// - service_return   -> group 1
        /// - appointment      -> group 2 (trừ khi đến muộn >30p thì xuống group 3)
        /// - walkin/khác      -> group 3
        /// DoUuTien = group * 10
        /// </summary>
        public int TinhDoUuTien(QueueEnqueueRequest request)
        {
            // 1. Cấp cứu luôn trên hết
            if (request.CapCuu)
                return 0; // group 0

            var source = (request.Nguon ?? string.Empty).ToLowerInvariant();
            int group;

            switch (source)
            {
                case "service_return":
                    group = 1;
                    break;

                case "appointment":
                    // Appointment đến muộn >30p => coi như walkin
                    if (string.Equals(request.PhanLoaiDen, "den_muon", StringComparison.OrdinalIgnoreCase))
                        group = 3;
                    else
                        group = 2;
                    break;

                default:
                    group = 3; // walkin / unknown
                    break;
            }

            return group * 10;
        }

        // ====== THÊM VÀO HÀNG ĐỢI ======
        public async Task<QueueItemDto> ThemVaoHangDoiAsync(QueueEnqueueRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.MaBenhNhan))
                throw new ArgumentException("MaBenhNhan là bắt buộc");
            var isClsQueue = string.Equals(request.LoaiHangDoi, "can_lam_sang", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(request.MaPhong))
                throw new ArgumentException("MaPhong là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.LoaiHangDoi))
                throw new ArgumentException("LoaiHangDoi là bắt buộc");
            if (!isClsQueue && string.IsNullOrWhiteSpace(request.Nguon))
                throw new ArgumentException("Nguon là bắt buộc");

            // Kiểm tra tồn tại cơ bản
            var bnExists = await _db.BenhNhans
                .AnyAsync(b => b.MaBenhNhan == request.MaBenhNhan);
            if (!bnExists)
                throw new ArgumentException($"Không tìm thấy bệnh nhân {request.MaBenhNhan}");

            var phongExists = await _db.Phongs
                .AnyAsync(p => p.MaPhong == request.MaPhong);
            if (!phongExists)
                throw new ArgumentException($"Không tìm thấy phòng {request.MaPhong}");

            // Tránh duplicate theo phiếu khám (unique key MaPhieuKham)
            if (!string.IsNullOrWhiteSpace(request.MaPhieuKham))
            {
                var existedByPhieu = await _db.HangDois
                    .FirstOrDefaultAsync(h => h.MaPhieuKham == request.MaPhieuKham);
                if (existedByPhieu is not null)
                {
                    return await MapToDtoAsync(existedByPhieu);
                }
            }

            // Tránh duplicate theo ChiTietDV (queue CLS 1-1 với chi tiết DV)
            if (!string.IsNullOrWhiteSpace(request.MaChiTietDv))
            {
                var existedByCt = await _db.HangDois
                    .FirstOrDefaultAsync(h => h.MaChiTietDv == request.MaChiTietDv);
                if (existedByCt is not null)
                {
                    // Trả về queue cũ để BE/FE biết, không tạo queue mới
                    return await MapToDtoAsync(existedByCt);
                }
            }

            var now = DateTime.Now;

            // 🔁 Ưu tiên dùng phân loại đến đã được tính sẵn (nếu có)
            var phanLoaiDen = request.PhanLoaiDen;

            if (isClsQueue)
            {
                // CLS: không dùng nguồn/nhãn/phan_loai_den/lich_hen, ưu tiên 0, không cấp cứu
                request.Nguon = null;
                request.Nhan = null;
                request.ThoiGianLichHen = null;
                request.CapCuu = false;
                phanLoaiDen = null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(phanLoaiDen))
                {
                    // Fallback: nếu BE không được truyền vào thì tự tính như cũ
                    phanLoaiDen = TinhPhanLoaiDen(now, request.ThoiGianLichHen);
                }
                request.PhanLoaiDen = phanLoaiDen;
            }
            // BE tự tính độ ưu tiên: CLS = 0, còn lại theo nguồn
            var doUuTien = isClsQueue ? 0 : TinhDoUuTien(request);

            // Nếu đã có hàng đợi đang thực hiện cho đúng bệnh nhân + phòng -> cập nhật thông tin thay vì tạo mới
            var existingActive = await _db.HangDois
                .FirstOrDefaultAsync(h =>
                    h.MaBenhNhan == request.MaBenhNhan &&
                    h.MaPhong == request.MaPhong &&
                    h.TrangThai == "dang_thuc_hien");

            if (existingActive is not null)
            {
                // Hàng chờ quay lại LS (service_return): bỏ thông tin lịch hẹn/phan_loai, ưu tiên cao, đưa lại "cho_goi"
                request.Nguon ??= "service_return";
                request.ThoiGianLichHen = null;
                request.PhanLoaiDen = null;
                request.CapCuu = false;
                doUuTien = isClsQueue ? 0 : TinhDoUuTien(request);

                existingActive.MaBenhNhan = request.MaBenhNhan;
                existingActive.MaPhong = request.MaPhong;
                existingActive.LoaiHangDoi = request.LoaiHangDoi;
                existingActive.Nguon = isClsQueue ? null : request.Nguon;
                existingActive.Nhan = isClsQueue ? null : request.Nhan;
                existingActive.PhanLoaiDen = null;
                existingActive.ThoiGianLichHen = null;
                existingActive.ThoiGianCheckin = now;
                existingActive.CapCuu = false;
                existingActive.DoUuTien = doUuTien;
                existingActive.TrangThai = "cho_goi";
                await _db.SaveChangesAsync();

                if (request.LoaiHangDoi == "kham_ls")
                {
                    await _patients.CapNhatTrangThaiBenhNhanAsync(
                        request.MaBenhNhan,
                        new PatientStatusUpdateRequest { TrangThaiHomNay = "cho_kham" });
                }

                return await BroadcastAndReturnAsync(existingActive);
            }

            var entity = new HangDoi
            {
                MaHangDoi = GeneratorID.NewHangDoiId(),
                MaBenhNhan = request.MaBenhNhan,
                MaPhong = request.MaPhong,
                LoaiHangDoi = request.LoaiHangDoi,
                Nguon = isClsQueue ? null : request.Nguon,
                Nhan = isClsQueue ? null : request.Nhan,
                CapCuu = request.CapCuu,
                PhanLoaiDen = phanLoaiDen,
                ThoiGianCheckin = now,
                ThoiGianLichHen = request.ThoiGianLichHen,
                DoUuTien = doUuTien,
                TrangThai = "cho_goi",
                GhiChu = null,
                MaPhieuKham = request.MaPhieuKham,
                MaChiTietDv = request.MaChiTietDv
            };

            _db.HangDois.Add(entity);
            await _db.SaveChangesAsync();
            // 🔥 Cập nhật trạng thái hôm nay của bệnh nhân bằng PatientService
                // để tận dụng realtime + notification
                if (request.LoaiHangDoi == "kham_ls")
                    {
                await _patients.CapNhatTrangThaiBenhNhanAsync(
                request.MaBenhNhan,
                new PatientStatusUpdateRequest
                            {
                    TrangThaiHomNay = "cho_kham"
                                });
                    }
            var saved = await _db.HangDois
                .AsNoTracking()
                .FirstAsync(h => h.MaHangDoi == entity.MaHangDoi);

            var dto = await MapToDtoAsync(saved);

            // Realtime: cập nhật phòng + item
            var roomItems = await LayHangDoiTheoPhongAsync(dto.MaPhong);
            await _realtime.BroadcastQueueByRoomAsync(dto.MaPhong, roomItems);
            await _realtime.BroadcastQueueItemChangedAsync(dto);

            return dto;
        }

        public async Task<QueueItemDto?> LayHangDoiAsync(string maHangDoi)
        {
            var entity = await _db.HangDois
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.MaHangDoi == maHangDoi);

            return entity is null ? null : await MapToDtoAsync(entity);
        }

        public async Task<IReadOnlyList<QueueItemDto>> LayHangDoiTheoPhongAsync(
            string maPhong,
            string? loaiHangDoi = null,
            string? trangThai = null)
        {
            var query = _db.HangDois
                .AsNoTracking()
                .Where(h => h.MaPhong == maPhong);

            if (!string.IsNullOrWhiteSpace(loaiHangDoi))
                query = query.Where(h => h.LoaiHangDoi == loaiHangDoi);

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(h => h.TrangThai == trangThai);

            var list = await query.ToListAsync();

            var ordered = list
                .OrderBy(h => h.DoUuTien) // group: 0,10,20,30
                .ThenBy(h =>
                {
                    var isAppt = string.Equals(h.Nguon, "appointment", StringComparison.OrdinalIgnoreCase);
                    var isLate = string.Equals(h.PhanLoaiDen, "den_muon", StringComparison.OrdinalIgnoreCase);

                    // Appointment đúng giờ / sớm vừa phải -> xếp theo giờ hẹn
                    if (isAppt && !isLate && h.ThoiGianLichHen.HasValue)
                        return h.ThoiGianLichHen.Value;

                    // Còn lại (appointment đến sớm >15, đến muộn, walkin, service_return…) -> theo checkin
                    return h.ThoiGianCheckin;
                })
                .ToList();

            var items = new List<QueueItemDto>();
            foreach (var h in ordered)
            {
                items.Add(await MapToDtoAsync(h));
            }

            return items;
        }

        public async Task<QueueItemDto?> CapNhatTrangThaiHangDoiAsync(
            string maHangDoi,
            QueueStatusUpdateRequest request)
        {
            var entity = await _db.HangDois
                .FirstOrDefaultAsync(h => h.MaHangDoi == maHangDoi);

            if (entity is null)
                return null;

            entity.TrangThai = request.TrangThai;
            await _db.SaveChangesAsync();

            var dto = await MapToDtoAsync(entity);

            var roomItems = await LayHangDoiTheoPhongAsync(dto.MaPhong);
            await _realtime.BroadcastQueueByRoomAsync(dto.MaPhong, roomItems);
            await _realtime.BroadcastQueueItemChangedAsync(dto);

            return dto;
        }

        public async Task<QueueItemDto?> CapNhatThongTinHangDoiAsync(
            string maHangDoi,
            QueueEnqueueRequest request)
        {
            if (string.IsNullOrWhiteSpace(maHangDoi))
                return null;

            var entity = await _db.HangDois.FirstOrDefaultAsync(h => h.MaHangDoi == maHangDoi);
            if (entity is null)
                return null;

            var isClsQueue = string.Equals(request.LoaiHangDoi, "can_lam_sang", StringComparison.OrdinalIgnoreCase);
            var now = DateTime.Now;
            var phanLoaiDen = request.PhanLoaiDen;

            if (!isClsQueue)
            {
                request.Nguon ??= "service_return";
                if (string.IsNullOrWhiteSpace(phanLoaiDen))
                    phanLoaiDen = null; // quay lại không giữ phân loại lịch hẹn
            }
            else
            {
                request.Nguon = null;
                request.Nhan = null;
                request.ThoiGianLichHen = null;
                request.CapCuu = false;
                phanLoaiDen = null;
            }

            var doUuTien = isClsQueue ? 0 : TinhDoUuTien(request);

            entity.MaBenhNhan = request.MaBenhNhan;
            entity.MaPhong = request.MaPhong;
            entity.LoaiHangDoi = request.LoaiHangDoi;
            entity.Nguon = isClsQueue ? null : request.Nguon;
            entity.Nhan = isClsQueue ? null : request.Nhan;
            entity.PhanLoaiDen = isClsQueue ? null : phanLoaiDen;
            entity.ThoiGianLichHen = isClsQueue ? null : request.ThoiGianLichHen;
            entity.ThoiGianCheckin = now;
            entity.DoUuTien = doUuTien;
            entity.TrangThai = "cho_goi";
            entity.MaPhieuKham = request.MaPhieuKham ?? entity.MaPhieuKham;
            entity.MaChiTietDv = request.MaChiTietDv ?? entity.MaChiTietDv;

            await _db.SaveChangesAsync();

            if (request.LoaiHangDoi == "kham_ls")
            {
                await _patients.CapNhatTrangThaiBenhNhanAsync(
                    request.MaBenhNhan,
                    new PatientStatusUpdateRequest { TrangThaiHomNay = "cho_kham" });
            }

            return await BroadcastAndReturnAsync(entity);
        }

        public async Task<QueueItemDto?> LayTiepTheoTrongPhongAsync(
            string maPhong,
            string? loaiHangDoi = null)
        {
            var query = _db.HangDois
                .Where(h => h.MaPhong == maPhong && h.TrangThai == "cho_goi");

            if (!string.IsNullOrWhiteSpace(loaiHangDoi))
                query = query.Where(h => h.LoaiHangDoi == loaiHangDoi);

            var list = await query.ToListAsync();
            if (!list.Any())
                return null;

            var next = list
                .OrderBy(h => h.DoUuTien)
                .ThenBy(h =>
                {
                    var isAppt = string.Equals(h.Nguon, "appointment", StringComparison.OrdinalIgnoreCase);
                    var isLate = string.Equals(h.PhanLoaiDen, "den_muon", StringComparison.OrdinalIgnoreCase);

                    if (isAppt && !isLate && h.ThoiGianLichHen.HasValue)
                        return h.ThoiGianLichHen.Value;

                    return h.ThoiGianCheckin;
                })
                .First();

            next.TrangThai = "dang_goi";
            await _db.SaveChangesAsync();

            var dto = await MapToDtoAsync(next);

            var roomItems = await LayHangDoiTheoPhongAsync(dto.MaPhong);
            await _realtime.BroadcastQueueByRoomAsync(dto.MaPhong, roomItems);
            await _realtime.BroadcastQueueItemChangedAsync(dto);

            return dto;
        }

        public async Task<PagedResult<QueueItemDto>> TimKiemHangDoiAsync(QueueSearchFilter filter)
        {
            var query = _db.HangDois.AsNoTracking().AsQueryable();

            // ====== Map vai trò + nhân sự sang phòng phụ trách nếu không truyền MaPhong ======
            if (string.IsNullOrWhiteSpace(filter.MaPhong) &&
                !string.IsNullOrWhiteSpace(filter.MaNhanSu) &&
                !string.IsNullOrWhiteSpace(filter.Vaitro))
            {
                var nhanSu = await _db.NhanVienYTes
                    .Include(n => n.PhongsPhuTrach)
                    .FirstOrDefaultAsync(n => n.MaNhanVien == filter.MaNhanSu);

                if (nhanSu is null)
                {
                    var pageEmpty = filter.Page <= 0 ? 1 : filter.Page;
                    var pageSizeEmpty = filter.PageSize <= 0 ? 50 : filter.PageSize; // ✅ Chuẩn hóa: 50 items mặc định

                    return new PagedResult<QueueItemDto>
                    {
                        Items = new List<QueueItemDto>(),
                        TotalItems = 0,
                        Page = pageEmpty,
                        PageSize = pageSizeEmpty
                    };
                }

                var role = filter.Vaitro.ToLowerInvariant();
                var isYta = role == "y_ta";
                var isHanhChinh = isYta && string.Equals(nhanSu.LoaiYTa, "hanhchinh", StringComparison.OrdinalIgnoreCase);

                if (isHanhChinh)
                {
                    // Y tá hành chính: xem tất cả phòng, không filter
                }
                else if (isYta)
                {
                    // Y tá LS/CLS: lấy phòng theo lịch trực hiện tại, fallback phòng phụ trách
                    var targetTime = filter.FromTime ?? DateTime.Now;
                    var lich = await _db.LichTrucs
                        .AsNoTracking()
                        .Where(l =>
                            l.MaYTaTruc == filter.MaNhanSu &&
                            !l.NghiTruc &&
                            l.Ngay == targetTime.Date &&
                            l.GioBatDau <= targetTime.TimeOfDay &&
                            l.GioKetThuc >= targetTime.TimeOfDay)
                        .OrderBy(l => l.Ngay)
                        .ThenBy(l => l.GioBatDau)
                        .FirstOrDefaultAsync();

                    var maPhongTruc = lich?.MaPhong ?? nhanSu.PhongsPhuTrach?.MaPhong;

                    if (string.IsNullOrWhiteSpace(maPhongTruc))
                    {
                        var pageEmpty = filter.Page <= 0 ? 1 : filter.Page;
                        var pageSizeEmpty = filter.PageSize <= 0 ? 50 : filter.PageSize; // ✅ Chuẩn hóa: 50 items mặc định

                        return new PagedResult<QueueItemDto>
                        {
                            Items = new List<QueueItemDto>(),
                            TotalItems = 0,
                            Page = pageEmpty,
                            PageSize = pageSizeEmpty
                        };
                    }

                    query = query.Where(h => h.MaPhong == maPhongTruc);
                }
                else if (role == "bac_si")
                {
                    var maPhongPhuTrach = nhanSu.PhongsPhuTrach?.MaPhong;
                    if (string.IsNullOrWhiteSpace(maPhongPhuTrach))
                    {
                        var pageEmpty = filter.Page <= 0 ? 1 : filter.Page;
                        var pageSizeEmpty = filter.PageSize <= 0 ? 50 : filter.PageSize; // ✅ Chuẩn hóa: 50 items mặc định

                        return new PagedResult<QueueItemDto>
                        {
                            Items = new List<QueueItemDto>(),
                            TotalItems = 0,
                            Page = pageEmpty,
                            PageSize = pageSizeEmpty
                        };
                    }

                    query = query.Where(h => h.MaPhong == maPhongPhuTrach);
                }
            }
            else if (!string.IsNullOrWhiteSpace(filter.MaPhong))
            {
                // Case cũ: FE truyền thẳng MaPhong
                query = query.Where(h => h.MaPhong == filter.MaPhong);
            }

            // ====== các filter còn lại giữ nguyên ======
            if (!string.IsNullOrWhiteSpace(filter.LoaiHangDoi))
                query = query.Where(h => h.LoaiHangDoi == filter.LoaiHangDoi);

            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
                query = query.Where(h => h.TrangThai == filter.TrangThai);

            if (filter.FromTime.HasValue)
            {
                // ✅ Convert UTC to local time before comparing with DB datetime (which is stored as local)
                var localFromTime = filter.FromTime.Value.ToLocalTime();
                query = query.Where(h => h.ThoiGianCheckin >= localFromTime);
            }

            if (filter.ToTime.HasValue)
            {
                // ✅ Convert UTC to local time before comparing with DB datetime (which is stored as local)
                var localToTime = filter.ToTime.Value.ToLocalTime();
                query = query.Where(h => h.ThoiGianCheckin <= localToTime);
            }

            var sortBy = filter.SortBy?.ToLowerInvariant();
            var sortDir = (filter.SortDirection ?? "asc").ToLowerInvariant();

            query = (sortBy, sortDir) switch
            {
                ("douutien", "desc") => query.OrderByDescending(h => h.DoUuTien)
                                             .ThenBy(h => h.ThoiGianCheckin),
                ("douutien", _) => query.OrderBy(h => h.DoUuTien)
                                        .ThenBy(h => h.ThoiGianCheckin),

                ("thoigianlichhen", "desc") => query.OrderByDescending(h => h.ThoiGianLichHen)
                                                    .ThenBy(h => h.ThoiGianCheckin),
                ("thoigianlichhen", _) => query.OrderBy(h => h.ThoiGianLichHen)
                                               .ThenBy(h => h.ThoiGianCheckin),

                ("thoigiancheckin", "desc") => query.OrderByDescending(h => h.ThoiGianCheckin),
                ("thoigiancheckin", _) => query.OrderBy(h => h.ThoiGianCheckin),

                _ when sortDir == "desc" => query.OrderByDescending(h => h.DoUuTien)
                                                 .ThenBy(h => h.ThoiGianCheckin),
                _ => query.OrderBy(h => h.DoUuTien)
                          .ThenBy(h => h.ThoiGianCheckin)
            };

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 50 : filter.PageSize; // ✅ Chuẩn hóa: 50 items mặc định

            var total = await query.CountAsync();

            var list = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = new List<QueueItemDto>();
            foreach (var h in list)
            {
                items.Add(await MapToDtoAsync(h));
            }

            return new PagedResult<QueueItemDto>
            {
                Items = items,
                TotalItems = total,
                Page = page,
                PageSize = pageSize
            };
        }


        // Đổi sang instance async mapper để gọi các service khác và trả thêm DTO đầy đủ
        private async Task<QueueItemDto> MapToDtoAsync(HangDoi h)
        {
            var dto = new QueueItemDto
            {
                MaHangDoi = h.MaHangDoi,
                MaBenhNhan = h.MaBenhNhan,
                MaPhong = h.MaPhong,
                LoaiHangDoi = h.LoaiHangDoi,
                Nguon = h.Nguon,
                Nhan = h.Nhan,
                CapCuu = h.CapCuu,
                PhanLoaiDen = h.PhanLoaiDen,
                ThoiGianCheckin = h.ThoiGianCheckin,
                ThoiGianLichHen = h.ThoiGianLichHen,
                DoUuTien = h.DoUuTien,
                TrangThai = h.TrangThai,
                MaPhieuKham = h.MaPhieuKham,
                MaChiTietDv = h.MaChiTietDv
            };

            // ===================== 1. PHIẾU KHÁM LS =====================
            if (!string.IsNullOrWhiteSpace(h.MaPhieuKham))
            {
                var pk = await _db.PhieuKhamLamSangs
                    .AsNoTracking()
                    .Include(p => p.BenhNhan)
                    .Include(p => p.BacSiKham)
                    .Include(p => p.DichVuKham)
                        .ThenInclude(dv => dv.PhongThucHien)
                            .ThenInclude(p => p.KhoaChuyenMon)
                    .FirstOrDefaultAsync(p => p.MaPhieuKham == h.MaPhieuKham);

                if (pk != null)
                {
                    dto.PhieuKhamLs = new QueueClinicalExamInfoDto
                    {
                        MaPhieuKham = pk.MaPhieuKham,
                        MaBenhNhan = pk.MaBenhNhan,
                        TenBenhNhan = pk.BenhNhan?.HoTen,
                        TenDichVuKham = pk.DichVuKham?.TenDichVu,
                        HinhThucTiepNhan = pk.HinhThucTiepNhan,
                        TrangThai = pk.TrangThai,
                        NgayLap = pk.NgayLap,
                        GioLap = pk.GioLap
                    };

                    // 🔥 Tên bệnh nhân cho top-level
                    dto.TenBenhNhan = pk.BenhNhan?.HoTen;

                    // 🔥 Bác sĩ khám (chỉ có ở phiếu LS)
                    dto.MaBacSiKham = pk.MaBacSiKham;
                    dto.TenBacSiKham = pk.BacSiKham?.HoTen;

                    // 🔥 Phòng/khoa lấy từ phòng thực hiện của dịch vụ khám
                    var phongDv = pk.DichVuKham?.PhongThucHien;
                    if (phongDv != null)
                    {
                        dto.MaPhong = phongDv.MaPhong;
                        dto.TenPhong = phongDv.TenPhong;
                        dto.MaKhoa = phongDv.MaKhoa;
                        dto.TenKhoa = phongDv.KhoaChuyenMon?.TenKhoa;
                        dto.LoaiPhong = phongDv.LoaiPhong;
                    }

                    try
                    {
                        var clinical = _provider.GetService<IClinicalService>();
                        if (clinical != null)
                            dto.PhieuKhamLsFull = await clinical.LayPhieuKhamAsync(pk.MaPhieuKham);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            // ===================== 2. PHIẾU KHÁM CLS =====================
            if (!string.IsNullOrWhiteSpace(h.MaChiTietDv))
            {
                var ct = await _db.ChiTietDichVus
                    .AsNoTracking()
                    .Include(c => c.PhieuKhamCanLamSang)
                        .ThenInclude(cls => cls.PhieuKhamLamSang)
                            .ThenInclude(ls => ls.BenhNhan)
                    .Include(c => c.DichVuYTe)
                        .ThenInclude(dv => dv.PhongThucHien)
                            .ThenInclude(p => p.KhoaChuyenMon)
                    .FirstOrDefaultAsync(c => c.MaChiTietDv == h.MaChiTietDv);

                if (ct?.PhieuKhamCanLamSang != null)
                {
                    var cls = ct.PhieuKhamCanLamSang;

                    dto.PhieuKhamCls = new QueueClsExamInfoDto
                    {
                        MaPhieuKhamCls = cls.MaPhieuKhamCls,
                        MaPhieuKhamLs = cls.MaPhieuKhamLs,
                        NgayGioLap = cls.NgayGioLap,
                        AutoPublishEnabled = cls.AutoPublishEnabled,
                        TrangThai = cls.TrangThai,
                        TenDichVuCls = ct.DichVuYTe?.TenDichVu
                    };

                    // 🔥 Phòng / khoa lấy từ phòng thực hiện dịch vụ CLS
                    var phongTh = ct.DichVuYTe?.PhongThucHien;
                    if (phongTh != null)
                    {
                        dto.MaPhong = phongTh.MaPhong;
                        dto.TenPhong = phongTh.TenPhong;
                        dto.MaKhoa = phongTh.MaKhoa;
                        dto.TenKhoa = phongTh.KhoaChuyenMon?.TenKhoa;
                        dto.LoaiPhong = phongTh.LoaiPhong;
                    }

                    // 🔥 Tên bệnh nhân (qua phiếu LS gốc)
                    if (cls.PhieuKhamLamSang?.BenhNhan != null)
                    {
                        dto.TenBenhNhan ??= cls.PhieuKhamLamSang.BenhNhan.HoTen;
                    }

                    try
                    {
                        var clsService = _provider.GetService<IClsService>();
                        if (clsService != null)
                            dto.PhieuKhamClsFull = await clsService.LayPhieuClsAsync(cls.MaPhieuKhamCls);
                    }
                    catch
                    {
                        // ignore
                    }

                    var (maYTaThucHien, tenYTaThucHien) =
                        await LayYTaTrucTheoPhongAsync(phongTh?.MaPhong);

                    dto.PhieuKhamClsItem = new ClsItemDto
                    {
                        MaChiTietDv = ct.MaChiTietDv,
                        MaPhieuKhamCls = ct.MaPhieuKhamCls,
                        MaDichVu = ct.MaDichVu,
                        TenDichVu = ct.DichVuYTe?.TenDichVu ?? "",
                        MaPhong = phongTh?.MaPhong ?? "",
                        TenPhong = phongTh?.TenPhong ?? "",
                        MaYTaThucHien = maYTaThucHien,
                        TenYTaThucHien = tenYTaThucHien,
                        LoaiDichVu = ct.DichVuYTe?.LoaiDichVu ?? "",
                        PhiDV = ct.DichVuYTe?.DonGia.ToString("0") ?? "0",
                        GhiChu = ct.GhiChu,
                        TrangThai = ct.TrangThai?.ToLowerInvariant() switch
                        {
                            "da_co_ket_qua" => "da_co_ket_qua",
                            "dang_thuc_hien" => "dang_thuc_hien",
                            _ => "chua_co_ket_qua"
                        }
                    };
                }
            }

            // ===================== 3. FALLBACK PHÒNG/KHOA THEO MA PHÒNG QUEUE =====================
            if (string.IsNullOrEmpty(dto.MaPhong))
            {
                var phong = await _db.Phongs
                    .AsNoTracking()
                    .Include(p => p.KhoaChuyenMon)
                    .FirstOrDefaultAsync(p => p.MaPhong == h.MaPhong);

                if (phong != null)
                {
                    dto.MaPhong = phong.MaPhong;
                    dto.TenPhong ??= phong.TenPhong;
                    dto.MaKhoa ??= phong.MaKhoa;
                    dto.TenKhoa ??= phong.KhoaChuyenMon?.TenKhoa;
                    dto.LoaiPhong ??= phong.LoaiPhong;
                }
            }

            // ===================== 4. FALLBACK TÊN BỆNH NHÂN =====================
            if (dto.TenBenhNhan == null)
            {
                var bn = await _db.BenhNhans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.MaBenhNhan == h.MaBenhNhan);

                if (bn != null)
                    dto.TenBenhNhan = bn.HoTen;
            }

            // CLS không có bác sĩ → giữ MaBacSiKham / TenBacSiKham = null nếu chưa set ở trên.

            return dto;
        }

        private async Task<(string? MaYTa, string? TenYTa)> LayYTaTrucTheoPhongAsync(
            string? maPhong,
            DateTime? thoiDiem = null)
        {
            if (string.IsNullOrWhiteSpace(maPhong))
                return (null, null);

            var at = thoiDiem ?? DateTime.Now;
            var ngay = at.Date;
            var gio = at.TimeOfDay;

            var lich = await _db.LichTrucs
                .AsNoTracking()
                .Include(l => l.YTaTruc)
                .Where(l =>
                    l.MaPhong == maPhong &&
                    !l.NghiTruc &&
                    l.Ngay == ngay &&
                    l.GioBatDau <= gio &&
                    l.GioKetThuc >= gio)
                .OrderBy(l => l.GioBatDau)
                .FirstOrDefaultAsync();

            return (lich?.MaYTaTruc, lich?.YTaTruc?.HoTen);
        }

    }
}
