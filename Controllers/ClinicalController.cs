using System;
using System.Threading.Tasks;
using HealthCare.Attributes;
using HealthCare.DTOs;
using HealthCare.Services.OutpatientCare;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/clinical")]
    [Authorize]
    [RequireRole("bac_si")]
    public class ClinicalController(IClinicalService service) : ControllerBase
    {
        private readonly IClinicalService _service = service;

        [HttpPost]
        public async Task<ActionResult<ClinicalExamDto>> TaoPhieuKham(
            [FromBody] ClinicalExamCreateRequest request)
        {
            try
            {
                var result = await _service.TaoPhieuKhamAsync(request);
                return CreatedAtAction(nameof(LayPhieuKham), new { maPhieuKham = result.MaPhieuKham }, result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{maPhieuKham}")]
        public async Task<ActionResult<ClinicalExamDto>> LayPhieuKham(string maPhieuKham)
        {
            var result = await _service.LayPhieuKhamAsync(maPhieuKham);
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpPut("{maPhieuKham}/status")]
        public async Task<ActionResult<ClinicalExamDto>> CapNhatTrangThai(
            string maPhieuKham,
            [FromBody] ClinicalExamStatusUpdateRequest request)
        {
            var result = await _service.CapNhatTrangThaiPhieuKhamAsync(maPhieuKham, request);
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpPost("final-diagnosis")]
        public async Task<ActionResult<FinalDiagnosisDto>> TaoHoacCapNhatChanDoan(
            [FromBody] FinalDiagnosisCreateRequest request)
        {
            var result = await _service.TaoChanDoanCuoiAsync(request);
            return Ok(result);
        }

        [HttpGet("{maPhieuKham}/final-diagnosis")]
        public async Task<ActionResult<FinalDiagnosisDto>> LayChanDoanCuoi(string maPhieuKham)
        {
            var result = await _service.LayChanDoanCuoiAsync(maPhieuKham);
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<ActionResult<PagedResult<ClinicalExamDto>>> Search(
            [FromQuery] string? maBenhNhan,
            [FromQuery] string? maBacSi,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? trangThai,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _service.TimKiemPhieuKhamAsync(
                maBenhNhan, maBacSi, fromDate, toDate, trangThai, page, pageSize);

            return Ok(result);
        }
    }
}
