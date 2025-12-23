using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using HealthCare.Realtime;
using HealthCare.RenderID;
using Microsoft.EntityFrameworkCore;
using HealthCare.Services.UserInteraction;
using HealthCare.Services.Report;
using HealthCare.Services.PatientManagement;
using HealthCare.Services.MedicationBilling;

namespace HealthCare.Services.OutpatientCare
{
    public class HistoryService(
    DataContext db,
    IRealtimeService realtime,
    INotificationService notifications, IDashboardService dashboard,
    IPatientService patients,IQueueService queue,
        IClinicalService clinical, IPharmacyService pharmacy) : IHistoryService
    {
        private readonly DataContext _db = db;
        private readonly IRealtimeService _realtime = realtime;
        private readonly IDashboardService _dashboard = dashboard;
        private readonly INotificationService _notifications = notifications;
        private readonly IPatientService _patients = patients;
        private readonly IQueueService _queue = queue;
        private readonly IClinicalService _clinical = clinical;
        private readonly IPharmacyService _pharmacy = pharmacy;

        // ==================== LIST LỊCH SỬ KHÁM (TAB "KHÁM BỆNH") ====================

        public async Task<PagedResult<HistoryVisitRecordDto>> LayLichSuAsync(HistoryFilterRequest filter)
        {
            var q = _db.LuotKhamBenhs
                .AsNoTracking()
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.BenhNhan)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.Phong)
                        .ThenInclude(p => p.KhoaChuyenMon)
                .Include(l => l.NhanSuThucHien) // bác sĩ thực hiện
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.PhieuKhamLamSang)
                        .ThenInclude(pk => pk.PhieuChanDoanCuoi)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.PhieuKhamLamSang)
                        .ThenInclude(pk => pk.DichVuKham)
                .AsQueryable();

            // ----- scope: hôm nay / khoảng thời gian -----
            DateTime? from = filter.FromTime;
            DateTime? to = filter.ToTime;

            if (filter.OnlyToday == true)
            {
                var today = DateTime.Today;
                from = today;
                to = today.AddDays(1).AddTicks(-1);
            }

            if (from.HasValue)
            {
                q = q.Where(l => l.ThoiGianBatDau >= from.Value);
            }

            if (to.HasValue)
            {
                q = q.Where(l => l.ThoiGianBatDau <= to.Value);
            }

            // ----- theo bệnh nhân -----
            if (!string.IsNullOrWhiteSpace(filter.MaBenhNhan))
            {
                var maBn = filter.MaBenhNhan.Trim();
                q = q.Where(l => l.HangDoi.MaBenhNhan == maBn);
            }

            // ----- loai luot: kham_lam_sang / can_lam_sang (dua vao loai phong) -----
            var filterLoaiLuot = filter.LoaiLuot?.ToLowerInvariant();
            bool? laCls = filterLoaiLuot switch
            {
                "can_lam_sang" or "service" => true,   // chap nhan gia tri cu
                "kham_lam_sang" or "clinic" => false,
                "all" or null => null,
                _ => null
            };

            if (laCls.HasValue)
            {
                q = q.Where(l =>
                    (l.HangDoi.Phong.LoaiPhong == "phong_dich_vu") == laCls.Value);
            }

            // ----- keyword toàn văn -----
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();

                q = q.Where(l =>
                    l.HangDoi.BenhNhan.HoTen.Contains(kw) ||
                    l.HangDoi.BenhNhan.MaBenhNhan.Contains(kw) ||
                    (l.NhanSuThucHien != null && l.NhanSuThucHien.HoTen.Contains(kw)) ||
                    (l.HangDoi.Phong.KhoaChuyenMon.TenKhoa.Contains(kw)) ||
                    (l.HangDoi.PhieuKhamLamSang != null &&
                        (l.HangDoi.PhieuKhamLamSang.TrieuChung ?? "").Contains(kw)) ||
                    (l.HangDoi.PhieuKhamLamSang != null &&
                        l.HangDoi.PhieuKhamLamSang.PhieuChanDoanCuoi != null &&
                        ((l.HangDoi.PhieuKhamLamSang.PhieuChanDoanCuoi.ChanDoanCuoi ?? "")
                            .Contains(kw))));
            }

            // ----- phân trang -----
            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 50 : filter.PageSize;

            var totalItems = await q.CountAsync();

            var data = await q
                .OrderByDescending(l => l.ThoiGianBatDau)
                .ThenByDescending(l => l.MaLuotKham)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = data.Select(MapToVisitRecord).ToList();

            return new PagedResult<HistoryVisitRecordDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        // ==================== CHI TIẾT 1 LẦN KHÁM ====================

        public async Task<HistoryVisitDetailDto?> LayChiTietLichSuKhamAsync(string maLuotKham)
        {
            var luot = await _db.LuotKhamBenhs
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.BenhNhan)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.Phong)
                        .ThenInclude(p => p.KhoaChuyenMon)
                .Include(l => l.NhanSuThucHien)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.PhieuKhamLamSang)
                        .ThenInclude(pk => pk.DichVuKham)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.PhieuKhamLamSang)
                        .ThenInclude(pk => pk.PhieuChanDoanCuoi)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.PhieuKhamLamSang)
                        .ThenInclude(pk => pk.PhieuKhamCanLamSang)
                            .ThenInclude(cls => cls.ChiTietDichVus)
                                .ThenInclude(ct => ct.DichVuYTe)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.PhieuKhamLamSang)
                        .ThenInclude(pk => pk.PhieuKhamCanLamSang)
                            .ThenInclude(cls => cls.ChiTietDichVus)
                                .ThenInclude(ct => ct.KetQuaDichVu)
                .FirstOrDefaultAsync(l => l.MaLuotKham == maLuotKham);

            if (luot == null)
                return null;

            var h = luot.HangDoi;
            var bn = h.BenhNhan;
            var phong = h.Phong;
            var khoa = phong.KhoaChuyenMon;
            var bs = luot.NhanSuThucHien;
            var phieuLs = h.PhieuKhamLamSang;
            var pcd = phieuLs?.PhieuChanDoanCuoi;
            var phieuCls = phieuLs?.PhieuKhamCanLamSang;
            var phieuTongHop = phieuLs?.PhieuTongHopKetQua ?? phieuCls?.PhieuTongHopKetQua;

            bool laDichVu = phong.LoaiPhong == "phong_dich_vu";

            // Exam rows (tóm tắt khám)
            var examRows = new List<HistoryExamRowDto>();
            if (pcd != null)
            {
                if (!string.IsNullOrWhiteSpace(pcd.NoiDungKham))
                {
                    examRows.Add(new HistoryExamRowDto
                    {
                        Label = "Nội dung khám",
                        Value = pcd.NoiDungKham
                    });
                }
                if (!string.IsNullOrWhiteSpace(pcd.HuongXuTri))
                {
                    examRows.Add(new HistoryExamRowDto
                    {
                        Label = "Hướng xử trí",
                        Value = pcd.HuongXuTri
                    });
                }
            }

            // Dịch vụ CLS
            var services = new List<HistoryServiceResultDto>();
            if (phieuCls != null)
            {
                foreach (var ct in phieuCls.ChiTietDichVus)
                {
                    var dv = ct.DichVuYTe;
                    var kq = ct.KetQuaDichVu;

                    services.Add(new HistoryServiceResultDto
                    {
                        MaDichVu = dv.MaDichVu,
                        TenDichVu = dv.TenDichVu,
                        KetQua = kq?.NoiDungKetQua,
                        DonGia = dv.DonGia
                    });
                }
            }

            // Chẩn đoán
            HistoryDiagnosisDto? diag = null;
            if (pcd != null)
            {
                diag = new HistoryDiagnosisDto
                {
                    ChanDoanSoBo = pcd.ChanDoanSoBo,
                    ChanDoanXacDinh = pcd.ChanDoanCuoi,
                    PhacDoDieuTri = pcd.PhatDoDieuTri,
                    TuVanDanDo = pcd.LoiKhuyen
                };
            }

            var dto = new HistoryVisitDetailDto
            {
                ThoiGian = luot.ThoiGianBatDau,
                MaBenhNhan = bn.MaBenhNhan,
                TenBenhNhan = bn.HoTen,
                MaKhoa = khoa.MaKhoa,
                TenKhoa = khoa.TenKhoa,
                MaBacSi = bs?.MaNhanVien,
                TenBacSi = bs?.HoTen,
                LoaiLuot = laDichVu ? "can_lam_sang" : "kham_lam_sang",
                LaKhamDichVu = laDichVu,
                TomTatKham = pcd?.NoiDungKham ?? phieuLs?.TrieuChung,

                MaLuotKham = luot.MaLuotKham,
                MaPhieuKhamLs = phieuLs?.MaPhieuKham,
                MaPhieuKhamCls = phieuCls?.MaPhieuKhamCls,
                MaPhieuTongHopCls = phieuTongHop?.MaPhieuTongHop,
                MaPhieuChanDoanCuoi = pcd?.MaPhieuChanDoan,
                MaDonThuoc = pcd?.MaDonThuoc,

                KetQuaKham = examRows,
                KetQuaDichVu = services,
                ChanDoan = diag
            };

            return dto;
        }

        public async Task<HistoryVisitRecordDto> TaoLuotKhamAsync(HistoryVisitCreateRequest request)
        {
            var maHangDoiReq = request.MaHangDoi?.Trim();
            if (string.IsNullOrWhiteSpace(maHangDoiReq))
                throw new ArgumentException("MaHangDoi là bắt buộc", nameof(request.MaHangDoi));

            // Nếu hàng đợi này đã có lượt khám đang thực hiện, dùng lại để tránh tạo duplicate
            var luotDaCo = await _db.LuotKhamBenhs
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.BenhNhan)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.Phong)
                        .ThenInclude(p => p.KhoaChuyenMon)
                .Include(l => l.NhanSuThucHien)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.PhieuKhamLamSang)
                        .ThenInclude(pk => pk.PhieuChanDoanCuoi)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.PhieuKhamLamSang)
                        .ThenInclude(pk => pk.DichVuKham)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.ChiTietDichVu)
                        .ThenInclude(ct => ct.DichVuYTe)
                .FirstOrDefaultAsync(l => l.MaHangDoi == maHangDoiReq);

            if (luotDaCo is not null)
            {
                // Đảm bảo 1-1 giữa queue và lượt khám
                if (string.Equals(luotDaCo.TrangThai, "hoan_tat", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Queue này đã được phục vụ, hãy tạo queue mới để tiếp tục.");

                return MapToVisitRecord(luotDaCo);
            }

            // Tìm hàng đợi tương ứng
            var hangDoi = await _db.HangDois
                .Include(h => h.BenhNhan)
                .Include(h => h.Phong)
                    .ThenInclude(p => p.KhoaChuyenMon)
                .Include(h => h.PhieuKhamLamSang)
                .Include(h => h.ChiTietDichVu)              // 🔹 để dùng cho lượt CLS
                    .ThenInclude(ct => ct.DichVuYTe)
                .FirstOrDefaultAsync(h => h.MaHangDoi == maHangDoiReq);

            if (hangDoi is null)
                throw new KeyNotFoundException($"Không tìm thấy hàng đợi {maHangDoiReq}");

            if (string.IsNullOrWhiteSpace(hangDoi.MaHangDoi))
                throw new InvalidOperationException("Hàng đợi không có MaHangDoi hợp lệ, không thể tạo lượt khám.");

            var phieuLs = hangDoi.PhieuKhamLamSang;
            var maBenhNhan = hangDoi.MaBenhNhan;

            // Phân biệt khám LS vs CLS theo loại phòng
            var laPhongDichVu = string.Equals(
                hangDoi.Phong.LoaiPhong,
                "phong_dich_vu",
                StringComparison.OrdinalIgnoreCase
            );

            // Suy ra LoaiLuot: prefer request -> nhan -> theo loai phong
            string NormalizeLoaiLuot(string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var v = value.Trim().ToLowerInvariant();
                    if (v is "can_lam_sang" or "service")
                        return "can_lam_sang";
                    if (v is "kham_lam_sang" or "clinic")
                        return "kham_lam_sang";
                }

                return laPhongDichVu ? "can_lam_sang" : "kham_lam_sang";
            }

            var loaiLuot = NormalizeLoaiLuot(
                string.IsNullOrWhiteSpace(request.LoaiLuot) ? hangDoi.Nhan : request.LoaiLuot);

            var now = DateTime.Now;
            var batDau = request.ThoiGianBatDau ?? now;
            DateTime? ketThuc = request.ThoiGianKetThuc;

            // ===== 1. Xác định bác sĩ khám (MaNhanSuThucHien) =====
            string? maNhanSuThucHien = null;
            if (!laPhongDichVu && phieuLs is not null)
            {
                // Chỉ phòng khám LS mới có bác sĩ khám
                maNhanSuThucHien = phieuLs.MaBacSiKham;
            }
            // CLS / dịch vụ: maNhanSuThucHien = null

            // ===== 2. Xác định y tá hỗ trợ (MaYTaHoTro) =====
            string? maYTaHoTro = request.MaYTaHoTro;

            // Cho phép FE truyền null -> BE tự tìm theo lịch trực
            if (string.IsNullOrWhiteSpace(maYTaHoTro))
            {
                // Dùng MaHangDoi -> MaPhong -> Lịch trực trong ngày & khung giờ này
                maYTaHoTro = await TimYTaHoTroTheoPhongAsync(hangDoi.Phong.MaPhong, batDau);
            }

            LuotKhamBenh entity;

            await using (var tx = await _db.Database.BeginTransactionAsync())
            {
                entity = new LuotKhamBenh
                {
                    MaLuotKham = GeneratorID.NewLuotKhamId(),
                    MaHangDoi = hangDoi.MaHangDoi ?? maHangDoiReq,

                    MaNhanSuThucHien = maNhanSuThucHien,
                    MaYTaHoTro = maYTaHoTro,   // <- đã set ở trên, có thể null nếu không tìm được

                    ThoiGianBatDau = batDau,
                    ThoiGianKetThuc = ketThuc,

                    LoaiLuot = loaiLuot,
                    TrangThai = string.IsNullOrWhiteSpace(request.TrangThai)
                        ? "dang_kham"
                        : request.TrangThai,
                };

                _db.LuotKhamBenhs.Add(entity);
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
            }


            // =========================
            // 3. Sau khi tạo lượt khám
            // =========================

            // 3.1. CẬP NHẬT TRẠNG THÁI QUEUE (cho cả LS & CLS)
            await _queue.CapNhatTrangThaiHangDoiAsync(
                hangDoi.MaHangDoi,
                new QueueStatusUpdateRequest
                {
                    TrangThai = "dang_thuc_hien"
                });

            // 3.2. Nếu là LÂM SÀNG: cập nhật phiếu LS = "dang_thuc_hien"
            if (!laPhongDichVu && phieuLs is not null)
            {
                await _clinical.CapNhatTrangThaiPhieuKhamAsync(
                    phieuLs.MaPhieuKham!,
                    new ClinicalExamStatusUpdateRequest
                    {
                        TrangThai = "dang_thuc_hien"
                    });
            }

            // 3.3. Nếu là CẬN LÂM SÀNG (phòng dịch vụ):
            if (laPhongDichVu)
            {
                // Lấy chi tiết dịch vụ gắn với hàng đợi CLS (MaChiTietDv phải có)
                var chiTiet = hangDoi.ChiTietDichVu;

                if (chiTiet is null && !string.IsNullOrWhiteSpace(hangDoi.MaChiTietDv))
                {
                    chiTiet = await _db.ChiTietDichVus
                        .Include(c => c.DichVuYTe)
                        .FirstOrDefaultAsync(c => c.MaChiTietDv == hangDoi.MaChiTietDv);
                }

                if (chiTiet is null)
                {
                    // Hàng đợi CLS mà không có chi tiết DV thì coi như cấu hình sai schema
                    throw new InvalidOperationException(
                        $"Hàng đợi CLS {hangDoi.MaHangDoi} không gắn ChiTietDichVu (MaChiTietDv).");
                }

                // Khi bắt đầu thực hiện DV, set trạng thái chi tiết DV = "dang_thuc_hien"
                chiTiet.TrangThai = "dang_thuc_hien";
                await _db.SaveChangesAsync();
            }

            // Reload đầy đủ nav giống LayLichSuAsync để map DTO (thêm ChiTietDichVu cho CLS)
            var saved = await _db.LuotKhamBenhs
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.BenhNhan)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.Phong)
                        .ThenInclude(p => p.KhoaChuyenMon)
                .Include(l => l.NhanSuThucHien)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.PhieuKhamLamSang)
                        .ThenInclude(pk => pk.PhieuChanDoanCuoi)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.PhieuKhamLamSang)
                        .ThenInclude(pk => pk.DichVuKham)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.ChiTietDichVu)      // 🔹 CLS: phiếu Chi tiết DV
                        .ThenInclude(ct => ct.DichVuYTe)
                .FirstAsync(l => l.MaLuotKham == entity.MaLuotKham);

            var dto = MapToVisitRecord(saved);

            // 🔥 Cập nhật trạng thái hôm nay của bệnh nhân = "dang_kham" (anh có thể sau này tách:
            //  - LS  -> "dang_kham"
            //  - CLS -> "dang_cls"
            if (!string.IsNullOrWhiteSpace(maBenhNhan) )
            {
                await _patients.CapNhatTrangThaiBenhNhanAsync(
                    maBenhNhan!,
                    new PatientStatusUpdateRequest
                    {
                        TrangThaiHomNay = !laPhongDichVu ? "dang_kham" : "dang_kham_dv"
                    });
            }


            // realtime cho staff (bác sĩ + y tá)
            await _realtime.BroadcastVisitCreatedAsync(dto);
            // 🔄 cập nhật dashboard (KPIs + hoạt động gần đây)
            var dashboard = await _dashboard.LayDashboardHomNayAsync();
            await _realtime.BroadcastDashboardTodayAsync(dashboard);
            //await _realtime.BroadcastTodayExamOverviewAsync(dashboard.LuotKhamHomNay);
            //await _realtime.BroadcastRecentActivitiesAsync(dashboard.HoatDongGanDay);

            return dto;
        }


        // ==================== CẬP NHẬT TRẠNG THÁI LƯỢT KHÁM ====================

        public async Task<HistoryVisitRecordDto?> CapNhatTrangThaiLuotKhamAsync(
       string maLuotKham,
       HistoryVisitStatusUpdateRequest request)
        {
            var luot = await _db.LuotKhamBenhs
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.BenhNhan)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.Phong)
                        .ThenInclude(p => p.KhoaChuyenMon)
                .Include(l => l.NhanSuThucHien)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.PhieuKhamLamSang)
                        .ThenInclude(pk => pk.DichVuKham)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.PhieuKhamLamSang)
                        .ThenInclude(pk => pk.PhieuChanDoanCuoi)
                .FirstOrDefaultAsync(l => l.MaLuotKham == maLuotKham);

            if (luot is null)
                return null;

            // Chỉ cập nhật trạng thái/thời gian lượt; các cập nhật khác sẽ thực hiện ở bước chẩn đoán cuối
            if (!string.IsNullOrWhiteSpace(request.TrangThai))
            {
                luot.TrangThai = request.TrangThai;
            }

            if (request.ThoiGianKetThuc.HasValue)
            {
                luot.ThoiGianKetThuc = request.ThoiGianKetThuc.Value;
            }
            else if (string.Equals(request.TrangThai, "da_huy", StringComparison.OrdinalIgnoreCase))
            {
                luot.ThoiGianKetThuc ??= DateTime.Now;
            }

            await _db.SaveChangesAsync();

            // Map DTO lịch sử lượt khám
            var dto = MapToVisitRecord(luot);

            // 8) Realtime + dashboard + thông báo
            await _realtime.BroadcastVisitStatusUpdatedAsync(dto);
            await TaoThongBaoCapNhatLuotKhamAsync(dto);

            var dashboard = await _dashboard.LayDashboardHomNayAsync();
            await _realtime.BroadcastDashboardTodayAsync(dashboard);

            return dto;
        }



        /// <summary>
        /// Tìm y tá hỗ trợ theo phòng + thời gian khám, dựa vào bảng lich_truc.
        /// Ưu tiên ca trực:
        /// - Đúng ngày (Ngay == thoiGianBatDau.Date)
        /// - Không nghỉ trực (NghiTruc == false)
        /// - Giờ bắt đầu/kết thúc bao phủ thời điểm khám
        /// </summary>
        private async Task<string?> TimYTaHoTroTheoPhongAsync(string maPhong, DateTime thoiGianBatDau)
    {
        var ngay = thoiGianBatDau.Date;
        var gio = thoiGianBatDau.TimeOfDay;

        var lich = await _db.LichTrucs
            .AsNoTracking()
            .Where(l =>
l.MaPhong == maPhong &&
l.NghiTruc == false &&
l.Ngay == ngay &&
l.GioBatDau <= gio &&
l.GioKetThuc >= gio)
            .OrderBy(l => l.GioBatDau)
            .FirstOrDefaultAsync();

        // Nếu không tìm thấy ca nào phù hợp thì trả về null
        // FE có thể hiển thị "Chưa gán y tá hỗ trợ"
        return lich?.MaYTaTruc;
    }


        // ==================== MAPPING LIST RECORD ====================
        private async Task TaoThongBaoCapNhatLuotKhamAsync(HistoryVisitRecordDto luot)
        {
            if (luot == null) return;

            var title = "Cập nhật lượt khám";
            var body =
                $"Lượt khám của bệnh nhân {luot.TenBenhNhan} " +
                $"(Mã BN: {luot.MaBenhNhan}) tại khoa {luot.TenKhoa} đã hoàn tất.";

            var request = new NotificationCreateRequest
            {
                LoaiThongBao = "luot_kham",
                TieuDe = title,
                NoiDung = body,
                MucDoUuTien = "normal",
                NguonLienQuan = "luot_kham",
                MaDoiTuongLienQuan = luot.MaLuotKham,
                NguoiNhan = new List<NotificationRecipientCreateRequest>
                {
                    new NotificationRecipientCreateRequest
                    {
                        LoaiNguoiNhan = "y_ta",
                        MaNguoiNhan = null
                    }
                }
            };

            await _notifications.TaoThongBaoAsync(request);

        }
        private static HistoryVisitRecordDto MapToVisitRecord(LuotKhamBenh luot)
        {
            var h = luot.HangDoi;
            var bn = h.BenhNhan;
            var phong = h.Phong;
            var khoa = phong.KhoaChuyenMon;
            var bs = luot.NhanSuThucHien;
            var phieuLs = h.PhieuKhamLamSang;
            var pcd = phieuLs?.PhieuChanDoanCuoi;
            var phieuCls = phieuLs?.PhieuKhamCanLamSang;
            var phieuTongHop = phieuLs?.PhieuTongHopKetQua ?? phieuCls?.PhieuTongHopKetQua;

            bool laDichVu = phong.LoaiPhong == "phong_dich_vu";
            string loaiLuot = laDichVu ? "can_lam_sang" : "kham_lam_sang";

            string? note = pcd?.ChanDoanCuoi
                           ?? pcd?.NoiDungKham
                           ?? phieuLs?.TrieuChung;

            return new HistoryVisitRecordDto
            {
                ThoiGian = luot.ThoiGianBatDau,

                MaBenhNhan = bn.MaBenhNhan,
                TenBenhNhan = bn.HoTen,

                MaKhoa = khoa.MaKhoa,
                TenKhoa = khoa.TenKhoa,

                MaBacSi = bs?.MaNhanVien,
                TenBacSi = bs?.HoTen,

                LoaiLuot = loaiLuot,
                GhiChu = note,
                LaKhamDichVu = laDichVu,

                MaLuotKham = luot.MaLuotKham,
                MaPhieuKhamLs = phieuLs?.MaPhieuKham,
                MaPhieuKhamCls = phieuCls?.MaPhieuKhamCls,
                MaPhieuTongHopCls = phieuTongHop?.MaPhieuTongHop,
                MaPhieuChanDoanCuoi = pcd?.MaPhieuChanDoan,
                MaDonThuoc = pcd?.MaDonThuoc
            };
        }
    }
}
