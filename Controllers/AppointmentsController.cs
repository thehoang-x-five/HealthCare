using System;
using System.Threading.Tasks;
using HealthCare.Attributes;
using HealthCare.DTOs;
using HealthCare.Services.PatientManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        /// <summary>
        /// Tìm kiếm / phân trang lịch hẹn (dùng cho listByDate, listRange...)
        /// </summary>
        [HttpPost("search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<AppointmentReadRequestDto>>> Search(
            [FromBody] AppointmentFilterRequest filter)
        {
            filter ??= new AppointmentFilterRequest();

            try
            {
                var result = await _appointmentService.TimKiemLichHenAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Không throw ra ngoài nữa → trả JSON chuẩn để FE đọc được message
                return StatusCode(
                    500,
                    new
                    {
                        message = "Không thể tải danh sách lịch hẹn.",
                        detail = ex.Message
                    }
                );
            }
        }

        /// <summary>
        /// Lấy chi tiết lịch hẹn theo mã
        /// </summary>
        [HttpGet("{maLichHen}")]
        [Authorize]
        public async Task<ActionResult<AppointmentReadRequestDto>> GetById(string maLichHen)
        {
            try
            {
                var dto = await _appointmentService.LayLichHenAsync(maLichHen);
                return Ok(dto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Không thể lấy chi tiết lịch hẹn.",
                        detail = ex.Message
                    }
                );
            }
        }

        /// <summary>
        /// Tạo lịch hẹn mới - CHỈ Y tá HC + Admin
        /// </summary>
        [HttpPost]
        [Authorize]
        [RequireRole("y_ta")]
        [RequireNurseType("hanhchinh")]
        public async Task<ActionResult<AppointmentReadRequestDto>> Create(
            [FromBody] AppointmentCreateRequestDto request)
        {
            if (request == null)
                return BadRequest(new { message = "Request is required" });

            try
            {
                var dto = await _appointmentService.TaoLichHenAsync(request);
                // FE không dùng location nhưng CreatedAtAction cũng OK
                return CreatedAtAction(nameof(GetById), new { maLichHen = dto.MaLichHen }, dto);
            }
            catch (ArgumentException ex)
            {
                // Lỗi validate input
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Lỗi business rule: trùng SĐT / trùng giờ ...
                // FE đang đọc err.response.data.message → giữ đúng key "message"
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = ex.Message
                    }
                );
            }
        }

        /// <summary>
        /// Cập nhật trạng thái lịch hẹn (dang_cho / da_xac_nhan / da_checkin / da_huy) - CHỈ Y tá HC + Admin
        /// </summary>
        [HttpPut("{maLichHen}/status")]
        [Authorize]
        [RequireRole("y_ta")]
        [RequireNurseType("hanhchinh")]
        public async Task<ActionResult<AppointmentReadRequestDto>> UpdateStatus(
            string maLichHen,
            [FromBody] AppointmentStatusUpdateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request is required" });

            try
            {
                var dto = await _appointmentService.CapNhatTrangThaiLichHenAsync(maLichHen, request);
                if (dto is null)
                    return NotFound(new { message = $"Không tìm thấy lịch hẹn {maLichHen}" });

                return Ok(dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Ở service em đang throw InvalidOperationException khi trùng SĐT / giờ.
                // FE sẽ bắn toast err.response.data.message.
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = ex.Message
                    }
                );
            }
        }

        /// <summary>
        /// Cập nhật thông tin lịch hẹn (ngày, giờ, ghi chú...) - CHỈ Y tá HC + Admin
        /// </summary>
        [HttpPut("{maLichHen}")]
        [Authorize]
        [RequireRole("y_ta")]
        [RequireNurseType("hanhchinh")]
        public async Task<ActionResult<AppointmentReadRequestDto>> Update(
            string maLichHen,
            [FromBody] AppointmentUpdateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request is required" });

            try
            {
                var dto = await _appointmentService.CapNhatLichHenAsync(maLichHen, request);
                if (dto is null)
                    return NotFound(new { message = $"Không tìm thấy lịch hẹn {maLichHen}" });

                return Ok(dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Rule: trùng SĐT hoặc trùng giờ hẹn ở BE
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = ex.Message,
                    }
                );
            }
        }
    }
}
