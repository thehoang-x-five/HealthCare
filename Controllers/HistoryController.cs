using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HealthCare.Infrastructure.Repositories;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    }

    public class MedicalHistoryResponse
    {
        public string MaBenhNhan { get; set; } = string.Empty;
        public int TotalEvents { get; set; }
        public List<BsonDocument> Events { get; set; } = new();
    }
}
