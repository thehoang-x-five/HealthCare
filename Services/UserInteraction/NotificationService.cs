using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Entities;
using HealthCare.RenderID;
using Microsoft.EntityFrameworkCore;
using HealthCare.Realtime;
namespace HealthCare.Services.UserInteraction
{
    public class NotificationService(DataContext db, IRealtimeService realtime) : INotificationService
    {
        private readonly DataContext _db = db;
        private readonly IRealtimeService _realtime = realtime;
        // ========================
        // 1. Tạo thông báo
        // ========================
        public async Task<NotificationDto> TaoThongBaoAsync(NotificationCreateRequest request)
        {
            var now = DateTime.Now;

            var entity = new ThongBaoHeThong
            {
                MaThongBao = GeneratorID.NewThongBaoId(),
                TieuDe = request.TieuDe,
                NoiDung = request.NoiDung,
                LoaiThongBao = request.LoaiThongBao,
                DoUuTien = request.MucDoUuTien,
                TrangThai = "da_gui",
                ThoiGianGui = now
            };

            // Map NguonLienQuan -> các FK cụ thể nếu phù hợp
            if (!string.IsNullOrWhiteSpace(request.NguonLienQuan))
            {
                switch (request.NguonLienQuan)
                {
                    case "luot_kham":
                        entity.MaLuotKham = request.MaDoiTuongLienQuan;
                        break;
                    case "phieu_kham":
                        entity.MaPhieuKham = request.MaDoiTuongLienQuan;
                        break;
                    default:
                        // các loại khác hiện chưa có cột riêng -> bỏ qua
                        break;
                }
            }

            _db.ThongBaoHeThongs.Add(entity);

            // Tạo bản ghi người nhận
            var recipients = new List<ThongBaoNguoiNhan>();

            foreach (var r in request.NguoiNhan)
            {
                var loaiRaw = r.LoaiNguoiNhan ?? string.Empty;
                var loai = loaiRaw.Trim().ToLowerInvariant();

                // Chuẩn hoá alias:
                //  - "staff", "nhan_su" -> "nhan_vien_y_te"
                if (loai is "staff" or "nhan_su")
                    loai = "nhan_vien_y_te";

                var rec = new ThongBaoNguoiNhan
                {
                    MaThongBao = entity.MaThongBao,
                    LoaiNguoiNhan = loai,   // bac_si / y_ta / nhan_vien_y_te / benh_nhan...
                    DaDoc = false,
                    ThoiGianDoc = null
                };

                if (loai == "benh_nhan")
                {
                    if (string.IsNullOrWhiteSpace(r.MaNguoiNhan))
                        throw new ArgumentException("Thông báo bệnh nhân phải có MaNguoiNhan.");

                    rec.MaBenhNhan = r.MaNguoiNhan;
                    rec.MaNhanSu = null;
                }
                else
                {
                    // Cho phép gửi broadcast theo vai trò nếu không truyền mã người nhận cụ thể.
                    rec.MaNhanSu = string.IsNullOrWhiteSpace(r.MaNguoiNhan)
                        ? null
                        : r.MaNguoiNhan;
                    rec.MaBenhNhan = null;
                }

                recipients.Add(rec);
            }


            if (recipients.Count > 0)
            {
                _db.ThongBaoNguoiNhans.AddRange(recipients);
            }

            await _db.SaveChangesAsync();

            // Realtime: bắn theo từng recipient để bell/inbox nhận đúng phạm vi,
            // tránh trường hợp nhiều recipient nhưng bị fallback broadcast quá rộng.
            if (recipients.Count > 0)
            {
                var realtimeTasks = recipients
                    .Select(rec => _realtime.BroadcastNotificationCreatedAsync(
                        BuildNotificationDto(entity, rec, now)))
                    .ToList();

                await Task.WhenAll(realtimeTasks);
            }
            else
            {
                await _realtime.BroadcastNotificationCreatedAsync(
                    BuildNotificationDto(entity, null, now));
            }

            // DTO trả về không quan trọng bằng inbox/realtime; ưu tiên recipient đầu tiên nếu có.
            var dto = BuildNotificationDto(entity, recipients.FirstOrDefault(), now);
            return dto;
        }

