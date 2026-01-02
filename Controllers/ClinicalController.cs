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
    // ❌ REMOVED: [RequireRole("bac_si")] - Không áp dụng cho toàn controller
    // ✅ Sẽ áp dụng RequireRole riêng cho từng endpoint theo vai trò phù hợp
    public class ClinicalController(IClinicalService service) : ControllerBase
    {
        private readonly IClinicalService _service = service;

        // ✅ CHỈ Y tá hành chính có thể tạo phiếu khám (Bác sĩ KHÔNG có quyền)
        [HttpPost]
        [RequireRole("y_ta")]
        [RequireNurseType("hanhchinh")] // Chỉ Y tá hành chính
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

        // ✅ Xem phiếu khám - tất cả vai trò có thể xem
        [HttpGet("{maPhieuKham}")]
        public async Task<ActionResult<ClinicalExamDto>> LayPhieuKham(string maPhieuKham)
        {
            var result = await _service.LayPhieuKhamAsync(maPhieuKham);
            if (result is null) return NotFound();
            return Ok(result);
        }

        // ✅ Cập nhật trạng thái - CHỈ Y tá hành chính (Bác sĩ KHÔNG có quyền)
        [HttpPut("{maPhieuKham}/status")]
        [RequireRole("y_ta")]
        [RequireNurseType("hanhchinh")]
        public async Task<ActionResult<ClinicalExamDto>> CapNhatTrangThai(
            string maPhieuKham,
            [FromBody] ClinicalExamStatusUpdateRequest request)
        {
            var result = await _service.CapNhatTrangThaiPhieuKhamAsync(maPhieuKham, request);
            if (result is null) return NotFound();
            return Ok(result);
        }

        // ✅ Chẩn đoán - Bác sĩ + Y tá LS
        [HttpPost("final-diagnosis")]
        [RequireRole("bac_si", "y_ta")]
        [RequireNurseType("phong_kham")]
        public async Task<ActionResult<FinalDiagnosisDto>> TaoHoacCapNhatChanDoan(
            [FromBody] FinalDiagnosisCreateRequest request)
        {
            var result = await _service.TaoChanDoanCuoiAsync(request);
            return Ok(result);
        }

        // ✅ Xem chẩn đoán - tất cả vai trò
        [HttpGet("{maPhieuKham}/final-diagnosis")]
        public async Task<ActionResult<FinalDiagnosisDto>> LayChanDoanCuoi(string maPhieuKham)
        {
            var result = await _service.LayChanDoanCuoiAsync(maPhieuKham);
            if (result is null) return NotFound();
            return Ok(result);
        }

        // ✅ Hoàn tất khám - Bác sĩ + Y tá LS
        [HttpPost("{maPhieuKham}/complete")]
        [RequireRole("bac_si", "y_ta")]
        [RequireNurseType("phong_kham")]
        public async Task<ActionResult<ClinicalExamDto>> CompleteExam(
            string maPhieuKham,
            [FromBody] CompleteExamRequest? request = null)
        {
            try
            {
                request ??= new CompleteExamRequest();
                var result = await _service.CompleteExamAsync(maPhieuKham, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✅ Tìm kiếm - tất cả vai trò
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
