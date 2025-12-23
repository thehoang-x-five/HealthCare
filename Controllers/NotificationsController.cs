using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.DTOs;
using HealthCare.Services.UserInteraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/notification")]
    [Authorize] // Nếu chưa dùng auth thì có thể bỏ để test
    public class NotificationsController(INotificationService notifications) : ControllerBase
    {
        private readonly INotificationService _notifications = notifications;

        // ============================================================
        // 1. TẠO THÔNG BÁO + DANH SÁCH NGƯỜI NHẬN
        // ============================================================

        /// <summary>
        /// Tạo thông báo hệ thống và gán cho danh sách người nhận.
        /// Dùng cho màn LS/CLS/Billing khi cần push thông báo.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<NotificationDto>> CreateNotification(
            [FromBody] NotificationCreateRequest request)
        {
            if (request == null)
                return BadRequest("Request không hợp lệ");

            var dto = await _notifications.TaoThongBaoAsync(request);
            return Ok(dto);
        }

        // ============================================================
        // 2. HỘP THƯ NGƯỜI NHẬN (BN / NVYT)
        // ============================================================

        /// <summary>
        /// Lấy danh sách thông báo của một người nhận
        /// (benh_nhan / nhan_vien_y_te), có paging.
        /// </summary>
        [HttpGet("inbox")]
        public async Task<ActionResult<PagedResult<NotificationDto>>> GetInbox(
            [FromQuery] NotificationFilterRequest filter)
        {
            if (filter == null)
                return BadRequest("Filter không hợp lệ");

            if (string.IsNullOrWhiteSpace(filter.LoaiNguoiNhan) ||
                string.IsNullOrWhiteSpace(filter.MaNguoiNhan))
            {
                return BadRequest("LoaiNguoiNhan và MaNguoiNhan là bắt buộc");
            }

            var result = await _notifications.LayThongBaoNguoiNhanAsync(filter);
            return Ok(result);
        }

        // ============================================================
        // 3. ĐÁNH DẤU 1 THÔNG BÁO ĐÃ ĐỌC (THEO NGƯỜI NHẬN)
        // ============================================================

        /// <summary>
        /// Đánh dấu 1 bản ghi người nhận (thong_bao_nguoi_nhan) là đã đọc.
        /// </summary>
        [HttpPut("recipient/{maTbNguoiNhan:long}/read")]
        public async Task<ActionResult<NotificationDto>> MarkAsRead(
            [FromRoute] long maTbNguoiNhan)
        {
            var dto = await _notifications.DanhDauDaDocAsync(maTbNguoiNhan);
            if (dto == null)
                return NotFound();

            return Ok(dto);
        }

        // ============================================================
        // 4. TÌM KIẾM THÔNG BÁO (MÀN QUẢN TRỊ / LOG)
        // ============================================================

        /// <summary>
        /// Tìm kiếm thông báo toàn hệ thống (màn quản trị).
        /// </summary>
        [HttpGet("search")]
        [Authorize] // thường chỉ admin dùng
        public async Task<ActionResult<PagedResult<NotificationDto>>> SearchNotifications(
            [FromQuery] NotificationSearchFilter filter)
        {
            filter ??= new NotificationSearchFilter();

            var result = await _notifications.TimKiemThongBaoAsync(filter);
            return Ok(result);
        }

        // ============================================================
        // 5. CẬP NHẬT TRẠNG THÁI HEADER THÔNG BÁO
        // ============================================================

        /// <summary>
        /// Cập nhật trạng thái thông báo hệ thống (cho_gui, da_gui, da_doc).
        /// Thường dùng nếu có cơ chế queue gửi sau.
        /// </summary>
        [HttpPut("{maThongBao}/status")]
        public async Task<ActionResult<NotificationDto>> UpdateStatus(
            [FromRoute] string maThongBao,
            [FromBody] NotificationStatusUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(maThongBao))
                return BadRequest("MaThongBao là bắt buộc");

            if (request == null)
                return BadRequest("Request không hợp lệ");

            var dto = await _notifications.CapNhatTrangThaiThongBaoAsync(maThongBao, request);
            if (dto == null)
                return NotFound();

            return Ok(dto);
        }
    }
}
