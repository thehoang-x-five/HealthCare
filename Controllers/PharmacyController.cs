using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.Attributes;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Infrastructure.Security;
using HealthCare.Services.MedicationBilling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/pharmacy")]
    public class PharmacyController(IPharmacyService pharmacyService, DataContext db) : ControllerBase
    {
        private readonly IPharmacyService _pharmacyService = pharmacyService;
        private readonly DataContext _db = db;

        [HttpPost("stock")]
        [Authorize]
        [RequireRole("y_ta", "admin", "quan_tri_vien")]
        [RequireNurseType("hanhchinh")]
        public async Task<ActionResult<DrugDto>> UpsertDrug([FromBody] DrugDto dto)
        {
            if (dto == null) return BadRequest("Body is required");

            var result = await _pharmacyService.TaoHoacCapNhatThuocAsync(dto);
            return Ok(result);
        }

        [HttpGet("stock")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<DrugDto>>> GetAllDrugs()
        {
            var items = await _pharmacyService.LayDanhSachThuocAsync();
            return Ok(items);
        }

        [HttpPost("stock/search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<DrugDto>>> SearchStock(
            [FromBody] DrugSearchFilter filter)
        {
            if (filter == null) return BadRequest("Filter is required");

            var result = await _pharmacyService.TimKiemThuocAsync(filter);
            return Ok(result);
        }

        [HttpPost("prescriptions")]
        [Authorize]
        [RequireRole("bac_si")]
        public async Task<ActionResult<PrescriptionDto>> CreatePrescription(
            [FromBody] PrescriptionCreateRequest request)
        {
            if (request == null) return BadRequest("Body is required");

            var scope = User.GetUserScope();
            if (scope.IsDoctor &&
                !string.IsNullOrWhiteSpace(scope.MaNhanSu) &&
                !string.Equals(request.MaBacSiKeDon, scope.MaNhanSu, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var dto = await _pharmacyService.TaoDonThuocAsync(request);
            return CreatedAtAction(nameof(GetPrescriptionById),
                new { maDonThuoc = dto.MaDonThuoc }, dto);
        }

        [HttpGet("prescriptions/{maDonThuoc}")]
        [Authorize]
        public async Task<ActionResult<PrescriptionDto>> GetPrescriptionById(string maDonThuoc)
        {
            if (!await CanAccessPrescriptionAsync(maDonThuoc))
                return Forbid();

            var dto = await _pharmacyService.LayDonThuocAsync(maDonThuoc);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpPut("prescriptions/{maDonThuoc}/status")]
        [Authorize]
        [RequireRole("y_ta", "admin", "quan_tri_vien")]
        [RequireNurseType("hanhchinh")]
        public async Task<ActionResult<PrescriptionDto>> UpdatePrescriptionStatus(
            string maDonThuoc,
            [FromBody] PrescriptionStatusUpdateRequest request)
        {
            if (request == null) return BadRequest("Body is required");

            var dto = await _pharmacyService.CapNhatTrangThaiDonThuocAsync(maDonThuoc, request);
            if (dto == null) return NotFound();

            return Ok(dto);
        }

        [HttpGet("prescriptions")]
        [Authorize]
        public async Task<ActionResult<PagedResult<PrescriptionDto>>> SearchPrescriptions(
            [FromQuery] string? maBenhNhan,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? trangThai,
            [FromQuery] string? keyword,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var scope = User.GetUserScope();
            if (!scope.IsGlobal && string.IsNullOrWhiteSpace(scope.DepartmentScope))
                return Forbid();

            var result = await _pharmacyService.TimKiemDonThuocAsync(
                maBenhNhan,
                fromDate,
                toDate,
                trangThai,
                keyword,
                page,
                pageSize,
                scope.DepartmentScope);

            return Ok(result);
        }

        [HttpPut("prescriptions/{maDonThuoc}/cancel")]
        [Authorize]
        [RequireRole("y_ta", "bac_si", "admin", "quan_tri_vien")]
        public async Task<IActionResult> HuyDonThuoc(string maDonThuoc)
        {
            try
            {
                if (!await CanAccessPrescriptionAsync(maDonThuoc))
                    return Forbid();

                await _pharmacyService.HuyDonThuocAsync(maDonThuoc);
                return Ok(new { message = "Đã hủy đơn thuốc và hoàn kho thành công." });
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

        private async Task<bool> CanAccessPrescriptionAsync(string maDonThuoc)
        {
            var scope = User.GetUserScope();
            if (scope.IsGlobal)
                return true;
            if (string.IsNullOrWhiteSpace(scope.DepartmentScope))
                return false;

            return await _db.DonThuocs
                .AsNoTracking()
                .ApplyPrescriptionDepartmentScope(scope.DepartmentScope)
                .AnyAsync(d => d.MaDonThuoc == maDonThuoc);
        }
    }
}
