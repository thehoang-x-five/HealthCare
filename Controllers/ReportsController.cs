using System;
using System.Threading.Tasks;
using HealthCare.DTOs;
using HealthCare.Services.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/reports")]
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

            try
            {
                var dto = await _reportService.LayBaoCaoTongQuanAsync(filter);
                return Ok(dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
