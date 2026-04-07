using System.Security.Claims;
using System.Threading.Tasks;
using HealthCare.Attributes;
using HealthCare.DTOs;
using HealthCare.Services.UserInteraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/notification")]
    [Authorize]
    public class NotificationsController(INotificationService notifications) : ControllerBase
    {
        private readonly INotificationService _notifications = notifications;

        [HttpPost]
        [RequireRole("admin", "quan_tri_vien")]
        public async Task<ActionResult<NotificationDto>> CreateNotification(
            [FromBody] NotificationCreateRequest request)
        {
            if (request == null)
                return BadRequest("Request không hợp lệ");

            var dto = await _notifications.TaoThongBaoAsync(request);
            return Ok(dto);
        }

        [HttpGet("inbox")]
        public async Task<ActionResult<PagedResult<NotificationDto>>> GetInbox(
            [FromQuery] NotificationFilterRequest filter)
        {
            filter ??= new NotificationFilterRequest();

            var context = GetCurrentNotificationContext();
            if (string.IsNullOrWhiteSpace(context.LoaiNguoiNhan) ||
                string.IsNullOrWhiteSpace(context.MaNguoiNhan))
            {
                return Unauthorized(new { message = "Không xác định được người nhận thông báo hiện tại." });
            }

            filter.LoaiNguoiNhan = context.LoaiNguoiNhan;
            filter.MaNguoiNhan = context.MaNguoiNhan;
            filter.LoaiYTa = context.LoaiYTa;

            var result = await _notifications.LayThongBaoNguoiNhanAsync(filter);
            return Ok(result);
        }

        [HttpPut("recipient/{maTbNguoiNhan:long}/read")]
        public async Task<ActionResult<NotificationDto>> MarkAsRead(
            [FromRoute] long maTbNguoiNhan)
        {
            var context = GetCurrentNotificationContext();
            if (string.IsNullOrWhiteSpace(context.LoaiNguoiNhan))
                return Unauthorized(new { message = "Không xác định được người nhận thông báo hiện tại." });

            try
            {
                var dto = await _notifications.DanhDauDaDocAsync(
                    maTbNguoiNhan,
                    context.LoaiNguoiNhan,
                    context.MaNguoiNhan,
                    context.LoaiYTa);

                if (dto == null)
                    return NotFound();

                return Ok(dto);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpGet("search")]
        [RequireRole("admin", "quan_tri_vien")]
        public async Task<ActionResult<PagedResult<NotificationDto>>> SearchNotifications(
            [FromQuery] NotificationSearchFilter filter)
        {
            filter ??= new NotificationSearchFilter();

            var result = await _notifications.TimKiemThongBaoAsync(filter);
            return Ok(result);
        }

        [HttpPut("{maThongBao}/status")]
        [RequireRole("admin", "quan_tri_vien")]
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

        private (string LoaiNguoiNhan, string? MaNguoiNhan, string? LoaiYTa) GetCurrentNotificationContext()
        {
            var vaiTro = GetFirstClaim("VaiTro", ClaimTypes.Role)?.Trim().ToLowerInvariant() ?? string.Empty;
            var loaiYTa = GetFirstClaim("loai_y_ta");

            if (vaiTro == "benh_nhan")
            {
                return (
                    "benh_nhan",
                    GetFirstClaim("MaBenhNhan", "ma_benh_nhan", ClaimTypes.NameIdentifier, ClaimTypes.Name),
                    null
                );
            }

            return (
                vaiTro,
                GetFirstClaim(ClaimTypes.NameIdentifier, "sub", "MaNhanVien", "ma_nhan_su"),
                loaiYTa
            );
        }

        private string? GetFirstClaim(params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                var value = User.FindFirst(claimType)?.Value;
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }
    }
}
