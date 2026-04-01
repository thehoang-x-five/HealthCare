using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HealthCare.Datas;
using HealthCare.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/notification-templates")]
    [Authorize]
    public class ThongBaoMauController : ControllerBase
    {
        private readonly DataContext _db;

        public ThongBaoMauController(DataContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Lấy danh sách tất cả mẫu thông báo.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ThongBaoMau>>> GetAll()
        {
            var templates = await _db.ThongBaoMaus
                .AsNoTracking()
                .OrderBy(t => t.LoaiThongBao)
                .ThenBy(t => t.TenMau)
                .ToListAsync();

            return Ok(templates);
        }

        /// <summary>
        /// Lấy mẫu thông báo theo ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ThongBaoMau>> GetById(string id)
        {
            var template = await _db.ThongBaoMaus
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.MaMau == id);

            if (template == null)
                return NotFound(new { message = "Mẫu thông báo không tồn tại" });

            return Ok(template);
        }

        /// <summary>
        /// Tạo mẫu thông báo mới.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ThongBaoMau>> Create([FromBody] ThongBaoMauCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TenMau))
                return BadRequest(new { message = "TenMau là bắt buộc" });

            if (string.IsNullOrWhiteSpace(request.LoaiThongBao))
                return BadRequest(new { message = "LoaiThongBao là bắt buộc" });

            if (string.IsNullOrWhiteSpace(request.NoiDungMau))
                return BadRequest(new { message = "NoiDungMau là bắt buộc" });

            var maMau = System.Guid.NewGuid().ToString("N");

            var entity = new ThongBaoMau
            {
                MaMau = maMau,
                TenMau = request.TenMau,
                LoaiThongBao = request.LoaiThongBao,
                NoiDungMau = request.NoiDungMau,
                MucDoUuTien = request.MucDoUuTien ?? "normal",
                TrangThai = request.TrangThai ?? "hoat_dong"
            };

            _db.ThongBaoMaus.Add(entity);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = maMau }, entity);
        }

        /// <summary>
        /// Cập nhật mẫu thông báo.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ThongBaoMau>> Update(string id, [FromBody] ThongBaoMauUpdateRequest request)
        {
            var entity = await _db.ThongBaoMaus.FirstOrDefaultAsync(t => t.MaMau == id);

            if (entity == null)
                return NotFound(new { message = "Mẫu thông báo không tồn tại" });

            if (!string.IsNullOrWhiteSpace(request.TenMau))
                entity.TenMau = request.TenMau;

            if (!string.IsNullOrWhiteSpace(request.LoaiThongBao))
                entity.LoaiThongBao = request.LoaiThongBao;

            if (!string.IsNullOrWhiteSpace(request.NoiDungMau))
                entity.NoiDungMau = request.NoiDungMau;

            if (!string.IsNullOrWhiteSpace(request.MucDoUuTien))
                entity.MucDoUuTien = request.MucDoUuTien;

            if (!string.IsNullOrWhiteSpace(request.TrangThai))
                entity.TrangThai = request.TrangThai;

            await _db.SaveChangesAsync();

            return Ok(entity);
        }

        /// <summary>
        /// Xóa mẫu thông báo.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            var entity = await _db.ThongBaoMaus.FirstOrDefaultAsync(t => t.MaMau == id);

            if (entity == null)
                return NotFound(new { message = "Mẫu thông báo không tồn tại" });

            _db.ThongBaoMaus.Remove(entity);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }

    public class ThongBaoMauCreateRequest
    {
        public string TenMau { get; set; } = default!;
        public string LoaiThongBao { get; set; } = default!;
        public string NoiDungMau { get; set; } = default!;
        public string? MucDoUuTien { get; set; }
        public string? TrangThai { get; set; }
    }

    public class ThongBaoMauUpdateRequest
    {
        public string? TenMau { get; set; }
        public string? LoaiThongBao { get; set; }
        public string? NoiDungMau { get; set; }
        public string? MucDoUuTien { get; set; }
        public string? TrangThai { get; set; }
    }
}
