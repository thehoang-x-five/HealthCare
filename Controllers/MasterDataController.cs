using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.DTOs;
using HealthCare.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/master-data")]
    public class MasterDataController(IMasterDataService masterDataService) : ControllerBase
    {
        private readonly IMasterDataService _masterDataService = masterDataService;

        // ===== KHOA =====

        [HttpGet("departments")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> GetDepartments()
        {
            var list = await _masterDataService.LayDanhSachKhoaAsync();
            return Ok(list);
        }

        [HttpPost("departments/search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<DepartmentDto>>> SearchDepartments(
            [FromBody] DepartmentSearchFilter filter)
        {
            if (filter == null) return BadRequest("Filter is required");

            var result = await _masterDataService.TimKiemKhoaAsync(filter);
            return Ok(result);
        }

        // GET: api/masterdata/departments/overview
        [HttpGet("departments/overview")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<DepartmentOverviewDto>>> GetDepartmentsOverview(
            [FromQuery] DateTime? ngay,
            [FromQuery] TimeSpan? gio,
            [FromQuery] string? maDv)   // ⬅ thêm param mã dịch vụ
        {
            // Nếu KHÔNG truyền mã dịch vụ -> yêu cầu phải có ngày + giờ
            if (string.IsNullOrWhiteSpace(maDv))
            {
                if (ngay is null || gio is null)
                    return BadRequest("ngay and gio are required when maDv is not specified.");
            }

            // Service sẽ tự xử lý:
            // - Có maDv -> dùng ngày + giờ hiện tại
            // - Không có maDv -> dùng ngay + gio từ input
            var list = await _masterDataService.LayTongQuanKhoaAsync(ngay, gio, maDv);

            return Ok(list);
        }

        // ===== NHÂN SỰ – OVERVIEW THEO KHOA + NGÀY + GIỜ =====

        [HttpGet("staff/overview")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<StaffOverviewDto>>> GetStaffOverview(
            [FromQuery] string maKhoa,
            [FromQuery] DateTime? ngay,
            [FromQuery] TimeSpan? gio)
        {
            if (string.IsNullOrWhiteSpace(maKhoa))
                return BadRequest("maKhoa is required");

            // không ép buộc ngay/gio nữa
            // service sẽ tự:
            // - dùng ngay.Date nếu có
            // - nếu null thì dùng DateTime.Now.Date
            // - dùng gio nếu có, nếu null thì dùng DateTime.Now.TimeOfDay

            var list = await _masterDataService.LayTongQuanNhanSuAsync(maKhoa, ngay, gio);
            return Ok(list);
        }
        // GET: api/masterdata/services/overview
        [HttpGet("services/overview")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<ServiceOverviewDto>>> GetServicesOverview(
            [FromQuery] string? maPhong,
            [FromQuery] string? loaiDichVu) // ⬅ thêm param loại dịch vụ
        {
            // Không cần bắt buộc maPhong nữa:
            // - Có maPhong -> bỏ qua loaiDichVu, lọc theo phòng
            // - Không có maPhong -> service tự dùng loaiDichVu (mặc định "kham_lam_sang")
            var list = await _masterDataService.LayTongQuanDichVuAsync(maPhong, loaiDichVu);

            return Ok(list);
        }
        // ===== PHÒNG =====

        [HttpGet("rooms")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetRooms(
            [FromQuery] string? maKhoa,
            [FromQuery] string? loaiPhong)
        {
            var list = await _masterDataService.LayDanhSachPhongAsync(maKhoa, loaiPhong);
            return Ok(list);
        }

        [HttpPost("rooms/search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<RoomDto>>> SearchRooms(
            [FromBody] RoomSearchFilter filter)
        {
            if (filter == null) return BadRequest("Filter is required");

            var result = await _masterDataService.TimKiemPhongAsync(filter);
            return Ok(result);
        }
        // ===== PHÒNG – CARD & CHI TIẾT =====

        [HttpPost("rooms/cards/search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<RoomCardDto>>> SearchRoomCards(
            [FromBody] RoomSearchFilter filter)
        {
            if (filter == null) return BadRequest("Filter is required");

            var result = await _masterDataService.TimKiemPhongCardAsync(filter);
            return Ok(result);
        }

        [HttpGet("rooms/{maPhong}/detail")]
        [Authorize]
        public async Task<ActionResult<RoomDetailDto>> GetRoomDetail([FromRoute] string maPhong)
        {
            if (string.IsNullOrWhiteSpace(maPhong))
                return BadRequest("MaPhong is required");

            var detail = await _masterDataService.LayChiTietPhongAsync(maPhong);
            if (detail == null) return NotFound();

            return Ok(detail);
        }
        [HttpGet("rooms/{maPhong}/duty-week")]
        [Authorize]
        public async Task<ActionResult<RoomDutyWeekDto>> GetRoomDutyWeek(
           [FromRoute] string maPhong,
           [FromQuery] DateTime? today)
        {
            if (string.IsNullOrWhiteSpace(maPhong))
                return BadRequest("MaPhong is required");

            var result = await _masterDataService.LayLichDieuDuongPhongTuanAsync(maPhong, today);
            if (result == null) return NotFound();

            return Ok(result);
        }
        // ===== NHÂN SỰ =====

        [HttpGet("staff")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<StaffDto>>> GetStaff(
            [FromQuery] string? maKhoa,
            [FromQuery] string? vaiTro)
        {
            var list = await _masterDataService.LayDanhSachNhanSuAsync(maKhoa, vaiTro);
            return Ok(list);
        }

        [HttpPost("staff/search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<StaffDto>>> SearchStaff(
            [FromBody] StaffSearchFilter filter)
        {
            if (filter == null) return BadRequest("Filter is required");

            var result = await _masterDataService.TimKiemNhanSuAsync(filter);
            return Ok(result);
        }
        [HttpPost("staff/cards/search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<StaffCardDto>>> SearchStaffCards(
    [FromBody] StaffSearchFilter filter)
        {
            if (filter == null) return BadRequest("Filter is required");

            var result = await _masterDataService.TimKiemNhanSuCardAsync(filter);
            return Ok(result);
        }

        [HttpGet("staff/{maNhanVien}")]
        [Authorize]
        public async Task<ActionResult<StaffDetailDto>> GetStaffDetail([FromRoute] string maNhanVien)
        {
            if (string.IsNullOrWhiteSpace(maNhanVien))
                return BadRequest("MaNhanVien is required");

            var detail = await _masterDataService.LayChiTietNhanSuAsync(maNhanVien);
            if (detail == null) return NotFound();

            return Ok(detail);
        }

        [HttpGet("staff/{maNhanVien}/duty-week")]
        [Authorize]
        public async Task<ActionResult<StaffDutyWeekDto>> GetStaffDutyWeek(
            [FromRoute] string maNhanVien,
            [FromQuery] DateTime? today)
        {
            if (string.IsNullOrWhiteSpace(maNhanVien))
                return BadRequest("MaNhanVien is required");

            var result = await _masterDataService.LayLichTrucNhanSuTuanAsync(maNhanVien, today);
            if (result == null) return NotFound();

            return Ok(result);
        }

      

        // ===== LỊCH TRỰC =====

        [HttpGet("duty-schedules")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<DutyScheduleDto>>> GetDutySchedules(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? maPhong,
            [FromQuery] string? maYTaTruc)
        {
            var list = await _masterDataService.LayDanhSachLichTrucAsync(
                fromDate,
                toDate,
                maPhong,
                maYTaTruc);

            return Ok(list);
        }

        [HttpPost("duty-schedules/search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<DutyScheduleDto>>> SearchDutySchedules(
            [FromBody] DutyScheduleSearchFilter filter)
        {
            if (filter == null) return BadRequest("Filter is required");

            var result = await _masterDataService.TimKiemLichTrucAsync(filter);
            return Ok(result);
        }
        // ===== NEW: LỊCH TRỰC CHI TIẾT CHO BÁC SĨ (THEO KHOẢNG NGÀY) =====
        // GET: /api/masterdata/doctors/{maBacSi}/duty-schedules?fromDate=2025-11-29&toDate=2025-12-05
        [HttpGet("doctors/{maBacSi}/duty-schedules")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<DutyScheduleDto>>> GetDoctorDutySchedules(
            [FromRoute] string maBacSi,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            if (string.IsNullOrWhiteSpace(maBacSi))
                return BadRequest("maBacSi is required");

            var items = await _masterDataService.LayLichTrucBacSiAsync(maBacSi, fromDate, toDate);
            return Ok(items);
        }
        // ===== DỊCH VỤ Y TẾ =====

        [HttpGet("services")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetServices(
            [FromQuery] string? maKhoa,
            [FromQuery] string? maPhong,
            [FromQuery] string? loaiDichVu,
            [FromQuery] string? trangThai)
        {
            var list = await _masterDataService.LayDanhSachDichVuAsync(
                maKhoa,
                maPhong,
                loaiDichVu,
                trangThai);

            return Ok(list);
        }
      

        [HttpPost("services/search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<ServiceDto>>> SearchServices(
            [FromBody] ServiceSearchFilter filter)
        {
            if (filter == null) return BadRequest("Filter is required");

            var result = await _masterDataService.TimKiemDichVuAsync(filter);
            return Ok(result);
        }

        [HttpGet("services/info")]
        [Authorize]
        public async Task<ActionResult<ServiceDetailInfoDto>> GetServiceInfoByMaDv(
            [FromQuery] string maDv)
        {
            if (string.IsNullOrWhiteSpace(maDv))
                return BadRequest("maDv is required");

            var result = await _masterDataService.LayThongTinDichVuAsync(maDv);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}
