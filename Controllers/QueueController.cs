using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.DTOs;
using HealthCare.Services.OutpatientCare;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/queue")]
    public class QueueController : ControllerBase
    {
        private readonly IQueueService _queueService;

        public QueueController(IQueueService queueService)
        {
            _queueService = queueService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<QueueItemDto>> Enqueue(
            [FromBody] QueueEnqueueRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            // Lưu ý: DoUuTien trong request sẽ bị bỏ qua, BE tự tính
            var dto = await _queueService.ThemVaoHangDoiAsync(request);
            return Ok(dto);
        }

        [HttpGet("{maHangDoi}")]
        [Authorize]
        public async Task<ActionResult<QueueItemDto>> GetById(string maHangDoi)
        {
            var dto = await _queueService.LayHangDoiAsync(maHangDoi);
            if (dto is null)
                return NotFound();

            return Ok(dto);
        }

        [HttpGet("rooms/{maPhong}")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<QueueItemDto>>> GetByRoom(
            string maPhong,
            [FromQuery] string? loaiHangDoi,
            [FromQuery] string? trangThai)
        {
            var list = await _queueService.LayHangDoiTheoPhongAsync(maPhong, loaiHangDoi, trangThai);
            return Ok(list);
        }

        [HttpPut("{maHangDoi}/status")]
        [Authorize]
        public async Task<ActionResult<QueueItemDto>> UpdateStatus(
            string maHangDoi,
            [FromBody] QueueStatusUpdateRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            var dto = await _queueService.CapNhatTrangThaiHangDoiAsync(maHangDoi, request);
            if (dto is null)
                return NotFound();

            return Ok(dto);
        }

        [HttpPost("rooms/{maPhong}/next")]
        [Authorize]
        public async Task<ActionResult<QueueItemDto>> DequeueNext(
            string maPhong,
            [FromQuery] string? loaiHangDoi)
        {
            var dto = await _queueService.LayTiepTheoTrongPhongAsync(maPhong, loaiHangDoi);
            if (dto is null)
                return NotFound();

            return Ok(dto);
        }

        [HttpPost("search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<QueueItemDto>>> Search(
            [FromBody] QueueSearchFilter filter)
        {
            if (filter == null)
                return BadRequest("Filter is required");

            var result = await _queueService.TimKiemHangDoiAsync(filter);
            return Ok(result);
        }
    }
}
