using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HealthCare.Services
{
    public class ReportService(DataContext db) : IReportService
    {
        private readonly DataContext _db = db;

        public async Task<ReportOverviewDto> LayBaoCaoTongQuanAsync(ReportFilterRequest filter)
        {
            var from = filter.FromDate.Date;
            var toInclusive = filter.ToDate.Date;
            if (toInclusive < from)
                throw new ArgumentException("ToDate phải >= FromDate");

            var toExclusive = toInclusive.AddDays(1);
            var groupBy = filter.GroupBy?.ToLowerInvariant() ?? "day";

            // ===== DỮ LIỆU THÔ TRONG KHOẢNG =====

            var invoices = await _db.HoaDonThanhToans
                .Where(h => h.ThoiGian >= from && h.ThoiGian < toExclusive && h.TrangThai == "da_thu")
                .Select(h => new { h.ThoiGian, h.SoTien })
                .ToListAsync();

            var firstExams = await _db.PhieuKhamLamSangs
                .GroupBy(p => p.MaBenhNhan)
                .Select(g => new { MaBenhNhan = g.Key, FirstDate = g.Min(p => p.NgayLap) })
                .Where(x => x.FirstDate >= from && x.FirstDate < toExclusive)
                .ToListAsync();

            var revisits = await _db.LuotKhamBenhs
                .Where(l => l.ThoiGianBatDau >= from &&
                            l.ThoiGianBatDau < toExclusive &&
                            l.LoaiLuot == "tai_kham")
                .Select(l => l.ThoiGianBatDau)
                .ToListAsync();

            var appts = await _db.LichHenKhams
                .Where(l => l.NgayHen >= from && l.NgayHen < toExclusive && l.CoHieuLuc)
                .Select(l => new { l.NgayHen, l.TrangThai })
                .ToListAsync();

            // ===== BUCKET hoá =====
            var buckets = new SortedDictionary<DateTime, ReportOverviewItemDto>();

            DateTime BucketKey(DateTime d)
            {
                d = d.Date;
                return groupBy switch
                {
                    "week" => d.AddDays(-(int)d.DayOfWeek + (int)DayOfWeek.Monday),
                    "month" => new DateTime(d.Year, d.Month, 1),
                    _ => d
                };
            }

            void EnsureBucket(DateTime key)
            {
                if (!buckets.ContainsKey(key))
                {
                    buckets[key] = new ReportOverviewItemDto
                    {
                        Ngay = key,
                        DoanhThu = 0,
                        BenhNhanMoi = 0,
                        TaiKham = 0,
                        TyLeHuy = 0
                    };
                }
            }

            // Doanh thu
            foreach (var inv in invoices)
            {
                var key = BucketKey(inv.ThoiGian);
                EnsureBucket(key);
                var item = buckets[key];
                buckets[key] = item with { DoanhThu = item.DoanhThu + inv.SoTien };
            }

            // BN mới
            foreach (var bn in firstExams)
            {
                var key = BucketKey(bn.FirstDate);
                EnsureBucket(key);
                var item = buckets[key];
                buckets[key] = item with { BenhNhanMoi = item.BenhNhanMoi + 1 };
            }

            // Tái khám
            foreach (var rv in revisits)
            {
                var key = BucketKey(rv);
                EnsureBucket(key);
                var item = buckets[key];
                buckets[key] = item with { TaiKham = item.TaiKham + 1 };
            }

            // Tỷ lệ huỷ
            var apptGroups = appts.GroupBy(a => BucketKey(a.NgayHen));
            foreach (var g in apptGroups)
            {
                var key = g.Key;
                EnsureBucket(key);

                var total = g.Count();
                var cancelled = g.Count(a => a.TrangThai == "da_huy");
                decimal cancelRate = 0;
                if (total > 0)
                    cancelRate = (decimal)cancelled / total * 100m;

                var item = buckets[key];
                buckets[key] = item with { TyLeHuy = cancelRate };
            }

            var items = buckets.Values.ToList();

            // ===== KPI tổng + % thay đổi =====
            var totalRevenue = items.Sum(i => i.DoanhThu);
            var totalNewPatients = items.Sum(i => i.BenhNhanMoi);
            var totalRevisits = items.Sum(i => i.TaiKham);
            var avgCancelRate = items.Count > 0 ? items.Average(i => i.TyLeHuy) : 0;

            var periodDays = (toExclusive - from).TotalDays;
            var prevFrom = from.AddDays(-periodDays);
            var prevTo = from;

            var prevRevenue = await _db.HoaDonThanhToans
                .Where(h => h.ThoiGian >= prevFrom && h.ThoiGian < prevTo && h.TrangThai == "da_thu")
                .SumAsync(h => (decimal?)h.SoTien) ?? 0m;

            var prevFirstExamsCount = await _db.PhieuKhamLamSangs
                .GroupBy(p => p.MaBenhNhan)
                .Select(g => new { MaBenhNhan = g.Key, FirstDate = g.Min(p => p.NgayLap) })
                .Where(x => x.FirstDate >= prevFrom && x.FirstDate < prevTo)
                .CountAsync();

            var prevRevisitsCount = await _db.LuotKhamBenhs
                .Where(l => l.ThoiGianBatDau >= prevFrom &&
                            l.ThoiGianBatDau < prevTo &&
                            l.LoaiLuot == "tai_kham")
                .CountAsync();

            var prevAppts = await _db.LichHenKhams
                .Where(l => l.NgayHen >= prevFrom && l.NgayHen < prevTo && l.CoHieuLuc)
                .Select(l => new { l.NgayHen, l.TrangThai })
                .ToListAsync();

            decimal prevCancelRate = 0;
            if (prevAppts.Count > 0)
            {
                var totalPrevAppts = prevAppts.Count;
                var prevCancelled = prevAppts.Count(a => a.TrangThai == "da_huy");
                prevCancelRate = (decimal)prevCancelled / totalPrevAppts * 100m;
            }

            decimal ChangePercent(decimal current, decimal previous)
            {
                if (previous <= 0) return 0;
                return (current - previous) / previous * 100m;
            }

            var revenueKpi = new ReportRevenueKpiDto
            {
                TongDoanhThu = totalRevenue,
                DoanhThuChangePercent = ChangePercent(totalRevenue, prevRevenue),
                PhanBoTheoNgay = items
                    .Select(i => new ReportDateValueItemDto { Ngay = i.Ngay, GiaTri = i.DoanhThu })
                    .ToList()
            };

            var newPatientsKpi = new ReportNewPatientsKpiDto
            {
                TongBenhNhanMoi = totalNewPatients,
                BenhNhanMoiChangePercent = ChangePercent(totalNewPatients, prevFirstExamsCount),
                PhanBoTheoNgay = items
                    .Select(i => new ReportDateValueItemDto { Ngay = i.Ngay, GiaTri = i.BenhNhanMoi })
                    .ToList()
            };

            var revisitKpi = new ReportRevisitKpiDto
            {
                TongTaiKham = totalRevisits,
                TaiKhamChangePercent = ChangePercent(totalRevisits, prevRevisitsCount),
                PhanBoTheoNgay = items
                    .Select(i => new ReportDateValueItemDto { Ngay = i.Ngay, GiaTri = i.TaiKham })
                    .ToList()
            };

            var cancelKpi = new ReportCancelRateKpiDto
            {
                TyLeHuy = avgCancelRate,
                TyLeHuyChangePercent = ChangePercent(avgCancelRate, prevCancelRate),
                PhanBoTheoNgay = items
                    .Select(i => new ReportDateValueItemDto { Ngay = i.Ngay, GiaTri = i.TyLeHuy })
                    .ToList()
            };

            return new ReportOverviewDto
            {
                DoanhThu = revenueKpi,
                BenhNhanMoi = newPatientsKpi,
                TaiKham = revisitKpi,
                TyLeHuy = cancelKpi,
                Items = items
            };
        }
    }
}
