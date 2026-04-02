using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HealthCare.DTOs;
using HealthCare.Services.OutpatientCare;
using System.Threading.Tasks;

namespace HealthCare.Controllers
{
    /// <summary>
    /// SQL-based Visit History Controller (Lịch sử lượt khám).
    /// Giữ nguyên để FE history tab hoạt động ổn định.
    /// MongoDB medical-history timeline nằm ở HistoryController (route: api/patients/{id}/medical-history).
    /// </summary>
    [ApiController]
    [Route("api/history")]
    [Authorize]
    public class VisitHistoryController : ControllerBase
    {
        private readonly IHistoryService _historyService;

        public VisitHistoryController(IHistoryService historyService)
        {
            _historyService = historyService;
        }

        /// <summary>
        /// Tìm kiếm lịch sử khám (tab Khám bệnh).
        /// </summary>
        [HttpPost("visits/search")]
        public async Task<ActionResult<PagedResult<HistoryVisitRecordDto>>> SearchVisits(
            [FromBody] HistoryFilterRequest filter)
        {
            if (filter == null)
                return BadRequest("Filter is required");

            var result = await _historyService.LayLichSuAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Tạo lượt khám mới.
        /// </summary>
        [HttpPost("visits")]
        public async Task<ActionResult<HistoryVisitRecordDto>> CreateVisit(
            [FromBody] HistoryVisitCreateRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required");

            try
            {
                var dto = await _historyService.TaoLuotKhamAsync(request);

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
        /// Lấy chi tiết 1 lần khám.
        /// </summary>
        [HttpGet("visits/{maLuotKham}")]
        public async Task<ActionResult<HistoryVisitDetailDto>> GetVisitDetail(string maLuotKham)
        {
            var dto = await _historyService.LayChiTietLichSuKhamAsync(maLuotKham);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        /// <summary>
        /// Cập nhật trạng thái lượt khám.
        /// </summary>
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
