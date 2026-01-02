using System;
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
    /// <summary>
    /// Service quản lý phiếu khám lâm sàng + chẩn đoán cuối.
    /// </summary>
    public class ClinicalService(
    DataContext db,
    IRealtimeService realtime,
    IDashboardService dashboard,
    INotificationService notifications, IQueueService queue,
    IBillingService billing, IPatientService patients, IPharmacyService pharmacy) : IClinicalService
    {
        private readonly DataContext _db = db;
        private readonly IRealtimeService _realtime = realtime;
        private readonly IDashboardService _dashboard = dashboard;
        private readonly INotificationService _notifications = notifications;
        private readonly IQueueService _queue = queue;
        private readonly IBillingService _billing = billing;
        private readonly IPatientService _patients = patients;
        private readonly IPharmacyService _pharmacy = pharmacy;
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

        private static ClinicalExamDto MapClinicalExam(PhieuKhamLamSang phieu)
        {
            var bn = phieu.BenhNhan;
            var dv = phieu.DichVuKham;
            var lichHen = phieu.LichHenKham;

            var snapshotCls = phieu.PhieuTongHopKetQua?.SnapshotJson;

            return new ClinicalExamDto
            {
                MaPhieuKham = phieu.MaPhieuKham,
                MaBenhNhan = phieu.MaBenhNhan,
                HoTen = bn.HoTen,
                NgaySinh = bn.NgaySinh,
                GioiTinh = bn.GioiTinh,
                DienThoai = bn.DienThoai,
                Email = bn.Email,
                DiaChi = bn.DiaChi,

                // Khoa/phòng: hiện BE chưa join sang Phong/Khoa, FE có thể lấy qua queue
                MaKhoa = "",
                TenKhoa = null,
                MaPhong = dv?.MaPhongThucHien ?? "",
                TenPhong = null,

                MaBacSiKham = phieu.MaBacSiKham,
                TenBacSiKham = phieu.BacSiKham?.HoTen,
                MaNguoiLap = phieu.MaNguoiLap,
                TenNguoiLap = phieu.NguoiLap?.HoTen,

                MaDichVuKham = phieu.MaDichVuKham,
                TenDichVuKham = dv?.TenDichVu ?? "",
                LoaiDichVu = dv?.LoaiDichVu ?? "",
                PhiDV = dv?.DonGia.ToString("0") ?? "0",

                MaLichHen = phieu.MaLichHen,
                LoaiHen = lichHen?.LoaiHen,
                MaPhieuKqKhamCls = phieu.MaPhieuKqKhamCls,
                SnapshotKqKhamCls = snapshotCls,

                HinhThucTiepNhan = phieu.HinhThucTiepNhan,
                NgayLap = phieu.NgayLap,
                GioLap = phieu.GioLap,
                TrieuChung = phieu.TrieuChung,

                // Gộp toàn bộ thông tin bệnh sử thành 1 chuỗi
                ThongTinChiTiet = BuildThongTinChiTiet(bn),

                TrangThai = phieu.TrangThai
            };
        }

        // ================== 1. TẠO PHIẾU KHÁM ==================

        public async Task<ClinicalExamDto> TaoPhieuKhamAsync(ClinicalExamCreateRequest request)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(request.MaBenhNhan))
                throw new ArgumentException("MaBenhNhan là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.MaBacSiKham))
                throw new ArgumentException("MaBacSiKham là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.MaNguoiLap))
                throw new ArgumentException("MaNguoiLap là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.MaDichVuKham))
                throw new ArgumentException("MaDichVuKham là bắt buộc");

            // Use transaction for complex operations
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var benhNhan = await _db.BenhNhans
                    .FirstOrDefaultAsync(b => b.MaBenhNhan == request.MaBenhNhan)
                        ?? throw new InvalidOperationException("Không tìm thấy bệnh nhân");

            // Cập nhật hồ sơ bệnh nhân từ 8 field chi tiết
            if (request.DiUng is not null) benhNhan.DiUng = request.DiUng;
            if (request.ChongChiDinh is not null) benhNhan.ChongChiDinh = request.ChongChiDinh;
            if (request.ThuocDangDung is not null) benhNhan.ThuocDangDung = request.ThuocDangDung;
            if (request.TieuSuBenh is not null) benhNhan.TieuSuBenh = request.TieuSuBenh;
            if (request.TienSuPhauThuat is not null) benhNhan.TienSuPhauThuat = request.TienSuPhauThuat;
            if (request.NhomMau is not null) benhNhan.NhomMau = request.NhomMau;
            if (request.BenhManTinh is not null) benhNhan.BenhManTinh = request.BenhManTinh;
            if (request.SinhHieu is not null) benhNhan.SinhHieu = request.SinhHieu;

            var now = DateTime.Now;
            var ngay = request.NgayLap?.Date ?? now.Date;
            var gio = request.GioLap ?? now.TimeOfDay;

            // ===== 1. Kiểm tra lịch hẹn + tính phân loại đến (nếu có) =====
            DateTime? thoiGianLichHen = null;
            string? phanLoaiDen = null;
            // PATCH: giữ lại LoaiHen để gắn vào Nhan của queue
            string? loaiHen = null;
            if (!string.IsNullOrWhiteSpace(request.MaLichHen))
            {
                var lichHen = await _db.LichHenKhams
                    .FirstOrDefaultAsync(l => l.MaLichHen == request.MaLichHen)
                        ?? throw new InvalidOperationException("Không tìm thấy lịch hẹn");

                if (!lichHen.CoHieuLuc || lichHen.TrangThai != "da_checkin")
                    throw new InvalidOperationException("Lịch hẹn không còn hiệu lực hoặc chưa được check-in");
                loaiHen = lichHen.LoaiHen;
                // Nếu lịch hẹn đã gán bệnh nhân thì phải khớp
                if (!string.IsNullOrEmpty(lichHen.MaBenhNhan) &&
                    !string.Equals(lichHen.MaBenhNhan, request.MaBenhNhan, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Lịch hẹn không thuộc về bệnh nhân này");
                }

                // Nếu lịch hẹn chưa gán MaBenhNhan thì gán lại
                if (string.IsNullOrEmpty(lichHen.MaBenhNhan))
                {
                    lichHen.MaBenhNhan = request.MaBenhNhan;
                }

                thoiGianLichHen = lichHen.NgayHen.Date + lichHen.GioHen;

                var diff = now - thoiGianLichHen.Value; // >0: đến trễ
                if (diff.TotalMinutes > 30)
                    phanLoaiDen = "den_muon";
                else if (diff.TotalMinutes < -15)
                    phanLoaiDen = "den_som";
                else
                    phanLoaiDen = "dung_gio";
            }

            // ===== 2. Load dịch vụ khám để lấy phòng thực hiện =====
            var dichVuKham = await _db.DichVuYTes
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.MaDichVu == request.MaDichVuKham)
                    ?? throw new InvalidOperationException("Không tìm thấy dịch vụ khám");

            var maPhongKham = dichVuKham.MaPhongThucHien
                ?? throw new InvalidOperationException("Dịch vụ khám chưa cấu hình phòng thực hiện");

            // ===== 3. Rule: 1 bệnh nhân chỉ 1 phiếu LS đang hoạt động =====
            var existingActive = await _db.PhieuKhamLamSangs
                .Include(p => p.BenhNhan)
                .Include(p => p.DichVuKham)
                .Include(p => p.BacSiKham)
                .Include(p => p.NguoiLap)
                .Include(p => p.LichHenKham)
                .Include(p => p.PhieuTongHopKetQua)
                .FirstOrDefaultAsync(p =>
                    p.MaBenhNhan == request.MaBenhNhan &&
                    p.TrangThai != "da_hoan_tat" &&
                    p.TrangThai != "da_huy");

            PhieuKhamLamSang phieuInUse;
            var reusedExisting = false;

            if (existingActive is not null)
            {
                phieuInUse = existingActive;
                reusedExisting = true;
            }
            else
            {
                // Không có phiếu đang hoạt động -> tạo phiếu mới
                phieuInUse = new PhieuKhamLamSang
                {
                    MaPhieuKham = $"PKLS-{Guid.NewGuid():N}",
                    MaBenhNhan = benhNhan.MaBenhNhan,
                    MaBacSiKham = request.MaBacSiKham,
                    MaNguoiLap = request.MaNguoiLap,
                    MaDichVuKham = request.MaDichVuKham,
                    MaLichHen = request.MaLichHen,
                    NgayLap = ngay,
                    GioLap = gio,
                    TrieuChung = request.TrieuChung,
                    TrangThai = "da_lap"
                };

                _db.PhieuKhamLamSangs.Add(phieuInUse);
            }

            // ===== 4. Xác định HinhThucTiepNhan theo rule anh yêu cầu =====
            string hinhThucTiepNhan;

            if (!string.IsNullOrWhiteSpace(phieuInUse.MaPhieuKqKhamCls))
            {
                // Có mã phiếu tổng hợp kết quả CLS (phiếu cũ) -> tái khám / service_return
                hinhThucTiepNhan = "service_return";
            }
            else if (!string.IsNullOrWhiteSpace(phieuInUse.MaLichHen))
            {
                // Có mã lịch hẹn -> appointment
                hinhThucTiepNhan = "appointment";
            }
            else
            {
                // Còn lại -> walkin
                hinhThucTiepNhan = "walkin";
            }

            // Nếu đang dùng phiếu cũ chưa hoàn tất và không phải quay lại sau CLS -> chặn
            if (reusedExisting &&
                !string.Equals(hinhThucTiepNhan, "service_return", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(phieuInUse.TrangThai, "da_hoan_tat", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Phiếu khám của bệnh nhân này đang chưa hoàn tất, không thể tạo thêm phiếu/queue mới.");
            }

            // Nếu dùng phiếu cũ: cập nhật lại thông tin theo request
            if (reusedExisting)
            {
                phieuInUse.MaBacSiKham = request.MaBacSiKham;
                phieuInUse.MaNguoiLap = request.MaNguoiLap;
                phieuInUse.MaDichVuKham = request.MaDichVuKham;
                phieuInUse.MaLichHen = request.MaLichHen;
                phieuInUse.TrieuChung = request.TrieuChung ?? phieuInUse.TrieuChung;
                phieuInUse.NgayLap = ngay;
                phieuInUse.GioLap = gio;
            }

            phieuInUse.HinhThucTiepNhan = hinhThucTiepNhan;

            await _db.SaveChangesAsync();

            // ===== 5. Đẩy vào queue phòng khám =====
            // Cập nhật trạng thái BN trước khi thao tác queue
            await _patients.CapNhatTrangThaiBenhNhanAsync(
                phieuInUse.MaBenhNhan,
                new PatientStatusUpdateRequest { TrangThaiHomNay = "cho_kham" });

            var queueExisting = await _db.HangDois.AsNoTracking()
                .FirstOrDefaultAsync(h => h.MaPhieuKham == phieuInUse.MaPhieuKham);
            var skipBilling = false;

            if (queueExisting is not null)
            {
                // Đã có queue cho phiếu này: chỉ cho cập nhật nếu là service_return, ngược lại chặn
                if (!string.Equals(hinhThucTiepNhan, "service_return", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Phiếu khám của bệnh nhân này đang chưa hoàn tất, không thể tạo thêm phiếu/queue mới.");

                skipBilling = true; // tránh thu tiền lần nữa

                var reqUpdateQueue = new QueueEnqueueRequest
                {
                    MaBenhNhan = benhNhan.MaBenhNhan,
                    MaPhong = queueExisting.MaPhong,
                    LoaiHangDoi = queueExisting.LoaiHangDoi,
                    Nguon = "service_return",
                    Nhan = loaiHen,
                    CapCuu = false,
                    DoUuTien = 0,
                    ThoiGianLichHen = null,
                    MaPhieuKham = phieuInUse.MaPhieuKham,
                    MaChiTietDv = null,
                    PhanLoaiDen = null
                };

                await _queue.CapNhatThongTinHangDoiAsync(queueExisting.MaHangDoi, reqUpdateQueue);
            }
            else
            {
                var enqueueRequest = new QueueEnqueueRequest
                {
                    MaBenhNhan = benhNhan.MaBenhNhan,
                    MaPhong = maPhongKham,
                    LoaiHangDoi = "kham_lam_sang",                // queue khám lâm sàng
                    Nguon = hinhThucTiepNhan,              // 🔥 walkin / appointment / service_return
                    Nhan = loaiHen,
                    CapCuu = false,
                    DoUuTien = 0,                          // BE QueueService sẽ tự tính
                    ThoiGianLichHen = thoiGianLichHen,
                    MaPhieuKham = phieuInUse.MaPhieuKham,
                    MaChiTietDv = null,
                    PhanLoaiDen = phanLoaiDen             // tái sử dụng phân loại đến nếu có lịch hẹn
                };

                try
                {
                    await _queue.ThemVaoHangDoiAsync(enqueueRequest);
                }
                catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message?.Contains("IX_hang_doi_MaPhieuKham") == true)
                {
                    throw new InvalidOperationException("Phiếu khám này đã có hàng đợi, không thể tạo thêm.", dbEx);
                }
            }

            // ===== 6. Load lại phiếu để map DTO + realtime, dashboard, notify =====
            var loaded = await _db.PhieuKhamLamSangs
        .AsNoTracking()
        .Include(p => p.BenhNhan)
        .Include(p => p.DichVuKham)
        .Include(p => p.BacSiKham)
        .Include(p => p.NguoiLap)
        .Include(p => p.LichHenKham)
        .Include(p => p.PhieuTongHopKetQua)
        .FirstAsync(p => p.MaPhieuKham == phieuInUse.MaPhieuKham);

            // ===== AUTO BILLING CHO KHÁM LÂM SÀNG =====
            // Quy tắc:
            // - KHÔNG thu tiền nếu:
            //    + Hình thức tiếp nhận = service_return (quay lại sau CLS)
            //    + Hoặc lịch hẹn LoaiHen = "tai_kham" VÀ bệnh nhân không đến muộn
            // - Ngược lại: thu tiền khám LS (tạo hóa đơn "phi_kham_ls")
            var shouldCharge = true;

            // 1) Quay lại sau CLS thì miễn phí
            if (string.Equals(loaded.HinhThucTiepNhan, "service_return", StringComparison.OrdinalIgnoreCase))
            {
                shouldCharge = false;
            }

            var lichHenk = loaded?.LichHenKham;

            // 2) Tái khám: chỉ thu nếu đến muộn (den_muon)
            if (shouldCharge &&
                lichHenk != null &&
                string.Equals(lichHenk.LoaiHen, "tai_kham", StringComparison.OrdinalIgnoreCase))
            {
              

                // Tái khám đúng giờ / sớm -> miễn phí
                if (phanLoaiDen != "den_muon")
                    shouldCharge = false;
            }

            // 3) Thu tiền khám LS (walkin, appointment khám mới,
            //    hoặc tái khám nhưng đến muộn)
            if (!skipBilling && shouldCharge && loaded.DichVuKham is not null)
            {
                var dv = loaded.DichVuKham;

                var invoiceReq = new InvoiceCreateRequest
                {
                    MaBenhNhan = loaded.MaBenhNhan,
                    // Nhân sự lập phiếu LS coi như là nhân sự thu tiền luôn (thu tiền mặt)
                    MaNhanSuThu = loaded.MaNguoiLap,
                    MaPhieuKham = loaded.MaPhieuKham,
                    MaPhieuKhamCls = null,
                    MaDonThuoc = null,
                    LoaiDotThu = "kham_lam_sang",
                    SoTien = dv.DonGia,
                    PhuongThucThanhToan = "tien_mat",
                    NoiDung = $"Thu tiền khám lâm sàng ({dv.TenDichVu}) - Phiếu {loaded.MaPhieuKham}"
                };

                await _billing.TaoHoaDonAsync(invoiceReq);
            }

            // Commit transaction before broadcasting
            await transaction.CommitAsync();

            // ===== Broadcast realtime AFTER successful transaction =====
            var dto = MapClinicalExam(loaded);

            await _realtime.BroadcastClinicalExamCreatedAsync(dto);
            await TaoThongBaoPhieuKhamMoiAsync(dto);

            var dashboard = await _dashboard.LayDashboardHomNayAsync();
            await _realtime.BroadcastDashboardTodayAsync(dashboard);

            return dto;
        }
        catch (Exception)
        {
            // Rollback on any error
            await transaction.RollbackAsync();
            throw;
        }
    }


        // ================== 2. LẤY PHIẾU KHÁM ==================

        public async Task<ClinicalExamDto?> LayPhieuKhamAsync(string maPhieuKham)
        {
            if (string.IsNullOrWhiteSpace(maPhieuKham)) return null;

            var phieu = await _db.PhieuKhamLamSangs
                .AsNoTracking()
                .Include(p => p.BenhNhan)
                .Include(p => p.DichVuKham)
                .Include(p => p.BacSiKham)
                .Include(p => p.NguoiLap)
                .Include(p => p.LichHenKham)
                .Include(p => p.PhieuTongHopKetQua)
                .FirstOrDefaultAsync(p => p.MaPhieuKham == maPhieuKham);

            return phieu is null ? null : MapClinicalExam(phieu);
        }

        // ================== 3. CẬP NHẬT TRẠNG THÁI PHIẾU ==================

        public async Task<ClinicalExamDto?> CapNhatTrangThaiPhieuKhamAsync(
            string maPhieuKham,
            ClinicalExamStatusUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(maPhieuKham)) return null;
            if (string.IsNullOrWhiteSpace(request.TrangThai))
                throw new ArgumentException("TrangThai là bắt buộc");

            try
            {
                var phieu = await _db.PhieuKhamLamSangs
                    .Include(p => p.BenhNhan)
                    .Include(p => p.DichVuKham)
                    .Include(p => p.BacSiKham)
                    .Include(p => p.NguoiLap)
                    .Include(p => p.LichHenKham)
                    .Include(p => p.PhieuTongHopKetQua)
                    .FirstOrDefaultAsync(p => p.MaPhieuKham == maPhieuKham);

                if (phieu is null) return null;

                phieu.TrangThai = request.TrangThai;
                await _db.SaveChangesAsync();

                var dto = MapClinicalExam(phieu);
                
                // Broadcast after successful save
                await _realtime.BroadcastClinicalExamUpdatedAsync(dto);

                var dashboard = await _dashboard.LayDashboardHomNayAsync();
                await _realtime.BroadcastDashboardTodayAsync(dashboard);
                
                return dto;
            }
            catch (Exception ex)
            {
                // Log error and rethrow with context
                throw new InvalidOperationException($"Lỗi khi cập nhật trạng thái phiếu khám {maPhieuKham}", ex);
            }
        }

        // ================== 4. CHẨN ĐOÁN CUỐI ==================

        public async Task<FinalDiagnosisDto> TaoChanDoanCuoiAsync(
            FinalDiagnosisCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.MaPhieuKham))
                throw new ArgumentException("MaPhieuKham là bắt buộc");

            // Use transaction for complex cascade updates
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var phieu = await _db.PhieuKhamLamSangs
                    .Include(p => p.BenhNhan)
                    .Include(p => p.HangDois)
                        .ThenInclude(h => h.LuotKhamBenh)
                    .FirstOrDefaultAsync(p => p.MaPhieuKham == request.MaPhieuKham)
                    ?? throw new InvalidOperationException("Không tìm thấy phiếu khám");

                var hangDoi = phieu.HangDois;
                var luot = hangDoi?.LuotKhamBenh;
                var maBenhNhan = phieu.MaBenhNhan;

                var chanDoan = await _db.PhieuChanDoanCuois
                    .FirstOrDefaultAsync(c => c.MaPhieuKham == request.MaPhieuKham);

                if (chanDoan is null)
                {
                    chanDoan = new PhieuChanDoanCuoi
                    {
                        MaPhieuChanDoan = $"PCD-{Guid.NewGuid():N}",
                        MaPhieuKham = request.MaPhieuKham
                    };
                    _db.PhieuChanDoanCuois.Add(chanDoan);
                }

                chanDoan.MaDonThuoc = request.MaDonThuoc;
                chanDoan.ChanDoanSoBo = request.ChanDoanSoBo;
                chanDoan.ChanDoanCuoi = request.ChanDoanCuoi;
                chanDoan.NoiDungKham = request.NoiDungKham;
                chanDoan.HuongXuTri = request.HuongXuTri;
                chanDoan.LoiKhuyen = request.LoiKhuyen;
                chanDoan.PhatDoDieuTri = request.PhatDoDieuTri;

                // ===== CHỈ LƯU CHẨN ĐOÁN, KHÔNG ĐÓNG PHIẾU =====
                // Chuyển phiếu khám sang trạng thái "da_lap_chan_doan" (đã lập chẩn đoán, chờ xử lý)
                await CapNhatTrangThaiPhieuKhamAsync(
                    phieu.MaPhieuKham,
                    new ClinicalExamStatusUpdateRequest { TrangThai = "da_lap_chan_doan" });

                // Cập nhật trạng thái bệnh nhân → cho_xu_ly (chờ xử lý chẩn đoán)
                phieu.BenhNhan.TrangThaiHomNay = "cho_xu_ly";

                // KHÔNG đóng lượt khám, hàng đợi ở đây
                // Lượt khám vẫn: dang_kham
                // Hàng đợi vẫn: dang_thuc_hien

                await _db.SaveChangesAsync();

                PrescriptionDto? donThuocDto = null;
                if (request.DonThuoc is not null && request.DonThuoc.Count > 0)
                {
                    var maBacSiKeDon = request.MaBacSiKeDon;
                    if (string.IsNullOrWhiteSpace(maBacSiKeDon))
                    {
                        maBacSiKeDon = luot?.MaNhanSuThucHien ?? phieu.MaBacSiKham;
                    }

                    var prescriptionReq = new PrescriptionCreateRequest
                    {
                        MaBenhNhan = maBenhNhan,
                        MaBacSiKeDon = maBacSiKeDon!,
                        MaPhieuChanDoanCuoi = chanDoan.MaPhieuChanDoan,
                        TongTienDon = 0m,
                        Items = request.DonThuoc
                    };

                    donThuocDto = await _pharmacy.TaoDonThuocAsync(prescriptionReq);
                    chanDoan.MaDonThuoc = donThuocDto.MaDonThuoc;
                    await _db.SaveChangesAsync();
                }

                // Commit transaction before broadcasting
                await transaction.CommitAsync();

                // Broadcast after successful transaction
                var dto = new FinalDiagnosisDto
                {
                    MaPhieuChanDoan = chanDoan.MaPhieuChanDoan,
                    MaPhieuKham = chanDoan.MaPhieuKham,
                    MaBenhNhan = maBenhNhan,  // ✅ Thêm MaBenhNhan
                    MaDonThuoc = chanDoan.MaDonThuoc,
                    ChanDoanSoBo = chanDoan.ChanDoanSoBo,
                    ChanDoanCuoi = chanDoan.ChanDoanCuoi,
                    NoiDungKham = chanDoan.NoiDungKham,
                    HuongXuTri = chanDoan.HuongXuTri,
                    LoiKhuyen = chanDoan.LoiKhuyen,
                    PhatDoDieuTri = chanDoan.PhatDoDieuTri
                };

                await _realtime.BroadcastFinalDiagnosisChangedAsync(dto);

                if (!string.IsNullOrWhiteSpace(maBenhNhan))
                {
                    await _patients.CapNhatTrangThaiBenhNhanAsync(maBenhNhan, new PatientStatusUpdateRequest
                    {
                        TrangThaiHomNay = "cho_xu_ly"
                    });
                }
              
                var dashboard = await _dashboard.LayDashboardHomNayAsync();
                await _realtime.BroadcastDashboardTodayAsync(dashboard);
                await TaoThongBaoPhieuChuanDoanAsync(dto, phieu);
                
                return dto;
            }
            catch (Exception)
            {
                // Rollback on any error
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<FinalDiagnosisDto?> LayChanDoanCuoiAsync(string maPhieuKham)
        {
            if (string.IsNullOrWhiteSpace(maPhieuKham)) return null;

            var chanDoan = await _db.PhieuChanDoanCuois
                .AsNoTracking()
                .Include(c => c.PhieuKhamLamSang)  // ✅ Include để lấy MaBenhNhan
                .FirstOrDefaultAsync(c => c.MaPhieuKham == maPhieuKham);

            if (chanDoan is null) return null;

            return new FinalDiagnosisDto
            {
                MaPhieuChanDoan = chanDoan.MaPhieuChanDoan,
                MaPhieuKham = chanDoan.MaPhieuKham,
                MaBenhNhan = chanDoan.PhieuKhamLamSang?.MaBenhNhan,  // ✅ Lấy từ PhieuKhamLamSang
                MaDonThuoc = chanDoan.MaDonThuoc,
                ChanDoanSoBo = chanDoan.ChanDoanSoBo,
                ChanDoanCuoi = chanDoan.ChanDoanCuoi,
                NoiDungKham = chanDoan.NoiDungKham,
                HuongXuTri = chanDoan.HuongXuTri,
                LoiKhuyen = chanDoan.LoiKhuyen,
                PhatDoDieuTri = chanDoan.PhatDoDieuTri
            };
        }

        // ================== 4.1. HOÀN TẤT PHIẾU KHÁM ==================

        public async Task<ClinicalExamDto> CompleteExamAsync(
            string maPhieuKham,
            CompleteExamRequest request)
        {
            if (string.IsNullOrWhiteSpace(maPhieuKham))
                throw new ArgumentException("MaPhieuKham là bắt buộc");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var phieu = await _db.PhieuKhamLamSangs
                    .Include(p => p.BenhNhan)
                    .Include(p => p.HangDois)
                        .ThenInclude(h => h.LuotKhamBenh)
                    .Include(p => p.PhieuKhamCanLamSang)
                    .Include(p => p.PhieuChanDoanCuoi)
                        .ThenInclude(pcd => pcd.DonThuoc)
                    .FirstOrDefaultAsync(p => p.MaPhieuKham == maPhieuKham)
                    ?? throw new InvalidOperationException("Không tìm thấy phiếu khám");

                // Chỉ cho phép hoàn tất nếu đã có chẩn đoán
                if (phieu.TrangThai != "da_lap_chan_doan")
                    throw new InvalidOperationException(
                        "Phiếu khám chưa có chẩn đoán hoặc đã hoàn tất. Trạng thái hiện tại: " + phieu.TrangThai);

                // Kiểm tra các bước xử lý đã xong chưa (nếu không force)
                if (!request.ForceComplete)
                {
                    var hasPendingCls = await CheckClsPendingAsync(phieu);
                    var hasPendingPrescription = await CheckPrescriptionPendingAsync(phieu);
                    var hasPendingBilling = await CheckBillingPendingAsync(phieu);

                    if (hasPendingCls)
                        throw new InvalidOperationException("Còn dịch vụ CLS chưa hoàn tất.");

                    if (hasPendingPrescription)
                        throw new InvalidOperationException("Còn đơn thuốc chưa lấy.");

                    if (hasPendingBilling)
                        throw new InvalidOperationException("Còn thanh toán chưa xong.");
                }

                // Đóng tất cả
                var hangDoi = phieu.HangDois;
                var luot = hangDoi?.LuotKhamBenh;

                if (luot is not null)
                {
                    luot.TrangThai = "hoan_tat";
                    luot.ThoiGianKetThuc = DateTime.Now;
                }

                if (hangDoi is not null)
                {
                    hangDoi.TrangThai = "da_phuc_vu";
                    await _queue.CapNhatTrangThaiHangDoiAsync(
                        hangDoi.MaHangDoi,
                        new QueueStatusUpdateRequest { TrangThai = "da_phuc_vu" });
                }

                await CapNhatTrangThaiPhieuKhamAsync(
                    phieu.MaPhieuKham,
                    new ClinicalExamStatusUpdateRequest { TrangThai = "da_hoan_tat" });

                phieu.BenhNhan.TrangThaiHomNay = null; // Hoàn tất, không cần trạng thái

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                var dto = await LayPhieuKhamAsync(maPhieuKham);
                await _realtime.BroadcastClinicalExamUpdatedAsync(dto);

                var dashboard = await _dashboard.LayDashboardHomNayAsync();
                await _realtime.BroadcastDashboardTodayAsync(dashboard);

                return dto;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<bool> CheckClsPendingAsync(PhieuKhamLamSang phieu)
        {
            // Nếu không có phiếu CLS → không cần check
            if (phieu.PhieuKhamCanLamSang == null) return false;

            var cls = phieu.PhieuKhamCanLamSang;
            // Chỉ pending nếu trạng thái không phải "da_hoan_tat" và không phải "da_huy"
            return cls.TrangThai != "da_hoan_tat" && cls.TrangThai != "da_huy";
        }

        private async Task<bool> CheckPrescriptionPendingAsync(PhieuKhamLamSang phieu)
        {
            var chanDoan = phieu.PhieuChanDoanCuoi;
            if (chanDoan?.MaDonThuoc == null) return false;

            var donThuoc = await _db.DonThuocs
                .FirstOrDefaultAsync(d => d.MaDonThuoc == chanDoan.MaDonThuoc);

            // Đơn thuốc chưa được phát (TrangThai != "da_phat")
            // Trạng thái: da_ke, cho_phat, da_phat
            return donThuoc != null && donThuoc.TrangThai != "da_phat";
        }

        private async Task<bool> CheckBillingPendingAsync(PhieuKhamLamSang phieu)
        {
            // Kiểm tra có hóa đơn chưa thu tiền không
            // Nếu không có hóa đơn nào cho phiếu này, coi như không cần thanh toán (đã miễn phí)
            var hoaDon = await _db.HoaDonThanhToans
                .FirstOrDefaultAsync(h => h.MaPhieuKham == phieu.MaPhieuKham);

            // Nếu không có hóa đơn → không cần thanh toán → không pending
            if (hoaDon == null) return false;

            // Hóa đơn chưa thu tiền (TrangThai != "da_thu")
            // Trạng thái: da_thu, da_huy
            return hoaDon.TrangThai != "da_thu";
        }

        // ================== 5. SEARCH + PAGING ==================

        public async Task<PagedResult<ClinicalExamDto>> TimKiemPhieuKhamAsync(
            string? maBenhNhan,
            string? maBacSi,
            DateTime? fromDate,
            DateTime? toDate,
            string? trangThai,
            int page,
            int pageSize)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 20 : pageSize;

            var query = _db.PhieuKhamLamSangs
                .AsNoTracking()
                .Include(p => p.BenhNhan)
                .Include(p => p.DichVuKham)
                .Include(p => p.BacSiKham)
                .Include(p => p.NguoiLap)
                .Include(p => p.LichHenKham)
                .Include(p => p.PhieuTongHopKetQua)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(maBenhNhan))
                query = query.Where(p => p.MaBenhNhan == maBenhNhan);

            if (!string.IsNullOrWhiteSpace(maBacSi))
                query = query.Where(p => p.MaBacSiKham == maBacSi);

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(p => p.NgayLap >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(p => p.NgayLap < to);
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(p => p.TrangThai == trangThai);

            query = query.OrderByDescending(p => p.NgayLap)
                         .ThenByDescending(p => p.GioLap);

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = items.Select(MapClinicalExam).ToList();

            return new PagedResult<ClinicalExamDto>
            {
                Items = dtos,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }
        private async Task TaoThongBaoPhieuKhamMoiAsync(ClinicalExamDto phieu)
        {
            if (phieu == null) return;

            var title = "Phiếu khám lâm sàng mới";
            var body =
                $"Phiếu khám mới cho bệnh nhân {phieu.HoTen} (Mã phiếu: {phieu.MaPhieuKham}).";

            var request = new NotificationCreateRequest
            {
                LoaiThongBao = "phieu_kham",
                TieuDe = title,
                NoiDung = body,
                MucDoUuTien = "normal",

                // Map vào nguồn liên quan, NotificationService đang hỗ trợ "phieu_kham"
                NguonLienQuan = "phieu_kham",
                MaDoiTuongLienQuan = phieu.MaPhieuKham,

                // Gửi cho toàn bộ nhân sự y tế (bác sĩ + y tá)
                NguoiNhan = new List<NotificationRecipientCreateRequest>
                {
                    new NotificationRecipientCreateRequest
                    {
                        LoaiNguoiNhan =  "bac_si",
                        MaNguoiNhan = phieu.MaBacSiKham
                    }
                }
            };

            await _notifications.TaoThongBaoAsync(request);
        }
        private async Task TaoThongBaoPhieuChuanDoanAsync(
    FinalDiagnosisDto chanDoan,
    PhieuKhamLamSang phieu
)
        {
            if (chanDoan == null) return;
            if (phieu == null) return;
            if (string.IsNullOrWhiteSpace(phieu.MaBenhNhan)) return;

            // Lấy tên bệnh nhân nếu có
            var tenBenhNhan = phieu.BenhNhan?.HoTen;
            if (string.IsNullOrWhiteSpace(tenBenhNhan))
            {
                tenBenhNhan = await _db.BenhNhans
                    .Where(b => b.MaBenhNhan == phieu.MaBenhNhan)
                    .Select(b => b.HoTen)
                    .FirstOrDefaultAsync() ?? "bệnh nhân";
            }

            var title = "Kết quả khám & chẩn đoán cuối";
            var body =
                $"Kết quả khám và chẩn đoán cuối của bệnh nhân ({tenBenhNhan}) " +
                $"cho phiếu khám {phieu.MaPhieuKham} đã được cập nhật. ";

            var request = new NotificationCreateRequest
            {
                LoaiThongBao = "phieu_chan_doan",   // loại thông báo để FE lọc
                TieuDe = title,
                NoiDung = body,
                MucDoUuTien = "normal",

                // Gắn nguồn liên quan về phiếu khám để sau này có thể deep-link
                NguonLienQuan = "phieu_kham",
                MaDoiTuongLienQuan = phieu.MaPhieuKham,

             
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

       

    }
}
