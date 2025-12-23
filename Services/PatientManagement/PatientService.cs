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

namespace HealthCare.Services.PatientManagement
{
    public class PatientService(
     DataContext db,
     IRealtimeService realtime,
     INotificationService notifications
 ) : IPatientService
    {
        private readonly DataContext _db = db;
        private readonly IRealtimeService _realtime = realtime;
        private readonly INotificationService _notifications = notifications;

        // ============================================================
        // =   1. TẠO HOẶC CẬP NHẬT BỆNH NHÂN (CREATE / UPSERT)      =
        // ============================================================

        public async Task<PatientUpsertResultDto> TaoHoacCapNhatBenhNhanAsync(PatientCreateUpdateRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // ===== 1. VALIDATION CƠ BẢN =====
            if (string.IsNullOrWhiteSpace(request.HoTen))
                throw new ArgumentException("HoTen là bắt buộc");

            if (request.NgaySinh == default)
                throw new ArgumentException("NgaySinh là bắt buộc");

            if (request.NgaySinh.Date > DateTime.Today)
                throw new ArgumentException("NgaySinh không được lớn hơn ngày hiện tại");

            // Chuẩn hóa input để dùng lại
            var phoneNormalized = string.IsNullOrWhiteSpace(request.DienThoai)
                ? null
                : request.DienThoai.Trim();

            var emailNormalized = string.IsNullOrWhiteSpace(request.Email)
                ? null
                : request.Email.Trim();

            // ===== 2. XÁC ĐỊNH ENTITY (UPDATE THEO ID HOẶC UPSERT THEO HEURISTIC CŨ) =====
            BenhNhan? entity = null;
            var isNew = false;

            if (!string.IsNullOrWhiteSpace(request.MaBenhNhan))
            {
                // UPDATE: có MaBenhNhan
                entity = await _db.BenhNhans
                    .FirstOrDefaultAsync(b => b.MaBenhNhan == request.MaBenhNhan);

                if (entity is null)
                    throw new KeyNotFoundException("Không tìm thấy bệnh nhân để cập nhật");
            }
            else
            {
                // CREATE/UPSERT: không có MaBenhNhan -> tìm theo Họ tên + Ngày sinh + SĐT/Email
                if (!string.IsNullOrWhiteSpace(phoneNormalized))
                {
                    entity = await _db.BenhNhans.FirstOrDefaultAsync(b =>
                        b.HoTen == request.HoTen &&
                        b.NgaySinh == request.NgaySinh.Date &&
                        b.DienThoai == phoneNormalized);
                }

                if (entity is null && !string.IsNullOrWhiteSpace(emailNormalized))
                {
                    entity = await _db.BenhNhans.FirstOrDefaultAsync(b =>
                        b.HoTen == request.HoTen &&
                        b.NgaySinh == request.NgaySinh.Date &&
                        b.Email == emailNormalized);
                }

                if (entity is null)
                {
                    isNew = true;
                    entity = new BenhNhan
                    {
                        MaBenhNhan = GeneratorID.NewBenhNhanId(),
                        TrangThaiTaiKhoan = string.IsNullOrWhiteSpace(request.TrangThaiTaiKhoan)
                            ? "hoat_dong"
                            : request.TrangThaiTaiKhoan,
                        NgayTrangThai = DateTime.Today
                    };
                    _db.BenhNhans.Add(entity);
                }
            }

            var currentId = entity.MaBenhNhan;

            // ===== 3. CHECK TRÙNG SĐT / EMAIL (LOẠI CHÍNH MÌNH RA + CHỈ KHI ĐỔI GIÁ TRỊ) =====
                    if (!string.IsNullOrWhiteSpace(phoneNormalized))
                        {
                            // Tạo mới hoặc đổi sang số mới thì mới cần check
                            if (isNew || !string.Equals(entity.DienThoai, phoneNormalized, StringComparison.OrdinalIgnoreCase))
                                {
                    var phoneExists = await _db.BenhNhans
                                        .AnyAsync(b => b.MaBenhNhan != currentId && b.DienThoai == phoneNormalized);
                    
                                    if (phoneExists)
                        throw new ArgumentException("Số điện thoại đã tồn tại cho một bệnh nhân khác");
                                }
                        }
            
                    if (!string.IsNullOrWhiteSpace(emailNormalized))
                        {
                            if (isNew || !string.Equals(entity.Email, emailNormalized, StringComparison.OrdinalIgnoreCase))
                                {
                    var emailExists = await _db.BenhNhans
                                        .AnyAsync(b => b.MaBenhNhan != currentId && b.Email == emailNormalized);
                    
                                    if (emailExists)
                        throw new ArgumentException("Email đã tồn tại cho một bệnh nhân khác");
                                }
                        }

            // ===== 4. CẬP NHẬT THÔNG TIN HÀNH CHÍNH =====
            entity.HoTen = request.HoTen;
            entity.NgaySinh = request.NgaySinh.Date;
            entity.GioiTinh = request.GioiTinh ?? string.Empty;
            entity.DienThoai = phoneNormalized;
            entity.Email = emailNormalized;
            entity.DiaChi = request.DiaChi;
            // ===== 5. TRẠNG THÁI TÀI KHOẢN + TRẠNG THÁI HÔM NAY =====

            // 5.1. Apply trạng thái tài khoản nếu có truyền
            if (!string.IsNullOrWhiteSpace(request.TrangThaiTaiKhoan))
            {
                entity.TrangThaiTaiKhoan = request.TrangThaiTaiKhoan.Trim();
            }

            // 5.2. Đảm bảo luôn có default cho tài khoản
            if (string.IsNullOrWhiteSpace(entity.TrangThaiTaiKhoan))
            {
                entity.TrangThaiTaiKhoan = "hoat_dong";
            }

            var isActiveAccount = string.Equals(
                entity.TrangThaiTaiKhoan,
                "hoat_dong",
                StringComparison.OrdinalIgnoreCase);

            if (!isActiveAccount)
            {
                // ❗ Nếu tài khoản KHÔNG hoạt động (đã xóa / khóa / tạm dừng ...):
                // - Không giữ trạng thái hôm nay nữa
                // - Reset về null để FE không hiển thị gì
                entity.TrangThaiHomNay = null;
                entity.NgayTrangThai = DateTime.Today;
            }
            else
            {
                // Tài khoản đang hoạt động -> cho phép set trạng thái hôm nay nếu có truyền
                if (!string.IsNullOrWhiteSpace(request.TrangThaiHomNay))
                {
                    entity.TrangThaiHomNay = request.TrangThaiHomNay;
                    entity.NgayTrangThai = DateTime.Today;
                }

                // Nếu đã có trạng thái hôm nay mà ngày đang default -> fix lại
                if (!string.IsNullOrEmpty(entity.TrangThaiHomNay) && entity.NgayTrangThai == default)
                {
                    entity.NgayTrangThai = DateTime.Today;
                }
            }


            // ===== 6. CẬP NHẬT THÔNG TIN BỆNH ÁN =====
            entity.DiUng = request.DiUng;
            entity.ChongChiDinh = request.ChongChiDinh;
            entity.ThuocDangDung = request.ThuocDangDung;
            entity.TieuSuBenh = request.TieuSuBenh;
            entity.TienSuPhauThuat = request.TienSuPhauThuat;
            entity.NhomMau = request.NhomMau;
            entity.BenhManTinh = request.BenhManTinh;
            entity.SinhHieu = request.SinhHieu;

            // ===== 7. LƯU DB =====
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Lỗi khi lưu bệnh nhân vào cơ sở dữ liệu", ex);
            }

            var dto = MapToDto(entity);

            // ===== 8. REALTIME + THÔNG BÁO =====
            if (isNew)
            {
                await _realtime.BroadcastPatientCreatedAsync(dto);
                await TaoThongBaoBenhNhanMoiAsync(dto);
            }
            else
            {
                await _realtime.BroadcastPatientUpdatedAsync(dto);
                await TaoThongBaoCapNhatBenhNhanAsync(dto);
            }

            // ===== 9. BUILD RESULT: CREATE → chỉ cần Patient; UPDATE → thêm Detail =====
            PatientDetailDto? detail = null;
            if (!isNew)
            {
                detail = await LayBenhNhanAsync(dto.MaBenhNhan);
            }

            return new PatientUpsertResultDto
            {
                IsNew = isNew,
                Patient = dto,
                Detail = detail
            };
        }

