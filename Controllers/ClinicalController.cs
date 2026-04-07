using System;
using System.Threading.Tasks;
using HealthCare.Attributes;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Infrastructure.Security;
using HealthCare.Services.OutpatientCare;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/clinical")]
    [Authorize]
    public class ClinicalController(IClinicalService service, DataContext db) : ControllerBase
    {
        private readonly IClinicalService _service = service;
        private readonly DataContext _db = db;

        [HttpPost]
        [RequireRole("y_ta")]
        [RequireNurseType("hanhchinh")]
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
            if (!await CanAccessClinicalExamAsync(maPhieuKham))
                return Forbid();

            var result = await _service.LayPhieuKhamAsync(maPhieuKham);
            if (result is null) return NotFound();
            return Ok(result);
        }

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

        [HttpPost("final-diagnosis")]
        [RequireRole("bac_si", "y_ta")]
        [RequireNurseType("phong_kham")]
        public async Task<ActionResult<FinalDiagnosisDto>> TaoHoacCapNhatChanDoan(
            [FromBody] FinalDiagnosisCreateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.MaPhieuKham))
                return BadRequest(new { message = "MaPhieuKham là bắt buộc." });
            if (!await CanAccessClinicalExamAsync(request.MaPhieuKham))
                return Forbid();

            var result = await _service.TaoChanDoanCuoiAsync(request);
            return Ok(result);
        }

        [HttpGet("{maPhieuKham}/final-diagnosis")]
        public async Task<ActionResult<FinalDiagnosisDto>> LayChanDoanCuoi(string maPhieuKham)
        {
            if (!await CanAccessClinicalExamAsync(maPhieuKham))
                return Forbid();

            var result = await _service.LayChanDoanCuoiAsync(maPhieuKham);
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpPost("{maPhieuKham}/complete")]
        [RequireRole("bac_si", "y_ta")]
        [RequireNurseType("phong_kham")]
        public async Task<ActionResult<ClinicalExamDto>> CompleteExam(
            string maPhieuKham,
            [FromBody] CompleteExamRequest? request = null)
        {
            try
            {
                if (!await CanAccessClinicalExamAsync(maPhieuKham))
                    return Forbid();

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
            var scope = User.GetUserScope();
            if (!scope.IsGlobal && string.IsNullOrWhiteSpace(scope.DepartmentScope))
                return Forbid();

            var result = await _service.TimKiemPhieuKhamAsync(
                maBenhNhan,
                maBacSi,
                fromDate,
                toDate,
                trangThai,
                page,
                pageSize,
                scope.DepartmentScope);

            return Ok(result);
        }

        [HttpPut("visits/{maLuotKham}/cancel")]
        [RequireRole("bac_si", "y_ta")]
        [RequireNurseType("phong_kham")]
        public async Task<ActionResult> HuyLuotKham(string maLuotKham)
        {
            try
            {
                if (!await CanAccessVisitAsync(maLuotKham))
                    return Forbid();

                await _service.HuyLuotKhamAsync(maLuotKham);
                return Ok(new { message = "Đã hủy lượt khám thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task<bool> CanAccessClinicalExamAsync(string maPhieuKham)
        {
            var scope = User.GetUserScope();
            if (scope.IsGlobal)
                return true;
            if (string.IsNullOrWhiteSpace(scope.DepartmentScope))
                return false;

            return await _db.PhieuKhamLamSangs
                .AsNoTracking()
                .ApplyClinicalDepartmentScope(scope.DepartmentScope)
                .AnyAsync(p => p.MaPhieuKham == maPhieuKham);
        }

        private async Task<bool> CanAccessVisitAsync(string maLuotKham)
        {
            var scope = User.GetUserScope();
            if (scope.IsGlobal)
                return true;
            if (string.IsNullOrWhiteSpace(scope.DepartmentScope))
                return false;

            var maKhoa = scope.DepartmentScope;
            return await _db.LuotKhamBenhs
                .AsNoTracking()
                .AnyAsync(l =>
                    l.MaLuotKham == maLuotKham &&
                    (
                        l.HangDoi.Phong.MaKhoa == maKhoa ||
                        (l.MaNhanSuThucHien != null && l.NhanSuThucHien!.MaKhoa == maKhoa) ||
                        (l.MaYTaHoTro != null && l.YTaHoTro.MaKhoa == maKhoa)
                    ));
        }
    }
}
