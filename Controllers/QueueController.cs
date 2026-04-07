using System.Collections.Generic;
using System.Threading.Tasks;
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
    [Route("api/queue")]
    public class QueueController : ControllerBase
    {
        private readonly IQueueService _queueService;
        private readonly DataContext _db;

        public QueueController(IQueueService queueService, DataContext db)
        {
            _queueService = queueService;
            _db = db;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<QueueItemDto>> Enqueue(
            [FromBody] QueueEnqueueRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            var dto = await _queueService.ThemVaoHangDoiAsync(request);
            return Ok(dto);
        }

        [HttpGet("{maHangDoi}")]
        [Authorize]
        public async Task<ActionResult<QueueItemDto>> GetById(string maHangDoi)
        {
            var roomId = await _db.HangDois
                .AsNoTracking()
                .Where(h => h.MaHangDoi == maHangDoi)
                .Select(h => h.MaPhong)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(roomId))
                return NotFound();

            if (!await CanAccessRoomAsync(roomId))
                return Forbid();

            var dto = await _queueService.LayHangDoiAsync(maHangDoi);
            return Ok(dto);
        }

        [HttpGet("rooms/{maPhong}")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<QueueItemDto>>> GetByRoom(
            string maPhong,
            [FromQuery] string? loaiHangDoi,
            [FromQuery] string? trangThai)
        {
            if (!await CanAccessRoomAsync(maPhong))
                return Forbid();

            var list = await _queueService.LayHangDoiTheoPhongAsync(maPhong, loaiHangDoi, trangThai);
            return Ok(list);
        }

        [HttpPut("{maHangDoi}/status")]
        [Authorize]
        public async Task<ActionResult<QueueItemDto>> UpdateStatus(
            string maHangDoi,
            [FromBody] QueueStatusUpdateRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            if (!CanOperateQueue())
                return Forbid();

            var roomId = await _db.HangDois
                .AsNoTracking()
                .Where(h => h.MaHangDoi == maHangDoi)
                .Select(h => h.MaPhong)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(roomId))
                return NotFound();

            if (!await CanAccessRoomAsync(roomId))
                return Forbid();

            var dto = await _queueService.CapNhatTrangThaiHangDoiAsync(maHangDoi, request);
            if (dto is null)
                return NotFound();

            return Ok(dto);
        }

        [HttpPost("rooms/{maPhong}/next")]
        [Authorize]
        public async Task<ActionResult<QueueItemDto>> DequeueNext(
            string maPhong,
            [FromQuery] string? loaiHangDoi)
        {
            if (!CanOperateQueue())
                return Forbid();

            if (!await CanAccessRoomAsync(maPhong))
                return Forbid();

            var dto = await _queueService.LayTiepTheoTrongPhongAsync(maPhong, loaiHangDoi);
            if (dto is null)
                return NotFound();

            return Ok(dto);
        }

        [HttpPost("search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<QueueItemDto>>> Search(
            [FromBody] QueueSearchFilter filter)
        {
            if (filter == null)
                return BadRequest("Filter is required");

            var scope = User.GetUserScope();
            filter.Vaitro = scope.VaiTroRaw;
            filter.MaNhanSu = scope.MaNhanSu;

            if (!scope.IsGlobal)
            {
                if (string.IsNullOrWhiteSpace(scope.MaNhanSu))
                    return Forbid();

                filter.MaPhong = null;
            }

            var result = await _queueService.TimKiemHangDoiAsync(filter);
            return Ok(result);
        }

        private bool CanOperateQueue()
        {
            var scope = User.GetUserScope();
            return scope.IsDoctor || scope.IsClinicalNurse || scope.IsClsNurse || scope.IsTechnician;
        }

        private async Task<bool> CanAccessRoomAsync(string maPhong)
        {
            var scope = User.GetUserScope();
            if (scope.IsGlobal)
                return true;

            var accessibleRooms = await GetAccessibleRoomsAsync(scope);
            return accessibleRooms.Contains(maPhong);
        }

        private async Task<HashSet<string>> GetAccessibleRoomsAsync(UserScopeContext scope)
        {
            var result = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(scope.MaNhanSu))
                return result;

            var nhanSu = await _db.NhanVienYTes
                .AsNoTracking()
                .Include(n => n.PhongsPhuTrach)
                .FirstOrDefaultAsync(n => n.MaNhanVien == scope.MaNhanSu);

            if (nhanSu == null)
                return result;

            if (scope.IsClinicalNurse || scope.IsClsNurse || scope.IsTechnician)
            {
                var now = System.DateTime.Now;
                var roomIds = await _db.LichTrucs
                    .AsNoTracking()
                    .Where(l =>
                        l.MaYTaTruc == scope.MaNhanSu &&
                        !l.NghiTruc &&
                        l.Ngay == now.Date &&
                        l.GioBatDau <= now.TimeOfDay &&
                        l.GioKetThuc >= now.TimeOfDay)
                    .Select(l => l.MaPhong)
                    .Distinct()
                    .ToListAsync();

                foreach (var roomId in roomIds)
                {
                    if (!string.IsNullOrWhiteSpace(roomId))
                        result.Add(roomId);
                }
            }

            if (scope.IsDoctor || scope.IsClinicalNurse || scope.IsClsNurse || scope.IsTechnician)
            {
                var fallbackRoom = nhanSu.PhongsPhuTrach?.MaPhong;
                if (!string.IsNullOrWhiteSpace(fallbackRoom))
                    result.Add(fallbackRoom);
            }

            return result;
        }
    }
}