        // ========================
        // 2. Hộp thư người nhận
        // ========================
        // ========================
        // 2. Hộp thư người nhận
        // ========================
        public async Task<PagedResult<NotificationDto>> LayThongBaoNguoiNhanAsync(NotificationFilterRequest filter)
        {
            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 50 : filter.PageSize;

            var loaiRaw = filter.LoaiNguoiNhan ?? string.Empty;
            var loai = loaiRaw.Trim().ToLowerInvariant();

            // Join header + recipient
            var query =
                from tn in _db.ThongBaoNguoiNhans.AsNoTracking()
                join tb in _db.ThongBaoHeThongs.AsNoTracking()
                    on tn.MaThongBao equals tb.MaThongBao
                select new { tb, tn };

            // -------------------------
            // 1) Phân nhánh BN / Nhân sự
            // -------------------------

            if (loai == "benh_nhan")
            {
                // Chỉ lấy thông báo cho bệnh nhân
                query = query.Where(x => x.tn.LoaiNguoiNhan == "benh_nhan");

                if (!string.IsNullOrWhiteSpace(filter.MaNguoiNhan))
                {
                    query = query.Where(x => x.tn.MaBenhNhan == filter.MaNguoiNhan);
                }
            }
            else
            {
                // Nhân sự: lấy cả targeted (MaNhanSu == mã mình)
                // VÀ broadcast phù hợp role (MaNhanSu == null + LoaiNguoiNhan match)
                var allowedTypes = GetAllowedNotifTypes(loai, filter.LoaiYTa);

                if (!string.IsNullOrWhiteSpace(filter.MaNguoiNhan))
                {
                    query = query.Where(x =>
                        x.tn.MaNhanSu == filter.MaNguoiNhan ||
                        (x.tn.MaNhanSu == null && allowedTypes.Contains(x.tn.LoaiNguoiNhan))
                    );
                }
                else
                {
                    // Không có mã cụ thể → chỉ filter theo loại phù hợp
                    query = query.Where(x => allowedTypes.Contains(x.tn.LoaiNguoiNhan));
                }
            }

            // -------------------------
            // 2) Các filter chung
            // -------------------------

            if (filter.OnlyUnread == true)
            {
                query = query.Where(x => !x.tn.DaDoc);
            }

            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
            {
                query = query.Where(x => x.tb.TrangThai == filter.TrangThai);
            }

            if (filter.FromTime.HasValue)
            {
                query = query.Where(x => x.tb.ThoiGianGui >= filter.FromTime.Value);
            }

            if (filter.ToTime.HasValue)
            {
                query = query.Where(x => x.tb.ThoiGianGui <= filter.ToTime.Value);
            }

            // Keyword search (tìm trong tiêu đề và nội dung)
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();
                query = query.Where(x =>
                    (x.tb.TieuDe != null && x.tb.TieuDe.Contains(kw)) ||
                    (x.tb.NoiDung != null && x.tb.NoiDung.Contains(kw)));
            }

            // LoaiThongBao filter
            if (!string.IsNullOrWhiteSpace(filter.LoaiThongBao))
            {
                query = query.Where(x => x.tb.LoaiThongBao == filter.LoaiThongBao);
            }

            // MucDoUuTien filter (map từ filter.MucDoUuTien sang field DoUuTien trong DB)
            if (!string.IsNullOrWhiteSpace(filter.MucDoUuTien))
            {
                query = query.Where(x => x.tb.DoUuTien == filter.MucDoUuTien);
            }

            // -------------------------
            // 3) Sorting
            // -------------------------
            var sortBy = filter.SortBy?.Trim();
            var sortDir = filter.SortDirection?.Trim().ToLowerInvariant() ?? "desc";

            if (sortBy == "MucDoUuTien" || sortBy == "mucDoUuTien" || sortBy == "DoUuTien")
            {
                // Sort by priority: cao = 0, thuong = 1, others = 2
                if (sortDir == "asc")
                {
                    query = query.OrderBy(x => x.tb.DoUuTien == "cao" ? 0 : x.tb.DoUuTien == "thuong" ? 1 : 2)
                                 .ThenByDescending(x => x.tb.ThoiGianGui);
                }
                else
                {
                    query = query.OrderByDescending(x => x.tb.DoUuTien == "cao" ? 0 : x.tb.DoUuTien == "thuong" ? 1 : 2)
                                 .ThenByDescending(x => x.tb.ThoiGianGui);
                }
            }
            else
            {
                // Default: sort by ThoiGianGui
                if (sortDir == "asc")
                {
                    query = query.OrderBy(x => x.tb.ThoiGianGui);
                }
                else
                {
                    query = query.OrderByDescending(x => x.tb.ThoiGianGui);
                }
            }

            // -------------------------
            // 4) Phân trang + map DTO
            // -------------------------

            var total = await query.CountAsync();

            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = data
                .Select(x => BuildNotificationDto(x.tb, x.tn, x.tb.ThoiGianGui))
                .ToList();

            return new PagedResult<NotificationDto>
            {
                Items = items,
                TotalItems = total,
                Page = page,
                PageSize = pageSize
            };
        }


