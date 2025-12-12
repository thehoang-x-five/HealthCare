using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using HealthCare.Realtime;
using Microsoft.EntityFrameworkCore;
using HealthCare.Services;
namespace HealthCare.Services
{
    public class PharmacyService(
     DataContext db,
     IRealtimeService realtime,
     INotificationService notifications,
     IDashboardService dashboard) : IPharmacyService
    {
        private readonly DataContext _db = db;
        private readonly IRealtimeService _realtime = realtime;
        private readonly INotificationService _notifications = notifications;
        private readonly IDashboardService _dashboard = dashboard;
        // ========= KHO THUỐC =========

        public async Task<DrugDto> TaoHoacCapNhatThuocAsync(DrugDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.MaThuoc))
                throw new ArgumentException("MaThuoc là bắt buộc");

            var entity = await _db.KhoThuocs
                .FirstOrDefaultAsync(k => k.MaThuoc == dto.MaThuoc);

            if (entity == null)
            {
                entity = new KhoThuoc
                {
                    MaThuoc = dto.MaThuoc
                };
                _db.KhoThuocs.Add(entity);
            }

            entity.TenThuoc = dto.TenThuoc;
            entity.DonViTinh = dto.DonViTinh;
            entity.CongDung = dto.CongDung;
            entity.GiaNiemYet = dto.GiaNiemYet;
            entity.SoLuongTon = dto.SoLuongTon;
            entity.HanSuDung = dto.HanSuDung;
            entity.SoLo = dto.SoLo;

            // 🔑 Chỉ nhận 2 trạng thái từ FE: hoat_dong / tam_dung
            var rawStatus = (dto.TrangThai ?? "").ToLowerInvariant();
            if (rawStatus == DrugStatuses.TamDung)
                entity.TrangThai = DrugStatuses.TamDung;
            else
                entity.TrangThai = DrugStatuses.HoatDong;

            // 🔑 Tính lại trạng thái thực tế (het_han / sap_het_han / sap_het_ton / hoat_dong)
            var calculator = new DrugStatusCalculator();
            entity.TrangThai = calculator.CalculateStatus(entity);

            await _db.SaveChangesAsync();

            var result = MapDrug(entity);

            // Realtime: thông báo kho thuốc thay đổi cho nhóm y tá / hành chính
            await _realtime.BroadcastDrugChangedAsync(result);
            
