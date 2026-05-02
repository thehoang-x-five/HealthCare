using System;
using System.Linq;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using HealthCare.Realtime;
using Microsoft.EntityFrameworkCore;
using HealthCare.Services.UserInteraction;
using HealthCare.Services.Report;
using HealthCare.Infrastructure.Repositories;
using HealthCare.Services.OutpatientCare;
using HealthCare.Infrastructure.Security;
using MongoDB.Bson;

namespace HealthCare.Services.MedicationBilling
{
    public class BillingService(
        DataContext db,
        IRealtimeService realtime,
        IDashboardService dashboard,
        INotificationService notifications,
        IMongoHistoryRepository mongoHistory,
        IOverdueWorkflowCleanupService overdueCleanup) : IBillingService
    {
        private readonly DataContext _db = db;
        private readonly IRealtimeService _realtime = realtime;
        private readonly IDashboardService _dashboard = dashboard;
        private readonly INotificationService _notifications = notifications;
        private readonly IMongoHistoryRepository _mongoHistory = mongoHistory;
        private readonly IOverdueWorkflowCleanupService _overdueCleanup = overdueCleanup;

        // ============================================================
        // =                  1. TẠO HÓA ĐƠN                        =
        // ============================================================

        public async Task<InvoiceDto> TaoHoaDonAsync(InvoiceCreateRequest request)
        {
            await _overdueCleanup.CleanupAsync();

            // ===== VALIDATION =====
            if (string.IsNullOrWhiteSpace(request.MaBenhNhan))
                throw new ArgumentException("MaBenhNhan là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.MaNhanSuThu))
                throw new ArgumentException("MaNhanSuThu là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.LoaiDotThu))
                throw new ArgumentException("LoaiDotThu là bắt buộc");
            if (request.SoTien <= 0)
                throw new ArgumentException("SoTien phải > 0");

            var bn = await _db.BenhNhans
                .FirstOrDefaultAsync(b => b.MaBenhNhan == request.MaBenhNhan)
                ?? throw new ArgumentException("Bệnh nhân không tồn tại");

            var ns = await _db.NhanVienYTes
                .FirstOrDefaultAsync(n => n.MaNhanVien == request.MaNhanSuThu)
                ?? throw new ArgumentException("Nhân sự thu không tồn tại");

            // ===== CHECK ĐƠN THUỐC =====
            DonThuoc? donThuoc = null;
            if (!string.IsNullOrWhiteSpace(request.MaDonThuoc))
            {
                donThuoc = await _db.DonThuocs
                    .FirstOrDefaultAsync(d => d.MaDonThuoc == request.MaDonThuoc)
                    ?? throw new ArgumentException("Đơn thuốc không tồn tại");

                if (donThuoc.MaBenhNhan != request.MaBenhNhan)
                    throw new ArgumentException("Đơn thuốc không thuộc bệnh nhân này");
            }

            // ===== CHECK PHIẾU KHÁM LS =====
            if (!string.IsNullOrWhiteSpace(request.MaPhieuKham))
            {
                var phieuLs = await _db.PhieuKhamLamSangs
                    .FirstOrDefaultAsync(p => p.MaPhieuKham == request.MaPhieuKham)
                    ?? throw new ArgumentException("Phiếu khám lâm sàng không tồn tại");

                if (phieuLs.MaBenhNhan != request.MaBenhNhan)
                    throw new ArgumentException("Phiếu khám LS không thuộc bệnh nhân này");
            }

            // ===== CHECK PHIẾU CLS =====
            if (!string.IsNullOrWhiteSpace(request.MaPhieuKhamCls))
            {
                var phieuCls = await _db.PhieuKhamCanLamSangs
                    .Include(p => p.PhieuKhamLamSang)
                    .FirstOrDefaultAsync(p => p.MaPhieuKhamCls == request.MaPhieuKhamCls)
                    ?? throw new ArgumentException("Phiếu CLS không tồn tại");

                // CLS không có MaBenhNhan, phải kiểm tra qua phiếu khám LS
                if (phieuCls.PhieuKhamLamSang.MaBenhNhan != request.MaBenhNhan)
                    throw new ArgumentException("Phiếu CLS không thuộc bệnh nhân này");
            }

            // ===== TÍNH TIỀN =====
            decimal soTien = request.SoTien;

            // Với đợt thu "thuoc" thì ưu tiên lấy đúng tổng tiền đơn thuốc
            if (donThuoc != null && request.LoaiDotThu == "thuoc")
            {
                soTien = donThuoc.TongTienDon;
            }

            // ===== TẠO HÓA ĐƠN =====
            var maHoaDon = HealthCare.RenderID.GeneratorID.NewHoaDonId();
            var now = DateTime.Now;

            var entity = new HoaDonThanhToan
            {
                MaHoaDon = maHoaDon,
                MaBenhNhan = request.MaBenhNhan,
                MaNhanSuThu = request.MaNhanSuThu,
                MaPhieuKhamCls = request.MaPhieuKhamCls,
                MaPhieuKham = request.MaPhieuKham,
                MaDonThuoc = request.MaDonThuoc,

                LoaiDotthu = request.LoaiDotThu,
                SoTien = soTien,
                PhuongThucThanhToan = request.PhuongThucThanhToan,

                ThoiGian = now,
                TrangThai = "chua_thu",
                NoiDung = request.NoiDung
            };

            _db.HoaDonThanhToans.Add(entity);
            await _db.SaveChangesAsync();

            // Load lại để map DTO
            var saved = await QueryHoaDon()
                .AsNoTracking()
                .FirstAsync(h => h.MaHoaDon == maHoaDon);

            var dto = MapInvoice(saved);

            // ===== REALTIME: Hóa đơn mới =====
            await _realtime.BroadcastInvoiceChangedAsync(dto);

            // ===== REALTIME KPI DOANH THU + HOẠT ĐỘNG GẦN ĐÂY =====
            var dashboard = await _dashboard.LayDashboardHomNayAsync();
            await _realtime.BroadcastDashboardTodayAsync(dashboard);
            //await _realtime.BroadcastTodayRevenueKpiAsync(dashboard.DoanhThuHomNay);
            //await _realtime.BroadcastRecentActivitiesAsync(dashboard.HoatDongGanDay);

            // ===== THÔNG BÁO: Hóa đơn mới =====
            await TaoThongBaoHoaDonMoiAsync(dto);

            return dto;
        }

        // ============================================================
        // =                2. LẤY CHI TIẾT HÓA ĐƠN                  =
        // ============================================================

        public async Task<InvoiceDto?> LayHoaDonAsync(string maHoaDon, string? maKhoaScope = null)
        {
            await _overdueCleanup.CleanupAsync();

            var query = QueryHoaDon()
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(maKhoaScope))
            {
                var scopedIds = _db.ScopedPatientIdsByDepartment(maKhoaScope);
                query = query.Where(h => scopedIds.Contains(h.MaBenhNhan));
            }

            var h = await query.FirstOrDefaultAsync(x => x.MaHoaDon == maHoaDon);

            return h == null ? null : MapInvoice(h);
        }

        // ============================================================
        // =        3. TÌM KIẾM / LỊCH SỬ GIAO DỊCH (PAGING)         =
        // ============================================================

        public async Task<PagedResult<InvoiceHistoryRecordDto>> TimKiemHoaDonAsync(
            InvoiceSearchFilter filter,
            string? maKhoaScope = null)
        {
            await _overdueCleanup.CleanupAsync();

            var q = QueryHoaDon()
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(maKhoaScope))
            {
                var scopedIds = _db.ScopedPatientIdsByDepartment(maKhoaScope);
                q = q.Where(h => scopedIds.Contains(h.MaBenhNhan));
            }

            // ===== FILTER =====
            if (!string.IsNullOrWhiteSpace(filter.MaBenhNhan))
            {
                var maBn = filter.MaBenhNhan.Trim();
                q = q.Where(h => h.MaBenhNhan == maBn);
            }

            if (filter.FromTime.HasValue)
                q = q.Where(h => h.ThoiGian >= filter.FromTime.Value);

            if (filter.ToTime.HasValue)
                q = q.Where(h => h.ThoiGian <= filter.ToTime.Value);

            if (!string.IsNullOrWhiteSpace(filter.LoaiDotThu))
                q = q.Where(h => h.LoaiDotthu == filter.LoaiDotThu);

            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
                q = q.Where(h => h.TrangThai == filter.TrangThai);

            if (!string.IsNullOrWhiteSpace(filter.PhuongThucThanhToan))
                q = q.Where(h => h.PhuongThucThanhToan == filter.PhuongThucThanhToan);

            if (filter.MinAmount.HasValue)
                q = q.Where(h => h.SoTien >= filter.MinAmount.Value);

            if (filter.MaxAmount.HasValue)
                q = q.Where(h => h.SoTien <= filter.MaxAmount.Value);

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();
                q = q.Where(h =>
                    h.MaHoaDon.Contains(kw) ||
                    h.MaBenhNhan.Contains(kw) ||
                    h.BenhNhan.HoTen.Contains(kw) ||
                    h.NoiDung.Contains(kw));
            }

            // ===== PAGING =====
            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 50 : filter.PageSize;

            var totalItems = await q.CountAsync();

            var sortBy = filter.SortBy?.Trim().ToLowerInvariant();
            var sortDirection = filter.SortDirection?.Trim().ToLowerInvariant() == "asc"
                ? "asc"
                : "desc";

            q = sortBy switch
            {
                "sotien" => sortDirection == "asc"
                    ? q.OrderBy(h => h.SoTien).ThenBy(h => h.MaHoaDon)
                    : q.OrderByDescending(h => h.SoTien).ThenByDescending(h => h.MaHoaDon),
                "thoigian" => sortDirection == "asc"
                    ? q.OrderBy(h => h.ThoiGian).ThenBy(h => h.MaHoaDon)
                    : q.OrderByDescending(h => h.ThoiGian).ThenByDescending(h => h.MaHoaDon),
                _ => q.OrderByDescending(h => h.ThoiGian).ThenByDescending(h => h.MaHoaDon)
            };

            var list = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = list.Select(MapInvoiceHistory).ToList();

            return new PagedResult<InvoiceHistoryRecordDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        // ============================================================
        // =                 4. CẬP NHẬT TRẠNG THÁI                  =
        // ============================================================

        public async Task<InvoiceDto?> CapNhatTrangThaiHoaDonAsync(
            string maHoaDon,
            InvoiceStatusUpdateRequest request)
        {
            await _overdueCleanup.CleanupAsync();

            var entity = await QueryHoaDon()
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

            if (entity == null)
                return null;

            // ===== TRANSITION GUARD =====
            var oldStatus = entity.TrangThai;
            var newStatus = request.TrangThai;

            // Terminal states: đã thu / đã hủy không chuyển tiếp nữa.
            // Bảo lưu vẫn được phép thu sau.
            var terminalStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "da_thu", "da_huy" };
            if (terminalStates.Contains(oldStatus))
                throw new InvalidOperationException(
                    $"Hóa đơn đang ở trạng thái '{oldStatus}' — không thể chuyển sang '{newStatus}'.");

            var allowedTransitions = string.Equals(oldStatus, "bao_luu", StringComparison.OrdinalIgnoreCase)
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "da_thu", "da_huy" }
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "da_thu", "da_huy", "bao_luu" };

            if (!allowedTransitions.Contains(newStatus))
                throw new InvalidOperationException(
                    $"Trạng thái '{newStatus}' không hợp lệ. Chỉ cho phép: {string.Join(", ", allowedTransitions)}.");

            entity.TrangThai = request.TrangThai;
            await _db.SaveChangesAsync();

            // ===== LOG TO MONGODB: Payment Event =====
            if (string.Equals(request.TrangThai, "da_thu", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(oldStatus, "da_thu", StringComparison.OrdinalIgnoreCase))
            {
                var chiTietArr = new BsonArray
                {
                    new BsonDocument
                    {
                        { "ten", entity.NoiDung ?? "Thanh toán dịch vụ y tế" },
                        { "so_tien", entity.SoTien }
                    }
                };

                var payload = new BsonDocument
                {
                    { "ma_hoa_don", entity.MaHoaDon },
                    { "loai_dot_thu", entity.LoaiDotthu ?? (BsonValue)BsonNull.Value },
                    { "chi_tiet", chiTietArr },
                    { "tong_tien", entity.SoTien },
                    { "so_tien_tra", entity.SoTienPhaiTra },
                    { "phuong_thuc", entity.PhuongThucThanhToan ?? (BsonValue)BsonNull.Value },
                    { "ma_giao_dich", entity.MaGiaoDich ?? (BsonValue)BsonNull.Value },
                    { "nhan_su_thu", entity.MaNhanSuThu ?? (BsonValue)BsonNull.Value },
                    { "ma_phieu_kham", entity.MaPhieuKham ?? (BsonValue)BsonNull.Value },
                    { "ma_phieu_kham_cls", entity.MaPhieuKhamCls ?? (BsonValue)BsonNull.Value },
                    { "ma_don_thuoc", entity.MaDonThuoc ?? (BsonValue)BsonNull.Value },
                    { "thoi_gian", entity.ThoiGian }
                };

                try
                {
                    await _mongoHistory.LogEventAsync(entity.MaBenhNhan, "thanh_toan", payload, entity.MaNhanSuThu);
                }
                catch (Exception)
                {
                    // MongoDB dual-write fail → log miss, MySQL data vẫn OK
                }
            }

            var updated = await QueryHoaDon()
                .AsNoTracking()
                .FirstAsync(h => h.MaHoaDon == maHoaDon);

            var dto = MapInvoice(updated);

            await _realtime.BroadcastInvoiceChangedAsync(dto);

            var dashboard = await _dashboard.LayDashboardHomNayAsync();
            await _realtime.BroadcastDashboardTodayAsync(dashboard);
            //await _realtime.BroadcastTodayRevenueKpiAsync(dashboard.DoanhThuHomNay);
            //await _realtime.BroadcastRecentActivitiesAsync(dashboard.HoatDongGanDay);



            return dto;
        }

        // ============================================================
        // =                     HELPER FUNCTIONS                     =
        // ============================================================

        private IQueryable<HoaDonThanhToan> QueryHoaDon()
        {
            return _db.HoaDonThanhToans
                .Include(h => h.BenhNhan)
                .Include(h => h.NhanSuThu)
                .Include(h => h.DonThuoc);
        }

        private static InvoiceDto MapInvoice(HoaDonThanhToan h)
        {
            decimal? tienThuoc = h.DonThuoc?.TongTienDon;

            return new InvoiceDto
            {
                MaHoaDon = h.MaHoaDon,
                MaBenhNhan = h.MaBenhNhan,
                TenBenhNhan = h.BenhNhan?.HoTen ?? string.Empty,

                MaNhanSuThu = h.MaNhanSuThu,
                TenNhanSuThu = h.NhanSuThu?.HoTen ?? string.Empty,

                MaPhieuKhamCls = h.MaPhieuKhamCls,
                MaPhieuKham = h.MaPhieuKham,
                MaDonThuoc = h.MaDonThuoc,

                LoaiDotThu = h.LoaiDotthu,
                SoTien = h.SoTien,
                TienThuoc = tienThuoc,
                ThoiGian = h.ThoiGian,

                TrangThai = h.TrangThai,
                ThoiGianXuLy = h.ThoiGianHuy,
                NoiDung = h.NoiDung,
                PhuongThucThanhToan = h.PhuongThucThanhToan
            };
        }

        private static InvoiceHistoryRecordDto MapInvoiceHistory(HoaDonThanhToan h)
        {
            decimal? tienThuoc = h.DonThuoc?.TongTienDon;

            var content = string.IsNullOrWhiteSpace(h.NoiDung)
                ? $"{h.LoaiDotthu} - {h.SoTien:n0}đ"
                : h.NoiDung;

            return new InvoiceHistoryRecordDto
            {
                MaHoaDon = h.MaHoaDon,
                ThoiGian = h.ThoiGian,
                MaBenhNhan = h.MaBenhNhan,
                TenBenhNhan = h.BenhNhan?.HoTen ?? string.Empty,

                MaNhanSuThu = h.MaNhanSuThu,
                TenNhanSuThu = h.NhanSuThu?.HoTen ?? string.Empty,

                LoaiDotThu = h.LoaiDotthu,
                SoTien = h.SoTien,
                TienThuoc = tienThuoc,

                TrangThai = h.TrangThai,
                ThoiGianXuLy = h.ThoiGianHuy,
                PhuongThucThanhToan = h.PhuongThucThanhToan,

                NoiDung = content,

                MaPhieuKham = h.MaPhieuKham,
                MaPhieuKhamCls = h.MaPhieuKhamCls,
                MaDonThuoc = h.MaDonThuoc
            };
        }

        // ============================================================
        // =                 5. HỦY HÓA ĐƠN                          =
        // ============================================================

        public async Task<InvoiceDto?> HuyHoaDonAsync(string maHoaDon, string? lyDo = null)
        {
            await _overdueCleanup.CleanupAsync();

            var entity = await QueryHoaDon()
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

            if (entity == null)
                return null;

            // Chỉ cho phép hủy khi: chua_thu
            if (!string.Equals(entity.TrangThai, "chua_thu", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Hóa đơn đang ở trạng thái '{entity.TrangThai}' — chỉ hủy được khi 'chua_thu'.");

            entity.TrangThai = "da_huy";
            entity.ThoiGianHuy = DateTime.Now;

            await _db.SaveChangesAsync();

            var updated = await QueryHoaDon()
                .AsNoTracking()
                .FirstAsync(h => h.MaHoaDon == maHoaDon);

            var dto = MapInvoice(updated);

            // Realtime: hóa đơn đã hủy
            await _realtime.BroadcastInvoiceChangedAsync(dto);

            // Dashboard refresh
            var dashboard = await _dashboard.LayDashboardHomNayAsync();
            await _realtime.BroadcastDashboardTodayAsync(dashboard);

            // Thông báo cho y_ta_hanh_chinh (phụ trách thanh toán)
            await TaoThongBaoHuyHoaDonAsync(dto, lyDo);

            return dto;
        }

        private async Task TaoThongBaoHuyHoaDonAsync(InvoiceDto invoice, string? lyDo)
        {
            var tenBn = string.IsNullOrWhiteSpace(invoice.TenBenhNhan)
                ? invoice.MaBenhNhan
                : $"{invoice.TenBenhNhan} ({invoice.MaBenhNhan})";

            var noiDung = $"Hóa đơn {invoice.MaHoaDon} của bệnh nhân {tenBn} đã bị hủy.";
            if (!string.IsNullOrWhiteSpace(lyDo))
                noiDung += $" Lý do: {lyDo}";

            var request = new NotificationCreateRequest
            {
                LoaiThongBao = "hoa_don",
                TieuDe = "Hóa đơn đã hủy",
                NoiDung = noiDung,
                MucDoUuTien = "normal",

                NguonLienQuan = "hoa_don",
                MaDoiTuongLienQuan = invoice.MaHoaDon,

                NguoiNhan = new List<NotificationRecipientCreateRequest>
                {
                    new NotificationRecipientCreateRequest
                    {
                        LoaiNguoiNhan = "y_ta_hanh_chinh", // chỉ y tá hành chính phụ trách thanh toán
                        MaNguoiNhan = null
                    }
                }
            };

            await _notifications.TaoThongBaoAsync(request);
        }

        // ==========================
        // =   THÔNG BÁO - HÓA ĐƠN  =
        // ==========================

        private async Task TaoThongBaoHoaDonMoiAsync(InvoiceDto invoice)
        {
            var request = new NotificationCreateRequest
            {
                LoaiThongBao = "hoa_don",
                TieuDe = "Hóa đơn mới",
                NoiDung = RenderThongBaoHoaDonMoi(invoice),
                MucDoUuTien = "normal",

                NguonLienQuan = "hoa_don",
                MaDoiTuongLienQuan = invoice.MaHoaDon,

                NguoiNhan = new List<NotificationRecipientCreateRequest>
{
    new NotificationRecipientCreateRequest
    {
        LoaiNguoiNhan = "y_ta_hanh_chinh", // chỉ y tá hành chính thu tiền
        MaNguoiNhan = null
    }
}
            };

            await _notifications.TaoThongBaoAsync(request);
        }

        private static string RenderThongBaoHoaDonMoi(InvoiceDto invoice)
        {
            var tenBn = string.IsNullOrWhiteSpace(invoice.TenBenhNhan)
                ? invoice.MaBenhNhan
                : $"{invoice.TenBenhNhan} ({invoice.MaBenhNhan})";

            return $"Có hóa đơn mới {invoice.MaHoaDon} cho bệnh nhân {tenBn} " +
                   $"với số tiền {invoice.SoTien:n0}đ ở trạng thái chưa thu.";
        }

        // ============================================================
        // =        6. XÁC NHẬN THANH TOÁN (INLINE PAYMENT WIZARD)    =
        // ============================================================

        public async Task<InvoiceDto?> XacNhanThanhToanAsync(string maHoaDon, PaymentConfirmRequest request)
        {
            await _overdueCleanup.CleanupAsync();

            var entity = await QueryHoaDon()
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

            if (entity == null)
                return null;

            // Cho phép thu ngay từ hóa đơn chưa thu hoặc hóa đơn đã bảo lưu/công nợ.
            if (!string.Equals(entity.TrangThai, "chua_thu", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(entity.TrangThai, "bao_luu", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Hóa đơn đang ở trạng thái '{entity.TrangThai}' — chỉ xác nhận thanh toán được khi 'chua_thu' hoặc 'bao_luu'.");

            // Cập nhật thông tin thanh toán
            entity.TrangThai = "da_thu";
            entity.PhuongThucThanhToan = request.PhuongThucThanhToan ?? "tien_mat";
            entity.MaGiaoDich = request.MaGiaoDich;
            entity.SoTienPhaiTra = entity.SoTien;
            entity.ThoiGian = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(request.MaNhanSuThu))
                entity.MaNhanSuThu = request.MaNhanSuThu;

            await _db.SaveChangesAsync();

            // ===== LOG TO MONGODB =====
            var chiTietArr = new BsonArray
            {
                new BsonDocument
                {
                    { "ten", entity.NoiDung ?? "Thanh toán dịch vụ y tế" },
                    { "so_tien", entity.SoTien }
                }
            };

            var payload = new BsonDocument
            {
                { "ma_hoa_don", entity.MaHoaDon },
                { "loai_dot_thu", entity.LoaiDotthu ?? (BsonValue)BsonNull.Value },
                { "chi_tiet", chiTietArr },
                { "tong_tien", entity.SoTien },
                { "so_tien_tra", entity.SoTienPhaiTra },
                { "phuong_thuc", entity.PhuongThucThanhToan ?? (BsonValue)BsonNull.Value },
                { "ma_giao_dich", entity.MaGiaoDich ?? (BsonValue)BsonNull.Value },
                { "nhan_su_thu", entity.MaNhanSuThu ?? (BsonValue)BsonNull.Value },
                { "thoi_gian", entity.ThoiGian }
            };

            try
            {
                await _mongoHistory.LogEventAsync(entity.MaBenhNhan, "thanh_toan", payload, entity.MaNhanSuThu);
            }
            catch (Exception)
            {
                // MongoDB dual-write fail → MySQL data vẫn OK
            }

            var updated = await QueryHoaDon()
                .AsNoTracking()
                .FirstAsync(h => h.MaHoaDon == maHoaDon);

            var dto = MapInvoice(updated);

            await _realtime.BroadcastInvoiceChangedAsync(dto);
            var dashboard = await _dashboard.LayDashboardHomNayAsync();
            await _realtime.BroadcastDashboardTodayAsync(dashboard);

            return dto;
        }

    }
}
