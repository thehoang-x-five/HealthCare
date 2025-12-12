using System;
using System.Linq;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using HealthCare.Realtime;
using Microsoft.EntityFrameworkCore;

namespace HealthCare.Services
{
    public class BillingService(DataContext db, IRealtimeService realtime, IDashboardService dashboard, INotificationService notifications) : IBillingService
    {
        private readonly DataContext _db = db;
        private readonly IRealtimeService _realtime = realtime;
        private readonly IDashboardService _dashboard = dashboard;
        private readonly INotificationService _notifications = notifications;

        // ============================================================
        // =                  1. TẠO HÓA ĐƠN                        =
        // ============================================================

        public async Task<InvoiceDto> TaoHoaDonAsync(InvoiceCreateRequest request)
        {
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
                TrangThai = "da_thu",
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

        public async Task<InvoiceDto?> LayHoaDonAsync(string maHoaDon)
        {
            var h = await QueryHoaDon()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaHoaDon == maHoaDon);

            return h == null ? null : MapInvoice(h);
        }

        // ============================================================
        // =        3. TÌM KIẾM / LỊCH SỬ GIAO DỊCH (PAGING)         =
        // ============================================================

        public async Task<PagedResult<InvoiceHistoryRecordDto>> TimKiemHoaDonAsync(
            InvoiceSearchFilter filter)
        {
            var q = QueryHoaDon()
                .AsNoTracking();

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

            var list = await q
                .OrderByDescending(h => h.ThoiGian)
                .ThenByDescending(h => h.MaHoaDon)
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
            var entity = await QueryHoaDon()
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

            if (entity == null)
                return null;

            entity.TrangThai = request.TrangThai;
            await _db.SaveChangesAsync();

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
                PhuongThucThanhToan = h.PhuongThucThanhToan,

                NoiDung = content,

                MaPhieuKham = h.MaPhieuKham,
                MaPhieuKhamCls = h.MaPhieuKhamCls,
                MaDonThuoc = h.MaDonThuoc
            };
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
        LoaiNguoiNhan = "y_ta",
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
                   $"với số tiền {invoice.SoTien:n0}đ. đã được thanh toán.";
        }



    }
}
