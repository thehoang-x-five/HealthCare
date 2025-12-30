using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using Microsoft.EntityFrameworkCore;
using HealthCare.Realtime;
using HealthCare.Services.UserInteraction;
using HealthCare.Services.Report;
using HealthCare.Services.PatientManagement;
using HealthCare.Services.MedicationBilling;
using Microsoft.Extensions.Hosting;

namespace HealthCare.Services.OutpatientCare
{
    /// <summary>
    /// Service quản lý phiếu CLS, chi tiết DV, kết quả và phiếu tổng hợp.
    /// </summary>
    public class ClsService : IClsService
    {
        private readonly DataContext _db;
        private readonly IRealtimeService _realtime;
        private readonly INotificationService _notifications;
        private readonly IDashboardService _dashboard;
        private readonly IPatientService _patients;
        private readonly IQueueService _queue;
        private readonly IHistoryService _history;
        private readonly IBillingService _billing;

        public ClsService(
            DataContext db,
            IRealtimeService realtime,
            INotificationService notifications,
            IDashboardService dashboard,
            IPatientService patients,
            IQueueService queue,
            IHistoryService history,
            IBillingService billing)
        {
            _db = db;
            _realtime = realtime;
            _notifications = notifications;
            _dashboard = dashboard;
            _patients = patients;
            _queue = queue;
            _history = history;
            _billing = billing;
        }
        // ================== HELPER ==================

        private static string? BuildThongTinChiTiet(BenhNhan bn)
        {
            var parts = new List<string>();

            void Add(string label, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    parts.Add($"{label}: {value}");
            }

            Add("Dị ứng", bn.DiUng);
            Add("Chống chỉ định", bn.ChongChiDinh);
            Add("Thuốc đang dùng", bn.ThuocDangDung);
            Add("Tiền sử bệnh", bn.TieuSuBenh);
            Add("Tiền sử phẫu thuật", bn.TienSuPhauThuat);
            Add("Nhóm máu", bn.NhomMau);
            Add("Bệnh mạn tính", bn.BenhManTinh);
            Add("Sinh hiệu", bn.SinhHieu);

            return parts.Count == 0 ? null : string.Join(" | ", parts);
        }

        private async Task<IReadOnlyList<ClsItemDto>> MapClsItemsAsync(
            IEnumerable<ChiTietDichVu> chiTietList,
            DateTime? thoiDiem = null)
        {
            var list = chiTietList.ToList();
            if (list.Count == 0)
                return Array.Empty<ClsItemDto>();

            var now = thoiDiem ?? DateTime.Now;
            var ngay = now.Date;
            var gio = now.TimeOfDay;

            var maPhongs = list
                .Select(ct => ct.DichVuYTe?.MaPhongThucHien)
                .Where(mp => !string.IsNullOrWhiteSpace(mp))
                .Select(mp => mp!)
                .Distinct()
                .ToList();

            var lichTrucs = maPhongs.Count == 0
                ? new List<LichTruc>()
                : await _db.LichTrucs
                    .AsNoTracking()
                    .Include(l => l.YTaTruc)
                    .Where(l =>
                        maPhongs.Contains(l.MaPhong) &&
                        !l.NghiTruc &&
                        l.Ngay == ngay &&
                        l.GioBatDau <= gio &&
                        l.GioKetThuc >= gio)
                    .ToListAsync();

            var ytaByPhong = lichTrucs
                .GroupBy(l => l.MaPhong)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(l => l.GioBatDau).First());

            return list.Select(ct => MapClsItem(ct, ytaByPhong)).ToList();
        }

        private async Task<ClsItemDto> MapClsItemAsync(ChiTietDichVu c, DateTime? thoiDiem = null)
        {
            var mapped = await MapClsItemsAsync(new[] { c }, thoiDiem);
            return mapped.First();
        }

        private static ClsItemDto MapClsItem(
            ChiTietDichVu c,
            IDictionary<string, LichTruc> ytaByPhong)
        {
            var dv = c.DichVuYTe;
            var trangThai = c.TrangThai?.ToLowerInvariant();
            var statusNormalized = trangThai switch
            {
                "da_co_ket_qua" => "da_co_ket_qua",
                "dang_thuc_hien" => "dang_thuc_hien",
                _ => "chua_co_ket_qua"
            };

            var maPhong = dv?.MaPhongThucHien ?? "";
            ytaByPhong.TryGetValue(maPhong, out var lichYTa);

            return new ClsItemDto
            {
                MaChiTietDv = c.MaChiTietDv,
                MaPhieuKhamCls = c.MaPhieuKhamCls,
                MaDichVu = c.MaDichVu,
                TenDichVu = dv?.TenDichVu ?? "",
                LoaiDichVu = dv?.LoaiDichVu ?? "",
                PhiDV = dv?.DonGia.ToString("0") ?? "0",
                MaPhong = maPhong,
                TenPhong = dv?.PhongThucHien?.TenPhong ?? "",
                MaYTaThucHien = lichYTa?.MaYTaTruc,
                TenYTaThucHien = lichYTa?.YTaTruc?.HoTen,
                GhiChu = c.GhiChu,
                TrangThai = statusNormalized
            };
        }

        private async Task<ClsOrderDto?> BuildClsOrderDtoAsync(string maPhieuKhamCls)
        {
            var row =
                await (from cls in _db.PhieuKhamCanLamSangs.AsNoTracking()
                       join ls in _db.PhieuKhamLamSangs.AsNoTracking()
                           on cls.MaPhieuKhamLs equals ls.MaPhieuKham
                       join bn in _db.BenhNhans.AsNoTracking()
                           on ls.MaBenhNhan equals bn.MaBenhNhan
                       where cls.MaPhieuKhamCls == maPhieuKhamCls
                       select new { cls, ls, bn })
                .FirstOrDefaultAsync();

            if (row is null) return null;

            var chiTietList = await _db.ChiTietDichVus
                .AsNoTracking()
                .Where(c => c.MaPhieuKhamCls == maPhieuKhamCls)
                .Include(c => c.DichVuYTe)
                .ToListAsync();

            var itemDtos = await MapClsItemsAsync(chiTietList);

            var firstDv = chiTietList.FirstOrDefault()?.DichVuYTe;

            return new ClsOrderDto
            {
                MaPhieuKhamCls = row.cls.MaPhieuKhamCls,
                MaPhieuKhamLs = row.cls.MaPhieuKhamLs,
                MaBenhNhan = row.bn.MaBenhNhan,

                HoTen = row.bn.HoTen,
                TenBenhNhan = row.bn.HoTen,
                NgaySinh = row.bn.NgaySinh,
                GioiTinh = row.bn.GioiTinh,
                DienThoai = row.bn.DienThoai,
                Email = row.bn.Email,
                DiaChi = row.bn.DiaChi,

                TrangThai = row.cls.TrangThai,
                AutoPublishEnabled = row.cls.AutoPublishEnabled,
                GhiChu = row.cls.GhiChu,
                NgayLap = row.cls.NgayGioLap,
                GioLap = row.cls.NgayGioLap.TimeOfDay,

                MaKhoa = "",
                TenKhoa = null,

                MaNguoiLap = row.ls.MaNguoiLap,
                TenNguoiLap = null,

                ThongTinChiTiet = BuildThongTinChiTiet(row.bn),
                ListItemDV = itemDtos
            };
        }