        // ============================================================
        // =   2. LẤY THÔNG TIN CHI TIẾT BỆNH NHÂN (VIEW MODAL)      =
        // ============================================================

        /// <summary>
        /// Lấy chi tiết bệnh nhân cho PatientModal:
        /// - Thông tin hành chính + bệnh án
        /// - Trạng thái tài khoản + trong ngày
        /// - Lịch sử khám rút gọn
        /// - Lịch sử giao dịch rút gọn
        /// </summary>
        public async Task<PatientDetailDto?> LayBenhNhanAsync(string maBenhNhan)
        {
            if (string.IsNullOrWhiteSpace(maBenhNhan))
                throw new ArgumentException("maBenhNhan là bắt buộc");

            var bn = await _db.BenhNhans
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.MaBenhNhan == maBenhNhan);

            if (bn is null)
                return null;

            // Lấy lịch sử khám & giao dịch cho bệnh nhân (chạy song song)
            var visits = await LayLichSuKhamBenhNhanAsync(maBenhNhan);
            var txns = await LayLichSuGiaoDichBenhNhanAsync(maBenhNhan);

            // Chuẩn hóa trạng thái
            var trangThaiTaiKhoan = string.IsNullOrWhiteSpace(bn.TrangThaiTaiKhoan)
                ? "hoat_dong"
                : bn.TrangThaiTaiKhoan;

