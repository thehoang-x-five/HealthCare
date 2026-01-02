using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.Attributes;
using HealthCare.DTOs;
using HealthCare.Services.OutpatientCare;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/cls")]
    [Authorize]
    public class ClsController(IClsService service) : ControllerBase
    {
        private readonly IClsService _service = service;

        // ===== PHIẾU CLS =====

        [HttpPost("orders")]
        [RequireRole("bac_si", "y_ta")]
        [RequireNurseType("phong_kham")]
        public async Task<ActionResult<ClsOrderDto>> TaoPhieuCls(
            [FromBody] ClsOrderCreateRequest request)
        {
            var result = await _service.TaoPhieuClsAsync(request);
            return CreatedAtAction(nameof(LayPhieuCls), new { maPhieuKhamCls = result.MaPhieuKhamCls }, result);
        }

        [HttpGet("orders/{maPhieuKhamCls}")]
        public async Task<ActionResult<ClsOrderDto>> LayPhieuCls(string maPhieuKhamCls)
        {
            var result = await _service.LayPhieuClsAsync(maPhieuKhamCls);
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpPut("orders/{maPhieuKhamCls}/status")]
        [RequireRole("ky_thuat_vien", "y_ta")]
        [RequireNurseType("can_lam_sang")]
        public async Task<ActionResult<ClsOrderDto>> CapNhatTrangThaiPhieu(
            string maPhieuKhamCls,
            [FromBody] string trangThai)
        {
            try
            {
                var result = await _service.CapNhatTrangThaiPhieuClsAsync(maPhieuKhamCls, trangThai);
                if (result is null) return NotFound();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("orders")]
        public async Task<ActionResult<PagedResult<ClsOrderDto>>> SearchOrders(
            [FromQuery] string? maBenhNhan,
            [FromQuery] string? maBacSi,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? trangThai,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _service.TimKiemPhieuClsAsync(
                maBenhNhan, maBacSi, fromDate, toDate, trangThai, page, pageSize);
            return Ok(result);
        }

        // ===== CHI TIẾT DỊCH VỤ =====

        [HttpPost("items")]
        public async Task<ActionResult<ClsItemDto>> TaoChiTietDichVu(
            [FromBody] ClsItemCreateRequest request)
        {
            var result = await _service.TaoChiTietDichVuAsync(request);
            return Ok(result);
        }

        [HttpGet("orders/{maPhieuKhamCls}/items")]
        public async Task<ActionResult<IReadOnlyList<ClsItemDto>>> LayDanhSachDichVu(
            string maPhieuKhamCls)
        {
            var result = await _service.LayDanhSachDichVuClsAsync(maPhieuKhamCls);
            return Ok(result);
        }

        // ===== KẾT QUẢ CLS =====

        [HttpPost("results")]
        [RequireRole("ky_thuat_vien", "y_ta")]
        [RequireNurseType("can_lam_sang")]
        public async Task<ActionResult<ClsResultDto>> TaoKetQua(
            [FromBody] ClsResultCreateRequest request)
        {
            var result = await _service.TaoKetQuaClsAsync(request);
            return Ok(result);
        }

        [HttpGet("orders/{maPhieuKhamCls}/results")]
        public async Task<ActionResult<IReadOnlyList<ClsResultDto>>> LayKetQuaTheoPhieu(
            string maPhieuKhamCls)
        {
            var result = await _service.LayKetQuaTheoPhieuClsAsync(maPhieuKhamCls);
            return Ok(result);
        }

        // ===== TỔNG HỢP KQ =====

        [HttpPost("summary/{maPhieuKhamCls}")]
        [RequireRole("ky_thuat_vien", "y_ta")]
        [RequireNurseType("can_lam_sang")]
        public async Task<ActionResult<ClsSummaryDto>> TaoTongHop(string maPhieuKhamCls)
        {
            var result = await _service.TaoTongHopAsync(maPhieuKhamCls);
            return Ok(result);
        }

        [HttpGet("summary/{maPhieuTongHop}")]
        public async Task<ActionResult<ClsSummaryDto>> LayTongHop(string maPhieuTongHop)
        {
            var result = await _service.LayPhieuTongHopKetQuaAsync(maPhieuTongHop);
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpGet("summary")]
        public async Task<ActionResult<PagedResult<ClsSummaryDto>>> SearchSummary(
            [FromQuery] string maBenhNhan,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? trangThai,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var filter = new ClsSummaryFilter
            {
                MaBenhNhan = maBenhNhan,
                FromDate = fromDate,
                ToDate = toDate,
                TrangThai = trangThai,
                Page = page,
                PageSize = pageSize
            };

            var result = await _service.LayTongHopKetQuaChoLapPhieuKhamAsync(filter);
            return Ok(result);
        }

        [HttpPut("summary/{maPhieuTongHop}/status")]
        public async Task<ActionResult<ClsSummaryDto>> CapNhatTrangThaiSummary(
            string maPhieuTongHop,
            [FromBody] ClsSummaryStatusUpdateRequest request)
        {
            var result = await _service.CapNhatTrangThaiTongHopAsync(maPhieuTongHop, request);
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpPut("summary/{maPhieuTongHop}")]
        [RequireRole("ky_thuat_vien", "y_ta")]
        [RequireNurseType("can_lam_sang")]
        public async Task<ActionResult<ClsSummaryDto>> CapNhatSummary(
            string maPhieuTongHop,
            [FromBody] ClsSummaryUpdateRequest request)
        {
            var result = await _service.CapNhatPhieuTongHopAsync(maPhieuTongHop, request);
            if (result is null) return NotFound();
            return Ok(result);
        }
    }
}