        // ================== 1. PHIẾU CLS ==================

        public async Task<ClsOrderDto> TaoPhieuClsAsync(ClsOrderCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.MaBenhNhan))
                throw new ArgumentException("MaBenhNhan là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.MaPhieuKhamLs))
                throw new ArgumentException("MaPhieuKhamLs là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.MaNguoiLap))
                throw new ArgumentException("MaNguoiLap là bắt buộc");

            // Đảm bảo phiếu CLS luôn gắn với đúng phiếu LS + BN
            var phieuLs = await _db.PhieuKhamLamSangs
                            .Include(p => p.BenhNhan)
                            .Include(p => p.DichVuKham)
                            .FirstOrDefaultAsync(p =>
            p.MaPhieuKham == request.MaPhieuKhamLs &&
            p.MaBenhNhan == request.MaBenhNhan)
                                ?? throw new InvalidOperationException("Không tìm thấy phiếu khám LS tương ứng");

            // Nếu đã có phiếu CLS gắn với phiếu LS này thì trả về, không tạo mới (tránh duplicate)
            var existedCls = await _db.PhieuKhamCanLamSangs
                .Include(c => c.ChiTietDichVus)
                .FirstOrDefaultAsync(c => c.MaPhieuKhamLs == request.MaPhieuKhamLs);
            if (existedCls is not null)
            {
                if (!string.Equals(existedCls.TrangThai, "da_hoan_tat", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Phiếu CLS cho phiếu khám này đang chưa hoàn tất, không thể tạo thêm.");

                var dtoDup = await BuildClsOrderDtoAsync(existedCls.MaPhieuKhamCls);
                if (dtoDup is not null)
                    return dtoDup;

                throw new InvalidOperationException("Phiếu CLS cho phiếu khám này đã tồn tại.");
            }

            var now = DateTime.Now;
            PhieuKhamCanLamSang phieuCls;

            // Transaction: tạo phiếu CLS + chi tiết DV một lượt
            await using (var tx = await _db.Database.BeginTransactionAsync())
            {
                phieuCls = new PhieuKhamCanLamSang
                {
                    MaPhieuKhamCls = $"CLS-{Guid.NewGuid():N}",
                    MaPhieuKhamLs = phieuLs.MaPhieuKham,
                    NgayGioLap = now,
                    AutoPublishEnabled = request.AutoPublishEnabled,
                    TrangThai = request.TrangThai ?? "da_lap",
                    GhiChu = request.GhiChu
                };

                _db.PhieuKhamCanLamSangs.Add(phieuCls);
                await _db.SaveChangesAsync();

                // Tạo danh sách chi tiết DV nếu có
                if (request.ListItemDV is not null && request.ListItemDV.Count > 0)
                {
                    foreach (var item in request.ListItemDV)
                    {
                        // Mỗi dòng BS nhập trên UI -> 1 ChiTietDichVu
                        var createReq = new ClsItemCreateRequest
                        {
                            MaPhieuKhamCls = phieuCls.MaPhieuKhamCls,
                            MaDichVu = item.MaDichVu,
                            GhiChu = item.GhiChu,
                            TrangThai = string.IsNullOrWhiteSpace(item.TrangThai)
                                ? "chua_co_ket_qua"
                                : item.TrangThai
                        };

                        // Tận dụng hàm có sẵn (tự map + load DichVuYTe)
                        await TaoChiTietDichVuAsync(createReq);
                    }
                }

                await tx.CommitAsync();
            }

            // Sau khi chỉ định CLS, cập nhật trạng thái trong ngày của BN
            await _patients.CapNhatTrangThaiBenhNhanAsync(
                phieuLs.MaBenhNhan,
                new PatientStatusUpdateRequest
                {
                    TrangThaiHomNay = "cho_tiep_nhan_dv"
                });

            var dto = await BuildClsOrderDtoAsync(phieuCls.MaPhieuKhamCls)
                      ?? throw new InvalidOperationException("Không build được DTO CLS");

            // Realtime: phiếu CLS mới
            await _realtime.BroadcastClsOrderCreatedAsync(dto);

            // Thông báo: chỉ định CLS mới
            await TaoThongBaoChiDinhClsMoiAsync(dto);

            // Cập nhật Dashboard: lượt khám + hoạt động gần đây
            var dashboard = await _dashboard.LayDashboardHomNayAsync();

            await _realtime.BroadcastDashboardTodayAsync(dashboard);
            //await _realtime.BroadcastTodayExamOverviewAsync(dashboard.LuotKhamHomNay);
            //await _realtime.BroadcastRecentActivitiesAsync(dashboard.HoatDongGanDay);

            return dto;
        }

        public Task<ClsOrderDto?> LayPhieuClsAsync(string maPhieuKhamCls)
        {
            if (string.IsNullOrWhiteSpace(maPhieuKhamCls))
                return Task.FromResult<ClsOrderDto?>(null);

            return BuildClsOrderDtoAsync(maPhieuKhamCls);
        }

        public async Task<ClsOrderDto?> CapNhatTrangThaiPhieuClsAsync(string maPhieuKhamCls, string trangThai)
        {
            if (string.IsNullOrWhiteSpace(maPhieuKhamCls))
                return null;
            if (string.IsNullOrWhiteSpace(trangThai))
                throw new ArgumentException("TrangThai là bắt buộc");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var phieu = await _db.PhieuKhamCanLamSangs
                    .Include(p => p.PhieuKhamLamSang)
                        .ThenInclude(ls => ls.BenhNhan)
                    .Include(p => p.ChiTietDichVus)
                        .ThenInclude(ct => ct.DichVuYTe)
                    .FirstOrDefaultAsync(p => p.MaPhieuKhamCls == maPhieuKhamCls);

                if (phieu is null) return null;

                // Auto-billing: tạo hóa đơn khi chuyển sang "dang_thuc_hien"
                if (string.Equals(trangThai, "dang_thuc_hien", StringComparison.OrdinalIgnoreCase))
                {
                    var daCoHoaDon = await _db.HoaDonThanhToans
                        .AsNoTracking()
                        .AnyAsync(hd => hd.MaPhieuKhamCls == phieu.MaPhieuKhamCls);

                    if (!daCoHoaDon)
                    {
                        var maBenhNhan = phieu.PhieuKhamLamSang?.MaBenhNhan;
                        if (!string.IsNullOrWhiteSpace(maBenhNhan))
                        {
                            // Tính tổng tiền từ tất cả dịch vụ
                            var tongTien = phieu.ChiTietDichVus.Sum(ct => ct.DichVuYTe?.DonGia ?? 0m);
                            
                            // Lấy nhân sự thu (có thể là người lập phiếu hoặc nhân sự mặc định)
                            var thuNgan = await _db.NhanVienYTes
                                .AsNoTracking()
                                .FirstOrDefaultAsync(nv => nv.VaiTro == "y_ta" && nv.LoaiYTa == "hanhchinh")
                                ?? await _db.NhanVienYTes.AsNoTracking().FirstOrDefaultAsync()
                                ?? throw new InvalidOperationException("Không tìm thấy nhân sự thu");

                            var invoiceReq = new InvoiceCreateRequest
                            {
                                MaBenhNhan = maBenhNhan,
                                MaNhanSuThu = thuNgan.MaNhanVien,
                                LoaiDotThu = "can_lam_sang",
                                SoTien = tongTien,
                                MaPhieuKhamCls = phieu.MaPhieuKhamCls,
                                PhuongThucThanhToan = "tien_mat",
                                NoiDung = $"Thu phí cận lâm sàng - Phiếu {phieu.MaPhieuKhamCls}"
                            };
                            await _billing.TaoHoaDonAsync(invoiceReq);
                        }
                    }
                }

                phieu.TrangThai = trangThai;
                await _db.SaveChangesAsync();

                var maBenhNhan2 = phieu.PhieuKhamLamSang?.MaBenhNhan;
                var firstCt = phieu.ChiTietDichVus.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(maBenhNhan2))
                {
                    Console.WriteLine($"[CLS] Updating patient {maBenhNhan2} status to cho_kham_dv");
                    try
                    {
                        await _patients.CapNhatTrangThaiBenhNhanAsync(
                            maBenhNhan2,
                            new PatientStatusUpdateRequest { TrangThaiHomNay = "cho_kham_dv" });
                        Console.WriteLine($"[CLS] Patient status updated successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CLS] Failed to update patient status: {ex.Message}");
                        throw;
                    }
                }

                if (firstCt is not null)
                {
                    await CapNhatTrangThaiChiTietDVAsync(firstCt.MaChiTietDv, trangThai);
                }

                // Queue management: tạo hàng đợi cho dịch vụ đầu tiên
                if (firstCt is not null && !string.IsNullOrWhiteSpace(maBenhNhan2))
                {
                    var daCoHangDoi = await _db.HangDois.AnyAsync(h => h.MaChiTietDv == firstCt.MaChiTietDv);
                    if (!daCoHangDoi)
                    {
                        var phongDv = firstCt.DichVuYTe?.MaPhongThucHien ?? "CLS_XN_01";
                        await _queue.ThemVaoHangDoiAsync(new QueueEnqueueRequest
                        {
                            MaBenhNhan = maBenhNhan2!,
                            MaPhong = phongDv,
                            LoaiHangDoi = "can_lam_sang",
                            MaChiTietDv = firstCt.MaChiTietDv,
                            MaPhieuKham = null,
                            Nguon = null,
                            Nhan = null,
                            ThoiGianLichHen = null
                        });
                    }
                }

                await transaction.CommitAsync();

                // Broadcast after successful transaction
                var dto = await BuildClsOrderDtoAsync(maPhieuKhamCls);

                if (dto is not null)
                {
                    await _realtime.BroadcastClsOrderStatusUpdatedAsync(dto);
                    var dashboard = await _dashboard.LayDashboardHomNayAsync();
                    await _realtime.BroadcastDashboardTodayAsync(dashboard);
                }

                return dto;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ClsItemDto?> CapNhatTrangThaiChiTietDVAsync(string maChiTietDv, string trangThai)
        {
            if (string.IsNullOrWhiteSpace(maChiTietDv))
                return null;
            if (string.IsNullOrWhiteSpace(trangThai))
                throw new ArgumentException("TrangThai là bắt buộc");

            var ct = await _db.ChiTietDichVus
                .Include(c => c.DichVuYTe)
                .FirstOrDefaultAsync(c => c.MaChiTietDv == maChiTietDv);

            if (ct is null) return null;

            ct.TrangThai = trangThai;
            await _db.SaveChangesAsync();

            var dto = await MapClsItemAsync(ct);

            // Broadcast cập nhật chi tiết DV để FE/queue nắm trạng thái
            await _realtime.BroadcastClsItemUpdatedAsync(dto);

            return dto;
        }

// ================== 2. SEARCH + PAGING PHIẾU CLS ==================

        public async Task<PagedResult<ClsOrderDto>> TimKiemPhieuClsAsync(
            string? maBenhNhan,
            string? maBacSi,
            DateTime? fromDate,
            DateTime? toDate,
            string? trangThai,
            int page,
            int pageSize)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 50 : pageSize; // ✅ Chuẩn hóa: 50 items mặc định

            var query =
                from cls in _db.PhieuKhamCanLamSangs.AsNoTracking()
                join ls in _db.PhieuKhamLamSangs.AsNoTracking()
                    on cls.MaPhieuKhamLs equals ls.MaPhieuKham
                join bn in _db.BenhNhans.AsNoTracking()
                    on ls.MaBenhNhan equals bn.MaBenhNhan
                select new { cls, ls, bn };

            if (!string.IsNullOrWhiteSpace(maBenhNhan))
                query = query.Where(x => x.bn.MaBenhNhan == maBenhNhan);

            if (!string.IsNullOrWhiteSpace(maBacSi))
                query = query.Where(x => x.ls.MaBacSiKham == maBacSi);

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(x => x.cls.NgayGioLap >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.cls.NgayGioLap < to);
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(x => x.cls.TrangThai == trangThai);

            query = query.OrderByDescending(x => x.cls.NgayGioLap);

            var totalItems = await query.CountAsync();
            var pageData = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var maClsList = pageData.Select(x => x.cls.MaPhieuKhamCls).ToList();

            var itemsByHeader = await _db.ChiTietDichVus
                .AsNoTracking()
                .Where(c => maClsList.Contains(c.MaPhieuKhamCls))
                .Include(c => c.DichVuYTe)
                .GroupBy(c => c.MaPhieuKhamCls)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            var dtos = new List<ClsOrderDto>();

            foreach (var row in pageData)
            {
                itemsByHeader.TryGetValue(row.cls.MaPhieuKhamCls, out var list);
                var itemDtos = await MapClsItemsAsync(list ?? new List<ChiTietDichVu>());

                var firstDv = (list ?? new List<ChiTietDichVu>())
                    .FirstOrDefault()?.DichVuYTe;

                dtos.Add(new ClsOrderDto
                {
                    MaPhieuKhamCls = row.cls.MaPhieuKhamCls,
                    MaPhieuKhamLs = row.cls.MaPhieuKhamLs,
                    MaBenhNhan = row.bn.MaBenhNhan,

                    HoTen = row.bn.HoTen,
                    TenBenhNhan = row.bn.HoTen,
                    NgaySinh = row.bn.NgaySinh,
                    GioiTinh = row.bn.GioiTinh,
                    DienThoai = row.bn.DienThoai,
                    Email = row.bn.Email,
                    DiaChi = row.bn.DiaChi,

                    TrangThai = row.cls.TrangThai,
                    AutoPublishEnabled = row.cls.AutoPublishEnabled,
                    GhiChu = row.cls.GhiChu,
                    NgayLap = row.cls.NgayGioLap,
                    GioLap = row.cls.NgayGioLap.TimeOfDay,

                    MaKhoa = "",
                    TenKhoa = null,

                    MaNguoiLap = row.ls.MaNguoiLap,
                    TenNguoiLap = null,

                    ThongTinChiTiet = BuildThongTinChiTiet(row.bn),
                    ListItemDV = itemDtos
                });
            }

            return new PagedResult<ClsOrderDto>
            {
                Items = dtos,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        // ================== 3. CHI TIẾT DỊCH VỤ ==================

        public async Task<ClsItemDto> TaoChiTietDichVuAsync(ClsItemCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.MaPhieuKhamCls))
                throw new ArgumentException("MaPhieuKhamCls là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.MaDichVu))
                throw new ArgumentException("MaDichVu là bắt buộc");

            var headerExists = await _db.PhieuKhamCanLamSangs
                .AnyAsync(p => p.MaPhieuKhamCls == request.MaPhieuKhamCls);

            if (!headerExists)
                throw new InvalidOperationException("Không tìm thấy phiếu CLS");

            var dichVu = await _db.DichVuYTes
                .FirstOrDefaultAsync(d => d.MaDichVu == request.MaDichVu)
                    ?? throw new InvalidOperationException("Không tìm thấy dịch vụ CLS");

            var chiTiet = new ChiTietDichVu
            {
                MaChiTietDv = $"DV-{Guid.NewGuid():N}",
                MaPhieuKhamCls = request.MaPhieuKhamCls,
                MaDichVu = request.MaDichVu,
                TrangThai = "chua_co_ket_qua",
                GhiChu = request.GhiChu
            };

            _db.ChiTietDichVus.Add(chiTiet);
            await _db.SaveChangesAsync();

            var loaded = await _db.ChiTietDichVus
                .AsNoTracking()
                .Include(c => c.DichVuYTe)
                .FirstAsync(c => c.MaChiTietDv == chiTiet.MaChiTietDv);

            return await MapClsItemAsync(loaded);
        }

        public async Task<IReadOnlyList<ClsItemDto>> LayDanhSachDichVuClsAsync(string maPhieuKhamCls)
        {
            var list = await _db.ChiTietDichVus
                .AsNoTracking()
                .Where(c => c.MaPhieuKhamCls == maPhieuKhamCls)
                .Include(c => c.DichVuYTe)
                .ToListAsync();

            return await MapClsItemsAsync(list);
        }

        /// <summary>
        /// Lấy chi tiết của một dịch vụ CLS (ChiTietDichVu) dưới dạng DTO `ClsItemDto`.
        /// Trả về null nếu không tìm thấy.
        /// </summary>
        public async Task<ClsItemDto?> LayChiTietDichVuAsync(string maChiTietDv)
        {
            if (string.IsNullOrWhiteSpace(maChiTietDv))
                return null;

            var ct = await _db.ChiTietDichVus
                .AsNoTracking()
                .Include(c => c.DichVuYTe)
                .FirstOrDefaultAsync(c => c.MaChiTietDv == maChiTietDv);

            if (ct is null) return null;

            return await MapClsItemAsync(ct);
        }

        // ================== 4. KẾT QUẢ CLS ==================

        public async Task<ClsResultDto> TaoKetQuaClsAsync(ClsResultCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.MaChiTietDv))
                throw new ArgumentException("MaChiTietDv là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.TrangThaiChot))
                throw new ArgumentException("TrangThaiChot là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.MaNhanSuThucHien))
                throw new ArgumentException("MaNhanSuThucHien là bắt buộc");

            var chiTiet = await _db.ChiTietDichVus
                .Include(c => c.DichVuYTe)
                .FirstOrDefaultAsync(c => c.MaChiTietDv == request.MaChiTietDv)
                    ?? throw new InvalidOperationException("Không tìm thấy chi tiết dịch vụ");

            var phieuCls = await _db.PhieuKhamCanLamSangs
                .Include(p => p.PhieuKhamLamSang)
                    .ThenInclude(ls => ls.BenhNhan)
                .FirstOrDefaultAsync(p => p.MaPhieuKhamCls == chiTiet.MaPhieuKhamCls);

            var ketQua = await _db.KetQuaDichVus
                .FirstOrDefaultAsync(k => k.MaChiTietDv == request.MaChiTietDv);

            var now = DateTime.Now;

            if (ketQua is null)
            {
                ketQua = new KetQuaDichVu
                {
                    MaKetQua = $"KQ-{Guid.NewGuid():N}",
                    MaChiTietDv = chiTiet.MaChiTietDv,
                    TrangThaiChot = request.TrangThaiChot,
                    NoiDungKetQua = request.NoiDungKetQua ?? "",
                    MaNguoiTao = request.MaNhanSuThucHien,
                    ThoiGianTao = now,
                    TepDinhKem = request.TepDinhKem
                };
                _db.KetQuaDichVus.Add(ketQua);
            }
            else
            {
                ketQua.TrangThaiChot = request.TrangThaiChot;
                ketQua.NoiDungKetQua = request.NoiDungKetQua ?? "";
                ketQua.MaNguoiTao = request.MaNhanSuThucHien;
                ketQua.TepDinhKem = request.TepDinhKem;
            }

            chiTiet.TrangThai = "da_co_ket_qua";

            await _db.SaveChangesAsync();

            await _db.Entry(ketQua).Reference(k => k.NhanVienYTes).LoadAsync();

            var dto = new ClsResultDto
            {
                MaKetQua = ketQua.MaKetQua,
                MaChiTietDv = ketQua.MaChiTietDv,
                MaPhieuKhamCls = chiTiet.MaPhieuKhamCls,
                MaDichVu = chiTiet.MaDichVu,
                TenDichVu = chiTiet.DichVuYTe?.TenDichVu ?? "",
                TrangThaiChot = ketQua.TrangThaiChot,
                NoiDungKetQua = ketQua.NoiDungKetQua,
                MaNhanSuThucHien = ketQua.MaNguoiTao,
                TenNhanSuThucHien = ketQua.NhanVienYTes?.HoTen ?? "",
                ThoiGianTao = ketQua.ThoiGianTao,
                TepDinhKem = ketQua.TepDinhKem
            };

            // Realtime: có kết quả CLS mới
            await _realtime.BroadcastClsResultCreatedAsync(dto);

            // Targeted broadcast: gửi notification cho bác sĩ chỉ định
            if (phieuCls?.PhieuKhamLamSang is not null)
            {
                var maBacSiChiDinh = phieuCls.PhieuKhamLamSang.MaBacSiKham;
                if (!string.IsNullOrWhiteSpace(maBacSiChiDinh))
                {
                    var notificationReq = new NotificationCreateRequest
                    {
                        LoaiThongBao = "ket_qua_cls",
                        TieuDe = "Kết quả CLS mới",
                        NoiDung = $"Kết quả {chiTiet.DichVuYTe?.TenDichVu ?? "dịch vụ CLS"} cho bệnh nhân {phieuCls.PhieuKhamLamSang.BenhNhan?.HoTen ?? ""} đã sẵn sàng",
                        MucDoUuTien = "normal",
                        NguonLienQuan = "phieu_cls",
                        MaDoiTuongLienQuan = chiTiet.MaPhieuKhamCls,
                        NguoiNhan = new List<NotificationRecipientCreateRequest>
                        {
                            new NotificationRecipientCreateRequest
                            {
                                LoaiNguoiNhan = "bac_si",
                                MaNguoiNhan = maBacSiChiDinh
                            }
                        }
                    };
                    await _notifications.TaoThongBaoAsync(notificationReq);
                }
            }

            // ===== Sau khi chốt kết quả DV CLS =====
            // 1) Cập nhật trạng thái chi tiết DV (đã làm ở trên) -> broadcast để FE nắm
            await _realtime.BroadcastClsItemUpdatedAsync(await MapClsItemAsync(chiTiet));

            // 2) Cập nhật trạng thái hàng đợi CLS của DV hiện tại
            var hangDoi = await _db.HangDois.FirstOrDefaultAsync(h => h.MaChiTietDv == chiTiet.MaChiTietDv);
            if (hangDoi is not null)
            {
                await _queue.CapNhatTrangThaiHangDoiAsync(
                    hangDoi.MaHangDoi,
                    new QueueStatusUpdateRequest { TrangThai = "da_phuc_vu" });

                // 3) Cập nhật trạng thái lượt khám CLS gắn với hàng đợi (nếu có)
                var luot = await _db.LuotKhamBenhs.FirstOrDefaultAsync(l => l.MaHangDoi == hangDoi.MaHangDoi);
                if (luot is not null)
                {
                    await _history.CapNhatTrangThaiLuotKhamAsync(
                        luot.MaLuotKham,
                        new HistoryVisitStatusUpdateRequest
                        {
                            TrangThai = "hoan_tat",
                            ThoiGianKetThuc = now
                        });
                }
            }

            // 4) Xác định còn DV khác không và enqueue DV tiếp theo nếu có
            static bool IsCompleted(string? status) =>
                string.Equals(status, "da_co_ket_qua", StringComparison.OrdinalIgnoreCase);

            var allDv = await _db.ChiTietDichVus
                .Include(c => c.DichVuYTe)
                .Where(c => c.MaPhieuKhamCls == chiTiet.MaPhieuKhamCls)
                .OrderBy(c => c.MaChiTietDv)
                .ToListAsync();

            var remaining = allDv
                .Where(c => !string.Equals(c.MaChiTietDv, chiTiet.MaChiTietDv, StringComparison.OrdinalIgnoreCase))
                .Where(c => !IsCompleted(c.TrangThai))
                .ToList();

            var maBenhNhan = phieuCls?.PhieuKhamLamSang?.MaBenhNhan;

            if (remaining.Any())
            {
                // Chuyển sang DV kế tiếp: trạng thái BN = chờ khám DV
                if (!string.IsNullOrWhiteSpace(maBenhNhan))
                {
                    await _patients.CapNhatTrangThaiBenhNhanAsync(
                        maBenhNhan,
                        new PatientStatusUpdateRequest { TrangThaiHomNay = "cho_kham_dv" });
                }

                // Tạo hàng đợi cho DV tiếp theo nếu chưa có
                var next = remaining.First();
                var daCoHangDoi = await _db.HangDois.AnyAsync(h => h.MaChiTietDv == next.MaChiTietDv);
                if (!daCoHangDoi && !string.IsNullOrWhiteSpace(maBenhNhan))
                {
                    var maPhongNext = next.DichVuYTe?.MaPhongThucHien ?? "CLS_XN_01";
                    await _queue.ThemVaoHangDoiAsync(new QueueEnqueueRequest
                    {
                        MaBenhNhan = maBenhNhan,
                        MaPhong = maPhongNext,
                        LoaiHangDoi = "can_lam_sang",
                        MaChiTietDv = next.MaChiTietDv,
                        MaPhieuKham = null,
                        Nguon = null,
                        Nhan = null,
                        ThoiGianLichHen = null,
                        DoUuTien = 0,
                        CapCuu = false
                    });
                }
            }
            else
            {
                // Hoàn tất tất cả DV CLS
                if (!string.IsNullOrWhiteSpace(maBenhNhan))
                {
                    await _patients.CapNhatTrangThaiBenhNhanAsync(
                        maBenhNhan,
                        new PatientStatusUpdateRequest { TrangThaiHomNay = "cho_xu_ly_dv" });
                }
            }

            // 5) AUTO PUBLISH: nếu DV cuối cùng đã xong thì tự lập phiếu tổng hợp
            await TryAutoPublishTongHopIfCompletedAsync(chiTiet.MaPhieuKhamCls);

            return dto;
        }

        public async Task<IReadOnlyList<ClsResultDto>> LayKetQuaTheoPhieuClsAsync(string maPhieuKhamCls)
        {
            var list = await _db.KetQuaDichVus
                .AsNoTracking()
                .Include(k => k.NhanVienYTes)
                .Include(k => k.ChiTietDichVu)
                    .ThenInclude(ct => ct.DichVuYTe)
                .Where(k => k.ChiTietDichVu.MaPhieuKhamCls == maPhieuKhamCls)
                .ToListAsync();

            return list.Select(k => new ClsResultDto
            {
                MaKetQua = k.MaKetQua,
                MaChiTietDv = k.MaChiTietDv,
                MaPhieuKhamCls = k.ChiTietDichVu.MaPhieuKhamCls,
                MaDichVu = k.ChiTietDichVu.MaDichVu,
                TenDichVu = k.ChiTietDichVu.DichVuYTe?.TenDichVu ?? "",
                TrangThaiChot = k.TrangThaiChot,
                NoiDungKetQua = k.NoiDungKetQua,
                MaNhanSuThucHien = k.MaNguoiTao,
                TenNhanSuThucHien = k.NhanVienYTes?.HoTen ?? "",
                ThoiGianTao = k.ThoiGianTao,
                TepDinhKem = k.TepDinhKem
            }).ToList();
        }

        // ================== 5. PHIẾU TỔNG HỢP KQ CLS ==================

        public async Task<ClsSummaryDto> TaoTongHopAsync(string maPhieuKhamCls)
        {
            if (string.IsNullOrWhiteSpace(maPhieuKhamCls))
                throw new ArgumentException("MaPhieuKhamCls là bắt buộc");

            var phieuCls = await _db.PhieuKhamCanLamSangs
                .Include(p => p.PhieuKhamLamSang)
                    .ThenInclude(ls => ls.BenhNhan)
                .Include(p => p.PhieuKhamLamSang)
                    .ThenInclude(ls => ls.DichVuKham)
                .FirstOrDefaultAsync(p => p.MaPhieuKhamCls == maPhieuKhamCls)
                    ?? throw new InvalidOperationException("Không tìm thấy phiếu CLS");

            var phieuLs = phieuCls.PhieuKhamLamSang
                ?? throw new InvalidOperationException("Phiếu CLS không gắn với phiếu khám LS");

            var bn = phieuLs.BenhNhan;
            var clsResults = await LayKetQuaTheoPhieuClsAsync(maPhieuKhamCls);

            var snapshotObj = new
            {
                PhieuCls = new
                {
                    phieuCls.MaPhieuKhamCls,
                    phieuCls.MaPhieuKhamLs,
                    phieuCls.NgayGioLap,
                    phieuCls.TrangThai,
                    phieuCls.GhiChu
                },
                BenhNhan = new
                {
                    bn.MaBenhNhan,
                    bn.HoTen,
                    bn.NgaySinh,
                    bn.GioiTinh,
                    bn.DienThoai,
                    bn.DiaChi
                },
                KetQua = clsResults
            };

            var snapshotJson = JsonSerializer.Serialize(snapshotObj);
            var now = DateTime.Now;

            var summary = await _db.PhieuTongHopKetQuas
                .FirstOrDefaultAsync(s => s.MaPhieuKhamCls == maPhieuKhamCls);

            var isNew = summary is null;

            if (summary is null)
            {
                summary = new PhieuTongHopKetQua
                {
                    MaPhieuTongHop = $"THKQ-{Guid.NewGuid():N}",
                    MaPhieuKhamCls = maPhieuKhamCls,
                    LoaiPhieu = "tong_hop_cls",
                    TrangThai = "cho_xu_ly",
                    ThoiGianXuLy = now,
                    SnapshotJson = snapshotJson
                };
                _db.PhieuTongHopKetQuas.Add(summary);
            }
            else
            {
                summary.SnapshotJson = snapshotJson;
                summary.ThoiGianXuLy = now;
            }

            // ===== Gắn mã phiếu tổng hợp vào lại phiếu khám LS =====
            phieuLs.MaPhieuKqKhamCls = summary.MaPhieuTongHop;
            await _db.SaveChangesAsync();

            // ===== Tạo lại hàng chờ cho phiếu LS để quay lại khám =====
            // Tìm hàng chờ hiện có của phiếu LS
            var queueExisting = await _db.HangDois
                .Include(h => h.PhieuKhamLamSang)
                    .ThenInclude(p => p.DichVuKham)
                .FirstOrDefaultAsync(h => h.MaPhieuKham == phieuLs.MaPhieuKham);

            var maPhongKham = phieuLs.DichVuKham?.MaPhongThucHien;
            
            if (queueExisting is not null && !string.IsNullOrWhiteSpace(maPhongKham))
            {
                // Cập nhật hàng chờ hiện có: chuyển về "cho_goi", Nguon = "service_return"
                // Cập nhật hàng chờ hiện có: chuyển về "cho_goi", Nguon = "service_return"
                // CapNhatThongTinHangDoiAsync đã tự động set TrangThai = "cho_goi"
                await _queue.CapNhatThongTinHangDoiAsync(queueExisting.MaHangDoi, new QueueEnqueueRequest
                {
                    MaBenhNhan = phieuLs.MaBenhNhan,
                    MaPhong = maPhongKham,
                    LoaiHangDoi = "kham_lam_sang",
                    Nguon = "service_return",
                    Nhan = null,
                    CapCuu = false,
                    DoUuTien = 0, // QueueService sẽ tự tính độ ưu tiên cho service_return
                    ThoiGianLichHen = null,
                    MaPhieuKham = phieuLs.MaPhieuKham,
                    MaChiTietDv = null,
                    PhanLoaiDen = null
                });
            }
            else if (!string.IsNullOrWhiteSpace(maPhongKham))
            {
                // Tạo hàng chờ mới nếu chưa có (trường hợp hiếm - hàng chờ bị xóa)
                await _queue.ThemVaoHangDoiAsync(new QueueEnqueueRequest
                {
                    MaBenhNhan = phieuLs.MaBenhNhan,
                    MaPhong = maPhongKham,
                    LoaiHangDoi = "kham_lam_sang",
                    Nguon = "service_return",
                    Nhan = null,
                    CapCuu = false,
                    DoUuTien = 0,
                    ThoiGianLichHen = null,
                    MaPhieuKham = phieuLs.MaPhieuKham,
                    MaChiTietDv = null,
                    PhanLoaiDen = null
                });
            }

            // Cập nhật trạng thái bệnh nhân → cho_kham (chờ khám lại)
            await _patients.CapNhatTrangThaiBenhNhanAsync(
                phieuLs.MaBenhNhan,
                new PatientStatusUpdateRequest { TrangThaiHomNay = "cho_kham" });

            var dto = new ClsSummaryDto
            {
                MaPhieuTongHop = summary.MaPhieuTongHop,
                MaPhieuKhamCls = summary.MaPhieuKhamCls,
                MaBenhNhan = bn.MaBenhNhan,
                TenBenhNhan = bn.HoTen,
                NgayGioLapPhieuCls = phieuCls.NgayGioLap,
                ThoiGianXuLy = summary.ThoiGianXuLy,
                TrangThai = summary.TrangThai,
                SnapshotJson = summary.SnapshotJson
            };

            if (isNew)
            {
                await _realtime.BroadcastClsSummaryCreatedAsync(dto);
                // Thông báo: lập phiếu tổng hợp lần đầu
                await TaoThongBaoTongHopClsAsync(dto);
            }
            else
            {
                await _realtime.BroadcastClsSummaryUpdatedAsync(dto);
            }

            return dto;
        }

        public async Task<PagedResult<ClsSummaryDto>> LayTongHopKetQuaChoLapPhieuKhamAsync(ClsSummaryFilter filter)
        {
            if (string.IsNullOrWhiteSpace(filter.MaBenhNhan))
                throw new ArgumentException("MaBenhNhan là bắt buộc trong filter");

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 20 : filter.PageSize;

            var query =
                from s in _db.PhieuTongHopKetQuas.AsNoTracking()
                join cls in _db.PhieuKhamCanLamSangs.AsNoTracking()
                    on s.MaPhieuKhamCls equals cls.MaPhieuKhamCls
                join ls in _db.PhieuKhamLamSangs.AsNoTracking()
                    on cls.MaPhieuKhamLs equals ls.MaPhieuKham
                join bn in _db.BenhNhans.AsNoTracking()
                    on ls.MaBenhNhan equals bn.MaBenhNhan
                where bn.MaBenhNhan == filter.MaBenhNhan
                select new { s, cls, ls, bn };

            if (filter.FromDate.HasValue)
            {
                var from = filter.FromDate.Value.Date;
                query = query.Where(x => x.s.ThoiGianXuLy >= from);
            }

            if (filter.ToDate.HasValue)
            {
                var to = filter.ToDate.Value.Date.AddDays(1);
                query = query.Where(x => x.s.ThoiGianXuLy < to);
            }

            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
                query = query.Where(x => x.s.TrangThai == filter.TrangThai);

            query = query.OrderByDescending(x => x.s.ThoiGianXuLy);

            var totalItems = await query.CountAsync();
            var pageData = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = pageData.Select(x => new ClsSummaryDto
            {
                MaPhieuTongHop = x.s.MaPhieuTongHop,
                MaPhieuKhamCls = x.s.MaPhieuKhamCls,
                MaBenhNhan = x.bn.MaBenhNhan,
                TenBenhNhan = x.bn.HoTen,
                NgayGioLapPhieuCls = x.cls.NgayGioLap,
                ThoiGianXuLy = x.s.ThoiGianXuLy,
                TrangThai = x.s.TrangThai,
                SnapshotJson = x.s.SnapshotJson
            }).ToList();

            return new PagedResult<ClsSummaryDto>
            {
                Items = dtos,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ClsSummaryDto?> LayPhieuTongHopKetQuaAsync(string maPhieuTongHop)
        {
            if (string.IsNullOrWhiteSpace(maPhieuTongHop))
                return null;

            var row =
                await (from s in _db.PhieuTongHopKetQuas.AsNoTracking()
                       join cls in _db.PhieuKhamCanLamSangs.AsNoTracking()
                            on s.MaPhieuKhamCls equals cls.MaPhieuKhamCls
                       join ls in _db.PhieuKhamLamSangs.AsNoTracking()
                            on cls.MaPhieuKhamLs equals ls.MaPhieuKham
                       join bn in _db.BenhNhans.AsNoTracking()
                            on ls.MaBenhNhan equals bn.MaBenhNhan
                       where s.MaPhieuTongHop == maPhieuTongHop
                       select new { s, cls, ls, bn })
                .FirstOrDefaultAsync();

            if (row is null) return null;

            return new ClsSummaryDto
            {
                MaPhieuTongHop = row.s.MaPhieuTongHop,
                MaPhieuKhamCls = row.s.MaPhieuKhamCls,
                MaBenhNhan = row.bn.MaBenhNhan,
                TenBenhNhan = row.bn.HoTen,
                NgayGioLapPhieuCls = row.cls.NgayGioLap,
                ThoiGianXuLy = row.s.ThoiGianXuLy,
                TrangThai = row.s.TrangThai,
                SnapshotJson = row.s.SnapshotJson
            };
        }

        public async Task<ClsSummaryDto?> CapNhatTrangThaiTongHopAsync(
            string maPhieuTongHop,
            ClsSummaryStatusUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(maPhieuTongHop))
                return null;
            if (string.IsNullOrWhiteSpace(request.TrangThai))
                throw new ArgumentException("TrangThai là bắt buộc");

            var summary = await _db.PhieuTongHopKetQuas
                .FirstOrDefaultAsync(s => s.MaPhieuTongHop == maPhieuTongHop);

            if (summary is null) return null;

            summary.TrangThai = request.TrangThai;
            summary.ThoiGianXuLy = DateTime.Now;

            await _db.SaveChangesAsync();

            var dto = await LayPhieuTongHopKetQuaAsync(maPhieuTongHop);

            if (dto is not null)
            {
                await _realtime.BroadcastClsSummaryUpdatedAsync(dto);
            }

            return dto;
        }

        public async Task<ClsSummaryDto?> CapNhatPhieuTongHopAsync(
            string maPhieuTongHop,
            ClsSummaryUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(maPhieuTongHop))
                return null;

            var summary = await _db.PhieuTongHopKetQuas
                .FirstOrDefaultAsync(s => s.MaPhieuTongHop == maPhieuTongHop);

            if (summary is null) return null;

            if (!string.IsNullOrWhiteSpace(request.TrangThai))
                summary.TrangThai = request.TrangThai;

            if (!string.IsNullOrWhiteSpace(request.MaNhanSuXuLy))
                summary.MaNhanSuXuLy = request.MaNhanSuXuLy; // FE là nguồn chuẩn

            if (request.SnapshotJson is not null)
                summary.SnapshotJson = request.SnapshotJson;

            summary.ThoiGianXuLy = request.ThoiGianXuLy ?? DateTime.Now;

            await _db.SaveChangesAsync();

            var dto = await LayPhieuTongHopKetQuaAsync(maPhieuTongHop);

            if (dto is not null)
            {
                await _realtime.BroadcastClsSummaryUpdatedAsync(dto);
            }

            return dto;
        }
        // ==========================
        // =   THÔNG BÁO - CLS      =
        // ==========================

        // Auto-publish: khi tất cả DV trong 1 phiếu CLS đã có kết quả và AutoPublishEnabled = true
        // thì tự động tạo / cập nhật phiếu tổng hợp + gắn lại vào phiếu khám LS.
        private async Task TryAutoPublishTongHopIfCompletedAsync(string maPhieuKhamCls)
        {
            if (string.IsNullOrWhiteSpace(maPhieuKhamCls))
                return;

            var phieuCls = await _db.PhieuKhamCanLamSangs
                .Include(p => p.PhieuKhamLamSang)
                .FirstOrDefaultAsync(p => p.MaPhieuKhamCls == maPhieuKhamCls);

            if (phieuCls is null)
                return;

            // Check AutoPublishEnabled flag
            if (!phieuCls.AutoPublishEnabled)
                return;

            // Còn chi tiết DV nào chưa hoàn tất không?
            var conChuaHoanTat = await _db.ChiTietDichVus
                .AnyAsync(c =>
                    c.MaPhieuKhamCls == maPhieuKhamCls &&
                    !string.Equals(c.TrangThai, "da_co_ket_qua", StringComparison.OrdinalIgnoreCase));

            if (conChuaHoanTat)
                return;

            // Tất cả DV đã hoàn tất -> chốt phiếu CLS
            phieuCls.TrangThai = "da_hoan_tat";
            await _db.SaveChangesAsync();

            // Lập / cập nhật phiếu tổng hợp
            var summary = await TaoTongHopAsync(maPhieuKhamCls);

            // Gắn MaPhieuKqKhamCls vào phiếu khám LS
            if (phieuCls.PhieuKhamLamSang is not null && !string.IsNullOrWhiteSpace(summary.MaPhieuTongHop))
            {
                phieuCls.PhieuKhamLamSang.MaPhieuKqKhamCls = summary.MaPhieuTongHop;
                await _db.SaveChangesAsync();
            }
        }



        private async Task TaoThongBaoChiDinhClsMoiAsync(ClsOrderDto order)
        {
            var request = new NotificationCreateRequest
            {
                LoaiThongBao = "cls",
                TieuDe = "Chỉ định CLS mới",
                NoiDung = RenderThongBaoChiDinhClsMoi(order),
                MucDoUuTien = "normal",

                NguonLienQuan = "phieu_cls",
                MaDoiTuongLienQuan = order.MaPhieuKhamCls,

                // Broadcast cho nhân viên y tế (CLS / điều dưỡng hỗ trợ)
                NguoiNhan = new List<NotificationRecipientCreateRequest>
{
    new NotificationRecipientCreateRequest
    {
        LoaiNguoiNhan = "y_ta",//y_ta can_lam_sang
        MaNguoiNhan = null   // broadcast trong nhóm y_ta
    }
}
            };

            await _notifications.TaoThongBaoAsync(request);
        }

        private static string RenderThongBaoChiDinhClsMoi(ClsOrderDto order)
        {
            var tenBn = string.IsNullOrWhiteSpace(order.TenBenhNhan)
                ? order.MaBenhNhan
                : $"{order.TenBenhNhan} ({order.MaBenhNhan})";

            return $"Có chỉ định cận lâm sàng mới cho bệnh nhân {tenBn}. " +
                   "Vui lòng sắp xếp thực hiện và cập nhật kết quả.";
        }


        private async Task TaoThongBaoTongHopClsAsync(ClsSummaryDto summary)
        {
            var request = new NotificationCreateRequest
            {
                LoaiThongBao = "cls_tong_hop",
                TieuDe = "Phiếu tổng hợp kết quả CLS",
                NoiDung = RenderThongBaoTongHopCls(summary),
                MucDoUuTien = "normal",

                NguonLienQuan = "tong_hop_cls",
                MaDoiTuongLienQuan = summary.MaPhieuTongHop,

                NguoiNhan = new List<NotificationRecipientCreateRequest>
                {
                    new NotificationRecipientCreateRequest
                    {
                        LoaiNguoiNhan = "nhan_vien_y_te",
                        MaNguoiNhan = null
                    }
                }
            };

            await _notifications.TaoThongBaoAsync(request);
        }

        private static string RenderThongBaoTongHopCls(ClsSummaryDto summary)
        {
            var tenBn = string.IsNullOrWhiteSpace(summary.TenBenhNhan)
                ? summary.MaBenhNhan
                : $"{summary.TenBenhNhan} ({summary.MaBenhNhan})";

            return $"Đã có phiếu tổng hợp kết quả cận lâm sàng cho bệnh nhân {tenBn}.";
        }

    }
}