        // ========================
        // 3. Đánh dấu đã đọc
        // ========================
        public async Task<NotificationDto?> DanhDauDaDocAsync(
            long maTbNguoiNhan,
            string loaiNguoiNhan,
            string? maNguoiNhan,
            string? loaiYTa = null)
        {
            var rec = await _db.ThongBaoNguoiNhans
                .FirstOrDefaultAsync(tn => tn.MaTbNguoiNhan == maTbNguoiNhan);

            if (rec == null)
                return null;

            if (!CoQuyenTruyCapNguoiNhan(rec, loaiNguoiNhan, maNguoiNhan, loaiYTa))
                throw new UnauthorizedAccessException("Bạn không có quyền đánh dấu thông báo này.");

            if (!rec.DaDoc)
            {
                rec.DaDoc = true;
                rec.ThoiGianDoc = DateTime.Now;
            }

            var header = await _db.ThongBaoHeThongs
                .FirstOrDefaultAsync(tb => tb.MaThongBao == rec.MaThongBao);

            if (header == null)
                return null;

            // Nếu có ít nhất 1 người nhận đã đọc thì set trạng thái 'da_doc'
            if (header.TrangThai != "da_doc")
            {
                header.TrangThai = "da_doc";
            }

            await _db.SaveChangesAsync();

            return BuildNotificationDto(header, rec, header.ThoiGianGui);
        }

