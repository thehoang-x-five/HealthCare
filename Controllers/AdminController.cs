using System.Security.Claims;
using HealthCare.Attributes;
using HealthCare.DTOs;
using HealthCare.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    [RequireRole("admin", "quan_tri_vien")]
    public class AdminController(IAdminService adminService) : ControllerBase
    {
        private readonly IAdminService _adminService = adminService;

        /// <summary>GET /api/admin/users — Lấy danh sách nhân viên (paged).</summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] AdminUserFilter filter)
        {
            var result = await _adminService.GetUsersAsync(filter);
            return Ok(result);
        }

        /// <summary>GET /api/admin/users/{id} — Chi tiết 1 nhân viên.</summary>
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
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

        /// <summary>PUT /api/admin/users/{id}/lock-status — Khóa / Mở khóa tài khoản đăng nhập.</summary>
        [HttpPut("users/{id}/lock-status")]
        public async Task<IActionResult> LockUnlock(string id, [FromBody] AdminAccountStatusRequest request)
        {
            try
            {
                await _adminService.LockUnlockAsync(id, request);
                return Ok(new { message = $"Cập nhật trạng thái tài khoản thành '{request.TrangThai}' thành công." });
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
    }
}
