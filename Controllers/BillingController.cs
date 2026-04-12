using System;
using System.Threading.Tasks;
using HealthCare.Attributes;
using HealthCare.DTOs;
using HealthCare.Services.MedicationBilling;
using HealthCare.Services.Banking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/billing")]
    [Authorize]
    [RequireRole("y_ta", "admin", "quan_tri_vien")]
    [RequireNurseType("hanhchinh")]
    public class BillingController : ControllerBase
    {
        private readonly IBillingService _billingService;
        private readonly VietQRService _vietQRService;

        public BillingController(IBillingService billingService, VietQRService vietQRService)
        {
            _billingService = billingService;
            _vietQRService = vietQRService;
        }

        /// <summary>
        /// Tạo hoá đơn mới.
        /// </summary>
        [HttpPost("invoices")]
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

        /// <summary>
        /// Hủy hoá đơn (chỉ khi chua_thu).
        /// </summary>
        [HttpPut("invoices/{maHoaDon}/cancel")]
        [RequireRole("admin", "quan_tri_vien")]
        public async Task<IActionResult> CancelInvoice(string maHoaDon, [FromBody] CancelInvoiceRequest? request = null)
        {
            try
            {
                var dto = await _billingService.HuyHoaDonAsync(maHoaDon, request?.LyDo);
                if (dto == null) return NotFound(new { message = "Không tìm thấy hóa đơn" });
                return Ok(new { message = "Đã hủy hóa đơn thành công.", data = dto });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xác nhận thanh toán inline (PaymentWizard).
        /// Chuyển chua_thu → da_thu + cập nhật phương thức + mã giao dịch.
        /// </summary>
        [HttpPut("invoices/{maHoaDon}/confirm")]
        public async Task<IActionResult> ConfirmPayment(
            string maHoaDon,
            [FromBody] PaymentConfirmRequest request)
        {
            if (request == null)
                return BadRequest("Body is required");

            try
            {
                var dto = await _billingService.XacNhanThanhToanAsync(maHoaDon, request);
                if (dto == null) return NotFound(new { message = "Không tìm thấy hóa đơn" });
                return Ok(dto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/billing/invoices/{maHoaDon}/generate-qr
        /// Tạo mã QR VietQR cho hóa đơn (sử dụng VietQR.io API).
        /// </summary>
        [HttpPost("invoices/{maHoaDon}/generate-qr")]
        public async Task<IActionResult> GenerateQR(
            string maHoaDon,
            [FromBody] VietQRRequest? request = null)
        {
            var invoice = await _billingService.LayHoaDonAsync(maHoaDon);
            if (invoice == null)
                return NotFound(new { message = "Không tìm thấy hóa đơn" });

            var soTien = request?.SoTien > 0 ? request.SoTien : invoice.SoTien;
            var noiDung = request?.NoiDung;

            var qr = _vietQRService.GenerateQR(maHoaDon, soTien, noiDung);
            return Ok(qr);
        }
    }

    public class CancelInvoiceRequest
    {
        public string? LyDo { get; set; }
    }
}