            return result;
        }

        public async Task<IReadOnlyList<DrugDto>> LayDanhSachThuocAsync()
        {
            var list = await _db.KhoThuocs
                .AsNoTracking()
                .OrderBy(k => k.TenThuoc)
                .ToListAsync();

            return list.Select(MapDrug).ToList();
        }

        public async Task<PagedResult<DrugDto>> TimKiemThuocAsync(DrugSearchFilter filter)
        {
            var query = _db.KhoThuocs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();
                query = query.Where(k =>
                    k.MaThuoc.Contains(kw) ||
                    k.TenThuoc.Contains(kw) ||
                    (k.SoLo ?? "").Contains(kw) ||
                    (k.CongDung ?? "").Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
            {
                query = query.Where(k => k.TrangThai == filter.TrangThai);
            }

            if (filter.HanSuDungFrom.HasValue)
            {
                var from = filter.HanSuDungFrom.Value.Date;
                query = query.Where(k => k.HanSuDung >= from);
            }

            if (filter.HanSuDungTo.HasValue)
            {
                var to = filter.HanSuDungTo.Value.Date.AddDays(1);
                query = query.Where(k => k.HanSuDung < to);
            }

            if (filter.TonToiThieu.HasValue)
            {
                query = query.Where(k => k.SoLuongTon >= filter.TonToiThieu.Value);
            }

            if (filter.TonToiDa.HasValue)
            {
                query = query.Where(k => k.SoLuongTon <= filter.TonToiDa.Value);
            }

            // Sort
            var sortBy = filter.SortBy?.Trim();
            var dir = (filter.SortDirection ?? "asc").ToLowerInvariant();

            query = sortBy switch
            {
                "SoLuongTon" => (dir == "desc"
                    ? query.OrderByDescending(k => k.SoLuongTon)
                    : query.OrderBy(k => k.SoLuongTon)),
                "HanSuDung" => (dir == "desc"
                    ? query.OrderByDescending(k => k.HanSuDung)
                    : query.OrderBy(k => k.HanSuDung)),
                _ => (dir == "desc"
                    ? query.OrderByDescending(k => k.TenThuoc)
                    : query.OrderBy(k => k.TenThuoc))
            };

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 500 : filter.PageSize;

            var totalItems = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<DrugDto>
            {
                Items = items.Select(MapDrug).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        // ========= ĐƠN THUỐC =========

        public async Task<PrescriptionDto> TaoDonThuocAsync(PrescriptionCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.MaBenhNhan))
                throw new ArgumentException("MaBenhNhan là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.MaBacSiKeDon))
                throw new ArgumentException("MaBacSiKeDon là bắt buộc");
            if (request.Items == null || request.Items.Count == 0)
                throw new ArgumentException("Đơn thuốc phải có ít nhất 1 thuốc");

            var bn = await _db.BenhNhans
                .FirstOrDefaultAsync(b => b.MaBenhNhan == request.MaBenhNhan);
            if (bn == null) throw new ArgumentException("Bệnh nhân không tồn tại");

            var bs = await _db.NhanVienYTes
                .FirstOrDefaultAsync(n => n.MaNhanVien == request.MaBacSiKeDon);
            if (bs == null) throw new ArgumentException("Bác sĩ kê đơn không tồn tại");

            PhieuChanDoanCuoi? pcdc = null;
            if (!string.IsNullOrWhiteSpace(request.MaPhieuChanDoanCuoi))
            {
                pcdc = await _db.PhieuChanDoanCuois
                    .FirstOrDefaultAsync(p => p.MaPhieuChanDoan == request.MaPhieuChanDoanCuoi);
                if (pcdc == null)
                    throw new ArgumentException("Phiếu chẩn đoán cuối không tồn tại");
            }

            // Tính tổng tiền từ Items (BE chủ động)
            decimal tongTien = request.Items.Sum(i => i.ThanhTien);

            var maDonThuoc = HealthCare.RenderID.GeneratorID.NewDonThuocId();
            var now = DateTime.Now;

            var don = new DonThuoc
            {
                MaDonThuoc = maDonThuoc,
                MaBenhNhan = request.MaBenhNhan,
                MaBacSiKeDon = request.MaBacSiKeDon,
                ThoiGianKeDon = now,
                TrangThai = "da_ke",
                TongTienDon = tongTien,
                BacSiKeDon = bs,
                BenhNhan = bn
            };

            if (pcdc != null)
            {
                // Link 2 chiều
                don.PhieuChanDoanCuoi = pcdc;
                pcdc.MaDonThuoc = maDonThuoc;
            }

            _db.DonThuocs.Add(don);

            foreach (var item in request.Items)
            {
                if (item.SoLuong <= 0)
                    throw new ArgumentException("SoLuong phải > 0");

                var detail = new ChiTietDonThuoc
                {
                    MaChiTietDon = Guid.NewGuid().ToString("N"),
                    MaDonThuoc = maDonThuoc,
                    MaThuoc = item.MaThuoc,
                    SoLuong = item.SoLuong,
                    ChiDinhSuDung = item.ChiDinhSuDung,
                    ThanhTien = item.ThanhTien
                };
                _db.ChiTietDonThuocs.Add(detail);
            }

            await _db.SaveChangesAsync();

            // Reload full để map DTO
            var full = await QueryDonThuoc()
                .FirstAsync(d => d.MaDonThuoc == maDonThuoc);

            var dto = MapPrescription(full);

            // Realtime: đơn thuốc mới cho bác sĩ + y tá
            await _realtime.BroadcastPrescriptionCreatedAsync(dto);

            // Thông báo: đơn thuốc mới
            await TaoThongBaoDonThuocMoiAsync(dto);

            // Cập nhật Dashboard: hoạt động gần đây (kê đơn thuốc)
            var dashboard = await _dashboard.LayDashboardHomNayAsync();
            await _realtime.BroadcastDashboardTodayAsync(dashboard);
            //await _realtime.BroadcastRecentActivitiesAsync(dashboard.HoatDongGanDay);

            return dto;
        }

        public async Task<PrescriptionDto?> LayDonThuocAsync(string maDonThuoc)
        {
            var don = await QueryDonThuoc()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.MaDonThuoc == maDonThuoc);

            if (don == null) return null;

            return MapPrescription(don);
        }

        public async Task<PrescriptionDto?> CapNhatTrangThaiDonThuocAsync(
            string maDonThuoc,
            PrescriptionStatusUpdateRequest request)
        {
            var don = await QueryDonThuoc()
                .FirstOrDefaultAsync(d => d.MaDonThuoc == maDonThuoc);

            if (don == null) return null;

            var oldStatus = don.TrangThai;
            var newStatus = request.TrangThai;

            don.TrangThai = newStatus;

            // Nếu chuyển sang "da_phat" lần đầu tiên => trừ kho + kiểm tra hạn
            if (!string.Equals(oldStatus, "da_phat", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(newStatus, "da_phat", StringComparison.OrdinalIgnoreCase))
            {
                var details = await _db.ChiTietDonThuocs
                    .Where(c => c.MaDonThuoc == maDonThuoc)
                    .ToListAsync();

                var thuocCodes = details.Select(d => d.MaThuoc).Distinct().ToList();

                var stockMap = await _db.KhoThuocs
                    .Where(k => thuocCodes.Contains(k.MaThuoc))
                    .ToDictionaryAsync(k => k.MaThuoc, k => k);

                var today = DateTime.Today;
                var calculator = new DrugStatusCalculator();
                foreach (var d in details)
                {
                    if (!stockMap.TryGetValue(d.MaThuoc, out var stock))
                        throw new InvalidOperationException($"Thuốc {d.MaThuoc} không tồn tại trong kho");

                    // Chặn thuốc đã hết hạn (trạng thái hoặc hạn dùng)
                    if (string.Equals(stock.TrangThai, DrugStatuses.HetHan, StringComparison.OrdinalIgnoreCase) ||
                        stock.HanSuDung.Date < today)
                    {
                        throw new InvalidOperationException($"Thuốc {d.MaThuoc} đã hết hạn, không thể phát");
                    }

                    // Cho phép 'sap_het_han' / 'sap_het_ton' / 'hoat_dong' nhưng vẫn kiểm soát tồn
                    if (stock.SoLuongTon < d.SoLuong)
                        throw new InvalidOperationException($"Không đủ tồn kho cho thuốc {d.MaThuoc}");

                    stock.SoLuongTon -= d.SoLuong;
                    stock.TrangThai = calculator.CalculateStatus(stock);
                }
            }

            await _db.SaveChangesAsync();

            // reload để có đầy đủ nav sau khi EF track
            var updated = await QueryDonThuoc()
                .AsNoTracking()
                .FirstAsync(d => d.MaDonThuoc == maDonThuoc);

            var dto = MapPrescription(updated);

            // Realtime: trạng thái đơn thuốc thay đổi (vd: da_phat)
            await _realtime.BroadcastPrescriptionStatusUpdatedAsync(dto);

            // Thông báo: (ví dụ khi đơn chuyển sang đã phát)
            await TaoThongBaoTrangThaiDonThuocAsync(dto);
            // Cập nhật Dashboard: hoạt động gần đây liên quan đơn thuốc
            var dashboard = await _dashboard.LayDashboardHomNayAsync();
            await _realtime.BroadcastDashboardTodayAsync(dashboard);
            //await _realtime.BroadcastRecentActivitiesAsync(dashboard.HoatDongGanDay);
            return dto;
        }

        public async Task<PagedResult<PrescriptionDto>> TimKiemDonThuocAsync(
            string? maBenhNhan,
            DateTime? fromDate,
            DateTime? toDate,
            string? trangThai,
            int page,
            int pageSize)
        {
            var query = QueryDonThuoc().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(maBenhNhan))
            {
                query = query.Where(d => d.MaBenhNhan == maBenhNhan);
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value;
                query = query.Where(d => d.ThoiGianKeDon >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value;
                query = query.Where(d => d.ThoiGianKeDon < to);
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(d => d.TrangThai == trangThai);
            }

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 500 : pageSize;

            var totalItems = await query.CountAsync();

            var list = await query
                .OrderByDescending(d => d.ThoiGianKeDon)
                .ThenByDescending(d => d.MaDonThuoc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = list.Select(MapPrescription).ToList();

            return new PagedResult<PrescriptionDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        // ========= HELPERS =========

        private static DrugDto MapDrug(KhoThuoc k) => new DrugDto
        {
            MaThuoc = k.MaThuoc,
            TenThuoc = k.TenThuoc,
            DonViTinh = k.DonViTinh,
            CongDung = k.CongDung,
            GiaNiemYet = k.GiaNiemYet,
            SoLuongTon = k.SoLuongTon,
            HanSuDung = k.HanSuDung,
            SoLo = k.SoLo,
            TrangThai = k.TrangThai
        };

        private IQueryable<DonThuoc> QueryDonThuoc()
        {
            return _db.DonThuocs
                .Include(d => d.BenhNhan)
                .Include(d => d.BacSiKeDon)
                .Include(d => d.PhieuChanDoanCuoi)
                .Include(d => d.ChiTietDonThuocs)
                    .ThenInclude(c => c.KhoThuoc);
        }

        private static PrescriptionDto MapPrescription(DonThuoc d)
        {
            var items = d.ChiTietDonThuocs?.Select(c =>
            {
                var tenThuoc = c.KhoThuoc?.TenThuoc ?? string.Empty;
                var donGia = c.SoLuong > 0 ? c.ThanhTien / c.SoLuong : 0m;

                return new PrescriptionItemDto
                {
                    MaThuoc = c.MaThuoc,
                    TenThuoc = tenThuoc,
                    ChiDinhSuDung = c.ChiDinhSuDung,
                    SoLuong = c.SoLuong,
                    DonGia = donGia,
                    ThanhTien = c.ThanhTien
                };
            }).ToList() ?? new List<PrescriptionItemDto>();

            return new PrescriptionDto
            {
                MaDonThuoc = d.MaDonThuoc,
                MaBenhNhan = d.MaBenhNhan,
                TenBenhNhan = d.BenhNhan?.HoTen ?? string.Empty,
                MaBacSiKeDon = d.MaBacSiKeDon,
                TenBacSiKeDon = d.BacSiKeDon?.HoTen ?? string.Empty,
                MaPhieuChanDoanCuoi = d.PhieuChanDoanCuoi?.MaPhieuChanDoan,
                ChanDoan = d.PhieuChanDoanCuoi?.ChanDoanCuoi,
                ThoiGianKeDon = d.ThoiGianKeDon,
                TrangThai = d.TrangThai,
                TongTienDon = d.TongTienDon,
                ChiTiet = items
            };
        }


        // ==========================
        // =   THÔNG BÁO - ĐƠN THUỐC =
        // ==========================

        private async Task TaoThongBaoDonThuocMoiAsync(PrescriptionDto donThuoc)
        {
            // Gửi cho toàn bộ nhân viên y tế (phát thuốc / y tá hành chính...)
            var request = new NotificationCreateRequest
            {
                LoaiThongBao = "don_thuoc",
                TieuDe = "Đơn thuốc mới",
                NoiDung = RenderThongBaoDonThuocMoi(donThuoc),
                MucDoUuTien = "normal",

                NguonLienQuan = "don_thuoc",
                MaDoiTuongLienQuan = donThuoc.MaDonThuoc,

                NguoiNhan = new List<NotificationRecipientCreateRequest>
                {
                    // Broadcast cho tất cả nhân viên y tế
                    new NotificationRecipientCreateRequest
                    {
                        LoaiNguoiNhan = "nhan_vien_y_te",
                        MaNguoiNhan = null // => NotificationService vẫn lưu được, realtime sẽ broadcast theo role
                    }
                }
            };

            await _notifications.TaoThongBaoAsync(request);
        }

        private static string RenderThongBaoDonThuocMoi(PrescriptionDto donThuoc)
        {
            var tenBn = string.IsNullOrWhiteSpace(donThuoc.TenBenhNhan)
                ? donThuoc.MaBenhNhan
                : $"{donThuoc.TenBenhNhan} ({donThuoc.MaBenhNhan})";

            var bacSi = string.IsNullOrWhiteSpace(donThuoc.TenBacSiKeDon)
                ? "Bác sĩ kê đơn"
                : $"Bác sĩ {donThuoc.TenBacSiKeDon}";

            return $"Có đơn thuốc mới {donThuoc.MaDonThuoc} cho bệnh nhân {tenBn} do {bacSi} kê. " +
                   "Vui lòng kiểm tra và phát thuốc.";
        }

        private async Task TaoThongBaoTrangThaiDonThuocAsync(PrescriptionDto donThuoc)
        {
            // Chỉ care khi đơn đã phát xong
            if (!string.Equals(donThuoc.TrangThai, "da_phat", StringComparison.OrdinalIgnoreCase))
                return;

            if (string.IsNullOrWhiteSpace(donThuoc.MaBacSiKeDon))
                return;

            var request = new NotificationCreateRequest
            {
                LoaiThongBao = "don_thuoc",
                TieuDe = "Đơn thuốc đã phát",
                NoiDung = RenderThongBaoDonThuocDaPhat(donThuoc),
                MucDoUuTien = "normal",

                NguonLienQuan = "don_thuoc",
                MaDoiTuongLienQuan = donThuoc.MaDonThuoc,

                NguoiNhan = new List<NotificationRecipientCreateRequest>
{
    new NotificationRecipientCreateRequest
    {
        LoaiNguoiNhan = "bac_si",
        MaNguoiNhan = donThuoc.MaBacSiKeDon
    }
}
            };

            await _notifications.TaoThongBaoAsync(request);
        }

        private static string RenderThongBaoDonThuocDaPhat(PrescriptionDto donThuoc)
        {
            var tenBn = string.IsNullOrWhiteSpace(donThuoc.TenBenhNhan)
                ? donThuoc.MaBenhNhan
                : $"{donThuoc.TenBenhNhan} ({donThuoc.MaBenhNhan})";

            return $"Đơn thuốc {donThuoc.MaDonThuoc} của bệnh nhân {tenBn} đã được phát xong.";
        }

        public class DrugStatusCalculator(int nearExpiryDays = 30, int lowStockQty = 10)
        {
            private readonly int _nearExpiryDays = nearExpiryDays;
            private readonly int _lowStockQty = lowStockQty;

            public string CalculateStatus(KhoThuoc entity)
            {
                if (entity == null) throw new ArgumentNullException(nameof(entity));

                // 🔒 Ưu tiên "tạm dừng": đã tạm dừng thì giữ nguyên, không quan tâm tồn kho / HSD
                if (string.Equals(entity.TrangThai, DrugStatuses.TamDung, StringComparison.OrdinalIgnoreCase))
                {
                    return DrugStatuses.TamDung;
                }

                var today = DateTime.Today;
                var daysLeft = (entity.HanSuDung.Date - today).TotalDays;

                // 1. Hết hạn / sắp hết hạn theo HSD
                if (!double.IsNaN(daysLeft))
                {
                    if (daysLeft < 0)
                        return DrugStatuses.HetHan;

                    if (daysLeft <= _nearExpiryDays)
                        return DrugStatuses.SapHetHan;
                }

                // 2. Nếu chưa gần hết hạn, check tồn kho
                if (entity.SoLuongTon <= _lowStockQty)
                    return DrugStatuses.SapHetTon;

                // 3. Còn lại là hoạt động
                return DrugStatuses.HoatDong;
            }
        }
       

    }

    public static class DrugStatuses
    {
        public const string HoatDong = "hoat_dong";
        public const string TamDung = "tam_dung";
        public const string HetHan = "het_han";
        public const string SapHetHan = "sap_het_han";
        public const string SapHetTon = "sap_het_ton";
    }
}