        // ========================
        // 4. Tìm kiếm thông báo (màn quản trị)
        // ========================
        public async Task<PagedResult<NotificationDto>> TimKiemThongBaoAsync(NotificationSearchFilter filter)
        {
            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 50 : filter.PageSize;

            var query = _db.ThongBaoHeThongs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.LoaiThongBao))
            {
                query = query.Where(tb => tb.LoaiThongBao == filter.LoaiThongBao);
            }

            if (!string.IsNullOrWhiteSpace(filter.MucDoUuTien))
            {
                query = query.Where(tb => tb.DoUuTien == filter.MucDoUuTien);
            }

            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
            {
                query = query.Where(tb => tb.TrangThai == filter.TrangThai);
            }

            if (filter.FromTime.HasValue)
            {
                query = query.Where(tb => tb.ThoiGianGui >= filter.FromTime.Value);
            }

            if (filter.ToTime.HasValue)
            {
                query = query.Where(tb => tb.ThoiGianGui <= filter.ToTime.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim();
                query = query.Where(tb =>
                    tb.TieuDe.Contains(keyword) ||
                    tb.NoiDung.Contains(keyword));
            }

            var total = await query.CountAsync();

            var headers = await query
                .OrderByDescending(tb => tb.ThoiGianGui)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Màn quản trị: không cần gắn theo người nhận cụ thể
            var items = headers
                .Select(tb => BuildNotificationDto(tb, null, tb.ThoiGianGui))
                .ToList();

            return new PagedResult<NotificationDto>
            {
                Items = items,
                TotalItems = total,
                Page = page,
                PageSize = pageSize
            };
        }

        // ========================
        // 5. Cập nhật trạng thái thông báo
        // ========================
        public async Task<NotificationDto?> CapNhatTrangThaiThongBaoAsync(
            string maThongBao,
            NotificationStatusUpdateRequest request)
        {
            var header = await _db.ThongBaoHeThongs
                .FirstOrDefaultAsync(tb => tb.MaThongBao == maThongBao);

            if (header == null)
                return null;

            var newStatus = request.TrangThai;

            if (!string.IsNullOrWhiteSpace(newStatus) &&
                !string.Equals(header.TrangThai, newStatus, StringComparison.OrdinalIgnoreCase))
            {
                header.TrangThai = newStatus;

                // Nếu chuyển sang "da_gui" mà chưa set thời gian gửi, set luôn
                if (newStatus == "da_gui" && header.ThoiGianGui == default)
                {
                    header.ThoiGianGui = DateTime.Now;
                }
            }

            await _db.SaveChangesAsync();

            return BuildNotificationDto(header, null, header.ThoiGianGui);
        }

        private static bool CoQuyenTruyCapNguoiNhan(
            ThongBaoNguoiNhan recipient,
            string loaiNguoiNhan,
            string? maNguoiNhan,
            string? loaiYTa = null)
        {
            var normalizedLoai = (loaiNguoiNhan ?? string.Empty).Trim().ToLowerInvariant();

            if (recipient.LoaiNguoiNhan == "benh_nhan")
            {
                return normalizedLoai == "benh_nhan"
                    && !string.IsNullOrWhiteSpace(maNguoiNhan)
                    && string.Equals(recipient.MaBenhNhan, maNguoiNhan, StringComparison.OrdinalIgnoreCase);
            }

            if (normalizedLoai == "benh_nhan")
                return false;

            if (!string.IsNullOrWhiteSpace(maNguoiNhan)
                && !string.IsNullOrWhiteSpace(recipient.MaNhanSu)
                && string.Equals(recipient.MaNhanSu, maNguoiNhan, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(recipient.MaNhanSu))
            {
                var allowedTypes = GetAllowedNotifTypes(normalizedLoai, loaiYTa);
                return allowedTypes.Contains(recipient.LoaiNguoiNhan ?? string.Empty);
            }

            return false;
        }

        // ========================
        // Helper: map entity -> DTO
        // ========================
        private static NotificationDto BuildNotificationDto(
     ThongBaoHeThong header,
     ThongBaoNguoiNhan? recipient,
     DateTime thoiGianTao)
        {
            long? maTbNguoiNhan = null;
            string loaiNguoiNhan = string.Empty;
            string maNguoiNhan = string.Empty;
            bool daDoc = false;
            DateTime? thoiGianDoc = null;

            if (recipient != null)
            {
                maTbNguoiNhan = recipient.MaTbNguoiNhan;
                loaiNguoiNhan = recipient.LoaiNguoiNhan ?? string.Empty;

                // Nếu là bệnh nhân → lấy MaBenhNhan
                if (recipient.LoaiNguoiNhan == "benh_nhan")
                {
                    maNguoiNhan = recipient.MaBenhNhan ?? string.Empty;
                }
                else
                {
                    // Các loại nhân sự: bac_si / y_ta / nhan_vien_y_te...
                    maNguoiNhan = recipient.MaNhanSu ?? string.Empty;
                }

                daDoc = recipient.DaDoc;
                thoiGianDoc = recipient.ThoiGianDoc;
            }

            return new NotificationDto
            {
                MaThongBao = header.MaThongBao,
                LoaiThongBao = header.LoaiThongBao,
                TieuDe = header.TieuDe,
                NoiDung = header.NoiDung,
                MucDoUuTien = header.DoUuTien,
                TrangThai = header.TrangThai,
                ThoiGianTao = thoiGianTao,
                ThoiGianGui = header.ThoiGianGui,

                MaTbNguoiNhan = maTbNguoiNhan,
                LoaiNguoiNhan = loaiNguoiNhan,
                MaNguoiNhan = maNguoiNhan,
                DaDoc = daDoc,
                ThoiGianDoc = thoiGianDoc
            };
        }


        /// <summary>
        /// Trả về tập LoaiNguoiNhan mà người có role [loaiNguoiNhan] được phép nhìn thấy.
        /// Dùng cho inbox query: lấy broadcast notifications phù hợp vai trò.
        /// </summary>
        private static HashSet<string> GetAllowedNotifTypes(string loaiNguoiNhan, string? loaiYTa = null)
        {
            // Tất cả nhân viên đều nhận broadcast chung
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "nhan_vien_y_te", "nhan_su", "staff"
            };

            if (loaiNguoiNhan is "bac_si")
            {
                set.Add("bac_si");
            }
            else if (loaiNguoiNhan is "y_ta")
            {
                // Tất cả y tá nhận broadcast chung "y_ta"
                set.Add("y_ta");

                // Chỉ thêm sub-type ĐÚNG của người dùng (không thêm tất cả)
                var normalizedYTa = (loaiYTa ?? "").Trim().ToLowerInvariant();
                switch (normalizedYTa)
                {
                    case "hanhchinh":
                    case "hanh_chinh":
                        set.Add("y_ta_hanh_chinh");
                        break;
                    case "cls":
                    case "can_lam_sang":
                        set.Add("y_ta_cls");
                        set.Add("y_ta_can_lam_sang");
                        break;
                    case "phong_kham":
                    case "lam_sang":
                        set.Add("y_ta_phong_kham");
                        set.Add("y_ta_lam_sang");
                        break;
                    default:
                        // Không xác định loại cụ thể → cho xem tất cả (fallback)
                        set.Add("y_ta_hanh_chinh");
                        set.Add("y_ta_phong_kham");
                        set.Add("y_ta_cls");
                        set.Add("y_ta_lam_sang");
                        set.Add("y_ta_can_lam_sang");
                        break;
                }
            }
            else if (loaiNguoiNhan is "ky_thuat_vien" or "kythuatvien" or "ktv")
            {
                set.Add("ky_thuat_vien");
                set.Add("ktv");
            }
            else if (loaiNguoiNhan is "admin" or "quan_tri_vien")
            {
                set.Add("admin");
                set.Add("quan_tri_vien");
            }

            return set;
        }

    }
}
