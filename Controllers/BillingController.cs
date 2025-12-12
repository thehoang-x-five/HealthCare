using System;
using System.Threading.Tasks;
using HealthCare.DTOs;
using HealthCare.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/billing")]
    public class BillingController(IBillingService billingService) : ControllerBase
    {
        private readonly IBillingService _billingService = billingService;

        /// <summary>
        /// Tạo hoá đơn mới.
        /// </summary>
        [HttpPost("invoices")]
        [Authorize]
        public async Task<ActionResult<InvoiceDto>> CreateInvoice(
            [FromBody] InvoiceCreateRequest request)
        {
            if (request == null)
                return BadRequest("Body is required");

            var dto = await _billingService.TaoHoaDonAsync(request);

            return CreatedAtAction(
                nameof(GetInvoiceById),
                new { maHoaDon = dto.MaHoaDon },
                dto);
        }

        /// <summary>
        /// Lấy chi tiết 1 hoá đơn.
        /// </summary>
        [HttpGet("invoices/{maHoaDon}")]
        [Authorize]
        public async Task<ActionResult<InvoiceDto>> GetInvoiceById(string maHoaDon)
        {
            var dto = await _billingService.LayHoaDonAsync(maHoaDon);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        /// <summary>
        /// Tìm kiếm / lịch sử giao dịch (bảng history).
        /// </summary>
        [HttpPost("invoices/search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<InvoiceHistoryRecordDto>>> SearchInvoices(
            [FromBody] InvoiceSearchFilter filter)
        {
            if (filter == null)
                return BadRequest("Filter is required");

            var result = await _billingService.TimKiemHoaDonAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật trạng thái hoá đơn (da_thu, da_huy...).
        /// </summary>
        [HttpPut("invoices/{maHoaDon}/status")]
        [Authorize]
        public async Task<ActionResult<InvoiceDto>> UpdateInvoiceStatus(
            string maHoaDon,
            [FromBody] InvoiceStatusUpdateRequest request)
        {
            if (request == null)
                return BadRequest("Body is required");

            var dto = await _billingService.CapNhatTrangThaiHoaDonAsync(maHoaDon, request);
            if (dto == null) return NotFound();

            return Ok(dto);
        }
    }
}
