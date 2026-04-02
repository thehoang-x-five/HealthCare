using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using HealthCare.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HealthCare.Services.Report
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly DataContext _db;
        private readonly IMongoDbContext _mongoContext;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(
            DataContext db,
            IMongoDbContext mongoContext,
            ILogger<AnalyticsService> logger)
        {
            _db = db;
            _mongoContext = mongoContext;
            _logger = logger;
        }

        public async Task<AbnormalStatsDto> GetAbnormalStatsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!_mongoContext.IsConnected)
            {
                _logger.LogWarning("MongoDB not connected - returning empty abnormal stats");
                return new AbnormalStatsDto();
            }

            try
            {
                var collection = _mongoContext.GetCollection<BsonDocument>("medical_history");

                var matchFilter = Builders<BsonDocument>.Filter.Eq("event_type", "xet_nghiem");
                
                if (fromDate.HasValue)
                    matchFilter &= Builders<BsonDocument>.Filter.Gte("timestamp", fromDate.Value);
                
                if (toDate.HasValue)
                    matchFilter &= Builders<BsonDocument>.Filter.Lte("timestamp", toDate.Value);

                var pipeline = new[]
                {
                    new BsonDocument("$match", matchFilter.Render(collection.DocumentSerializer, collection.Settings.SerializerRegistry)),
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", "$payload.trang_thai_chot" },
                        { "count", new BsonDocument("$sum", 1) }
                    })
                };

                var results = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

                var totalTests = results.Sum(r => r["count"].AsInt32);
                var abnormalCount = results
                    .Where(r => r["_id"].AsString?.Contains("bat_thuong") == true || 
                               r["_id"].AsString?.Contains("abnormal") == true)
                    .Sum(r => r["count"].AsInt32);

                var abnormalPercentage = totalTests > 0 ? (decimal)abnormalCount / totalTests * 100 : 0;

                return new AbnormalStatsDto
                {
                    TotalTests = totalTests,
                    AbnormalCount = abnormalCount,
                    AbnormalPercentage = Math.Round(abnormalPercentage, 2),
                    FromDate = fromDate,
                    ToDate = toDate,
                    ByTestType = results.Select(r => new AbnormalTestTypeDto
                    {
                        TestType = r["_id"].AsString ?? "Unknown",
                        Count = r["count"].AsInt32,
                        Percentage = totalTests > 0 ? Math.Round((decimal)r["count"].AsInt32 / totalTests * 100, 2) : 0
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting abnormal stats from MongoDB");
                return new AbnormalStatsDto();
            }
        }

        public async Task<DiseaseTrendsDto> GetDiseaseTrendsAsync(DateTime? fromDate = null, DateTime? toDate = null, int topN = 10)
        {
            if (!_mongoContext.IsConnected)
            {
                _logger.LogWarning("MongoDB not connected - returning empty disease trends");
                return new DiseaseTrendsDto();
            }

            try
            {
                var collection = _mongoContext.GetCollection<BsonDocument>("medical_history");

                var matchFilter = Builders<BsonDocument>.Filter.Eq("event_type", "kham_lam_sang");
                
                if (fromDate.HasValue)
                    matchFilter &= Builders<BsonDocument>.Filter.Gte("timestamp", fromDate.Value);
                
                if (toDate.HasValue)
                    matchFilter &= Builders<BsonDocument>.Filter.Lte("timestamp", toDate.Value);

                var pipeline = new[]
                {
                    new BsonDocument("$match", matchFilter.Render(collection.DocumentSerializer, collection.Settings.SerializerRegistry)),
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", "$payload.chan_doan_cuoi" },
                        { "count", new BsonDocument("$sum", 1) }
                    }),
                    new BsonDocument("$sort", new BsonDocument("count", -1)),
                    new BsonDocument("$limit", topN)
                };

                var results = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

                var totalDiagnoses = results.Sum(r => r["count"].AsInt32);

                return new DiseaseTrendsDto
                {
                    TotalDiagnoses = totalDiagnoses,
                    FromDate = fromDate,
                    ToDate = toDate,
                    TopDiseases = results.Select(r => new DiseaseStatDto
                    {
                        DiseaseName = r["_id"].AsString ?? "Unknown",
                        ICD10Code = "",
                        Count = r["count"].AsInt32,
                        Percentage = totalDiagnoses > 0 ? Math.Round((decimal)r["count"].AsInt32 / totalDiagnoses * 100, 2) : 0
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting disease trends from MongoDB");
                return new DiseaseTrendsDto();
            }
        }

        public async Task<PopularDrugsDto> GetPopularDrugsAsync(DateTime? fromDate = null, DateTime? toDate = null, int topN = 10)
        {
            var query = _db.DonThuocs
                .AsNoTracking()
                .Where(d => d.TrangThai == "da_phat");

            if (fromDate.HasValue)
                query = query.Where(d => d.ThoiGianKeDon >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(d => d.ThoiGianKeDon <= toDate.Value);

            var drugStats = await query
                .SelectMany(d => d.ChiTietDonThuocs)
                .GroupBy(c => new { c.MaThuoc, c.KhoThuoc.TenThuoc })
                .Select(g => new DrugStatDto
                {
                    MaThuoc = g.Key.MaThuoc,
                    TenThuoc = g.Key.TenThuoc ?? "",
                    PrescriptionCount = g.Count(),
                    TotalQuantity = g.Sum(c => c.SoLuong),
                    TotalRevenue = g.Sum(c => c.ThanhTien)
                })
                .OrderByDescending(d => d.PrescriptionCount)
                .Take(topN)
                .ToListAsync();

            var totalPrescriptions = await query.CountAsync();

            return new PopularDrugsDto
            {
                TotalPrescriptions = totalPrescriptions,
                FromDate = fromDate,
                ToDate = toDate,
                TopDrugs = drugStats
            };
        }
    }
}
