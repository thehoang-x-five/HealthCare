using System.Security.Claims;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Infrastructure.Security;
using HealthCare.Services.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly DataContext _db;

        public DashboardController(IDashboardService dashboardService, DataContext db)
        {
            _dashboardService = dashboardService;
            _db = db;
        }

        /// <summary>
        /// Lấy dữ liệu Dashboard hôm nay.
        /// ✅ RBAC: Admin/YTHC xem global. Các vai trò khác auto-scope theo MaKhoa trong JWT.
        /// FE có thể override bằng query param ?maKhoa=...
        /// </summary>
        [HttpGet("today")]
        [Authorize]
        public async Task<ActionResult<DashboardTodayDto>> GetToday([FromQuery] string? maKhoa = null)
        {
            // Đọc vai trò và MaKhoa từ JWT claims
            var vaiTro = User.FindFirst("VaiTro")?.Value
                      ?? User.FindFirst(ClaimTypes.Role)?.Value
                      ?? "";
            var jwtMaKhoa = User.FindFirst("ma_khoa")?.Value;

            // Admin và YTHC → global scope (maKhoa = null)
            var isGlobal = vaiTro == "admin"
                        || (vaiTro == "y_ta" && (User.FindFirst("loai_y_ta")?.Value ?? "").Replace("_", "").ToLower().Contains("hanhchinh"));

            if (!isGlobal && string.IsNullOrWhiteSpace(jwtMaKhoa))
                return Forbid();

            // Admin/YTHC mới được đổi scope bằng query param.
            // Các vai trò theo khoa luôn bị khóa về khoa trong JWT.
            string? effectiveScope = isGlobal ? maKhoa : jwtMaKhoa;

            var scope = User.GetUserScope();
            string? effectiveRoomScope = null;
            if (scope.IsTechnician)
            {
                effectiveRoomScope = await _db.Phongs
                    .AsNoTracking()
                    .Where(p => p.MaKTVPhuTrach == scope.MaNhanSu)
                    .Select(p => p.MaPhong)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(effectiveRoomScope))
                    return Forbid();
            }

            var dto = await _dashboardService.LayDashboardHomNayAsync(effectiveScope, effectiveRoomScope);
            return Ok(dto);
        }
    }
}
