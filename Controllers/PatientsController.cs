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
    [Route("api/patient")]
    [Authorize]
    public class PatientsController(IPatientService patients) : ControllerBase
    {
        private readonly IPatientService _patients = patients;

        /// <summary>
        /// Tạo mới hoặc cập nhật bệnh nhân - CHỈ Y tá HC + Admin
        /// </summary>
        [HttpPost]
        [RequireRole("y_ta")]
        [RequireNurseType("hanhchinh")]
        public async Task<ActionResult<PatientDto>> UpsertPatient(
            [FromBody] PatientCreateUpdateRequest request)
        {
            if (request == null)
                return BadRequest("Request không hợp lệ.");

            try
            {
                var result = await _patients.TaoHoacCapNhatBenhNhanAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                // Lỗi validate đầu vào: thiếu họ tên, ngày sinh, trùng SĐT/Email, ...
                // Ví dụ: "HoTen là bắt buộc", "NgaySinh là bắt buộc", "Số điện thoại đã tồn tại..."
                // TODO: log ex
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                // Không tìm thấy bệnh nhân để cập nhật
                // TODO: log ex
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                // Lỗi khi lưu DB (đã được wrap trong service)
                // TODO: log ex
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                // Lỗi không mong muốn khác
                // TODO: log ex
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Đã xảy ra lỗi không mong muốn khi xử lý bệnh nhân.");
            }
        }



        /// <summary>
        /// Lấy chi tiết bệnh nhân, bao gồm lịch sử khám & giao dịch.
        /// </summary>
        [HttpGet("{maBenhNhan}")]
        public async Task<ActionResult<PatientDetailDto>> GetPatientDetail(
            [FromRoute] string maBenhNhan)
        {
            if (string.IsNullOrWhiteSpace(maBenhNhan))
                return BadRequest("MaBenhNhan là bắt buộc");

            var result = await _patients.LayBenhNhanAsync(maBenhNhan);
            if (result == null) return NotFound();

            return Ok(result);
        }


        /// <summary>
        /// Tìm kiếm bệnh nhân theo nhiều tiêu chí (paging).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<PatientDto>>> SearchPatients(
            [FromQuery] PatientSearchFilter filter)
        {
            // đảm bảo filter không null
            filter ??= new PatientSearchFilter();

            var result = await _patients.TimKiemBenhNhanAsync(filter);
            return Ok(result);
        }


        /// <summary>
        /// Cập nhật trạng thái trong ngày của bệnh nhân (cho_tiep_nhan, cho_kham, dang_kham, ...) - CHỈ Y tá HC + Admin
        /// </summary>
        [HttpPut("{maBenhNhan}/status")]
        [RequireRole("y_ta")]
        [RequireNurseType("hanhchinh")]
        public async Task<ActionResult<PatientDto>> UpdateDailyStatus(
            [FromRoute] string maBenhNhan,
            [FromBody] PatientStatusUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(maBenhNhan))
                return BadRequest("MaBenhNhan là bắt buộc");

            if (request == null)
                return BadRequest("Request không hợp lệ");

            var result = await _patients.CapNhatTrangThaiBenhNhanAsync(maBenhNhan, request);
            if (result == null) return NotFound();

            return Ok(result);
        }


        /// <summary>
        /// Lịch sử khám rút gọn cho 1 bệnh nhân (tab Lịch sử khám trong PatientModal).
        /// </summary>
        [HttpGet("{maBenhNhan}/visits")]
        public async Task<ActionResult<IReadOnlyList<PatientVisitSummaryDto>>> GetPatientVisits(
            [FromRoute] string maBenhNhan)
        {
            if (string.IsNullOrWhiteSpace(maBenhNhan))
                return BadRequest("MaBenhNhan là bắt buộc");

            var result = await _patients.LayLichSuKhamBenhNhanAsync(maBenhNhan);
            return Ok(result);
        }


        /// <summary>
        /// Lịch sử giao dịch rút gọn cho 1 bệnh nhân (tab Giao dịch trong PatientModal).
        /// </summary>
        [HttpGet("{maBenhNhan}/transactions")]
        public async Task<ActionResult<IReadOnlyList<PatientTransactionSummaryDto>>> GetPatientTransactions(
            [FromRoute] string maBenhNhan)
        {
            if (string.IsNullOrWhiteSpace(maBenhNhan))
                return BadRequest("MaBenhNhan là bắt buộc");

            var result = await _patients.LayLichSuGiaoDichBenhNhanAsync(maBenhNhan);
            return Ok(result);
        }
    }
}
