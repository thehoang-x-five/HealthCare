using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HealthCare.Datas;
using HealthCare.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HealthCare.Services.Report
{
    public class DashboardService : IDashboardService
    {
        private readonly DataContext _db;

        public DashboardService(DataContext db)
        {
            _db = db;
        }

        public async Task<DashboardTodayDto> LayDashboardHomNayAsync(string? maKhoa = null)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var yesterday = today.AddDays(-1);
            // ===== 1. KPI: BỆNH NHÂN TRONG NGÀY (THEO PHIẾU LS) =====
            // Nếu maKhoa != null, lấy danh sách MaPhong thuộc khoa đó
            List<string>? scopeRooms = null;
            if (!string.IsNullOrWhiteSpace(maKhoa))
            {
                scopeRooms = await _db.Phongs
                    .Where(p => p.MaKhoa == maKhoa)
                    .Select(p => p.MaPhong)
                    .ToListAsync();
            }

            IQueryable<string>? scopedLsExamIdsQuery = null;
            if (scopeRooms != null)
            {
                scopedLsExamIdsQuery = _db.HangDois
                    .Where(h => h.MaPhieuKham != null && scopeRooms.Contains(h.MaPhong))
                    .Select(h => h.MaPhieuKham!);
            }

            // Chỉ tính các phiếu khám lâm sàng trong ngày
            var lsQuery = _db.PhieuKhamLamSangs
                        .Where(p => p.NgayLap >= today && p.NgayLap < tomorrow);
            // ✅ RBAC: scope theo khoa qua MaPhong trong hàng đợi
            if (scopedLsExamIdsQuery != null)
            {
                lsQuery = lsQuery.Where(p => scopedLsExamIdsQuery.Contains(p.MaPhieuKham));
            }
            var todayPatients = await lsQuery
                        .Select(p => new { p.MaBenhNhan, p.TrangThai, p.NgayLap, p.GioLap, p.MaPhieuKham })
                       .ToListAsync();

            var todayLsPatients = todayPatients
                .GroupBy(p => p.MaBenhNhan)
                .Select(g => new
                {
                    MaBenhNhan = g.Key,
                    Gio = g.Min(x => x.GioLap.Hours),
                    TrangThaiTongHop = ResolvePatientDailyStatus(g.Select(x => x.TrangThai))
                })
                .ToList();

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

            var yesterdayLsPatientsQuery = _db.PhieuKhamLamSangs
                .Where(p => p.NgayLap >= yesterday && p.NgayLap < today);
            if (scopedLsExamIdsQuery != null)
            {
                yesterdayLsPatientsQuery = yesterdayLsPatientsQuery
                    .Where(p => scopedLsExamIdsQuery.Contains(p.MaPhieuKham));
            }
            var yesterdayLsPatientsCount = await yesterdayLsPatientsQuery
                .Select(p => p.MaBenhNhan)
                .Distinct()
                .CountAsync();

            int tongBenhNhanLs = todayLsPatients.Count;
            int daXuLyLs = todayLsPatients.Count(p => p.TrangThaiTongHop == "da_xu_ly");
            int daHuyLs = todayLsPatients.Count(p => p.TrangThaiTongHop == "da_huy");
            int choXuLyLs = Math.Max(0, tongBenhNhanLs - daXuLyLs - daHuyLs);
            decimal patientLsGrowth = 0;
            if (yesterdayLsPatientsCount > 0)
            {
                patientLsGrowth = ((decimal)tongBenhNhanLs - yesterdayLsPatientsCount) /
                    yesterdayLsPatientsCount * 100m;
            }

            var patientLsByHour = todayLsPatients
                .GroupBy(p => p.Gio)
                .Select(g => new TodayHourValueItemDto
                {
                    Gio = g.Key,
                    GiaTri = g.Count()
                })
                .OrderBy(x => x.Gio)
                .ToList();
            patientLsByHour = EnsureFullDaySeries(patientLsByHour);



            var benhNhanKpi = new TodayPatientsKpiDto
            {
                TongSoBenhNhan = tongBenhNhan,
                DaXuLy = daXuLy,
                ChoXuLy = choXuLy,
                DaHuy = daHuy,
                TangTruongPhanTram = patientGrowth,
                PhanBoTheoGio = patientByHour
            };

            var benhNhanLsKpi = new TodayPatientsKpiDto
            {
                TongSoBenhNhan = tongBenhNhanLs,
                DaXuLy = daXuLyLs,
                ChoXuLy = choXuLyLs,
                DaHuy = daHuyLs,
                TangTruongPhanTram = patientLsGrowth,
                PhanBoTheoGio = patientLsByHour
            };

            // ===== 2. KPI: LỊCH HẸN HÔM NAY =====
            var apptQuery = _db.LichHenKhams
                .Where(l => l.NgayHen >= today && l.NgayHen < tomorrow && l.CoHieuLuc);
            // ✅ RBAC: scope lịch hẹn theo lịch trực phòng thuộc khoa
            if (scopeRooms != null)
            {
                apptQuery = apptQuery.Where(l => l.LichTruc != null && scopeRooms.Contains(l.LichTruc.MaPhong));
            }
            var todayAppointments = await apptQuery
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
            var invoiceQuery = _db.HoaDonThanhToans
                .Where(h => h.ThoiGian >= today && h.ThoiGian < tomorrow && h.TrangThai == "da_thu");
            // ✅ RBAC: scope doanh thu qua PhieuKham → HangDoi → MaPhong
            if (scopeRooms != null)
            {
                // Lấy MaPhieuKham từ HangDoi thuộc phòng trong khoa
                invoiceQuery = invoiceQuery.Where(h =>
                    (h.MaPhieuKham != null && scopedLsExamIdsQuery!.Contains(h.MaPhieuKham)) ||
                    (h.MaPhieuKhamCls != null && _db.PhieuKhamCanLamSangs
                        .Where(cls => scopedLsExamIdsQuery!.Contains(cls.MaPhieuKhamLs))
                        .Select(cls => cls.MaPhieuKhamCls)
                        .Contains(h.MaPhieuKhamCls)) ||
                    (h.MaDonThuoc != null && _db.DonThuocs
                        .Where(d => _db.NhanVienYTes
                            .Where(nv => nv.MaKhoa == maKhoa)
                            .Select(nv => nv.MaNhanVien)
                            .Contains(d.MaBacSiKeDon))
                        .Select(d => d.MaDonThuoc)
                        .Contains(h.MaDonThuoc))
                );
            }
            var todayInvoices = await invoiceQuery
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
            // ✅ RBAC: scope LS + CLS theo khoa
            var lsExamQuery = _db.PhieuKhamLamSangs
                            .Where(p => p.NgayLap >= today && p.NgayLap < tomorrow);
            if (scopeRooms != null)
            {
                lsExamQuery = lsExamQuery.Where(p => scopedLsExamIdsQuery!.Contains(p.MaPhieuKham));
            }
            var todayLs = await lsExamQuery
                            .Select(p => new { p.TrangThai, p.GioLap })
                            .ToListAsync();
            
                        // CLS: mỗi ChiTietDichVu = 1 lượt khám
            var clsExamQueryBase = 
            from ct in _db.ChiTietDichVus
                                join cls in _db.PhieuKhamCanLamSangs
                                    on ct.MaPhieuKhamCls equals cls.MaPhieuKhamCls
                                join dv in _db.DichVuYTes
                                    on ct.MaDichVu equals dv.MaDichVu
                                where cls.NgayGioLap >= today && cls.NgayGioLap < tomorrow
                                select new { ct, cls, dv };
            // ✅ RBAC: scope CLS theo phòng CLS thuộc khoa
            if (scopeRooms != null)
            {
                clsExamQueryBase = clsExamQueryBase.Where(x => scopeRooms.Contains(x.dv.MaPhongThucHien));
            }
            var todayCls = await clsExamQueryBase
                            .Select(x => new { x.ct.TrangThai, x.cls.NgayGioLap })
                            .ToListAsync();

            var clsOrderQueryBase =
                from cls in _db.PhieuKhamCanLamSangs
                join ls in _db.PhieuKhamLamSangs on cls.MaPhieuKhamLs equals ls.MaPhieuKham
                join ct in _db.ChiTietDichVus on cls.MaPhieuKhamCls equals ct.MaPhieuKhamCls
                join dv in _db.DichVuYTes on ct.MaDichVu equals dv.MaDichVu
                where cls.NgayGioLap >= today && cls.NgayGioLap < tomorrow
                select new
                {
                    cls.MaPhieuKhamCls,
                    cls.TrangThai,
                    cls.NgayGioLap,
                    ls.MaBenhNhan,
                    dv.MaPhongThucHien
                };
            if (scopeRooms != null)
            {
                clsOrderQueryBase = clsOrderQueryBase.Where(x => scopeRooms.Contains(x.MaPhongThucHien));
            }
            var todayClsOrders = await clsOrderQueryBase
                .Select(x => new { x.MaPhieuKhamCls, x.TrangThai, x.NgayGioLap, x.MaBenhNhan })
                .Distinct()
                .ToListAsync();

            var todayClsPatients = todayClsOrders
                .GroupBy(x => x.MaBenhNhan)
                .Select(g => new
                {
                    MaBenhNhan = g.Key,
                    Gio = g.Min(x => x.NgayGioLap.Hour),
                    TrangThaiTongHop = ResolvePatientDailyStatus(g.Select(x => x.TrangThai))
                })
                .ToList();
            
            int tongLuot = todayLs.Count + todayCls.Count;
            
                        // Chờ khám:
                        //  - LS: TrangThai = "da_lap"
                        //  - CLS: TrangThai chi tiết DV = "da_lap"
            int choKham = todayLs.Count(p => p.TrangThai == "da_lap") +
            todayCls.Count(p => p.TrangThai == "da_lap");
            
                        // Đang khám / đang thực hiện:
                       //  - LS: "dang_thuc_hien"
                       //  - CLS: "dang_thuc_hien"
           int dangKham = todayLs.Count(p => p.TrangThai == "dang_thuc_hien") +
           todayCls.Count(p => p.TrangThai == "dang_thuc_hien");
           
                        // Đã hoàn tất:
                        //  - LS: "da_hoan_tat"
                       //  - CLS: "da_hoan_tat"
            int daHoanTat = todayLs.Count(p => p.TrangThai == "da_hoan_tat") +
            todayCls.Count(p => p.TrangThai == "da_hoan_tat");

            // Đã hủy:
            int examDaHuy = todayLs.Count(p => p.TrangThai == "da_huy") +
            todayCls.Count(p => p.TrangThai == "da_huy");
            
                        // So sánh với hôm qua: cũng tính CLS theo số ChiTietDichVu
            var yesterdayLsQuery = _db.PhieuKhamLamSangs
                            .Where(p => p.NgayLap >= yesterday && p.NgayLap < today);
            if (scopedLsExamIdsQuery != null)
            {
                yesterdayLsQuery = yesterdayLsQuery.Where(p => scopedLsExamIdsQuery.Contains(p.MaPhieuKham));
            }
            var yesterdayLsCount = await yesterdayLsQuery.CountAsync();
            
            var yesterdayClsCountQuery =
            from ct in _db.ChiTietDichVus
                                join cls in _db.PhieuKhamCanLamSangs
                                    on ct.MaPhieuKhamCls equals cls.MaPhieuKhamCls
                                join dv in _db.DichVuYTes
                                    on ct.MaDichVu equals dv.MaDichVu
                                where cls.NgayGioLap >= yesterday && cls.NgayGioLap < today
                                select new { ct.MaChiTietDv, dv.MaPhongThucHien };
            if (scopeRooms != null)
            {
                yesterdayClsCountQuery = yesterdayClsCountQuery.Where(x => scopeRooms.Contains(x.MaPhongThucHien));
            }
            var yesterdayClsCount = await yesterdayClsCountQuery.CountAsync();

            var yesterdayClsOrderQueryBase =
                from cls in _db.PhieuKhamCanLamSangs
                join ls in _db.PhieuKhamLamSangs on cls.MaPhieuKhamLs equals ls.MaPhieuKham
                join ct in _db.ChiTietDichVus on cls.MaPhieuKhamCls equals ct.MaPhieuKhamCls
                join dv in _db.DichVuYTes on ct.MaDichVu equals dv.MaDichVu
                where cls.NgayGioLap >= yesterday && cls.NgayGioLap < today
                select new
                {
                    cls.MaPhieuKhamCls,
                    cls.TrangThai,
                    cls.NgayGioLap,
                    ls.MaBenhNhan,
                    dv.MaPhongThucHien
                };
            if (scopeRooms != null)
            {
                yesterdayClsOrderQueryBase = yesterdayClsOrderQueryBase.Where(x => scopeRooms.Contains(x.MaPhongThucHien));
            }
            var yesterdayClsOrderCount = await yesterdayClsOrderQueryBase
                .Select(x => x.MaPhieuKhamCls)
                .Distinct()
                .CountAsync();

            var yesterdayClsPatientsCount = await yesterdayClsOrderQueryBase
                .Select(x => x.MaBenhNhan)
                .Distinct()
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
                DaHuy = examDaHuy,
                TangTruongPhanTram = examGrowth,
                PhanBoTheoGio = examByHourList
            };

            int tongLuotLs = todayLs.Count;
            int choKhamLs = todayLs.Count(p => p.TrangThai == "da_lap");
            int dangKhamLs = todayLs.Count(p => p.TrangThai == "dang_thuc_hien");
            int daHoanTatLs = todayLs.Count(p => p.TrangThai == "da_hoan_tat");
            int daHuyLsExam = todayLs.Count(p => p.TrangThai == "da_huy");
            decimal examLsGrowth = 0;
            if (yesterdayLsCount > 0)
            {
                examLsGrowth = (decimal)(tongLuotLs - yesterdayLsCount) / yesterdayLsCount * 100m;
            }

            var examLsByHour = todayLs
                .GroupBy(x => x.GioLap.Hours)
                .Select(g => new TodayHourValueItemDto
                {
                    Gio = g.Key,
                    GiaTri = g.Count()
                })
                .OrderBy(x => x.Gio)
                .ToList();
            examLsByHour = EnsureFullDaySeries(examLsByHour);

            var luotKhamLsKpi = new TodayExamOverviewDto
            {
                TongLuotKham = tongLuotLs,
                ChoKham = choKhamLs,
                DangKham = dangKhamLs,
                DaHoanTat = daHoanTatLs,
                DaHuy = daHuyLsExam,
                TangTruongPhanTram = examLsGrowth,
                PhanBoTheoGio = examLsByHour
            };

            int tongLuotCls = todayClsOrders.Count;
            int choKhamCls = todayClsOrders.Count(p => p.TrangThai == "da_lap");
            int dangKhamCls = todayClsOrders.Count(p => p.TrangThai == "dang_thuc_hien");
            int daHoanTatCls = todayClsOrders.Count(p => p.TrangThai == "da_hoan_tat");
            int daHuyClsExam = todayClsOrders.Count(p => p.TrangThai == "da_huy");
            decimal examClsGrowth = 0;
            if (yesterdayClsOrderCount > 0)
            {
                examClsGrowth = (decimal)(tongLuotCls - yesterdayClsOrderCount) / yesterdayClsOrderCount * 100m;
            }

            var examClsByHour = todayClsOrders
                .GroupBy(x => x.NgayGioLap.Hour)
                .Select(g => new TodayHourValueItemDto
                {
                    Gio = g.Key,
                    GiaTri = g.Count()
                })
                .OrderBy(x => x.Gio)
                .ToList();
            examClsByHour = EnsureFullDaySeries(examClsByHour);

            var luotKhamClsKpi = new TodayExamOverviewDto
            {
                TongLuotKham = tongLuotCls,
                ChoKham = choKhamCls,
                DangKham = dangKhamCls,
                DaHoanTat = daHoanTatCls,
                DaHuy = daHuyClsExam,
                TangTruongPhanTram = examClsGrowth,
                PhanBoTheoGio = examClsByHour
            };

            int tongBenhNhanCls = todayClsPatients.Count;
            int daXuLyClsPatient = todayClsPatients.Count(p => p.TrangThaiTongHop == "da_xu_ly");
            int daHuyClsPatient = todayClsPatients.Count(p => p.TrangThaiTongHop == "da_huy");
            int choXuLyClsPatient = Math.Max(0, tongBenhNhanCls - daXuLyClsPatient - daHuyClsPatient);
            decimal patientClsGrowth = 0;
            if (yesterdayClsPatientsCount > 0)
            {
                patientClsGrowth = (decimal)(tongBenhNhanCls - yesterdayClsPatientsCount) /
                    yesterdayClsPatientsCount * 100m;
            }

            var patientClsByHour = todayClsPatients
                .GroupBy(p => p.Gio)
                .Select(g => new TodayHourValueItemDto
                {
                    Gio = g.Key,
                    GiaTri = g.Count()
                })
                .OrderBy(x => x.Gio)
                .ToList();
            patientClsByHour = EnsureFullDaySeries(patientClsByHour);

            var benhNhanClsKpi = new TodayPatientsKpiDto
            {
                TongSoBenhNhan = tongBenhNhanCls,
                DaXuLy = daXuLyClsPatient,
                ChoXuLy = choXuLyClsPatient,
                DaHuy = daHuyClsPatient,
                TangTruongPhanTram = patientClsGrowth,
                PhanBoTheoGio = patientClsByHour
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

            // ===== 7. KPI: DỊCH VỤ CLS HÔM NAY (cho vai trò CLS/KTV) =====
            int dichVuHoanTat = todayCls.Count(p => p.TrangThai == "da_hoan_tat");
            int dichVuDangLam = todayCls.Count(p => p.TrangThai == "dang_thuc_hien");
            int dichVuDaHuy = todayCls.Count(p => p.TrangThai == "da_huy");
            int tongDichVu = todayCls.Count;

            var servicesByHour = todayCls
                .GroupBy(x => x.NgayGioLap.Hour)
                .Select(g => new TodayHourValueItemDto { Gio = g.Key, GiaTri = g.Count() })
                .OrderBy(x => x.Gio)
                .ToList();
            servicesByHour = EnsureFullDaySeries(servicesByHour);

            decimal svcGrowth = 0;
            if (yesterdayClsCount > 0)
                svcGrowth = (decimal)(tongDichVu - yesterdayClsCount) / yesterdayClsCount * 100m;

            var dichVuKpi = new TodayServicesKpiDto
            {
                TongDichVu = tongDichVu,
                HoanTat = dichVuHoanTat,
                DangLam = dichVuDangLam,
                DaHuy = dichVuDaHuy,
                TangTruongPhanTram = svcGrowth,
                PhanBoTheoGio = servicesByHour
            };

            // ===== 8. DỊCH VỤ SẮP LÀM (cho CLS/KTV) =====
            // KPI cho khối lâm sàng: tổng số chẩn đoán và các hướng xử trí chính trong ngày.
            var diagnosisBaseQuery = _db.PhieuChanDoanCuois.AsQueryable();
            if (scopedLsExamIdsQuery != null)
            {
                diagnosisBaseQuery = diagnosisBaseQuery.Where(pc => scopedLsExamIdsQuery.Contains(pc.MaPhieuKham));
            }

            var todayDiagnoses = await diagnosisBaseQuery
                .Where(pc => pc.ThoiGianTao >= today && pc.ThoiGianTao < tomorrow)
                .Select(pc => new
                {
                    pc.ThoiGianTao,
                    pc.HuongXuTri,
                    pc.MaDonThuoc,
                    pc.NgayTaiKham
                })
                .ToListAsync();

            var yesterdayDiagnosisCount = await diagnosisBaseQuery
                .Where(pc => pc.ThoiGianTao >= yesterday && pc.ThoiGianTao < today)
                .CountAsync();

            int tongChanDoan = todayDiagnoses.Count;
            int chanDoanChoVe = todayDiagnoses.Count(x => ContainsNormalizedKeyword(x.HuongXuTri, "cho ve"));
            int chanDoanTaiKham = todayDiagnoses.Count(x =>
                x.NgayTaiKham != null || ContainsNormalizedKeyword(x.HuongXuTri, "tai kham"));
            int chanDoanChoThuoc = todayDiagnoses.Count(x =>
                !string.IsNullOrWhiteSpace(x.MaDonThuoc) || ContainsNormalizedKeyword(x.HuongXuTri, "cho thuoc"));

            decimal diagnosisGrowth = 0;
            if (yesterdayDiagnosisCount > 0)
            {
                diagnosisGrowth = (decimal)(tongChanDoan - yesterdayDiagnosisCount) / yesterdayDiagnosisCount * 100m;
            }

            var diagnosisByHour = todayDiagnoses
                .GroupBy(x => x.ThoiGianTao.Hour)
                .Select(g => new TodayHourValueItemDto { Gio = g.Key, GiaTri = g.Count() })
                .OrderBy(x => x.Gio)
                .ToList();
            diagnosisByHour = EnsureFullDaySeries(diagnosisByHour);

            var chanDoanKpi = new TodayDiagnosisKpiDto
            {
                TongChanDoan = tongChanDoan,
                ChoVe = chanDoanChoVe,
                TaiKham = chanDoanTaiKham,
                ChoThuoc = chanDoanChoThuoc,
                TangTruongPhanTram = diagnosisGrowth,
                PhanBoTheoGio = diagnosisByHour
            };

            // KPI cho khối CLS: tổng số kết quả và phân loại theo tiến độ so với thời gian dự kiến.
            var resultBaseQuery =
                from kq in _db.KetQuaDichVus
                join ct in _db.ChiTietDichVus on kq.MaChiTietDv equals ct.MaChiTietDv
                join cls in _db.PhieuKhamCanLamSangs on ct.MaPhieuKhamCls equals cls.MaPhieuKhamCls
                join dv in _db.DichVuYTes on ct.MaDichVu equals dv.MaDichVu
                select new
                {
                    CompletedAt = kq.ThoiGianChot ?? kq.ThoiGianTao,
                    cls.NgayGioLap,
                    dv.ThoiGianDuKienPhut,
                    dv.MaPhongThucHien
                };
            if (scopeRooms != null)
            {
                resultBaseQuery = resultBaseQuery.Where(x => scopeRooms.Contains(x.MaPhongThucHien));
            }

            var todayResults = await resultBaseQuery
                .Where(x => x.CompletedAt >= today && x.CompletedAt < tomorrow)
                .ToListAsync();

            var yesterdayResultsCount = await resultBaseQuery
                .Where(x => x.CompletedAt >= yesterday && x.CompletedAt < today)
                .CountAsync();

            int tongKetQua = todayResults.Count;
            int ketQuaSom = 0;
            int ketQuaDungGio = 0;
            int ketQuaTre = 0;
            foreach (var item in todayResults)
            {
                var expectedAt = item.NgayGioLap.AddMinutes(item.ThoiGianDuKienPhut);
                switch (GetResultTimeliness(item.CompletedAt, expectedAt))
                {
                    case "som":
                        ketQuaSom++;
                        break;
                    case "tre":
                        ketQuaTre++;
                        break;
                    default:
                        ketQuaDungGio++;
                        break;
                }
            }

            decimal resultGrowth = 0;
            if (yesterdayResultsCount > 0)
            {
                resultGrowth = (decimal)(tongKetQua - yesterdayResultsCount) / yesterdayResultsCount * 100m;
            }

            var resultByHour = todayResults
                .GroupBy(x => x.CompletedAt.Hour)
                .Select(g => new TodayHourValueItemDto { Gio = g.Key, GiaTri = g.Count() })
                .OrderBy(x => x.Gio)
                .ToList();
            resultByHour = EnsureFullDaySeries(resultByHour);

            var ketQuaKpi = new TodayResultKpiDto
            {
                TongKetQua = tongKetQua,
                Som = ketQuaSom,
                DungGio = ketQuaDungGio,
                Tre = ketQuaTre,
                TangTruongPhanTram = resultGrowth,
                PhanBoTheoGio = resultByHour
            };

            var dichVuSapLam = await (
                from ct in _db.ChiTietDichVus
                join cls in _db.PhieuKhamCanLamSangs
                    on ct.MaPhieuKhamCls equals cls.MaPhieuKhamCls
                join ls in _db.PhieuKhamLamSangs
                    on cls.MaPhieuKhamLs equals ls.MaPhieuKham
                join bn in _db.BenhNhans
                    on ls.MaBenhNhan equals bn.MaBenhNhan
                join dv in _db.DichVuYTes
                    on ct.MaDichVu equals dv.MaDichVu
                where cls.NgayGioLap >= today && cls.NgayGioLap < tomorrow
                      && (ct.TrangThai == "da_lap" || ct.TrangThai == "dang_thuc_hien")
                      && (scopeRooms == null || scopeRooms.Contains(dv.MaPhongThucHien))
                orderby cls.NgayGioLap
                select new UpcomingServiceItemDto
                {
                    MaChiTietDV = ct.MaChiTietDv,
                    TenDichVu = dv.TenDichVu,
                    TenBenhNhan = bn.HoTen,
                    TrangThai = ct.TrangThai,
                    GioChiDinh = cls.NgayGioLap,
                    CoKetQua = ct.KetQuaDichVu != null
                }
            ).Take(20).ToListAsync();

            // ===== 9. DỊCH VỤ TĂNG MẠNH (cho Admin/HC) =====
            // Gồm cả LS (khám lâm sàng = 1 lượt/phiếu) và CLS (theo chi tiết dịch vụ)
            var trendingLs = todayLs
                .GroupBy(_ => "Khám lâm sàng")
                .Select(g => new TrendingServiceItemDto
                {
                    TenDichVu = g.Key,
                    LoaiDichVu = "ls",
                    SoLuong = g.Count()
                }).ToList();

            var trendingCls = await (
                from ct in _db.ChiTietDichVus
                join cls in _db.PhieuKhamCanLamSangs
                    on ct.MaPhieuKhamCls equals cls.MaPhieuKhamCls
                join dv in _db.DichVuYTes
                    on ct.MaDichVu equals dv.MaDichVu
                where cls.NgayGioLap >= today && cls.NgayGioLap < tomorrow
                      && (scopeRooms == null || scopeRooms.Contains(dv.MaPhongThucHien))
                group ct by new { dv.TenDichVu } into g
                select new TrendingServiceItemDto
                {
                    TenDichVu = g.Key.TenDichVu,
                    LoaiDichVu = "cls",
                    SoLuong = g.Count()
                }
            ).ToListAsync();

            var dichVuTangManh = trendingLs
                .Concat(trendingCls)
                .OrderByDescending(x => x.SoLuong)
                .Take(10)
                .ToList();

            return new DashboardTodayDto
            {
                Ngay = today,
                BenhNhanTrongNgay = benhNhanKpi,
                LichHenHomNay = lichHenKpi,
                DoanhThuHomNay = doanhThuKpi,
                LuotKhamHomNay = examKpi,
                BenhNhanLsHomNay = benhNhanLsKpi,
                BenhNhanClsHomNay = benhNhanClsKpi,
                LuotKhamLsHomNay = luotKhamLsKpi,
                LuotKhamClsHomNay = luotKhamClsKpi,
                DichVuHomNay = dichVuKpi,
                ChanDoanHomNay = chanDoanKpi,
                KetQuaHomNay = ketQuaKpi,
                DichVuSapLam = dichVuSapLam,
                DichVuTangManh = dichVuTangManh,
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

        // Chuẩn hóa text để so khớp các nhãn tiếng Việt như "Cho về", "Tái khám".
        private static string NormalizeSearchText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value
                .Replace('đ', 'd')
                .Replace('Đ', 'D')
                .Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(char.ToLowerInvariant(ch));
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static bool ContainsNormalizedKeyword(string? source, string keyword)
        {
            var normalizedSource = NormalizeSearchText(source);
            var normalizedKeyword = NormalizeSearchText(keyword);
            return !string.IsNullOrWhiteSpace(normalizedSource)
                && !string.IsNullOrWhiteSpace(normalizedKeyword)
                && normalizedSource.Contains(normalizedKeyword, StringComparison.Ordinal);
        }

        private static string ResolvePatientDailyStatus(IEnumerable<string?> statuses)
        {
            var normalized = statuses
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (normalized.Contains("da_lap") || normalized.Contains("dang_thuc_hien"))
            {
                return "cho_xu_ly";
            }

            if (normalized.Contains("da_hoan_tat"))
            {
                return "da_xu_ly";
            }

            if (normalized.Contains("da_huy"))
            {
                return "da_huy";
            }

            return "cho_xu_ly";
        }

        private static string GetResultTimeliness(DateTime actualAt, DateTime expectedAt)
        {
            const int toleranceMinutes = 5;
            var deltaMinutes = (actualAt - expectedAt).TotalMinutes;

            if (deltaMinutes < -toleranceMinutes)
            {
                return "som";
            }

            if (deltaMinutes > toleranceMinutes)
            {
                return "tre";
            }

            return "dung_gio";
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
