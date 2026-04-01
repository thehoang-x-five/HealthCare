using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HealthCare.Services.Report;
using HealthCare.DTOs;
using System;
using System.Threading.Tasks;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// Thống kê tần suất xuất hiện chỉ số bất thường.
        /// </summary>
        /// <param name="fromDate">Từ ngày (ISO 8601 format)</param>
        /// <param name="toDate">Đến ngày (ISO 8601 format)</param>
        /// <returns>Thống kê chỉ số bất thường</returns>
        [HttpGet("abnormal-stats")]
        public async Task<ActionResult<AbnormalStatsDto>> GetAbnormalStats(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var stats = await _analyticsService.GetAbnormalStatsAsync(fromDate, toDate);
            return Ok(stats);
        }

        /// <summary>
        /// Thống kê xu hướng bệnh tật theo mã ICD-10.
        /// </summary>
        /// <param name="fromDate">Từ ngày (ISO 8601 format)</param>
        /// <param name="toDate">Đến ngày (ISO 8601 format)</param>
        /// <param name="topN">Số lượng bệnh phổ biến nhất (mặc định 10)</param>
        /// <returns>Xu hướng bệnh tật</returns>
        [HttpGet("disease-trends")]
        public async Task<ActionResult<DiseaseTrendsDto>> GetDiseaseTrends(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int topN = 10)
        {
            if (topN <= 0 || topN > 100)
                topN = 10;

            var trends = await _analyticsService.GetDiseaseTrendsAsync(fromDate, toDate, topN);
            return Ok(trends);
        }

        /// <summary>
        /// Lấy danh sách TOP thuốc tiêu thụ nhiều nhất.
        /// </summary>
        /// <param name="fromDate">Từ ngày (ISO 8601 format)</param>
        /// <param name="toDate">Đến ngày (ISO 8601 format)</param>
        /// <param name="topN">Số lượng thuốc (mặc định 10)</param>
        /// <returns>Danh sách thuốc phổ biến</returns>
        [HttpGet("popular-drugs")]
        public async Task<ActionResult<PopularDrugsDto>> GetPopularDrugs(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int topN = 10)
        {
            if (topN <= 0 || topN > 100)
                topN = 10;

            var drugs = await _analyticsService.GetPopularDrugsAsync(fromDate, toDate, topN);
            return Ok(drugs);
        }
    }
}