            var ngayTrangThai = bn.NgayTrangThai == default
                ? DateTime.Today
                : bn.NgayTrangThai;

            // Chỉ coi là trạng thái "hôm nay" nếu đúng ngày hôm nay
            //var trangThaiHomNay = ngayTrangThai.Date == DateTime.Today
            //    ? bn.TrangThaiHomNay
            //    : null;

            var detail = new PatientDetailDto
            {
                MaBenhNhan = bn.MaBenhNhan,
                HoTen = bn.HoTen,
                NgaySinh = bn.NgaySinh,
                GioiTinh = bn.GioiTinh,
                DienThoai = bn.DienThoai,
                Email = bn.Email,
                DiaChi = bn.DiaChi,

                TrangThaiTaiKhoan = trangThaiTaiKhoan,
                TrangThaiHomNay = bn.TrangThaiHomNay,
                NgayTrangThai = ngayTrangThai,

                DiUng = bn.DiUng,
                ChongChiDinh = bn.ChongChiDinh,
                ThuocDangDung = bn.ThuocDangDung,
                TieuSuBenh = bn.TieuSuBenh,
                TienSuPhauThuat = bn.TienSuPhauThuat,
                NhomMau = bn.NhomMau,
                BenhManTinh = bn.BenhManTinh,
                SinhHieu = bn.SinhHieu,

                LichSuKham = visits,
                LichSuGiaoDich = txns
            };

