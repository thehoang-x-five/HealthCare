using System;
using System.Threading.Tasks;
using HealthCare.Attributes;
using HealthCare.DTOs;
using HealthCare.Services.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    [RequireRole("admin", "quan_tri_vien", "bac_si", "y_ta")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpPost("overview")]
        [Authorize]
        public async Task<ActionResult<ReportOverviewDto>> GetReportOverview([FromBody] ReportFilterRequest filter)
        {
            if (filter is null)
                return BadRequest("Filter is required");

            var vaiTro = User.FindFirst("VaiTro")?.Value?.Trim().ToLowerInvariant();
            var loaiYTa = User.FindFirst("loai_y_ta")?.Value?.Trim().ToLowerInvariant();

            var isDoctorOrAdmin =
                vaiTro == "admin" ||
                vaiTro == "quan_tri_vien" ||
                vaiTro == "bac_si";

            var isReceptionNurse =
                vaiTro == "y_ta" &&
                (
                    loaiYTa == "hanhchinh" ||
                    loaiYTa == "hanh_chinh" ||
                    loaiYTa == "y_ta_hanh_chinh" ||
                    loaiYTa == "hc"
                );

            if (!isDoctorOrAdmin && !isReceptionNurse)
                return Forbid();

            try
            {
                var dto = await _reportService.LayBaoCaoTongQuanAsync(
                    filter,
                    includeRevenue: isDoctorOrAdmin
                );
                return Ok(dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
