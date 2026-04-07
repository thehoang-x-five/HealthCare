using System;
using System.Collections.Generic;
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
    [Route("api/cls")]
    [Authorize]
    public class ClsController(IClsService service, DataContext db) : ControllerBase
    {
        private readonly IClsService _service = service;
        private readonly DataContext _db = db;

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
            if (!await CanAccessOrderAsync(maPhieuKhamCls))
                return Forbid();

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
                if (!await CanAccessOrderAsync(maPhieuKhamCls))
                    return Forbid();

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
            var scope = User.GetUserScope();
            if (!scope.IsGlobal && string.IsNullOrWhiteSpace(scope.DepartmentScope))
                return Forbid();

            var result = await _service.TimKiemPhieuClsAsync(
                maBenhNhan,
                maBacSi,
                fromDate,
                toDate,
                trangThai,
                page,
                pageSize,
                GetOriginDepartmentScope(scope),
                GetServiceDepartmentScope(scope));

            return Ok(result);
        }

        [HttpPost("items")]
        [RequireRole("bac_si", "y_ta")]
        [RequireNurseType("phong_kham")]
        public async Task<ActionResult<ClsItemDto>> TaoChiTietDichVu(
            [FromBody] ClsItemCreateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.MaPhieuKhamCls))
                return BadRequest("MaPhieuKhamCls là bắt buộc");
            if (!await CanAccessOrderAsync(request.MaPhieuKhamCls))
                return Forbid();

            var result = await _service.TaoChiTietDichVuAsync(request);
            return Ok(result);
        }

        [HttpGet("orders/{maPhieuKhamCls}/items")]
        public async Task<ActionResult<IReadOnlyList<ClsItemDto>>> LayDanhSachDichVu(
            string maPhieuKhamCls)
        {
            if (!await CanAccessOrderAsync(maPhieuKhamCls))
                return Forbid();

            var result = await _service.LayDanhSachDichVuClsAsync(maPhieuKhamCls);
            return Ok(result);
        }

        [HttpPost("results")]
        [RequireRole("ky_thuat_vien", "y_ta")]
        [RequireNurseType("can_lam_sang")]
        public async Task<ActionResult<ClsResultDto>> TaoKetQua(
            [FromBody] ClsResultCreateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.MaChiTietDv))
                return BadRequest("MaChiTietDv là bắt buộc");
            if (!await CanAccessItemAsync(request.MaChiTietDv))
                return Forbid();

            var result = await _service.TaoKetQuaClsAsync(request);
            return Ok(result);
        }

        [HttpGet("orders/{maPhieuKhamCls}/results")]
        public async Task<ActionResult<IReadOnlyList<ClsResultDto>>> LayKetQuaTheoPhieu(
            string maPhieuKhamCls)
        {
            if (!await CanAccessOrderAsync(maPhieuKhamCls))
                return Forbid();

            var result = await _service.LayKetQuaTheoPhieuClsAsync(maPhieuKhamCls);
            return Ok(result);
        }

        [HttpPost("summary/{maPhieuKhamCls}")]
        [RequireRole("ky_thuat_vien", "y_ta")]
        [RequireNurseType("can_lam_sang")]
        public async Task<ActionResult<ClsSummaryDto>> TaoTongHop(string maPhieuKhamCls)
        {
            if (!await CanAccessOrderAsync(maPhieuKhamCls))
                return Forbid();

            var result = await _service.TaoTongHopAsync(maPhieuKhamCls);
            return Ok(result);
        }

        [HttpGet("summary/{maPhieuTongHop}")]
        public async Task<ActionResult<ClsSummaryDto>> LayTongHop(string maPhieuTongHop)
        {
            if (!await CanAccessSummaryAsync(maPhieuTongHop))
                return Forbid();

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
            var scope = User.GetUserScope();
            if (!scope.IsGlobal && string.IsNullOrWhiteSpace(scope.DepartmentScope))
                return Forbid();

            var filter = new ClsSummaryFilter
            {
                MaBenhNhan = maBenhNhan,
                FromDate = fromDate,
                ToDate = toDate,
                TrangThai = trangThai,
                Page = page,
                PageSize = pageSize
            };

            var result = await _service.LayTongHopKetQuaChoLapPhieuKhamAsync(
                filter,
                GetOriginDepartmentScope(scope),
                GetServiceDepartmentScope(scope));

            return Ok(result);
        }

        [HttpPut("summary/{maPhieuTongHop}/status")]
        public async Task<ActionResult<ClsSummaryDto>> CapNhatTrangThaiSummary(
            string maPhieuTongHop,
            [FromBody] ClsSummaryStatusUpdateRequest request)
        {
            if (!await CanAccessSummaryAsync(maPhieuTongHop))
                return Forbid();

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
            if (!await CanAccessSummaryAsync(maPhieuTongHop))
                return Forbid();

            var result = await _service.CapNhatPhieuTongHopAsync(maPhieuTongHop, request);
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpPut("orders/{maPhieuKhamCls}/cancel")]
        [RequireRole("bac_si", "y_ta")]
        [RequireNurseType("phong_kham")]
        public async Task<IActionResult> HuyPhieuCls(string maPhieuKhamCls)
        {
            try
            {
                if (!await CanAccessOrderAsync(maPhieuKhamCls))
                    return Forbid();

                await _service.HuyPhieuClsAsync(maPhieuKhamCls);
                return Ok(new { message = "Đã hủy phiếu CLS thành công." });
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

        private static string? GetOriginDepartmentScope(UserScopeContext scope)
        {
            return !scope.IsGlobal && scope.IsClinicalCareRole ? scope.DepartmentScope : null;
        }

        private static string? GetServiceDepartmentScope(UserScopeContext scope)
        {
            return !scope.IsGlobal && scope.IsClsRole ? scope.DepartmentScope : null;
        }

        private async Task<bool> CanAccessOrderAsync(string maPhieuKhamCls)
        {
            var scope = User.GetUserScope();
            if (scope.IsGlobal)
                return true;
            if (string.IsNullOrWhiteSpace(scope.DepartmentScope))
                return false;

            var baseQuery = _db.PhieuKhamCanLamSangs.AsNoTracking();
            if (scope.IsClinicalCareRole)
            {
                return await baseQuery
                    .ApplyClsOriginDepartmentScope(scope.DepartmentScope)
                    .AnyAsync(p => p.MaPhieuKhamCls == maPhieuKhamCls);
            }

            if (scope.IsClsRole)
            {
                return await baseQuery
                    .ApplyClsServiceDepartmentScope(scope.DepartmentScope)
                    .AnyAsync(p => p.MaPhieuKhamCls == maPhieuKhamCls);
            }

            return false;
        }

        private async Task<bool> CanAccessSummaryAsync(string maPhieuTongHop)
        {
            var scope = User.GetUserScope();
            if (scope.IsGlobal)
                return true;
            if (string.IsNullOrWhiteSpace(scope.DepartmentScope))
                return false;

            var baseQuery = _db.PhieuTongHopKetQuas.AsNoTracking();
            if (scope.IsClinicalCareRole)
            {
                return await baseQuery
                    .ApplyClsSummaryOriginDepartmentScope(scope.DepartmentScope)
                    .AnyAsync(p => p.MaPhieuTongHop == maPhieuTongHop);
            }

            if (scope.IsClsRole)
            {
                return await baseQuery
                    .ApplyClsSummaryServiceDepartmentScope(scope.DepartmentScope)
                    .AnyAsync(p => p.MaPhieuTongHop == maPhieuTongHop);
            }

            return false;
        }

        private async Task<bool> CanAccessItemAsync(string maChiTietDv)
        {
            var scope = User.GetUserScope();
            if (scope.IsGlobal)
                return true;
            if (string.IsNullOrWhiteSpace(scope.DepartmentScope))
                return false;

            if (scope.IsClsRole)
            {
                return await _db.ChiTietDichVus
                    .AsNoTracking()
                    .AnyAsync(ct =>
                        ct.MaChiTietDv == maChiTietDv &&
                        ct.DichVuYTe.PhongThucHien.MaKhoa == scope.DepartmentScope);
            }

            if (scope.IsClinicalCareRole)
            {
                return await _db.ChiTietDichVus
                    .AsNoTracking()
                    .AnyAsync(ct =>
                        ct.MaChiTietDv == maChiTietDv &&
                        (
                            ct.PhieuKhamCanLamSang.PhieuKhamLamSang.BacSiKham.MaKhoa == scope.DepartmentScope ||
                            ct.PhieuKhamCanLamSang.PhieuKhamLamSang.NguoiLap.MaKhoa == scope.DepartmentScope
                        ));
            }

            return false;
        }
    }
}