            return detail;
        }

        // ============================================================
        // =   3. TÌM KIẾM / DANH SÁCH BỆNH NHÂN (PAGING)            =
        // ============================================================

        public async Task<PagedResult<PatientDto>> TimKiemBenhNhanAsync(PatientSearchFilter filter)
        {
            var query = _db.BenhNhans.AsNoTracking().AsQueryable();


            if (!string.IsNullOrWhiteSpace(filter.MaBenhNhan))
            {
                query = query.Where(b => b.MaBenhNhan == filter.MaBenhNhan);
            }
            

            // PATCH: OnlyToday = true -> trạng thái hôm nay và đúng ngày hôm nay
            if (filter.OnlyToday)
            {
                var today = DateTime.Today;
                query = query.Where(b =>
                    !string.IsNullOrEmpty(b.TrangThaiHomNay) &&
                    b.NgayTrangThai == today);
            }

            if (!string.IsNullOrWhiteSpace(filter.DienThoai))
            {
                var phone = filter.DienThoai.Trim();
                query = query.Where(b => b.DienThoai == phone);
            }

            if (!string.IsNullOrWhiteSpace(filter.GioiTinh))
            {
                query = query.Where(b => b.GioiTinh == filter.GioiTinh);
            }

            if (!string.IsNullOrWhiteSpace(filter.TrangThaiTaiKhoan))
            {
                query = query.Where(b => b.TrangThaiTaiKhoan == filter.TrangThaiTaiKhoan);
            }

            if (!string.IsNullOrWhiteSpace(filter.TrangThaiHomNay))
            {
                query = query.Where(b => b.TrangThaiHomNay == filter.TrangThaiHomNay);
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();
                query = query.Where(b =>
                    b.HoTen.Contains(kw) ||
                    (b.MaBenhNhan != null && b.MaBenhNhan.Contains(kw)) ||
                    (b.Email != null && b.Email.Contains(kw)) ||
                    (b.DienThoai != null && b.DienThoai.Contains(kw)));
            }

            // Sắp xếp
            var sortBy = filter.SortBy?.ToLowerInvariant();
            var sortDir = (filter.SortDirection ?? "asc").ToLowerInvariant();

            query = (sortBy, sortDir) switch
            {
                ("ngaysinh", "desc") => query.OrderByDescending(b => b.NgaySinh),
                ("ngaysinh", _) => query.OrderBy(b => b.NgaySinh),

                ("ngaytrangthai", "desc") => query.OrderByDescending(b => b.NgayTrangThai),
                ("ngaytrangthai", _) => query.OrderBy(b => b.NgayTrangThai),

                ("hoten", "desc") => query.OrderByDescending(b => b.HoTen),
                ("hoten", _) => query.OrderBy(b => b.HoTen),

                _ when sortDir == "desc" => query.OrderByDescending(b => b.HoTen),
                _ => query.OrderBy(b => b.HoTen)
            };

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 50 : filter.PageSize;

            var total = await query.CountAsync();

            var list = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = list.Select(MapToDto).ToList();

            return new PagedResult<PatientDto>
            {
                Items = items,
                TotalItems = total,
                Page = page,
                PageSize = pageSize
            };
        }

        // ============================================================
        // =   4. CẬP NHẬT TRẠNG THÁI TRONG NGÀY CỦA BỆNH NHÂN       =
        // ============================================================

        public async Task<PatientDetailDto?> CapNhatTrangThaiBenhNhanAsync(
     string maBenhNhan,
     PatientStatusUpdateRequest request)
        {
            // PATCH: validate input rõ ràng
            if (string.IsNullOrWhiteSpace(maBenhNhan))
                throw new ArgumentException("maBenhNhan là bắt buộc", nameof(maBenhNhan));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.TrangThaiHomNay))
                throw new ArgumentException("TrangThaiHomNay là bắt buộc", nameof(request.TrangThaiHomNay));

            var entity = await _db.BenhNhans
                .FirstOrDefaultAsync(b => b.MaBenhNhan == maBenhNhan);

            if (entity is null)
                return null;

            // Chỉ cho phép cập nhật khi tài khoản đang hoạt động
            if (!string.Equals(entity.TrangThaiTaiKhoan, "hoat_dong", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Tài khoản bệnh nhân chưa được kích hoạt hoặc đã bị khóa, không thể cập nhật trạng thái trong ngày.");
            }

            entity.TrangThaiHomNay = request.TrangThaiHomNay;
            entity.NgayTrangThai = DateTime.Today;

            await _db.SaveChangesAsync();

            var dto = MapToDto(entity);

            await _realtime.BroadcastPatientStatusUpdatedAsync(dto);
            await TaoThongBaoCapNhatBenhNhanAsync(dto, laCapNhatTrangThai: true);

            // 🔥 Trả về PatientDetailDto thay vì PatientDto
            var detail = await LayBenhNhanAsync(dto.MaBenhNhan);
            return detail;
        }



        // ============================================================
        // =   5. LỊCH SỬ KHÁM BỆNH NHÂN (CHO PATIENT MODAL)         =
        // ============================================================

        /// <summary>
        /// Lịch sử khám rút gọn cho một bệnh nhân,
        /// dùng cho tab "Lịch sử khám" trong PatientModal.
        /// </summary>
        public async Task<IReadOnlyList<PatientVisitSummaryDto>> LayLichSuKhamBenhNhanAsync(
            string maBenhNhan)
        {
            if (string.IsNullOrWhiteSpace(maBenhNhan))
                throw new ArgumentException("maBenhNhan là bắt buộc");

            var q = _db.LuotKhamBenhs
                .AsNoTracking()
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
                .Where(l => l.HangDoi.MaBenhNhan == maBenhNhan);

            var list = await q
                .OrderByDescending(l => l.ThoiGianBatDau)
                .ThenByDescending(l => l.MaLuotKham)
                .Take(50) // giới hạn 50 lượt gần nhất cho modal
                .ToListAsync();

            return list.Select(MapToVisitSummary).ToList();
        }

        // ============================================================
        // =   6. LỊCH SỬ GIAO DỊCH BỆNH NHÂN (CHO PATIENT MODAL)    =
        // ============================================================

        /// <summary>
        /// Lịch sử giao dịch rút gọn cho một bệnh nhân,
        /// dùng cho tab "Giao dịch" trong PatientModal.
        /// </summary>
        public async Task<IReadOnlyList<PatientTransactionSummaryDto>> LayLichSuGiaoDichBenhNhanAsync(
            string maBenhNhan)
        {
            if (string.IsNullOrWhiteSpace(maBenhNhan))
                throw new ArgumentException("maBenhNhan là bắt buộc");

            var q = _db.HoaDonThanhToans
                .AsNoTracking()
                .Where(h => h.MaBenhNhan == maBenhNhan);

            var list = await q
                .OrderByDescending(h => h.ThoiGian)
                .ThenByDescending(h => h.MaHoaDon)
                .Take(50)
                .ToListAsync();

            return list.Select(MapToTransactionSummary).ToList();
        }

        // ============================================================
        // =                     HELPER MAPPERS                       =
        // ============================================================

        private static PatientDto MapToDto(BenhNhan b)
        {
            return new PatientDto
            {
                MaBenhNhan = b.MaBenhNhan,
                HoTen = b.HoTen,
                NgaySinh = b.NgaySinh,
                GioiTinh = b.GioiTinh,
                DienThoai = b.DienThoai,
                Email = b.Email,
                DiaChi = b.DiaChi,
                TrangThaiTaiKhoan= b.TrangThaiTaiKhoan,
                TrangThaiHomNay = b.TrangThaiHomNay,
                NgayTrangThai = b.NgayTrangThai
            };
        }

        private static PatientVisitSummaryDto MapToVisitSummary(LuotKhamBenh luot)
        {
            var h = luot.HangDoi;
            var phong = h.Phong;
            var khoa = phong.KhoaChuyenMon;
            var bs = luot.NhanSuThucHien;
            var phieuLs = h.PhieuKhamLamSang;
            var pcd = phieuLs?.PhieuChanDoanCuoi;
            var phieuCls = phieuLs?.PhieuKhamCanLamSang;

            bool laDichVu = phong.LoaiPhong == "phong_dich_vu";
            string loaiLuot = laDichVu ? "service" : "clinic";

            string? note = pcd?.ChanDoanCuoi
                           ?? pcd?.NoiDungKham
                           ?? phieuLs?.TrieuChung;

            var dept = $"{khoa.TenKhoa} / {phong.TenPhong}";

            return new PatientVisitSummaryDto
            {
                Date = luot.ThoiGianBatDau,
                Dept = dept,
                Doctor = bs?.HoTen ?? string.Empty,
                Note = note,
                Type = loaiLuot,
                By = bs?.HoTen ?? string.Empty,
                Ref = phieuLs?.MaPhieuKham ?? phieuCls?.MaPhieuKhamCls ?? luot.MaLuotKham,
                MaLuotKham = luot.MaLuotKham,
                MaPhieuKham = phieuLs?.MaPhieuKham
            };
        }

        private static PatientTransactionSummaryDto MapToTransactionSummary(HoaDonThanhToan h)
        {
            var content = string.IsNullOrWhiteSpace(h.NoiDung)
                ? $"{h.LoaiDotthu} - {h.SoTien:n0}đ"
                : h.NoiDung;

            return new PatientTransactionSummaryDto
            {
                Date = h.ThoiGian,
                Item = content,
                Amount = h.SoTien,
                Status = h.TrangThai,
                Ref = h.MaHoaDon,
                MaHoaDon = h.MaHoaDon,
                LoaiDotThu = h.LoaiDotthu,
                PhuongThucThanhToan = h.PhuongThucThanhToan
            };
        }
        private async Task TaoThongBaoBenhNhanMoiAsync(PatientDto bn)
        {
            if (bn == null) return;

            var title = "Bệnh nhân mới";
            var body =
                $"Bệnh nhân mới: {bn.HoTen} (Mã: {bn.MaBenhNhan}).";

            var request = new NotificationCreateRequest
            {
                LoaiThongBao = "benh_nhan",
                TieuDe = title,
                NoiDung = body,
                MucDoUuTien = "normal",

                // Hiện DB ThongBaoHeThong chưa có FK MaBenhNhan, nên NguonLienQuan/ MaDoiTuongLienQuan
                // chủ yếu phục vụ FE về sau (không ảnh hưởng logic hiện tại).
                NguonLienQuan = "benh_nhan",
                MaDoiTuongLienQuan = bn.MaBenhNhan,

                // Gửi broadcast cho toàn bộ nhân sự y tế
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

        private async Task TaoThongBaoCapNhatBenhNhanAsync(
            PatientDto bn,
            bool laCapNhatTrangThai = false)
        {
            if (bn == null) return;

            var title = laCapNhatTrangThai
                ? "Cập nhật trạng thái bệnh nhân"
                : "Cập nhật thông tin bệnh nhân";

            var body = laCapNhatTrangThai
                ? $"Trạng thái trong ngày của bệnh nhân {bn.HoTen} (Mã: {bn.MaBenhNhan}) đã được cập nhật."
                : $"Thông tin bệnh nhân {bn.HoTen} (Mã: {bn.MaBenhNhan}) đã được cập nhật.";

            var request = new NotificationCreateRequest
            {
                LoaiThongBao = "benh_nhan",
                TieuDe = title,
                NoiDung = body,
                MucDoUuTien = "normal",
                NguonLienQuan = "benh_nhan",
                MaDoiTuongLienQuan = bn.MaBenhNhan,
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

    }
}
