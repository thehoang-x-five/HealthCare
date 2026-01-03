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
                    rec.MaBenhNhan = r.MaNguoiNhan;
                    rec.MaNhanSu = null;
                }
                else
                {
                    // Tất cả các loại nhân sự: bac_si, y_ta, nhan_vien_y_te...
                    rec.MaNhanSu = r.MaNguoiNhan;
                    rec.MaBenhNhan = null;
                }

                recipients.Add(rec);
            }


            if (recipients.Count > 0)
            {
                _db.ThongBaoNguoiNhans.AddRange(recipients);
            }

            await _db.SaveChangesAsync();

            // Nếu chỉ có 1 người nhận thì trả về DTO gắn theo người nhận đó
            ThongBaoNguoiNhan? firstRec = recipients.Count == 1 ? recipients[0] : null;

            var dto = BuildNotificationDto(entity, firstRec, now);

            // Realtime: bắn thông báo mới cho đúng group (benh_nhan / nhan_vien_y_te / role…)
            await _realtime.BroadcastNotificationCreatedAsync(dto);

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
                // Nhân sự (bac_si / y_ta / nhan_vien_y_te / staff / nhan_su...)
                // -> Không filter theo LoaiNguoiNhan để tránh mismatch "staff" / "nhan_vien_y_te" / "bac_si"...
                if (!string.IsNullOrWhiteSpace(filter.MaNguoiNhan))
                {
                    query = query.Where(x => x.tn.MaNhanSu == filter.MaNguoiNhan);
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
        public async Task<NotificationDto?> DanhDauDaDocAsync(long maTbNguoiNhan)
        {
            var rec = await _db.ThongBaoNguoiNhans
                .FirstOrDefaultAsync(tn => tn.MaTbNguoiNhan == maTbNguoiNhan);

            if (rec == null)
                return null;

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

    }
}
