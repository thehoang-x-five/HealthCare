using System;
using System.Threading.Tasks;
using HealthCare.Attributes;
using HealthCare.DTOs;
using HealthCare.Services.MedicationBilling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/pharmacy")]
    public class PharmacyController(IPharmacyService pharmacyService) : ControllerBase
    {
        private readonly IPharmacyService _pharmacyService = pharmacyService;

        // ===== KHO THUỐC =====

        /// <summary>
        /// Tạo hoặc cập nhật thuốc trong kho.
        /// </summary>
        [HttpPost("stock")]
        [Authorize]
        public async Task<ActionResult<DrugDto>> UpsertDrug([FromBody] DrugDto dto)
        {
            if (dto == null) return BadRequest("Body is required");

            var result = await _pharmacyService.TaoHoacCapNhatThuocAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách thuốc (dropdown/autocomplete).
        /// </summary>
        [HttpGet("stock")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<DrugDto>>> GetAllDrugs()
        {
            var items = await _pharmacyService.LayDanhSachThuocAsync();
            return Ok(items);
        }

        /// <summary>
        /// Tìm kiếm kho thuốc (màn hình stock).
        /// </summary>
        [HttpPost("stock/search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<DrugDto>>> SearchStock(
            [FromBody] DrugSearchFilter filter)
        {
            if (filter == null) return BadRequest("Filter is required");

            var result = await _pharmacyService.TimKiemThuocAsync(filter);
            return Ok(result);
        }

        // ===== ĐƠN THUỐC =====

        /// <summary>
        /// Tạo đơn thuốc mới từ khám LS / nơi khác.
        /// </summary>
        [HttpPost("prescriptions")]
        [Authorize]
        [RequireRole("bac_si")]
        public async Task<ActionResult<PrescriptionDto>> CreatePrescription(
            [FromBody] PrescriptionCreateRequest request)
        {
            if (request == null) return BadRequest("Body is required");

            var dto = await _pharmacyService.TaoDonThuocAsync(request);
            return CreatedAtAction(nameof(GetPrescriptionById),
                new { maDonThuoc = dto.MaDonThuoc }, dto);
        }

        /// <summary>
        /// Lấy chi tiết đơn thuốc.
        /// </summary>
        [HttpGet("prescriptions/{maDonThuoc}")]
        [Authorize]
        public async Task<ActionResult<PrescriptionDto>> GetPrescriptionById(string maDonThuoc)
        {
            var dto = await _pharmacyService.LayDonThuocAsync(maDonThuoc);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        /// <summary>
        /// Cập nhật trạng thái đơn thuốc (da_ke, cho_phat, da_phat...).
        /// </summary>
        [HttpPut("prescriptions/{maDonThuoc}/status")]
        [Authorize]
        [RequireRole("y_ta_hanh_chinh")]
        public async Task<ActionResult<PrescriptionDto>> UpdatePrescriptionStatus(
            string maDonThuoc,
            [FromBody] PrescriptionStatusUpdateRequest request)
        {
            if (request == null) return BadRequest("Body is required");

            var dto = await _pharmacyService.CapNhatTrangThaiDonThuocAsync(maDonThuoc, request);
            if (dto == null) return NotFound();

            return Ok(dto);
        }

        /// <summary>
        /// Tìm kiếm đơn thuốc (theo BN, thời gian, trạng thái).
        /// </summary>
        [HttpGet("prescriptions")]
        [Authorize]
        public async Task<ActionResult<PagedResult<PrescriptionDto>>> SearchPrescriptions(
            [FromQuery] string? maBenhNhan,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? trangThai,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _pharmacyService.TimKiemDonThuocAsync(
                maBenhNhan, fromDate, toDate, trangThai, page, pageSize);

            return Ok(result);
        }
    }
}
