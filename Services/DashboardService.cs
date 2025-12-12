using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HealthCare.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly DataContext _db;

        public DashboardService(DataContext db)
        {
            _db = db;
        }

        public async Task<DashboardTodayDto> LayDashboardHomNayAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var yesterday = today.AddDays(-1);
            // ===== 1. KPI: BỆNH NHÂN TRONG NGÀY (THEO PHIẾU LS) =====
            // Chỉ tính các phiếu khám lâm sàng trong ngày, chưa có phiếu CLS đi kèm
            var todayPatients = await _db.PhieuKhamLamSangs
                        .Where(p => p.NgayLap >= today
                                    && p.NgayLap < tomorrow
                                   )
                        .Select(p => new { p.MaBenhNhan, p.TrangThai, p.NgayLap, p.GioLap })
                       .ToListAsync();

            var tongBenhNhan = todayPatients.Count;

            // Quy ước:
            // - da_hoan_tat     => đã xử lý
            // - da_huy          => đã huỷ
            // - còn lại         => chờ xử lý
            int daHuy = todayPatients.Count(p => p.TrangThai == "da_huy");
            int daXuLy = todayPatients.Count(p => p.TrangThai == "da_hoan_tat");
            int choXuLy = Math.Max(0, tongBenhNhan - daXuLy - daHuy);

            var yesterdayPatientsCount = await _db.PhieuKhamLamSangs
                        .Where(p => p.NgayLap >= yesterday
                                    && p.NgayLap < today
                                    )
                        .CountAsync();

            decimal patientGrowth = 0;
            if (yesterdayPatientsCount > 0)
            {
                patientGrowth = ((decimal)tongBenhNhan - yesterdayPatientsCount) /
                yesterdayPatientsCount * 100m;
            }

            // Phân bố theo giờ: dựa trên GioLap của phiếu LS
            var patientByHour = todayPatients
                        .GroupBy(p => p.GioLap.Hours)
                        .Select(g => new TodayHourValueItemDto
                        {
                            Gio = g.Key,
                            GiaTri = g.Count()
                        })
            .OrderBy(x => x.Gio)
            .ToList();

            patientByHour = EnsureFullDaySeries(patientByHour);
           


            var benhNhanKpi = new TodayPatientsKpiDto
            {
                TongSoBenhNhan = tongBenhNhan,
                DaXuLy = daXuLy,
                ChoXuLy = choXuLy,
                DaHuy = daHuy,
                TangTruongPhanTram = patientGrowth,
                PhanBoTheoGio = patientByHour
            };

            // ===== 2. KPI: LỊCH HẸN HÔM NAY =====
            var todayAppointments = await _db.LichHenKhams
                .Where(l => l.NgayHen >= today && l.NgayHen < tomorrow && l.CoHieuLuc)
                .Select(l => new { l.TrangThai, l.GioHen })
                .ToListAsync();

            int tongLichHen = todayAppointments.Count;
            int daXacNhan = todayAppointments.Count(a => a.TrangThai == "da_xac_nhan" || a.TrangThai == "da_checkin");
            int choXacNhan = todayAppointments.Count(a => a.TrangThai == "dang_cho");
            int lichHuy = todayAppointments.Count(a => a.TrangThai == "da_huy");

            var yesterdayAppointmentsCount = await _db.LichHenKhams
                .CountAsync(l => l.NgayHen >= yesterday && l.NgayHen < today && l.CoHieuLuc);

            decimal apptGrowth = 0;
            if (yesterdayAppointmentsCount > 0)
            {
                apptGrowth = ((decimal)tongLichHen - yesterdayAppointmentsCount) /
                             yesterdayAppointmentsCount * 100m;
            }

            var apptByHour = todayAppointments
                .GroupBy(x => x.GioHen.Hours)
                .Select(g => new TodayHourValueItemDto
                {
                    Gio = g.Key,
                    GiaTri = g.Count()
                })
                .OrderBy(x => x.Gio)
                .ToList();

            apptByHour = EnsureFullDaySeries(apptByHour);

            var lichHenKpi = new TodayAppointmentsKpiDto
            {
                TongSoLichHen = tongLichHen,
                DaXacNhan = daXacNhan,
                ChoXacNhan = choXacNhan,
                DaHuy = lichHuy,
                TangTruongPhanTram = apptGrowth,
                PhanBoTheoGio = apptByHour
            };

            // ===== 3. KPI: DOANH THU HÔM NAY =====
            var todayInvoices = await _db.HoaDonThanhToans
                .Where(h => h.ThoiGian >= today && h.ThoiGian < tomorrow && h.TrangThai == "da_thu")
                .Select(h => new { h.ThoiGian, h.LoaiDotthu, h.SoTien })
                .ToListAsync();

            decimal doanhThuTong = todayInvoices.Sum(h => h.SoTien);
            decimal doanhThuLs = todayInvoices.Where(h => h.LoaiDotthu == "kham_lam_sang").Sum(h => h.SoTien);
            decimal doanhThuCls = todayInvoices.Where(h => h.LoaiDotthu == "can_lam_sang").Sum(h => h.SoTien);
            decimal doanhThuThuoc = todayInvoices.Where(h => h.LoaiDotthu == "thuoc").Sum(h => h.SoTien);

            var yesterdayInvoicesTotal = await _db.HoaDonThanhToans
                .Where(h => h.ThoiGian >= yesterday && h.ThoiGian < today && h.TrangThai == "da_thu")
                .SumAsync(h => (decimal?)h.SoTien) ?? 0m;

            decimal revenueGrowth = 0;
            if (yesterdayInvoicesTotal > 0)
            {
                revenueGrowth = (doanhThuTong - yesterdayInvoicesTotal) /
                                yesterdayInvoicesTotal * 100m;
            }

            var revenueByHour = todayInvoices
                .GroupBy(h => h.ThoiGian.Hour)
                .Select(g => new TodayHourValueItemDto
                {
                    Gio = g.Key,
                    GiaTri = g.Sum(x => x.SoTien)
                })
                .OrderBy(x => x.Gio)
                .ToList();

            revenueByHour = EnsureFullDaySeries(revenueByHour);

            var doanhThuKpi = new TodayRevenueKpiDto
            {
                TongDoanhThu = doanhThuTong,
                DoanhThuKhamLs = doanhThuLs,
                DoanhThuCls = doanhThuCls,
                DoanhThuThuoc = doanhThuThuoc,
                TangTruongPhanTram = revenueGrowth,
                PhanBoTheoGio = revenueByHour
            };

            // ===== 4. KPI: LƯỢT KHÁM LS + CLS HÔM NAY =====
                        //  - Lâm sàng (LS): tính theo phiếu khám LS.
                        //  - Cận lâm sàng (CLS): tính theo CHI TIẾT DỊCH VỤ (ChiTietDichVu).
            
                        // LS: mỗi PhieuKhamLamSang = 1 lượt
            var todayLs = await _db.PhieuKhamLamSangs
                            .Where(p => p.NgayLap >= today && p.NgayLap < tomorrow)
                            .Select(p => new { p.TrangThai, p.GioLap })
                            .ToListAsync();
            
                        // CLS: mỗi ChiTietDichVu = 1 lượt khám
                      // join ChiTietDichVu với PhieuKhamCanLamSang để lọc theo NgayGioLap trong hôm nay
            var todayCls = await (
            from ct in _db.ChiTietDichVus
                                join cls in _db.PhieuKhamCanLamSangs
                                    on ct.MaPhieuKhamCls equals cls.MaPhieuKhamCls
                                where cls.NgayGioLap >= today && cls.NgayGioLap < tomorrow
                                select new { ct.TrangThai, cls.NgayGioLap }
                            )
                            .ToListAsync();
            
            int tongLuot = todayLs.Count + todayCls.Count;
            
                        // Chờ khám:
                        //  - LS: TrangThai = "da_lap"
                        //  - CLS: TrangThai chi tiết DV = "da_lap"
            int choKham = todayLs.Count(p => p.TrangThai == "da_lap") +
            todayCls.Count(p => p.TrangThai == "da_lap");
            
                        // Đang khám / đang thực hiện:
                       //  - LS: "dang_kham"
                       //  - CLS: "dang_thuc_hien"
           int dangKham = todayLs.Count(p => p.TrangThai == "dang_kham") +
           todayCls.Count(p => p.TrangThai == "dang_thuc_hien");
           
                        // Đã hoàn tất:
                        //  - LS: "da_hoan_tat"
                       //  - CLS: "da_hoan_tat"
            int daHoanTat = todayLs.Count(p => p.TrangThai == "da_hoan_tat") +
            todayCls.Count(p => p.TrangThai == "da_hoan_tat");
            
                        // So sánh với hôm qua: cũng tính CLS theo số ChiTietDichVu
            var yesterdayLsCount = await _db.PhieuKhamLamSangs
                            .CountAsync(p => p.NgayLap >= yesterday && p.NgayLap < today);
            
            var yesterdayClsCount = await (
            from ct in _db.ChiTietDichVus
                                join cls in _db.PhieuKhamCanLamSangs
                                    on ct.MaPhieuKhamCls equals cls.MaPhieuKhamCls
                                where cls.NgayGioLap >= yesterday && cls.NgayGioLap < today
                                select ct
                            )
                            .CountAsync();
            
            int yesterdayTotalExam = yesterdayLsCount + yesterdayClsCount;
            decimal examGrowth = 0;
           if (yesterdayTotalExam > 0)
           {
               examGrowth = (decimal)(tongLuot - yesterdayTotalExam) /
                            yesterdayTotalExam * 100m;
           }
            // Phân bố theo giờ:
                        //  - LS: dùng GioLap.Hours
                       //  - CLS: dùng NgayGioLap.Hour của phiếu CLS chứa chi tiết DV
            var examByHourLs = todayLs
                            .GroupBy(x => x.GioLap.Hours)
                            .ToDictionary(g => g.Key, g => g.Count());
            
            var examByHourCls = todayCls
                            .GroupBy(x => x.NgayGioLap.Hour)
                            .ToDictionary(g => g.Key, g => g.Count());

            var examByHourList = new List<TodayHourValueItemDto>();
            for (int hour = 0; hour < 24; hour++)
            {
                examByHourList.Add(new TodayHourValueItemDto
                {
                    Gio = hour,
                    GiaTri = (examByHourLs.TryGetValue(hour, out var c1) ? c1 : 0) +
                             (examByHourCls.TryGetValue(hour, out var c2) ? c2 : 0)
                });
            }

            var examKpi = new TodayExamOverviewDto
            {
                TongLuotKham = tongLuot,
                ChoKham = choKham,
                DangKham = dangKham,
                DaHoanTat = daHoanTat,
                TangTruongPhanTram = examGrowth,
                PhanBoTheoGio = examByHourList
            };

            // ===== 5. LỊCH HẸN SẮP TỚI (HÔM NAY) =====
            var now = DateTime.Now;

            // Bước 1: filter phần dễ translate trên DB
            var upcomingAppointmentsRaw = await _db.LichHenKhams
                        .Include(l => l.LichTruc)
                            .ThenInclude(lt => lt.Phong)
                                .ThenInclude(p => p.KhoaChuyenMon)
                        .Where(l => l.NgayHen == today &&
            l.CoHieuLuc &&
            (l.TrangThai == "dang_cho"
                                     || l.TrangThai == "da_xac_nhan"
                                     || l.TrangThai == "da_checkin"))
                        .OrderBy(l => l.NgayHen)
                        .ThenBy(l => l.GioHen)
                        .Take(50) // giới hạn để tránh load quá nhiều
                        .ToListAsync();

            // Bước 2: tính DateTime thực (Ngày + Giờ) ở memory
            var upcomingAppointments = upcomingAppointmentsRaw
                        .Where(l => l.NgayHen.Add(l.GioHen) >= now)
                        .OrderBy(l => l.NgayHen)
                        .ThenBy(l => l.GioHen)
                        .Take(10)
                        .ToList();

            var upcomingDtos = upcomingAppointments
                .Select(l => new UpcomingAppointmentDashboardItemDto
                {
                    NgayHen = l.NgayHen,
                    GioHen = l.GioHen,
                    TenBenhNhan = l.TenBenhNhan,
                    TenDichVuKham = null,
                    TenKhoa = l.LichTruc.Phong.KhoaChuyenMon.TenKhoa,
                    TrangThai = l.TrangThai
                })
                .ToList();

            // ===== 6. HOẠT ĐỘNG GẦN ĐÂY =====
            var activities = await BuildRecentActivitiesAsync(today, tomorrow);

            return new DashboardTodayDto
            {
                Ngay = today,
                BenhNhanTrongNgay = benhNhanKpi,
                LichHenHomNay = lichHenKpi,
                DoanhThuHomNay = doanhThuKpi,
                LuotKhamHomNay = examKpi,
                LichHenSapToi = upcomingDtos,
                HoatDongGanDay = activities
            };
        }

        // ============ HELPERS ============

        private static List<TodayHourValueItemDto> EnsureFullDaySeries(List<TodayHourValueItemDto> items)
        {
            var dict = items.ToDictionary(x => x.Gio, x => x.GiaTri);
            var result = new List<TodayHourValueItemDto>(24);
            for (int i = 0; i < 24; i++)
            {
                dict.TryGetValue(i, out var val);
                result.Add(new TodayHourValueItemDto { Gio = i, GiaTri = val });
            }
            return result;
        }

        private async Task<IReadOnlyList<DashboardActivityDto>> BuildRecentActivitiesAsync(
    DateTime from, DateTime toExclusive)
        {
            var activities = new List<DashboardActivityDto>();

            // 1. Hoá đơn: các khoản thu trong ngày
            var invoiceList = await _db.HoaDonThanhToans
                .Include(h => h.BenhNhan)
                .Where(h => h.ThoiGian >= from && h.ThoiGian < toExclusive)
                .OrderByDescending(h => h.ThoiGian)
                .Take(5)
                .ToListAsync();

            activities.AddRange(
                invoiceList.Select(h => new DashboardActivityDto
                {
                    MoTa = $"Thu {h.SoTien:n0}đ cho BN {h.BenhNhan?.HoTen ?? h.MaBenhNhan} ({MapLoaiDotThuText(h.LoaiDotthu)})",
                    ThoiGian = h.ThoiGian
                })
            );

            // 2. Lịch hẹn: chỉ lấy các trạng thái quan trọng
            var apptList = await _db.LichHenKhams
                .Where(l =>
                    l.NgayHen >= from && l.NgayHen < toExclusive &&
                    (l.TrangThai == "dang_cho" ||
                     l.TrangThai == "da_xac_nhan" ||
                     l.TrangThai == "da_checkin" ||
                     l.TrangThai == "da_huy"))
                .OrderByDescending(l => l.NgayHen)
                .ThenByDescending(l => l.GioHen)
                .Take(5)
                .ToListAsync();

            activities.AddRange(
                apptList.Select(l => new DashboardActivityDto
                {
                    MoTa = $"Lịch hẹn {l.TenBenhNhan} lúc {l.GioHen:hh\\:mm} ({MapTrangThaiLichHen(l.TrangThai)})",
                    ThoiGian = l.NgayHen.Add(l.GioHen)
                })
            );

            // 3. Phiếu khám LS: tạo phiếu khám trong ngày
            var examList = await _db.PhieuKhamLamSangs
                .Include(p => p.BenhNhan)
                .Where(p => p.NgayLap >= from && p.NgayLap < toExclusive)
                .OrderByDescending(p => p.NgayLap)
                .ThenByDescending(p => p.GioLap)
                .Take(5)
                .ToListAsync();

            activities.AddRange(
                examList.Select(p => new DashboardActivityDto
                {
                    MoTa = $"Khám LS cho BN {p.BenhNhan?.HoTen ?? p.MaBenhNhan}",
                    ThoiGian = p.NgayLap.Add(p.GioLap)
                })
            );

            // 4. Phiếu CLS: chỉ định CLS trong ngày
            var clsOrderList = await _db.PhieuKhamCanLamSangs
                .Include(cls => cls.PhieuKhamLamSang)
                    .ThenInclude(ls => ls.BenhNhan)
                .Where(cls => cls.NgayGioLap >= from && cls.NgayGioLap < toExclusive)
                .OrderByDescending(cls => cls.NgayGioLap)
                .Take(5)
                .ToListAsync();

            activities.AddRange(
                clsOrderList.Select(cls => new DashboardActivityDto
                {
                    MoTa = $"Chỉ định CLS cho BN {cls.PhieuKhamLamSang?.BenhNhan?.HoTen ?? cls.PhieuKhamLamSang?.MaBenhNhan}",
                    ThoiGian = cls.NgayGioLap
                })
            );

            // 5. Đơn thuốc: kê đơn trong ngày
            var prescriptionList = await _db.DonThuocs
                .Include(d => d.BenhNhan)
                .Where(d => d.ThoiGianKeDon >= from && d.ThoiGianKeDon < toExclusive)
                .OrderByDescending(d => d.ThoiGianKeDon)
                .Take(5)
                .ToListAsync();

            activities.AddRange(
                prescriptionList.Select(d => new DashboardActivityDto
                {
                    MoTa = $"Kê đơn thuốc cho BN {d.BenhNhan?.HoTen ?? d.MaBenhNhan}",
                    ThoiGian = d.ThoiGianKeDon
                })
            );

            // 🔥 LƯỢT KHÁM (LuotKhamBenh)
            var visitActs = await _db.LuotKhamBenhs
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.BenhNhan)
                .Include(l => l.HangDoi)
                    .ThenInclude(h => h.Phong)
                        .ThenInclude(p => p.KhoaChuyenMon)
                .Where(l => l.ThoiGianBatDau >= from && l.ThoiGianBatDau < toExclusive)
                .OrderByDescending(l => l.ThoiGianBatDau)
                .Take(10)
                .Select(l => new DashboardActivityDto
                {
                    MoTa =
                        $"Lượt khám cho BN {l.HangDoi.BenhNhan.HoTen} " +
                        $"tại {l.HangDoi.Phong.KhoaChuyenMon.TenKhoa}/{l.HangDoi.Phong.TenPhong}",
                    ThoiGian = l.ThoiGianBatDau
                })
                .ToListAsync();
            activities.AddRange(visitActs);
            // Gộp tất cả, sort theo thời gian, lấy tối đa 10 hoạt động mới nhất
            return activities
                .OrderByDescending(a => a.ThoiGian)
                .Take(10)
                .ToList();
        }
        private static string MapLoaiDotThuText(string? loai)
        {
            return loai switch
            {
                "kham_lam_sang" => "khám lâm sàng",
                "can_lam_sang" => "cận lâm sàng",
                "thuoc" => "tiền thuốc",
                _ => loai ?? "khác"
            };
        }

        private static string MapTrangThaiLichHen(string? trangThai)
        {
            return trangThai switch
            {
                "dang_cho" => "Đang chờ",
                "da_xac_nhan" => "Đã xác nhận",
                "da_checkin" => "Đã check-in",
                "da_huy" => "Đã huỷ",
                _ => trangThai ?? string.Empty
            };
        }

    }
}
