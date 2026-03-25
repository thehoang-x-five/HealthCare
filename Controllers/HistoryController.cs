using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HealthCare.Infrastructure.Repositories;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ===== WEEK 2 DEV 1: Old SQL-based imports (commented out) =====
// using HealthCare.DTOs;
// using HealthCare.Services.OutpatientCare;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/patients/{maBenhNhan}/medical-history")]
    [Authorize]
    public class HistoryController : ControllerBase
    {
        private readonly IMongoHistoryRepository _mongoHistory;

        public HistoryController(IMongoHistoryRepository mongoHistory)
        {
            _mongoHistory = mongoHistory;
        }

        /// <summary>
        /// Lấy lịch sử khám bệnh của bệnh nhân từ MongoDB.
        /// </summary>
        /// <param name="maBenhNhan">Mã bệnh nhân</param>
        /// <param name="eventType">Lọc theo loại sự kiện (kham_lam_sang, xet_nghiem, chan_doan_hinh_anh, don_thuoc, thanh_toan)</param>
        /// <param name="fromDate">Lọc từ ngày (ISO 8601 format)</param>
        /// <param name="toDate">Lọc đến ngày (ISO 8601 format)</param>
        /// <param name="limit">Số lượng bản ghi tối đa (mặc định 100)</param>
        /// <returns>Danh sách sự kiện y tế</returns>
        [HttpGet]
        public async Task<ActionResult<MedicalHistoryResponse>> GetMedicalHistory(
            string maBenhNhan,
            [FromQuery] string? eventType = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int limit = 100)
        {
            if (string.IsNullOrWhiteSpace(maBenhNhan))
                return BadRequest("MaBenhNhan là bắt buộc");

            if (limit <= 0 || limit > 500)
                limit = 100;

            var events = await _mongoHistory.GetPatientHistoryAsync(
                maBenhNhan,
                eventType,
                fromDate,
                toDate,
                limit);

            var response = new MedicalHistoryResponse
            {
                MaBenhNhan = maBenhNhan,
                TotalEvents = events.Count,
                Events = events
            };

            return Ok(response);
        }

        // ================================================================
        // ===== WEEK 2 DEV 1: Old SQL-based endpoints (commented out) ====
        // ================================================================
        //
        // [Route("api/history")]
        // public class HistoryController(IHistoryService historyService) : ControllerBase
        // {
        //     private readonly IHistoryService _historyService = historyService;
        //
        //     /// <summary>
        //     /// Tìm kiếm lịch sử khám (tab Khám bệnh).
        //     /// </summary>
        //     [HttpPost("visits/search")]
        //     [Authorize]
        //     public async Task<ActionResult<PagedResult<HistoryVisitRecordDto>>> SearchVisits(
        //         [FromBody] HistoryFilterRequest filter)
        //     {
        //         if (filter == null)
        //             return BadRequest("Filter is required");
        //
        //         var result = await _historyService.LayLichSuAsync(filter);
        //         return Ok(result);
        //     }
        //
        //     // POST /api/history/visits
        //     [HttpPost("visits")]
        //     public async Task<ActionResult<HistoryVisitRecordDto>> CreateVisit(
        //         [FromBody] HistoryVisitCreateRequest request)
        //     {
        //         if (request == null)
        //             return BadRequest("Request body is required");
        //
        //         try
        //         {
        //             var dto = await _historyService.TaoLuotKhamAsync(request);
        //
        //             // Trả về 201 + Location trỏ tới GET detail
        //             return CreatedAtAction(
        //                 nameof(GetVisitDetail),
        //                 new { maLuotKham = dto.MaLuotKham },
        //                 dto);
        //         }
        //         catch (InvalidOperationException ex)
        //         {
        //             return BadRequest(ex.Message);
        //         }
        //     }
        //
        //     /// <summary>
        //     /// Lấy chi tiết 1 lần khám (HistoryDetailModal type="visit").
        //     /// </summary>
        //     [HttpGet("visits/{maLuotKham}")]
        //     [Authorize]
        //     public async Task<ActionResult<HistoryVisitDetailDto>> GetVisitDetail(string maLuotKham)
        //     {
        //         var dto = await _historyService.LayChiTietLichSuKhamAsync(maLuotKham);
        //         if (dto == null) return NotFound();
        //         return Ok(dto);
        //     }
        //
        //     // PUT /api/history/visits/{maLuotKham}/status
        //     [HttpPut("visits/{maLuotKham}/status")]
        //     public async Task<ActionResult<HistoryVisitRecordDto>> UpdateVisitStatus(
        //         string maLuotKham,
        //         [FromBody] HistoryVisitStatusUpdateRequest request)
        //     {
        //         if (request == null)
        //             return BadRequest("Request body is required");
        //
        //         var dto = await _historyService.CapNhatTrangThaiLuotKhamAsync(maLuotKham, request);
        //         if (dto == null)
        //             return NotFound();
        //
        //         return Ok(dto);
        //     }
        // }
        // ================================================================
    }

    public class MedicalHistoryResponse
    {
        public string MaBenhNhan { get; set; } = string.Empty;
        public int TotalEvents { get; set; }
        public List<BsonDocument> Events { get; set; } = new();
    }
}
