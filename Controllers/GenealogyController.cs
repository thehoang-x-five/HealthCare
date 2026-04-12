using HealthCare.DTOs;
using HealthCare.Services.PatientManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/patients")]
    [Authorize]
    public class GenealogyController : ControllerBase
    {
        private readonly IGenealogyService _genealogy;

        public GenealogyController(IGenealogyService genealogy)
        {
            _genealogy = genealogy;
        }

        /// <summary>
        /// Lấy cây pha hệ đa đời của bệnh nhân (Recursive CTE)
        /// </summary>
        [HttpGet("{maBenhNhan}/genealogy")]
        public async Task<ActionResult<GenealogyTreeDto>> GetGenealogyTree(
            [FromRoute] string maBenhNhan)
        {
            if (string.IsNullOrWhiteSpace(maBenhNhan))
                return BadRequest("MaBenhNhan là bắt buộc");

            try
            {
                var result = await _genealogy.GetGenealogyTreeAsync(maBenhNhan);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Liên kết cha/mẹ cho bệnh nhân
        /// </summary>
        [HttpPost("{maBenhNhan}/link-parents")]
        public async Task<ActionResult<GenealogyNodeDto>> LinkParents(
            [FromRoute] string maBenhNhan,
            [FromBody] LinkParentsRequest request)
        {
            if (string.IsNullOrWhiteSpace(maBenhNhan))
                return BadRequest("MaBenhNhan là bắt buộc");
            if (request == null)
                return BadRequest("Request không hợp lệ");

            try
            {
                var result = await _genealogy.LinkParentsAsync(maBenhNhan, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        /// <summary>
        /// Lấy tiền sử bệnh gia đình (tổng hợp từ toàn bộ gia phả)
        /// </summary>
        [HttpGet("{maBenhNhan}/family-diseases")]
        public async Task<ActionResult<FamilyDiseaseSummaryDto>> GetFamilyDiseases(
            [FromRoute] string maBenhNhan)
        {
            if (string.IsNullOrWhiteSpace(maBenhNhan))
                return BadRequest("MaBenhNhan là bắt buộc");

            try
            {
                var result = await _genealogy.GetFamilyDiseasesAsync(maBenhNhan);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
