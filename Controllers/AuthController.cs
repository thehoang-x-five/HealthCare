// AuthController.cs :contentReference[oaicite:5]{index=5}
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using HealthCare.DTOs;
using HealthCare.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/auth")]

    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;

        [HttpPost("login")]
        public async Task<ActionResult<AuthTokenResponse>> Login([FromBody] AuthLoginRequest request)
        {
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _authService.LoginAsync(request, ip);

                AppendRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthTokenResponse>> Refresh()
        {
            if (!Request.Cookies.TryGetValue("refreshToken", out var cookieRt) ||
                string.IsNullOrWhiteSpace(cookieRt))
            {
                return Unauthorized(new { message = "Không có refresh token" });
            }

            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var result = await _authService.RefreshAsync(cookieRt, ip, userId);

                AppendRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                DeleteRefreshCookie();
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Flow "quên mật khẩu" – đặt lại mật khẩu mới theo TenDangNhap.
        /// Chú ý: service nên hạn chế, ví dụ chỉ cho admin hoặc có bước xác thực khác.
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] AuthForgotPasswordRequest request)
        {
            await _authService.ForgotPasswordAsync(request);
            return Ok(new { message = "Đã đặt lại mật khẩu. Vui lòng đăng nhập lại." });
        }

        /// <summary>
        /// Đổi mật khẩu cho user đang đăng nhập.
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] AuthChangePasswordRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không xác định được người dùng hiện tại." });

            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _authService.ChangePasswordAsync(userId, request, ip);

                // FE đang chỉ toast, nên trả message đơn giản
                return Ok(new { message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] AuthLogoutRequest? request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            string? cookieRt = null;
            if (Request.Cookies.TryGetValue("refreshToken", out var c) &&
                !string.IsNullOrWhiteSpace(c))
            {
                cookieRt = c;
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _authService.LogoutAsync(userId, request?.RefreshToken ?? cookieRt, ip);

            DeleteRefreshCookie();

            return Ok(new { message = "Đã đăng xuất" });
        }

        // =============== Helper: cookie cho refresh token ===============

        private void AppendRefreshCookie(string token, DateTime expUtc)
        {
            Response.Cookies.Append("refreshToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = expUtc
            });
        }

        private void DeleteRefreshCookie()
        {
            Response.Cookies.Append("refreshToken", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(-1)
            });
        }
    }
}
