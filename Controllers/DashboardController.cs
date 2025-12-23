using System.Threading.Tasks;
using HealthCare.DTOs;
using HealthCare.Services.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Lấy dữ liệu Dashboard hôm nay (4 KPI + lịch hẹn sắp tới + hoạt động gần đây).
        /// </summary>
        [HttpGet("today")]
        [Authorize]
        public async Task<ActionResult<DashboardTodayDto>> GetToday()
        {
            var dto = await _dashboardService.LayDashboardHomNayAsync();
            return Ok(dto);
        }
    }
}
