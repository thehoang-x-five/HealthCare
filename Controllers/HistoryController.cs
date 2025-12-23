using System.Threading.Tasks;
using HealthCare.DTOs;
using HealthCare.Services.OutpatientCare;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/history")]
    public class HistoryController(IHistoryService historyService) : ControllerBase
    {
        private readonly IHistoryService _historyService = historyService;

        /// <summary>
        /// Tìm kiếm lịch sử khám (tab Khám bệnh).
        /// </summary>
        [HttpPost("visits/search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<HistoryVisitRecordDto>>> SearchVisits(
            [FromBody] HistoryFilterRequest filter)
        {
            if (filter == null)
                return BadRequest("Filter is required");

            var result = await _historyService.LayLichSuAsync(filter);
            return Ok(result);
        }
        // POST /api/history/visits
        [HttpPost("visits")]
        public async Task<ActionResult<HistoryVisitRecordDto>> CreateVisit(
            [FromBody] HistoryVisitCreateRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required");

            try
            {
                var dto = await _historyService.TaoLuotKhamAsync(request);

                // Trả về 201 + Location trỏ tới GET detail
                return CreatedAtAction(
                    nameof(GetVisitDetail),
                    new { maLuotKham = dto.MaLuotKham },
                    dto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy chi tiết 1 lần khám (HistoryDetailModal type="visit").
        /// </summary>
        [HttpGet("visits/{maLuotKham}")]
        [Authorize]
        public async Task<ActionResult<HistoryVisitDetailDto>> GetVisitDetail(string maLuotKham)
        {
            var dto = await _historyService.LayChiTietLichSuKhamAsync(maLuotKham);
            if (dto == null) return NotFound();
            return Ok(dto);
        }
        // PUT /api/history/visits/{maLuotKham}/status
        [HttpPut("visits/{maLuotKham}/status")]
        public async Task<ActionResult<HistoryVisitRecordDto>> UpdateVisitStatus(
            string maLuotKham,
            [FromBody] HistoryVisitStatusUpdateRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required");

            var dto = await _historyService.CapNhatTrangThaiLuotKhamAsync(maLuotKham, request);
            if (dto == null)
                return NotFound();

            return Ok(dto);
        }
    }
}
