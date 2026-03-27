using System.Security.Claims;
using HealthCare.DTOs;
using HealthCare.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    public class AdminController(IAdminService adminService) : ControllerBase
    {
        private readonly IAdminService _adminService = adminService;

        /// <summary>Kiểm tra caller có phải admin không.</summary>
        private bool IsAdmin()
        {
            var role = User.FindFirstValue("ChucVu")
                    ?? User.FindFirstValue(ClaimTypes.Role);
            return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>GET /api/admin/users — Lấy danh sách nhân viên (paged).</summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] AdminUserFilter filter)
        {
            if (!IsAdmin()) return Forbid();
            var result = await _adminService.GetUsersAsync(filter);
            return Ok(result);
        }

        /// <summary>GET /api/admin/users/{id} — Chi tiết 1 nhân viên.</summary>
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            if (!IsAdmin()) return Forbid();
            try
            {
                var user = await _adminService.GetUserByIdAsync(id);
                return Ok(user);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>POST /api/admin/users — Tạo nhân viên mới.</summary>
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] AdminUserCreateRequest request)
        {
            if (!IsAdmin()) return Forbid();
            try
            {
                var user = await _adminService.CreateUserAsync(request);
                return CreatedAtAction(nameof(GetUser), new { id = user.MaNhanVien }, user);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>PUT /api/admin/users/{id} — Cập nhật thông tin nhân viên.</summary>
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] AdminUserUpdateRequest request)
        {
            if (!IsAdmin()) return Forbid();
            try
            {
                var user = await _adminService.UpdateUserAsync(id, request);
                return Ok(user);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>PUT /api/admin/users/{id}/status — Khóa / Mở khóa nhân viên.</summary>
        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] AdminStatusUpdateRequest request)
        {
            if (!IsAdmin()) return Forbid();
            try
            {
                await _adminService.UpdateStatusAsync(id, request);
                return Ok(new { message = "Cập nhật trạng thái thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>POST /api/admin/users/{id}/reset-password — Reset mật khẩu.</summary>
        [HttpPost("users/{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(string id, [FromBody] AdminResetPasswordRequest request)
        {
            if (!IsAdmin()) return Forbid();
            try
            {
                await _adminService.ResetPasswordAsync(id, request);
                return Ok(new { message = "Đặt lại mật khẩu thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
