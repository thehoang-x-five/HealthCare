using BCrypt.Net;
using HealthCare.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthCare.Datas
{
    public static class DataSeed
    {
        private const string DefaultPassword = "P@ssw0rd";
        private const string AdminPassword = "Admin@123";
        private const string KtvPassword = "KTV@123";

        public static async Task EnsureSeedAsync(DataContext db)
        {
            if (await db.NhanVienYTes.AnyAsync()) return;

            var clock = new SeedClock(DateTime.Today);
            Console.WriteLine($">>> Starting demo seed for {clock.Today:yyyy-MM-dd}...");

            var departments = BuildDepartments();
            var staff = BuildStaff();
            var rooms = BuildRooms();
            var services = BuildServices();
            var medicines = BuildMedicines(clock);
            var patients = BuildPatients(clock);
            var schedules = BuildSchedules(clock);
            var appointments = BuildAppointments(clock);
            var clinicalForms = BuildClinicalForms(clock);
            var clinicalQueues = BuildClinicalQueues(clock);
            var clinicalVisits = BuildClinicalVisits(clock);
            var clsForms = BuildClsForms(clock);
            var clsItems = BuildClsItems();
            var clsQueues = BuildClsQueues(clock);
            var clsVisits = BuildClsVisits(clock);
            var results = BuildResults(clock);
            var summaries = BuildSummaries(clock);
            var diagnoses = BuildDiagnoses(clock);
            var prescriptions = BuildPrescriptions(clock);
            var prescriptionItems = BuildPrescriptionItems(clock);
            ApplyPrescriptionTotals(prescriptions, prescriptionItems);
            var invoices = BuildInvoices(clock, prescriptions);
            var stockLogs = BuildStockLogs(clock);
            var notificationTemplates = BuildNotificationTemplates(clock);
            var (notifications, recipients) = BuildNotifications(clock);
            var missingScheduleIds = appointments
                .Select(x => x.MaLichTruc)
                .Distinct()
                .Except(schedules.Select(x => x.MaLichTruc))
                .ToList();

            if (missingScheduleIds.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Seed appointments are referencing missing duty schedules: {string.Join(", ", missingScheduleIds)}");
            }

            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                async Task SaveBatchAsync(Action addEntities)
                {
                    addEntities();
                    await db.SaveChangesAsync();
                }

                await SaveBatchAsync(() =>
                {
                    db.KhoaChuyenMons.AddRange(departments);
                    db.NhanVienYTes.AddRange(staff);
                    db.Phongs.AddRange(rooms);
                    db.DichVuYTes.AddRange(services);
                    db.KhoThuocs.AddRange(medicines);
                    db.BenhNhans.AddRange(patients);
                    db.LichTrucs.AddRange(schedules);
                    db.ThongBaoMaus.AddRange(notificationTemplates);
                });

                await SaveBatchAsync(() => db.LichHenKhams.AddRange(appointments));
                await SaveBatchAsync(() => db.PhieuKhamLamSangs.AddRange(clinicalForms));
                await SaveBatchAsync(() => db.PhieuKhamCanLamSangs.AddRange(clsForms));
                await SaveBatchAsync(() => db.ChiTietDichVus.AddRange(clsItems));

                await SaveBatchAsync(() =>
                {
                    db.HangDois.AddRange(clinicalQueues);
                    db.HangDois.AddRange(clsQueues);
                });

                await SaveBatchAsync(() =>
                {
                    db.LuotKhamBenhs.AddRange(clinicalVisits);
                    db.LuotKhamBenhs.AddRange(clsVisits);
                });

                await SaveBatchAsync(() => db.KetQuaDichVus.AddRange(results));
                await SaveBatchAsync(() => db.PhieuTongHopKetQuas.AddRange(summaries));

                var completedLs = clinicalForms.FirstOrDefault(x => x.MaPhieuKham == "PK_DEMO_001");
                if (completedLs is not null)
                {
                    completedLs.MaPhieuKqKhamCls = "PTH_DEMO_001";
                    await db.SaveChangesAsync();
                }

                await SaveBatchAsync(() => db.DonThuocs.AddRange(prescriptions));
                await SaveBatchAsync(() => db.ChiTietDonThuocs.AddRange(prescriptionItems));
                await SaveBatchAsync(() => db.PhieuChanDoanCuois.AddRange(diagnoses));
                await SaveBatchAsync(() => db.HoaDonThanhToans.AddRange(invoices));
                await SaveBatchAsync(() => db.LichSuXuatKhos.AddRange(stockLogs));
                await SaveBatchAsync(() => db.ThongBaoHeThongs.AddRange(notifications));
                await SaveBatchAsync(() => db.ThongBaoNguoiNhans.AddRange(recipients));

                await transaction.CommitAsync();
                Console.WriteLine(">>> Demo seed completed successfully.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static List<KhoaChuyenMon> BuildDepartments() =>
            new()
            {
                new KhoaChuyenMon { MaKhoa = "KHOA_TQ", TenKhoa = "Tổng quát", DienThoai = "02873000001", Email = "tongquat@demo.local", DiaDiem = "Tầng 1", TrangThai = "hoat_dong", MoTa = "Tiếp nhận và điều phối chung" },
                new KhoaChuyenMon { MaKhoa = "KHOA_NOI", TenKhoa = "Nội tổng quát", DienThoai = "02873000002", Email = "noi@demo.local", DiaDiem = "Tầng 2", TrangThai = "hoat_dong", MoTa = "Khám và điều trị nội khoa" },
                new KhoaChuyenMon { MaKhoa = "KHOA_NGOAI", TenKhoa = "Ngoại tổng quát", DienThoai = "02873000003", Email = "ngoai@demo.local", DiaDiem = "Tầng 2", TrangThai = "hoat_dong", MoTa = "Khám ngoại khoa và tiểu phẫu" },
                new KhoaChuyenMon { MaKhoa = "KHOA_NHI", TenKhoa = "Nhi", DienThoai = "02873000007", Email = "nhi@demo.local", DiaDiem = "Tầng 2", TrangThai = "hoat_dong", MoTa = "Khám và theo dõi bệnh nhi" },
                new KhoaChuyenMon { MaKhoa = "KHOA_RHM", TenKhoa = "Răng Hàm Mặt", DienThoai = "02873000008", Email = "rhm@demo.local", DiaDiem = "Tầng 2", TrangThai = "hoat_dong", MoTa = "Khám răng, nha chu và tiểu phẫu" },
                new KhoaChuyenMon { MaKhoa = "KHOA_TMH", TenKhoa = "Tai Mũi Họng", DienThoai = "02873000009", Email = "tmh@demo.local", DiaDiem = "Tầng 2", TrangThai = "hoat_dong", MoTa = "Khám tai mũi họng và nội soi cơ bản" },
                new KhoaChuyenMon { MaKhoa = "KHOA_XN", TenKhoa = "Xét nghiệm", DienThoai = "02873000004", Email = "xn@demo.local", DiaDiem = "Tầng 3", TrangThai = "hoat_dong", MoTa = "Xét nghiệm huyết học và sinh hóa" },
                new KhoaChuyenMon { MaKhoa = "KHOA_CDHA", TenKhoa = "Chẩn đoán hình ảnh", DienThoai = "02873000005", Email = "cdha@demo.local", DiaDiem = "Tầng 3", TrangThai = "hoat_dong", MoTa = "Siêu âm và X-quang" },
                new KhoaChuyenMon { MaKhoa = "KHOA_DUOC", TenKhoa = "Dược", DienThoai = "02873000006", Email = "duoc@demo.local", DiaDiem = "Tầng 1", TrangThai = "hoat_dong", MoTa = "Kho và cấp phát thuốc" }
            };

        private static List<NhanVienYTe> BuildStaff()
        {
            var defaultHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);
            var adminHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword);
            var ktvHash = BCrypt.Net.BCrypt.HashPassword(KtvPassword);

            return new List<NhanVienYTe>
            {
                new NhanVienYTe { MaNhanVien = "NV_ADMIN_01", TenDangNhap = "admin", MatKhauHash = adminHash, HoTen = "Quản trị hệ thống", VaiTro = "admin", ChucVu = "admin", Email = "admin@demo.local", DienThoai = "0903000000", MaKhoa = "KHOA_TQ", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 8, MoTa = "Tài khoản admin demo" },
                new NhanVienYTe { MaNhanVien = "NV_YT_HC_01", TenDangNhap = "yt_hc01", MatKhauHash = defaultHash, HoTen = "Điều dưỡng Hành chính 01", VaiTro = "y_ta", LoaiYTa = "hanhchinh", ChucVu = "y_ta_hanh_chinh", Email = "yt_hc01@demo.local", DienThoai = "0903000001", MaKhoa = "KHOA_TQ", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 5, MoTa = "Tiếp nhận và thu ngân" },
                new NhanVienYTe { MaNhanVien = "NV_YT_HC_02", TenDangNhap = "yt_hc02", MatKhauHash = defaultHash, HoTen = "Điều dưỡng Hành chính 02", VaiTro = "y_ta", LoaiYTa = "hanhchinh", ChucVu = "y_ta_hanh_chinh", Email = "yt_hc02@demo.local", DienThoai = "0903000009", MaKhoa = "KHOA_TQ", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 4, MoTa = "Hỗ trợ lịch hẹn và thanh toán" },
                new NhanVienYTe { MaNhanVien = "NV_YT_HC_03", TenDangNhap = "yt_hc03", MatKhauHash = defaultHash, HoTen = "Điều dưỡng Hành chính 03", VaiTro = "y_ta", LoaiYTa = "hanhchinh", ChucVu = "y_ta_hanh_chinh", Email = "yt_hc03@demo.local", DienThoai = "0903000010", MaKhoa = "KHOA_TQ", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 3, MoTa = "Hỗ trợ hướng dẫn bệnh nhân" },

                new NhanVienYTe { MaNhanVien = "NV_BS_NOI_01", TenDangNhap = "bs_noi01", MatKhauHash = defaultHash, HoTen = "Bác sĩ Nội 01", VaiTro = "bac_si", ChucVu = "bac_si", HocVi = "BS CKI", ChuyenMon = "Nội tổng quát", Email = "bs_noi01@demo.local", DienThoai = "0903000002", MaKhoa = "KHOA_NOI", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 10 },
                new NhanVienYTe { MaNhanVien = "NV_BS_NOI_02", TenDangNhap = "bs_noi02", MatKhauHash = defaultHash, HoTen = "Bác sĩ Nội 02", VaiTro = "bac_si", ChucVu = "bac_si", HocVi = "ThS.BS", ChuyenMon = "Nội tổng quát", Email = "bs_noi02@demo.local", DienThoai = "0903000011", MaKhoa = "KHOA_NOI", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 8 },
                new NhanVienYTe { MaNhanVien = "NV_BS_NGOAI_01", TenDangNhap = "bs_ngoai01", MatKhauHash = defaultHash, HoTen = "Bác sĩ Ngoại 01", VaiTro = "bac_si", ChucVu = "bac_si", HocVi = "ThS.BS", ChuyenMon = "Ngoại tổng quát", Email = "bs_ngoai01@demo.local", DienThoai = "0903000003", MaKhoa = "KHOA_NGOAI", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 9 },
                new NhanVienYTe { MaNhanVien = "NV_BS_NGOAI_02", TenDangNhap = "bs_ngoai02", MatKhauHash = defaultHash, HoTen = "Bác sĩ Ngoại 02", VaiTro = "bac_si", ChucVu = "bac_si", HocVi = "BS CKI", ChuyenMon = "Ngoại tổng quát", Email = "bs_ngoai02@demo.local", DienThoai = "0903000012", MaKhoa = "KHOA_NGOAI", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 7 },
                new NhanVienYTe { MaNhanVien = "NV_BS_NHI_01", TenDangNhap = "bs_nhi01", MatKhauHash = defaultHash, HoTen = "Bác sĩ Nhi 01", VaiTro = "bac_si", ChucVu = "bac_si", HocVi = "BS CKI", ChuyenMon = "Nhi", Email = "bs_nhi01@demo.local", DienThoai = "0903000013", MaKhoa = "KHOA_NHI", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 8 },
                new NhanVienYTe { MaNhanVien = "NV_BS_RHM_01", TenDangNhap = "bs_rhm01", MatKhauHash = defaultHash, HoTen = "Bác sĩ Răng Hàm Mặt 01", VaiTro = "bac_si", ChucVu = "bac_si", HocVi = "BS CKI", ChuyenMon = "Răng hàm mặt", Email = "bs_rhm01@demo.local", DienThoai = "0903000014", MaKhoa = "KHOA_RHM", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 6 },
                new NhanVienYTe { MaNhanVien = "NV_BS_TMH_01", TenDangNhap = "bs_tmh01", MatKhauHash = defaultHash, HoTen = "Bác sĩ Tai Mũi Họng 01", VaiTro = "bac_si", ChucVu = "bac_si", HocVi = "BS CKI", ChuyenMon = "Tai mũi họng", Email = "bs_tmh01@demo.local", DienThoai = "0903000015", MaKhoa = "KHOA_TMH", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 7 },

                new NhanVienYTe { MaNhanVien = "NV_YT_LS_01", TenDangNhap = "yt_ls01", MatKhauHash = defaultHash, HoTen = "Điều dưỡng Lâm sàng Nội 01", VaiTro = "y_ta", LoaiYTa = "ls", ChucVu = "y_ta_lam_sang", Email = "yt_ls01@demo.local", DienThoai = "0903000004", MaKhoa = "KHOA_NOI", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 6 },
                new NhanVienYTe { MaNhanVien = "NV_YT_LS_02", TenDangNhap = "yt_ls02", MatKhauHash = defaultHash, HoTen = "Điều dưỡng Lâm sàng Ngoại 01", VaiTro = "y_ta", LoaiYTa = "ls", ChucVu = "y_ta_lam_sang", Email = "yt_ls02@demo.local", DienThoai = "0903000005", MaKhoa = "KHOA_NGOAI", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 4 },
                new NhanVienYTe { MaNhanVien = "NV_YT_LS_03", TenDangNhap = "yt_ls03", MatKhauHash = defaultHash, HoTen = "Điều dưỡng Lâm sàng Nội 02", VaiTro = "y_ta", LoaiYTa = "ls", ChucVu = "y_ta_lam_sang", Email = "yt_ls03@demo.local", DienThoai = "0903000016", MaKhoa = "KHOA_NOI", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 5 },
                new NhanVienYTe { MaNhanVien = "NV_YT_LS_04", TenDangNhap = "yt_ls04", MatKhauHash = defaultHash, HoTen = "Điều dưỡng Lâm sàng Ngoại 02", VaiTro = "y_ta", LoaiYTa = "ls", ChucVu = "y_ta_lam_sang", Email = "yt_ls04@demo.local", DienThoai = "0903000017", MaKhoa = "KHOA_NGOAI", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 4 },
                new NhanVienYTe { MaNhanVien = "NV_YT_LS_05", TenDangNhap = "yt_ls05", MatKhauHash = defaultHash, HoTen = "Điều dưỡng Lâm sàng Nhi 01", VaiTro = "y_ta", LoaiYTa = "ls", ChucVu = "y_ta_lam_sang", Email = "yt_ls05@demo.local", DienThoai = "0903000018", MaKhoa = "KHOA_NHI", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 5 },
                new NhanVienYTe { MaNhanVien = "NV_YT_LS_06", TenDangNhap = "yt_ls06", MatKhauHash = defaultHash, HoTen = "Điều dưỡng Lâm sàng Răng Hàm Mặt 01", VaiTro = "y_ta", LoaiYTa = "ls", ChucVu = "y_ta_lam_sang", Email = "yt_ls06@demo.local", DienThoai = "0903000019", MaKhoa = "KHOA_RHM", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 4 },
                new NhanVienYTe { MaNhanVien = "NV_YT_LS_07", TenDangNhap = "yt_ls07", MatKhauHash = defaultHash, HoTen = "Điều dưỡng Lâm sàng Tai Mũi Họng 01", VaiTro = "y_ta", LoaiYTa = "ls", ChucVu = "y_ta_lam_sang", Email = "yt_ls07@demo.local", DienThoai = "0903000020", MaKhoa = "KHOA_TMH", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 4 },

                new NhanVienYTe { MaNhanVien = "NV_YT_CLS_01", TenDangNhap = "yt_cls01", MatKhauHash = defaultHash, HoTen = "Điều dưỡng Cận lâm sàng 01", VaiTro = "y_ta", LoaiYTa = "cls", ChucVu = "y_ta_can_lam_sang", Email = "yt_cls01@demo.local", DienThoai = "0903000006", MaKhoa = "KHOA_XN", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 4 },
                new NhanVienYTe { MaNhanVien = "NV_YT_CLS_02", TenDangNhap = "yt_cls02", MatKhauHash = defaultHash, HoTen = "Điều dưỡng Cận lâm sàng 02", VaiTro = "y_ta", LoaiYTa = "cls", ChucVu = "y_ta_can_lam_sang", Email = "yt_cls02@demo.local", DienThoai = "0903000021", MaKhoa = "KHOA_CDHA", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 5 },
                new NhanVienYTe { MaNhanVien = "NV_KTV_XN_01", TenDangNhap = "ktv_xn_01", MatKhauHash = ktvHash, HoTen = "Kỹ thuật viên Xét nghiệm 01", VaiTro = "ky_thuat_vien", ChucVu = "ky_thuat_vien", HocVi = "KTV XN", ChuyenMon = "Xét nghiệm", Email = "ktv_xn_01@demo.local", DienThoai = "0903000007", MaKhoa = "KHOA_XN", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 5, MoTa = "Phụ trách phòng xét nghiệm 01 và hỗ trợ lấy mẫu buổi sáng." },
                new NhanVienYTe { MaNhanVien = "NV_KTV_XN_02", TenDangNhap = "ktv_xn_02", MatKhauHash = ktvHash, HoTen = "Kỹ thuật viên Xét nghiệm 02", VaiTro = "ky_thuat_vien", ChucVu = "ky_thuat_vien", HocVi = "KTV XN", ChuyenMon = "Sinh hóa", Email = "ktv_xn_02@demo.local", DienThoai = "0903000022", MaKhoa = "KHOA_XN", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 4, MoTa = "Theo dõi máy sinh hóa và phụ trách phòng xét nghiệm 02." },
                new NhanVienYTe { MaNhanVien = "NV_KTV_CDH_01", TenDangNhap = "ktv_cdha_01", MatKhauHash = ktvHash, HoTen = "Kỹ thuật viên Chẩn đoán hình ảnh 01", VaiTro = "ky_thuat_vien", ChucVu = "ky_thuat_vien", HocVi = "KTV CĐHA", ChuyenMon = "Chẩn đoán hình ảnh", Email = "ktv_cdha_01@demo.local", DienThoai = "0903000008", MaKhoa = "KHOA_CDHA", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 6, MoTa = "Vận hành phòng chẩn đoán hình ảnh tổng hợp và phối hợp đọc phim." },
                new NhanVienYTe { MaNhanVien = "NV_KTV_CDH_02", TenDangNhap = "ktv_cdha_02", MatKhauHash = ktvHash, HoTen = "Kỹ thuật viên Chẩn đoán hình ảnh 02", VaiTro = "ky_thuat_vien", ChucVu = "ky_thuat_vien", HocVi = "KTV CĐHA", ChuyenMon = "X-quang", Email = "ktv_cdha_02@demo.local", DienThoai = "0903000023", MaKhoa = "KHOA_CDHA", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 4, MoTa = "Phụ trách vận hành phòng X-quang và xử lý hình ảnh kỹ thuật số." },
                new NhanVienYTe { MaNhanVien = "NV_KTV_SA_01", TenDangNhap = "ktv_sa_01", MatKhauHash = ktvHash, HoTen = "Kỹ thuật viên Siêu âm 01", VaiTro = "ky_thuat_vien", ChucVu = "ky_thuat_vien", HocVi = "KTV SA", ChuyenMon = "Siêu âm", Email = "ktv_sa_01@demo.local", DienThoai = "0903000024", MaKhoa = "KHOA_CDHA", TrangThaiCongTac = "dang_cong_tac", TrangThaiTaiKhoan = "hoat_dong", SoNamKinhNghiem = 5, MoTa = "Chuyên thực hiện siêu âm bụng, tuyến giáp và hỗ trợ kết quả CLS." }
            };
        }

        private static List<Phong> BuildRooms() =>
            new()
            {
                new Phong { MaPhong = "PK_NOI_01", TenPhong = "Phòng khám Nội 01", MaKhoa = "KHOA_NOI", LoaiPhong = "phong_kham_ls", MaBacSiPhuTrach = "NV_BS_NOI_01", SucChua = 20, ViTri = "Tầng 2 - A1", Email = "pk_noi_01@demo.local", DienThoai = "02873000101", GioMoCua = Time(8, 0), GioDongCua = Time(21, 0), TrangThai = "hoat_dong", ThietBi = new List<string> { "Máy đo huyết áp", "Máy đo SpO2", "Cân sức khỏe" } },
                new Phong { MaPhong = "PK_NOI_02", TenPhong = "Phòng khám Nội 02", MaKhoa = "KHOA_NOI", LoaiPhong = "phong_kham_ls", MaBacSiPhuTrach = "NV_BS_NOI_02", SucChua = 18, ViTri = "Tầng 2 - A2", Email = "pk_noi_02@demo.local", DienThoai = "02873000105", GioMoCua = Time(8, 0), GioDongCua = Time(21, 0), TrangThai = "hoat_dong", ThietBi = new List<string> { "Máy ECG mini", "Máy đo đường huyết", "Máy khí dung" } },
                new Phong { MaPhong = "PK_NGOAI_01", TenPhong = "Phòng khám Ngoại 01", MaKhoa = "KHOA_NGOAI", LoaiPhong = "phong_kham_ls", MaBacSiPhuTrach = "NV_BS_NGOAI_01", SucChua = 20, ViTri = "Tầng 2 - B1", Email = "pk_ngoai_01@demo.local", DienThoai = "02873000102", GioMoCua = Time(8, 0), GioDongCua = Time(21, 0), TrangThai = "hoat_dong", ThietBi = new List<string> { "Bộ tiểu phẫu", "Máy đo nhiệt độ", "Máy siêu âm mini" } },
                new Phong { MaPhong = "PK_NGOAI_02", TenPhong = "Phòng khám Ngoại 02", MaKhoa = "KHOA_NGOAI", LoaiPhong = "phong_kham_ls", MaBacSiPhuTrach = "NV_BS_NGOAI_02", SucChua = 18, ViTri = "Tầng 2 - B2", Email = "pk_ngoai_02@demo.local", DienThoai = "02873000106", GioMoCua = Time(8, 0), GioDongCua = Time(21, 0), TrangThai = "hoat_dong", ThietBi = new List<string> { "Đèn tiểu phẫu", "Bộ thay băng", "Máy monitor" } },
                new Phong { MaPhong = "PK_NHI_01", TenPhong = "Phòng khám Nhi 01", MaKhoa = "KHOA_NHI", LoaiPhong = "phong_kham_ls", MaBacSiPhuTrach = "NV_BS_NHI_01", SucChua = 16, ViTri = "Tầng 2 - C1", Email = "pk_nhi_01@demo.local", DienThoai = "02873000107", GioMoCua = Time(8, 0), GioDongCua = Time(21, 0), TrangThai = "hoat_dong", ThietBi = new List<string> { "Máy khí dung nhi", "Cân trẻ em", "Máy xông mũi họng" } },
                new Phong { MaPhong = "PK_RHM_01", TenPhong = "Phòng khám Răng Hàm Mặt 01", MaKhoa = "KHOA_RHM", LoaiPhong = "phong_kham_ls", MaBacSiPhuTrach = "NV_BS_RHM_01", SucChua = 12, ViTri = "Tầng 2 - C2", Email = "pk_rhm_01@demo.local", DienThoai = "02873000108", GioMoCua = Time(8, 0), GioDongCua = Time(21, 0), TrangThai = "hoat_dong", ThietBi = new List<string> { "Ghế nha khoa", "Máy chụp phim răng", "Bộ dụng cụ nha khoa" } },
                new Phong { MaPhong = "PK_TMH_01", TenPhong = "Phòng khám Tai Mũi Họng 01", MaKhoa = "KHOA_TMH", LoaiPhong = "phong_kham_ls", MaBacSiPhuTrach = "NV_BS_TMH_01", SucChua = 12, ViTri = "Tầng 2 - C3", Email = "pk_tmh_01@demo.local", DienThoai = "02873000109", GioMoCua = Time(8, 0), GioDongCua = Time(21, 0), TrangThai = "hoat_dong", ThietBi = new List<string> { "Đèn nội soi TMH", "Máy hút dịch", "Máy khí dung" } },
                new Phong { MaPhong = "CLS_XN_01", TenPhong = "Phòng Xét nghiệm 01", MaKhoa = "KHOA_XN", LoaiPhong = "phong_cls", MaKTVPhuTrach = "NV_KTV_XN_01", SucChua = 12, ViTri = "Tầng 3 - A1", Email = "cls_xn_01@demo.local", DienThoai = "02873000103", GioMoCua = Time(7, 30), GioDongCua = Time(20, 30), TrangThai = "hoat_dong", ThietBi = new List<string> { "Máy huyết học", "Máy sinh hóa", "Máy ly tâm" } },
                new Phong { MaPhong = "CLS_XN_02", TenPhong = "Phòng Xét nghiệm 02", MaKhoa = "KHOA_XN", LoaiPhong = "phong_cls", MaKTVPhuTrach = "NV_KTV_XN_02", SucChua = 10, ViTri = "Tầng 3 - A2", Email = "cls_xn_02@demo.local", DienThoai = "02873000110", GioMoCua = Time(7, 30), GioDongCua = Time(20, 30), TrangThai = "hoat_dong", ThietBi = new List<string> { "Máy sinh hóa tự động", "Máy nước tiểu", "Máy đông máu" } },
                new Phong { MaPhong = "CLS_CDH_01", TenPhong = "Phòng Chẩn đoán hình ảnh 01", MaKhoa = "KHOA_CDHA", LoaiPhong = "phong_cls", MaKTVPhuTrach = "NV_KTV_CDH_01", SucChua = 12, ViTri = "Tầng 3 - B1", Email = "cls_cdh_01@demo.local", DienThoai = "02873000104", GioMoCua = Time(7, 30), GioDongCua = Time(20, 30), TrangThai = "hoat_dong", ThietBi = new List<string> { "Máy X-quang", "Máy siêu âm", "Hệ thống PACS" } },
                new Phong { MaPhong = "CLS_XQ_01", TenPhong = "Phòng X-quang 01", MaKhoa = "KHOA_CDHA", LoaiPhong = "phong_cls", MaKTVPhuTrach = "NV_KTV_CDH_02", SucChua = 10, ViTri = "Tầng 3 - B2", Email = "cls_xq_01@demo.local", DienThoai = "02873000111", GioMoCua = Time(7, 30), GioDongCua = Time(20, 30), TrangThai = "hoat_dong", ThietBi = new List<string> { "Máy X-quang kỹ thuật số", "Buồng chụp", "Bộ đọc phim" } },
                new Phong { MaPhong = "CLS_SA_01", TenPhong = "Phòng Siêu âm 01", MaKhoa = "KHOA_CDHA", LoaiPhong = "phong_cls", MaKTVPhuTrach = "NV_KTV_SA_01", SucChua = 10, ViTri = "Tầng 3 - B3", Email = "cls_sa_01@demo.local", DienThoai = "02873000112", GioMoCua = Time(7, 30), GioDongCua = Time(20, 30), TrangThai = "hoat_dong", ThietBi = new List<string> { "Máy siêu âm tổng quát", "Đầu dò bụng", "Đầu dò tuyến giáp" } }
            };

        private static List<DichVuYTe> BuildServices() =>
            new()
            {
                new DichVuYTe { MaDichVu = "DV_KHAM_NOI_01", LoaiDichVu = "kham_lam_sang", TenDichVu = "Khám nội tổng quát", DonGia = 150000m, ThoiGianDuKienPhut = 20, MaPhongThucHien = "PK_NOI_01" },
                new DichVuYTe { MaDichVu = "DV_KHAM_NOI_02", LoaiDichVu = "kham_lam_sang", TenDichVu = "Khám nội tổng quát nâng cao", DonGia = 180000m, ThoiGianDuKienPhut = 25, MaPhongThucHien = "PK_NOI_02" },
                new DichVuYTe { MaDichVu = "DV_KHAM_NGOAI_01", LoaiDichVu = "kham_lam_sang", TenDichVu = "Khám ngoại tổng quát", DonGia = 180000m, ThoiGianDuKienPhut = 20, MaPhongThucHien = "PK_NGOAI_01" },
                new DichVuYTe { MaDichVu = "DV_KHAM_NGOAI_02", LoaiDichVu = "kham_lam_sang", TenDichVu = "Khám ngoại tiểu phẫu", DonGia = 220000m, ThoiGianDuKienPhut = 25, MaPhongThucHien = "PK_NGOAI_02" },
                new DichVuYTe { MaDichVu = "DV_KHAM_NHI_01", LoaiDichVu = "kham_lam_sang", TenDichVu = "Khám nhi tổng quát", DonGia = 160000m, ThoiGianDuKienPhut = 20, MaPhongThucHien = "PK_NHI_01" },
                new DichVuYTe { MaDichVu = "DV_KHAM_RHM_01", LoaiDichVu = "kham_lam_sang", TenDichVu = "Khám răng hàm mặt", DonGia = 200000m, ThoiGianDuKienPhut = 30, MaPhongThucHien = "PK_RHM_01" },
                new DichVuYTe { MaDichVu = "DV_KHAM_TMH_01", LoaiDichVu = "kham_lam_sang", TenDichVu = "Khám tai mũi họng", DonGia = 190000m, ThoiGianDuKienPhut = 25, MaPhongThucHien = "PK_TMH_01" },
                new DichVuYTe { MaDichVu = "DV_XN_CBC_01", LoaiDichVu = "can_lam_sang", TenDichVu = "Công thức máu", DonGia = 120000m, ThoiGianDuKienPhut = 15, MaPhongThucHien = "CLS_XN_01" },
                new DichVuYTe { MaDichVu = "DV_XN_SH_01", LoaiDichVu = "can_lam_sang", TenDichVu = "Sinh hóa máu cơ bản", DonGia = 180000m, ThoiGianDuKienPhut = 20, MaPhongThucHien = "CLS_XN_02" },
                new DichVuYTe { MaDichVu = "DV_XN_NT_01", LoaiDichVu = "can_lam_sang", TenDichVu = "Tổng phân tích nước tiểu", DonGia = 90000m, ThoiGianDuKienPhut = 15, MaPhongThucHien = "CLS_XN_02" },
                new DichVuYTe { MaDichVu = "DV_XQ_NGUC_01", LoaiDichVu = "can_lam_sang", TenDichVu = "X-quang ngực", DonGia = 220000m, ThoiGianDuKienPhut = 20, MaPhongThucHien = "CLS_XQ_01" },
                new DichVuYTe { MaDichVu = "DV_XQ_XUONG_01", LoaiDichVu = "can_lam_sang", TenDichVu = "X-quang xương chi", DonGia = 240000m, ThoiGianDuKienPhut = 20, MaPhongThucHien = "CLS_XQ_01" },
                new DichVuYTe { MaDichVu = "DV_SIEUAM_BUNG_01", LoaiDichVu = "can_lam_sang", TenDichVu = "Siêu âm bụng", DonGia = 300000m, ThoiGianDuKienPhut = 25, MaPhongThucHien = "CLS_SA_01" },
                new DichVuYTe { MaDichVu = "DV_SIEUAM_TUYEN_GIAP_01", LoaiDichVu = "can_lam_sang", TenDichVu = "Siêu âm tuyến giáp", DonGia = 260000m, ThoiGianDuKienPhut = 20, MaPhongThucHien = "CLS_SA_01" }
            };

        private static List<KhoThuoc> BuildMedicines(SeedClock clock) =>
            new()
            {
                new KhoThuoc { MaThuoc = "THUOC_001", TenThuoc = "Paracetamol 500mg", DonViTinh = "viên", CongDung = "Hạ sốt, giảm đau", GiaNiemYet = 2500m, SoLuongTon = 180, HanSuDung = clock.Today.AddMonths(18), SoLo = "LO-PARA-01", TrangThai = "hoat_dong" },
                new KhoThuoc { MaThuoc = "THUOC_002", TenThuoc = "Cetirizine 10mg", DonViTinh = "viên", CongDung = "Chống dị ứng", GiaNiemYet = 3000m, SoLuongTon = 110, HanSuDung = clock.Today.AddMonths(16), SoLo = "LO-CETI-01", TrangThai = "hoat_dong" },
                new KhoThuoc { MaThuoc = "THUOC_003", TenThuoc = "Omeprazole 20mg", DonViTinh = "viên", CongDung = "Giảm tiết acid", GiaNiemYet = 4000m, SoLuongTon = 95, HanSuDung = clock.Today.AddMonths(14), SoLo = "LO-OME-01", TrangThai = "hoat_dong" },
                new KhoThuoc { MaThuoc = "THUOC_004", TenThuoc = "Domperidone 10mg", DonViTinh = "viên", CongDung = "Giảm buồn nôn", GiaNiemYet = 3500m, SoLuongTon = 90, HanSuDung = clock.Today.AddMonths(12), SoLo = "LO-DOM-01", TrangThai = "hoat_dong" },
                new KhoThuoc { MaThuoc = "THUOC_005", TenThuoc = "Vitamin C 500mg", DonViTinh = "viên", CongDung = "Bổ sung vitamin", GiaNiemYet = 2000m, SoLuongTon = 40, HanSuDung = clock.Today.AddMonths(10), SoLo = "LO-VITC-01", TrangThai = "sap_het_ton" },
                new KhoThuoc { MaThuoc = "THUOC_006", TenThuoc = "Amoxicillin 500mg", DonViTinh = "viên", CongDung = "Kháng sinh đường hô hấp", GiaNiemYet = 5000m, SoLuongTon = 120, HanSuDung = clock.Today.AddMonths(15), SoLo = "LO-AMOX-01", TrangThai = "hoat_dong" },
                new KhoThuoc { MaThuoc = "THUOC_007", TenThuoc = "Alpha Chymotrypsin", DonViTinh = "viên", CongDung = "Giảm phù nề viêm họng", GiaNiemYet = 4500m, SoLuongTon = 100, HanSuDung = clock.Today.AddMonths(11), SoLo = "LO-ALPHA-01", TrangThai = "hoat_dong" },
                new KhoThuoc { MaThuoc = "THUOC_008", TenThuoc = "Oresol", DonViTinh = "gói", CongDung = "Bù nước điện giải", GiaNiemYet = 3500m, SoLuongTon = 80, HanSuDung = clock.Today.AddMonths(20), SoLo = "LO-ORE-01", TrangThai = "hoat_dong" }
            };

        private static List<BenhNhan> BuildPatients(SeedClock clock)
        {
            var patients = new List<BenhNhan>
            {
                new BenhNhan { MaBenhNhan = "BN_PARENT_01", HoTen = "Nguyễn Văn Bố", NgaySinh = clock.Today.AddYears(-58), GioiTinh = "Nam", DienThoai = "0911000001", Email = "bo.demo@demo.local", DiaChi = "Quận 1, TP HCM", CCCD = "079111111111", TieuSuBenh = "Tăng huyết áp", BenhManTinh = "tang_huyet_ap", NhomMau = "O+", TrangThaiTaiKhoan = "hoat_dong", NgayTrangThai = clock.Today, NgayTao = clock.Yesterday.AddDays(-30), NgayCapNhat = clock.Today },
                new BenhNhan { MaBenhNhan = "BN_PARENT_02", HoTen = "Trần Thị Mẹ", NgaySinh = clock.Today.AddYears(-55), GioiTinh = "Nữ", DienThoai = "0911000002", Email = "me.demo@demo.local", DiaChi = "Quận 1, TP HCM", CCCD = "079222222222", TieuSuBenh = "Rối loạn mỡ máu", BenhManTinh = "mo_mau_cao", NhomMau = "A+", TrangThaiTaiKhoan = "hoat_dong", NgayTrangThai = clock.Today, NgayTao = clock.Yesterday.AddDays(-30), NgayCapNhat = clock.Today },
                new BenhNhan { MaBenhNhan = "BN_DEMO_01", HoTen = "Lê Minh An", NgaySinh = clock.Today.AddYears(-29), GioiTinh = "Nam", DienThoai = "0911000003", Email = "an.demo@demo.local", DiaChi = "Thủ Đức, TP HCM", MaCha = "BN_PARENT_01", MaMe = "BN_PARENT_02", CCCD = "079333333333", DiUng = "Penicillin", ThuocDangDung = "Vitamin tổng hợp", TieuSuBenh = "Hen phế quản nhẹ", NhomMau = "O+", BenhManTinh = "hen_phe_quan", SinhHieu = "{\"nhiet_do\":36.8,\"huyet_ap\":\"118/76\",\"mach\":76}", TrangThaiTaiKhoan = "hoat_dong", TrangThaiHomNay = "hoan_thanh", NgayTrangThai = clock.Today, NgayTao = clock.Yesterday.AddDays(-12), NgayCapNhat = clock.Today },
                new BenhNhan { MaBenhNhan = "BN_DEMO_02", HoTen = "Phạm Gia Bảo", NgaySinh = clock.Today.AddYears(-34), GioiTinh = "Nam", DienThoai = "0911000004", Email = "bao.demo@demo.local", DiaChi = "Gò Vấp, TP HCM", CCCD = "079444444444", TieuSuBenh = "Viêm dạ dày", NhomMau = "B+", TrangThaiTaiKhoan = "hoat_dong", TrangThaiHomNay = "cho_kham", NgayTrangThai = clock.Today, NgayTao = clock.Yesterday.AddDays(-8), NgayCapNhat = clock.Today },
                new BenhNhan { MaBenhNhan = "BN_DEMO_03", HoTen = "Trần Thu Chi", NgaySinh = clock.Today.AddYears(-41), GioiTinh = "Nữ", DienThoai = "0911000005", Email = "chi.demo@demo.local", DiaChi = "Bình Thạnh, TP HCM", CCCD = "079555555555", ChongChiDinh = "Không dùng thuốc cản quang", TieuSuBenh = "Đau bụng kéo dài", NhomMau = "AB+", TrangThaiTaiKhoan = "hoat_dong", TrangThaiHomNay = "cho_xu_ly", NgayTrangThai = clock.Today, NgayTao = clock.Yesterday.AddDays(-6), NgayCapNhat = clock.Today },
                new BenhNhan { MaBenhNhan = "BN_DEMO_04", HoTen = "Hoàng Minh Dũng", NgaySinh = clock.Today.AddYears(-37), GioiTinh = "Nam", DienThoai = "0911000006", Email = "dung.demo@demo.local", DiaChi = "Tân Bình, TP HCM", CCCD = "079666666666", ThuocDangDung = "Men vi sinh", TieuSuBenh = "Rối loạn tiêu hóa", NhomMau = "A+", TrangThaiTaiKhoan = "hoat_dong", TrangThaiHomNay = "hoan_thanh", NgayTrangThai = clock.Today, NgayTao = clock.Yesterday.AddDays(-4), NgayCapNhat = clock.Today },
                new BenhNhan { MaBenhNhan = "BN_DEMO_05", HoTen = "Võ Thanh Em", NgaySinh = clock.Today.AddYears(-25), GioiTinh = "Nữ", DienThoai = "0911000007", Email = "em.demo@demo.local", DiaChi = "Quận 7, TP HCM", CCCD = "079777777777", TrangThaiTaiKhoan = "hoat_dong", TrangThaiHomNay = "huy", NgayTrangThai = clock.Today, NgayTao = clock.Yesterday.AddDays(-3), NgayCapNhat = clock.Today },
                new BenhNhan { MaBenhNhan = "BN_DEMO_06", HoTen = "Đỗ Quốc Gia", NgaySinh = clock.Today.AddYears(-31), GioiTinh = "Nam", DienThoai = "0911000008", Email = "gia.demo@demo.local", DiaChi = "Quận 10, TP HCM", CCCD = "079888888888", TieuSuBenh = "Tái khám sau điều trị", TrangThaiTaiKhoan = "hoat_dong", NgayTrangThai = clock.Today, NgayTao = clock.Yesterday.AddDays(-2), NgayCapNhat = clock.Today },
                new BenhNhan { MaBenhNhan = "BN_DEMO_07", HoTen = "Nguyễn Bảo Châu", NgaySinh = clock.Today.AddYears(-9), GioiTinh = "Nữ", DienThoai = "0911000009", Email = "chau.demo@demo.local", DiaChi = "Quận 3, TP HCM", CCCD = "079999999999", TieuSuBenh = "Viêm mũi dị ứng", TrangThaiTaiKhoan = "hoat_dong", TrangThaiHomNay = "cho_tiep_nhan", NgayTrangThai = clock.Today, NgayTao = clock.Yesterday.AddDays(-5), NgayCapNhat = clock.Today },
                new BenhNhan { MaBenhNhan = "BN_DEMO_08", HoTen = "Trần Đức Huy", NgaySinh = clock.Today.AddYears(-27), GioiTinh = "Nam", DienThoai = "0911000010", Email = "huy.demo@demo.local", DiaChi = "Quận 5, TP HCM", CCCD = "080000000000", TieuSuBenh = "Đau răng hàm dưới", TrangThaiTaiKhoan = "hoat_dong", TrangThaiHomNay = "cho_tiep_nhan", NgayTrangThai = clock.Today, NgayTao = clock.Yesterday.AddDays(-4), NgayCapNhat = clock.Today },
                new BenhNhan { MaBenhNhan = "BN_DEMO_09", HoTen = "Phan Quỳnh Như", NgaySinh = clock.Today.AddYears(-22), GioiTinh = "Nữ", DienThoai = "0911000011", Email = "nhu.demo@demo.local", DiaChi = "Quận 8, TP HCM", CCCD = "080111111111", TieuSuBenh = "Đau họng tái phát", TrangThaiTaiKhoan = "hoat_dong", TrangThaiHomNay = "cho_tiep_nhan", NgayTrangThai = clock.Today, NgayTao = clock.Yesterday.AddDays(-3), NgayCapNhat = clock.Today },
                new BenhNhan { MaBenhNhan = "BN_DEMO_10", HoTen = "Lê Hoàng Vy", NgaySinh = clock.Today.AddYears(-45), GioiTinh = "Nữ", DienThoai = "0911000012", Email = "vy.demo@demo.local", DiaChi = "Tân Phú, TP HCM", CCCD = "080222222222", TieuSuBenh = "Tái khám xét nghiệm đường huyết", TrangThaiTaiKhoan = "hoat_dong", TrangThaiHomNay = "cho_tiep_nhan", NgayTrangThai = clock.Today, NgayTao = clock.Yesterday.AddDays(-2), NgayCapNhat = clock.Today }
            };

            patients.AddRange(BuildReportPatients(clock));
            return patients;
        }

        private static IEnumerable<BenhNhan> BuildReportPatients(SeedClock clock)
        {
            var seeds = GetReportPatientSeeds();

            return seeds.Select((seed, index) =>
            {
                var createdAt = clock.Today.AddDays(seed.CreatedOffsetDays);
                var updatedAt = createdAt.AddDays(3);

                return new BenhNhan
                {
                    MaBenhNhan = seed.MaBenhNhan,
                    HoTen = seed.HoTen,
                    NgaySinh = clock.Today.AddYears(-seed.Tuoi).AddDays(-(index * 17 % 240)),
                    GioiTinh = seed.GioiTinh,
                    DienThoai = seed.DienThoai,
                    Email = seed.Email,
                    DiaChi = seed.DiaChi,
                    CCCD = $"081{index + 1:000000000}",
                    TieuSuBenh = seed.TieuSuBenh,
                    NhomMau = seed.NhomMau,
                    TrangThaiTaiKhoan = "hoat_dong",
                    NgayTrangThai = clock.Today,
                    NgayTao = createdAt,
                    NgayCapNhat = updatedAt > clock.Today ? clock.Today : updatedAt
                };
            });
        }

        private static List<LichTruc> BuildSchedules(SeedClock clock)
        {
            var schedules = new List<LichTruc>
            {
                new LichTruc { MaLichTruc = "LT_PK_NOI_YD_AM", Ngay = clock.Yesterday, CaTruc = "sang", GioBatDau = Time(8, 0), GioKetThuc = Time(12, 0), MaYTaTruc = "NV_YT_LS_01", MaPhong = "PK_NOI_01" },
                new LichTruc { MaLichTruc = "LT_PK_NGOAI_YD_PM", Ngay = clock.Yesterday, CaTruc = "chieu", GioBatDau = Time(13, 0), GioKetThuc = Time(17, 0), MaYTaTruc = "NV_YT_LS_02", MaPhong = "PK_NGOAI_01" },
                new LichTruc { MaLichTruc = "LT_PK_NOI_TD_AM", Ngay = clock.Today, CaTruc = "sang", GioBatDau = Time(8, 0), GioKetThuc = Time(12, 0), MaYTaTruc = "NV_YT_LS_01", MaPhong = "PK_NOI_01" },
                new LichTruc { MaLichTruc = "LT_PK_NGOAI_TD_AM", Ngay = clock.Today, CaTruc = "sang", GioBatDau = Time(8, 0), GioKetThuc = Time(12, 0), MaYTaTruc = "NV_YT_LS_02", MaPhong = "PK_NGOAI_01" },
                new LichTruc { MaLichTruc = "LT_XN_TD_AM", Ngay = clock.Today, CaTruc = "sang", GioBatDau = Time(7, 30), GioKetThuc = Time(11, 30), MaYTaTruc = "NV_KTV_XN_01", MaPhong = "CLS_XN_01" },
                new LichTruc { MaLichTruc = "LT_CDH_TD_AM", Ngay = clock.Today, CaTruc = "sang", GioBatDau = Time(7, 30), GioKetThuc = Time(11, 30), MaYTaTruc = "NV_KTV_CDH_01", MaPhong = "CLS_CDH_01" },
                new LichTruc { MaLichTruc = "LT_PK_NOI_TM_AM", Ngay = clock.Tomorrow, CaTruc = "sang", GioBatDau = Time(8, 0), GioKetThuc = Time(12, 0), MaYTaTruc = "NV_YT_LS_03", MaPhong = "PK_NOI_01" },
                new LichTruc { MaLichTruc = "LT_PK_NGOAI_TM_AM", Ngay = clock.Tomorrow, CaTruc = "sang", GioBatDau = Time(8, 0), GioKetThuc = Time(12, 0), MaYTaTruc = "NV_YT_LS_04", MaPhong = "PK_NGOAI_01" }
            };

            var roomIds = new[]
            {
                "PK_NOI_01", "PK_NOI_02", "PK_NGOAI_01", "PK_NGOAI_02",
                "PK_NHI_01", "PK_RHM_01", "PK_TMH_01",
                "CLS_XN_01", "CLS_XN_02", "CLS_CDH_01", "CLS_XQ_01", "CLS_SA_01"
            };

            foreach (var date in GetFullWeekDates(clock))
            {
                foreach (var roomId in roomIds)
                {
                    AddGeneratedSchedule(schedules, roomId, date, "sang");
                    AddGeneratedSchedule(schedules, roomId, date, "chieu");
                    AddGeneratedSchedule(schedules, roomId, date, "toi");
                }
            }

            return schedules;
        }

        private static List<LichHenKham> BuildAppointments(SeedClock clock)
        {
            var appointments = new List<LichHenKham>
            {
                new LichHenKham { MaLichHen = "LH_DEMO_001", MaBenhNhan = "BN_DEMO_01", TenBenhNhan = "Lê Minh An", SoDienThoai = "0911000003", LoaiHen = "tai_kham", NgayHen = clock.Yesterday, GioHen = Time(8, 30), ThoiLuongPhut = 30, MaLichTruc = "LT_PK_NOI_YD_AM", GhiChu = "Tái khám sau đợt điều trị trước", TrangThai = "da_checkin" },
                new LichHenKham { MaLichHen = "LH_DEMO_002", MaBenhNhan = "BN_DEMO_02", TenBenhNhan = "Phạm Gia Bảo", SoDienThoai = "0911000004", LoaiHen = "kham_moi", NgayHen = clock.Today, GioHen = Time(8, 45), ThoiLuongPhut = 30, MaLichTruc = "LT_PK_NOI_TD_AM", GhiChu = "Chờ thanh toán trước khi vào khám", TrangThai = "da_checkin" },
                new LichHenKham { MaLichHen = "LH_DEMO_003", MaBenhNhan = "BN_DEMO_03", TenBenhNhan = "Trần Thu Chi", SoDienThoai = "0911000005", LoaiHen = "kham_moi", NgayHen = clock.Today, GioHen = Time(9, 10), ThoiLuongPhut = 30, MaLichTruc = "LT_PK_NOI_TD_AM", GhiChu = "Đã tiếp nhận, đang chờ xử lý CLS", TrangThai = "da_checkin" },
                new LichHenKham { MaLichHen = "LH_DEMO_005", MaBenhNhan = "BN_DEMO_05", TenBenhNhan = "Võ Thanh Em", SoDienThoai = "0911000007", LoaiHen = "kham_moi", NgayHen = clock.Today, GioHen = Time(10, 0), ThoiLuongPhut = 30, MaLichTruc = "LT_PK_NGOAI_TD_AM", GhiChu = "Khách tự hủy lịch", TrangThai = "da_huy", CoHieuLuc = false },
                new LichHenKham { MaLichHen = "LH_DEMO_006", MaBenhNhan = "BN_DEMO_06", TenBenhNhan = "Đỗ Quốc Gia", SoDienThoai = "0911000008", LoaiHen = "tai_kham", NgayHen = clock.Tomorrow, GioHen = Time(9, 0), ThoiLuongPhut = 30, MaLichTruc = "LT_PK_NOI_TM_AM", GhiChu = "Tái khám theo hướng dẫn của bác sĩ", TrangThai = "dang_cho" },
                new LichHenKham { MaLichHen = "LH_DEMO_007", MaBenhNhan = "BN_DEMO_07", TenBenhNhan = "Nguyễn Bảo Châu", SoDienThoai = "0911000009", LoaiHen = "kham_moi", NgayHen = clock.Today, GioHen = Time(8, 20), ThoiLuongPhut = 30, MaLichTruc = ResolveScheduleId(clock, "PK_NHI_01", clock.Today, "sang"), GhiChu = "Hẹn khám nhi do sốt nhẹ", TrangThai = "dang_cho" },
                new LichHenKham { MaLichHen = "LH_DEMO_008", MaBenhNhan = "BN_DEMO_08", TenBenhNhan = "Trần Đức Huy", SoDienThoai = "0911000010", LoaiHen = "kham_moi", NgayHen = clock.Today, GioHen = Time(13, 30), ThoiLuongPhut = 30, MaLichTruc = ResolveScheduleId(clock, "PK_RHM_01", clock.Today, "chieu"), GhiChu = "Hẹn khám răng hàm mặt", TrangThai = "da_xac_nhan" },
                new LichHenKham { MaLichHen = "LH_DEMO_009", MaBenhNhan = "BN_DEMO_09", TenBenhNhan = "Phan Quỳnh Như", SoDienThoai = "0911000011", LoaiHen = "tai_kham", NgayHen = clock.Tomorrow, GioHen = Time(10, 15), ThoiLuongPhut = 30, MaLichTruc = ResolveScheduleId(clock, "PK_TMH_01", clock.Tomorrow, "sang"), GhiChu = "Tái khám TMH sau điều trị", TrangThai = "dang_cho" },
                new LichHenKham { MaLichHen = "LH_DEMO_010", MaBenhNhan = "BN_DEMO_10", TenBenhNhan = "Lê Hoàng Vy", SoDienThoai = "0911000012", LoaiHen = "tai_kham", NgayHen = clock.Tomorrow, GioHen = Time(14, 0), ThoiLuongPhut = 30, MaLichTruc = ResolveScheduleId(clock, "PK_NOI_02", clock.Tomorrow, "chieu"), GhiChu = "Tái khám nội tổng quát chiều", TrangThai = "da_xac_nhan" },
                new LichHenKham { MaLichHen = "LH_DEMO_011", MaBenhNhan = "BN_DEMO_02", TenBenhNhan = "Phạm Gia Bảo", SoDienThoai = "0911000004", LoaiHen = "tai_kham", NgayHen = clock.Tomorrow, GioHen = Time(18, 15), ThoiLuongPhut = 30, MaLichTruc = ResolveScheduleId(clock, "PK_NOI_01", clock.Tomorrow, "toi"), GhiChu = "Tái khám buổi tối sau giờ làm", TrangThai = "da_xac_nhan" },
                new LichHenKham { MaLichHen = "LH_DEMO_012", MaBenhNhan = "BN_DEMO_04", TenBenhNhan = "Hoàng Minh Dũng", SoDienThoai = "0911000006", LoaiHen = "kham_moi", NgayHen = clock.Tomorrow.AddDays(1), GioHen = Time(18, 30), ThoiLuongPhut = 30, MaLichTruc = ResolveScheduleId(clock, "PK_NGOAI_01", clock.Tomorrow.AddDays(1), "toi"), GhiChu = "Khung giờ tối cho bệnh nhân đi làm giờ hành chính", TrangThai = "dang_cho" },
                new LichHenKham { MaLichHen = "LH_DEMO_013", MaBenhNhan = "BN_DEMO_07", TenBenhNhan = "Nguyễn Bảo Châu", SoDienThoai = "0911000009", LoaiHen = "tai_kham", NgayHen = clock.Tomorrow.AddDays(2), GioHen = Time(18, 0), ThoiLuongPhut = 30, MaLichTruc = ResolveScheduleId(clock, "PK_NHI_01", clock.Tomorrow.AddDays(2), "toi"), GhiChu = "Tái khám nhi vào buổi tối cho phụ huynh tiện đưa đón", TrangThai = "da_xac_nhan" },
                new LichHenKham { MaLichHen = "LH_DEMO_014", MaBenhNhan = "BN_DEMO_08", TenBenhNhan = "Trần Đức Huy", SoDienThoai = "0911000010", LoaiHen = "tai_kham", NgayHen = clock.Tomorrow.AddDays(3), GioHen = Time(18, 20), ThoiLuongPhut = 30, MaLichTruc = ResolveScheduleId(clock, "PK_RHM_01", clock.Tomorrow.AddDays(3), "toi"), GhiChu = "Tái khám răng buổi tối sau giờ làm", TrangThai = "da_xac_nhan" },
                new LichHenKham { MaLichHen = "LH_DEMO_015", MaBenhNhan = "BN_DEMO_09", TenBenhNhan = "Phan Quỳnh Như", SoDienThoai = "0911000011", LoaiHen = "kham_moi", NgayHen = clock.Tomorrow.AddDays(4), GioHen = Time(18, 45), ThoiLuongPhut = 30, MaLichTruc = ResolveScheduleId(clock, "PK_TMH_01", clock.Tomorrow.AddDays(4), "toi"), GhiChu = "Khám TMH khung tối để tránh trùng giờ học", TrangThai = "dang_cho" }
            };

            appointments.AddRange(BuildReportAppointments(clock));
            return appointments;
        }

        private static List<PhieuKhamLamSang> BuildClinicalForms(SeedClock clock)
        {
            var forms = new List<PhieuKhamLamSang>
            {
                new PhieuKhamLamSang
                {
                    MaPhieuKham = "PK_DEMO_001",
                    MaBacSiKham = "NV_BS_NOI_01",
                    MaNguoiLap = "NV_YT_HC_01",
                    MaBenhNhan = "BN_DEMO_01",
                    MaLichHen = "LH_DEMO_001",
                    MaDichVuKham = "DV_KHAM_NOI_01",
                    HinhThucTiepNhan = "appointment",
                    NgayLap = clock.Yesterday,
                    GioLap = Time(8, 40),
                    TrieuChung = "Khó thở nhẹ, ho kéo dài",
                    TrangThai = "da_hoan_tat"
                },
                new PhieuKhamLamSang
                {
                    MaPhieuKham = "PK_DEMO_002",
                    MaBacSiKham = "NV_BS_NOI_01",
                    MaNguoiLap = "NV_YT_HC_01",
                    MaBenhNhan = "BN_DEMO_02",
                    MaLichHen = "LH_DEMO_002",
                    MaDichVuKham = "DV_KHAM_NOI_01",
                    HinhThucTiepNhan = "appointment",
                    NgayLap = clock.Today,
                    GioLap = Time(8, 50),
                    TrieuChung = "Đau thượng vị, đầy bụng",
                    TrangThai = "da_lap"
                },
                new PhieuKhamLamSang
                {
                    MaPhieuKham = "PK_DEMO_003",
                    MaBacSiKham = "NV_BS_NOI_01",
                    MaNguoiLap = "NV_YT_HC_01",
                    MaBenhNhan = "BN_DEMO_03",
                    MaLichHen = "LH_DEMO_003",
                    MaDichVuKham = "DV_KHAM_NOI_01",
                    HinhThucTiepNhan = "appointment",
                    NgayLap = clock.Today,
                    GioLap = Time(9, 15),
                    TrieuChung = "Đau bụng, nghi viêm dạ dày cấp",
                    TrangThai = "dang_thuc_hien"
                },
                new PhieuKhamLamSang
                {
                    MaPhieuKham = "PK_DEMO_004",
                    MaBacSiKham = "NV_BS_NGOAI_01",
                    MaNguoiLap = "NV_YT_HC_01",
                    MaBenhNhan = "BN_DEMO_04",
                    MaDichVuKham = "DV_KHAM_NGOAI_01",
                    HinhThucTiepNhan = "walkin",
                    NgayLap = clock.Today,
                    GioLap = Time(10, 5),
                    TrieuChung = "Khó tiêu, nóng rát dạ dày",
                    TrangThai = "da_hoan_tat"
                },
                new PhieuKhamLamSang
                {
                    MaPhieuKham = "PK_DEMO_005",
                    MaBacSiKham = "NV_BS_NHI_01",
                    MaNguoiLap = "NV_YT_HC_02",
                    MaBenhNhan = "BN_DEMO_07",
                    MaLichHen = "LH_DEMO_007",
                    MaDichVuKham = "DV_KHAM_NHI_01",
                    HinhThucTiepNhan = "appointment",
                    NgayLap = clock.Today,
                    GioLap = Time(8, 35),
                    TrieuChung = "Sốt nhẹ, nghẹt mũi và quấy khóc về đêm",
                    TrangThai = "da_hoan_tat"
                },
                new PhieuKhamLamSang
                {
                    MaPhieuKham = "PK_DEMO_006",
                    MaBacSiKham = "NV_BS_RHM_01",
                    MaNguoiLap = "NV_YT_HC_02",
                    MaBenhNhan = "BN_DEMO_08",
                    MaLichHen = "LH_DEMO_008",
                    MaDichVuKham = "DV_KHAM_RHM_01",
                    HinhThucTiepNhan = "appointment",
                    NgayLap = clock.Today,
                    GioLap = Time(13, 42),
                    TrieuChung = "Đau răng hàm dưới bên trái, ê buốt khi ăn nóng lạnh",
                    TrangThai = "da_hoan_tat"
                },
                new PhieuKhamLamSang
                {
                    MaPhieuKham = "PK_DEMO_007",
                    MaBacSiKham = "NV_BS_TMH_01",
                    MaNguoiLap = "NV_YT_HC_03",
                    MaBenhNhan = "BN_DEMO_09",
                    MaDichVuKham = "DV_KHAM_TMH_01",
                    HinhThucTiepNhan = "walkin",
                    NgayLap = clock.Today,
                    GioLap = Time(15, 5),
                    TrieuChung = "Đau họng, khàn tiếng và nuốt vướng",
                    TrangThai = "dang_thuc_hien"
                },
                new PhieuKhamLamSang
                {
                    MaPhieuKham = "PK_DEMO_008",
                    MaBacSiKham = "NV_BS_NOI_02",
                    MaNguoiLap = "NV_YT_HC_03",
                    MaBenhNhan = "BN_DEMO_10",
                    MaDichVuKham = "DV_KHAM_NOI_02",
                    HinhThucTiepNhan = "walkin",
                    NgayLap = clock.Today,
                    GioLap = Time(18, 18),
                    TrieuChung = "Mệt mỏi, cần kiểm tra đường huyết định kỳ",
                    TrangThai = "da_lap"
                }
            };

            forms.AddRange(BuildReportClinicalForms(clock));
            return forms;
        }

        private static List<HangDoi> BuildClinicalQueues(SeedClock clock)
        {
            var queues = new List<HangDoi>
            {
                new HangDoi { MaHangDoi = "HQ_LS_001", MaBenhNhan = "BN_DEMO_01", MaPhong = "PK_NOI_01", LoaiHangDoi = "kham_lam_sang", Nguon = "appointment", Nhan = "A001", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Yesterday, 8, 32), ThoiGianLichHen = At(clock.Yesterday, 8, 30), DoUuTien = 2, TrangThai = "da_phuc_vu", GhiChu = "Đã hoàn tất lượt khám hôm qua", MaPhieuKham = "PK_DEMO_001", SoLanGoi = 1, ThoiGianGoiGanNhat = At(clock.Yesterday, 8, 38), NgayTao = At(clock.Yesterday, 8, 32), NgayCapNhat = At(clock.Yesterday, 9, 30) },
                new HangDoi { MaHangDoi = "HQ_LS_002", MaBenhNhan = "BN_DEMO_02", MaPhong = "PK_NOI_01", LoaiHangDoi = "kham_lam_sang", Nguon = "appointment", Nhan = "A002", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Today, 8, 46), ThoiGianLichHen = At(clock.Today, 8, 45), DoUuTien = 2, TrangThai = "cho_goi", GhiChu = "Chờ hoàn tất thanh toán", MaPhieuKham = "PK_DEMO_002", SoLanGoi = 0, NgayTao = At(clock.Today, 8, 46), NgayCapNhat = At(clock.Today, 8, 46) },
                new HangDoi { MaHangDoi = "HQ_LS_003", MaBenhNhan = "BN_DEMO_03", MaPhong = "PK_NOI_01", LoaiHangDoi = "kham_lam_sang", Nguon = "appointment", Nhan = "A003", CapCuu = false, PhanLoaiDen = "den_som", ThoiGianCheckin = At(clock.Today, 9, 0), ThoiGianLichHen = At(clock.Today, 9, 10), DoUuTien = 3, TrangThai = "da_phuc_vu", GhiChu = "Đã khám xong, chờ CLS", MaPhieuKham = "PK_DEMO_003", SoLanGoi = 1, ThoiGianGoiGanNhat = At(clock.Today, 9, 8), NgayTao = At(clock.Today, 9, 0), NgayCapNhat = At(clock.Today, 9, 40) },
                new HangDoi { MaHangDoi = "HQ_LS_004", MaBenhNhan = "BN_DEMO_04", MaPhong = "PK_NGOAI_01", LoaiHangDoi = "kham_lam_sang", Nguon = "walkin", Nhan = "B001", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Today, 9, 55), DoUuTien = 2, TrangThai = "da_phuc_vu", GhiChu = "Khám trực tiếp đã hoàn tất", MaPhieuKham = "PK_DEMO_004", SoLanGoi = 1, ThoiGianGoiGanNhat = At(clock.Today, 10, 2), NgayTao = At(clock.Today, 9, 55), NgayCapNhat = At(clock.Today, 10, 35) },
                new HangDoi { MaHangDoi = "HQ_LS_005", MaBenhNhan = "BN_DEMO_07", MaPhong = "PK_NHI_01", LoaiHangDoi = "kham_lam_sang", Nguon = "appointment", Nhan = "C001", CapCuu = false, PhanLoaiDen = "den_som", ThoiGianCheckin = At(clock.Today, 8, 18), ThoiGianLichHen = At(clock.Today, 8, 20), DoUuTien = 2, TrangThai = "da_phuc_vu", GhiChu = "Khám nhi đã hoàn tất trong buổi sáng", MaPhieuKham = "PK_DEMO_005", SoLanGoi = 1, ThoiGianGoiGanNhat = At(clock.Today, 8, 30), NgayTao = At(clock.Today, 8, 18), NgayCapNhat = At(clock.Today, 9, 5) },
                new HangDoi { MaHangDoi = "HQ_LS_006", MaBenhNhan = "BN_DEMO_08", MaPhong = "PK_RHM_01", LoaiHangDoi = "kham_lam_sang", Nguon = "appointment", Nhan = "D001", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Today, 13, 25), ThoiGianLichHen = At(clock.Today, 13, 30), DoUuTien = 2, TrangThai = "da_phuc_vu", GhiChu = "Khám răng hàm mặt đã hoàn tất", MaPhieuKham = "PK_DEMO_006", SoLanGoi = 1, ThoiGianGoiGanNhat = At(clock.Today, 13, 38), NgayTao = At(clock.Today, 13, 25), NgayCapNhat = At(clock.Today, 14, 15) },
                new HangDoi { MaHangDoi = "HQ_LS_007", MaBenhNhan = "BN_DEMO_09", MaPhong = "PK_TMH_01", LoaiHangDoi = "kham_lam_sang", Nguon = "walkin", Nhan = "E001", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Today, 14, 58), DoUuTien = 2, TrangThai = "dang_phuc_vu", GhiChu = "Đang khám Tai Mũi Họng", MaPhieuKham = "PK_DEMO_007", SoLanGoi = 1, ThoiGianGoiGanNhat = At(clock.Today, 15, 3), NgayTao = At(clock.Today, 14, 58), NgayCapNhat = At(clock.Today, 15, 25) },
                new HangDoi { MaHangDoi = "HQ_LS_008", MaBenhNhan = "BN_DEMO_10", MaPhong = "PK_NOI_02", LoaiHangDoi = "kham_lam_sang", Nguon = "walkin", Nhan = "A010", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Today, 18, 10), DoUuTien = 2, TrangThai = "cho_goi", GhiChu = "Bệnh nhân đợi vào phòng khám Nội 02", MaPhieuKham = "PK_DEMO_008", SoLanGoi = 0, NgayTao = At(clock.Today, 18, 10), NgayCapNhat = At(clock.Today, 18, 10) }
            };

            queues.AddRange(BuildReportClinicalQueues(clock));
            return queues;
        }

        private static List<LuotKhamBenh> BuildClinicalVisits(SeedClock clock)
        {
            var visits = new List<LuotKhamBenh>
            {
                new LuotKhamBenh { MaLuotKham = "LK_LS_001", MaHangDoi = "HQ_LS_001", MaNhanSuThucHien = "NV_BS_NOI_01", MaYTaHoTro = "NV_YT_LS_01", LoaiLuot = "kham_lam_sang", ThoiGianBatDau = At(clock.Yesterday, 8, 45), ThoiGianKetThuc = At(clock.Yesterday, 9, 10), TrangThai = "hoan_tat", ThoiGianThucTe = At(clock.Yesterday, 9, 10), SinhHieuTruocKham = "{\"nhiet_do\":37.1,\"huyet_ap\":\"120/80\",\"mach\":84}", GhiChu = "Đã chỉ định thêm CLS", NgayTao = At(clock.Yesterday, 8, 45), NgayCapNhat = At(clock.Yesterday, 9, 10) },
                new LuotKhamBenh { MaLuotKham = "LK_LS_003", MaHangDoi = "HQ_LS_003", MaNhanSuThucHien = "NV_BS_NOI_01", MaYTaHoTro = "NV_YT_LS_01", LoaiLuot = "kham_lam_sang", ThoiGianBatDau = At(clock.Today, 9, 12), ThoiGianKetThuc = At(clock.Today, 9, 36), TrangThai = "hoan_tat", ThoiGianThucTe = At(clock.Today, 9, 36), SinhHieuTruocKham = "{\"nhiet_do\":36.9,\"huyet_ap\":\"116/72\",\"mach\":80}", GhiChu = "Đã lập phiếu CLS", NgayTao = At(clock.Today, 9, 12), NgayCapNhat = At(clock.Today, 9, 36) },
                new LuotKhamBenh { MaLuotKham = "LK_LS_004", MaHangDoi = "HQ_LS_004", MaNhanSuThucHien = "NV_BS_NGOAI_01", MaYTaHoTro = "NV_YT_LS_02", LoaiLuot = "kham_lam_sang", ThoiGianBatDau = At(clock.Today, 10, 6), ThoiGianKetThuc = At(clock.Today, 10, 30), TrangThai = "hoan_tat", ThoiGianThucTe = At(clock.Today, 10, 30), SinhHieuTruocKham = "{\"nhiet_do\":36.7,\"huyet_ap\":\"118/78\",\"mach\":74}", GhiChu = "Đã chẩn đoán và kê đơn", NgayTao = At(clock.Today, 10, 6), NgayCapNhat = At(clock.Today, 10, 30) },
                new LuotKhamBenh { MaLuotKham = "LK_LS_005", MaHangDoi = "HQ_LS_005", MaNhanSuThucHien = "NV_BS_NHI_01", MaYTaHoTro = "NV_YT_LS_05", LoaiLuot = "kham_lam_sang", ThoiGianBatDau = At(clock.Today, 8, 34), ThoiGianKetThuc = At(clock.Today, 8, 58), TrangThai = "hoan_tat", ThoiGianThucTe = At(clock.Today, 8, 58), SinhHieuTruocKham = "{\"nhiet_do\":37.6,\"huyet_ap\":\"102/66\",\"mach\":98}", GhiChu = "Khám nhi và hướng dẫn theo dõi tại nhà", NgayTao = At(clock.Today, 8, 34), NgayCapNhat = At(clock.Today, 8, 58) },
                new LuotKhamBenh { MaLuotKham = "LK_LS_006", MaHangDoi = "HQ_LS_006", MaNhanSuThucHien = "NV_BS_RHM_01", MaYTaHoTro = "NV_YT_LS_06", LoaiLuot = "kham_lam_sang", ThoiGianBatDau = At(clock.Today, 13, 40), ThoiGianKetThuc = At(clock.Today, 14, 8), TrangThai = "hoan_tat", ThoiGianThucTe = At(clock.Today, 14, 8), SinhHieuTruocKham = "{\"nhiet_do\":36.8,\"huyet_ap\":\"115/74\",\"mach\":77}", GhiChu = "Đã chỉ định thêm phim và siêu âm vùng hàm", NgayTao = At(clock.Today, 13, 40), NgayCapNhat = At(clock.Today, 14, 8) },
                new LuotKhamBenh { MaLuotKham = "LK_LS_007", MaHangDoi = "HQ_LS_007", MaNhanSuThucHien = "NV_BS_TMH_01", MaYTaHoTro = "NV_YT_LS_07", LoaiLuot = "kham_lam_sang", ThoiGianBatDau = At(clock.Today, 15, 6), ThoiGianKetThuc = null, TrangThai = "dang_thuc_hien", ThoiGianThucTe = null, SinhHieuTruocKham = "{\"nhiet_do\":37.0,\"huyet_ap\":\"112/70\",\"mach\":82}", GhiChu = "Đang nội soi và đánh giá vùng họng", NgayTao = At(clock.Today, 15, 6), NgayCapNhat = At(clock.Today, 15, 28) }
            };

            visits.AddRange(BuildReportClinicalVisits(clock));
            return visits;
        }

        private static List<PhieuKhamCanLamSang> BuildClsForms(SeedClock clock) =>
            new()
            {
                new PhieuKhamCanLamSang { MaPhieuKhamCls = "CLS_DEMO_001", MaPhieuKhamLs = "PK_DEMO_001", NgayGioLap = At(clock.Yesterday, 9, 12), TrangThai = "da_hoan_tat", GhiChu = "Hoàn tất và đã tổng hợp kết quả", AutoPublishEnabled = true },
                new PhieuKhamCanLamSang { MaPhieuKhamCls = "CLS_DEMO_002", MaPhieuKhamLs = "PK_DEMO_003", NgayGioLap = At(clock.Today, 9, 38), TrangThai = "da_lap", GhiChu = "Chờ thanh toán trước khi thực hiện", AutoPublishEnabled = true },
                new PhieuKhamCanLamSang { MaPhieuKhamCls = "CLS_DEMO_003", MaPhieuKhamLs = "PK_DEMO_004", NgayGioLap = At(clock.Today, 10, 28), TrangThai = "da_hoan_tat", GhiChu = "Đã có đầy đủ kết quả xét nghiệm trong ngày", AutoPublishEnabled = true },
                new PhieuKhamCanLamSang { MaPhieuKhamCls = "CLS_DEMO_004", MaPhieuKhamLs = "PK_DEMO_005", NgayGioLap = At(clock.Today, 9, 2), TrangThai = "da_hoan_tat", GhiChu = "Đã làm xét nghiệm nhi cơ bản", AutoPublishEnabled = true },
                new PhieuKhamCanLamSang { MaPhieuKhamCls = "CLS_DEMO_005", MaPhieuKhamLs = "PK_DEMO_006", NgayGioLap = At(clock.Today, 14, 12), TrangThai = "dang_thuc_hien", GhiChu = "Đang thực hiện chẩn đoán hình ảnh", AutoPublishEnabled = true },
                new PhieuKhamCanLamSang { MaPhieuKhamCls = "CLS_DEMO_006", MaPhieuKhamLs = "PK_DEMO_007", NgayGioLap = At(clock.Today, 15, 32), TrangThai = "dang_thuc_hien", GhiChu = "Đã thực hiện một phần CLS trong ngày", AutoPublishEnabled = true },
                new PhieuKhamCanLamSang { MaPhieuKhamCls = "CLS_DEMO_007", MaPhieuKhamLs = "PK_DEMO_008", NgayGioLap = At(clock.Today, 18, 42), TrangThai = "da_lap", GhiChu = "Đã lập phiếu CLS buổi tối", AutoPublishEnabled = true }
            };

        private static List<ChiTietDichVu> BuildClsItems() =>
            new()
            {
                new ChiTietDichVu { MaChiTietDv = "CTDV_DEMO_001", MaPhieuKhamCls = "CLS_DEMO_001", MaDichVu = "DV_XN_CBC_01", TrangThai = "da_hoan_tat", GhiChu = "Công thức máu đã hoàn tất" },
                new ChiTietDichVu { MaChiTietDv = "CTDV_DEMO_002", MaPhieuKhamCls = "CLS_DEMO_001", MaDichVu = "DV_XQ_NGUC_01", TrangThai = "da_hoan_tat", GhiChu = "Đã có kết quả hình ảnh" },
                new ChiTietDichVu { MaChiTietDv = "CTDV_DEMO_003", MaPhieuKhamCls = "CLS_DEMO_002", MaDichVu = "DV_SIEUAM_BUNG_01", TrangThai = "da_lap", GhiChu = "Chờ xếp hàng CLS" },
                new ChiTietDichVu { MaChiTietDv = "CTDV_DEMO_004", MaPhieuKhamCls = "CLS_DEMO_003", MaDichVu = "DV_XN_SH_01", TrangThai = "da_hoan_tat", GhiChu = "Sinh hóa máu đã hoàn tất" },
                new ChiTietDichVu { MaChiTietDv = "CTDV_DEMO_005", MaPhieuKhamCls = "CLS_DEMO_003", MaDichVu = "DV_XN_CBC_01", TrangThai = "da_hoan_tat", GhiChu = "Công thức máu bổ sung đã hoàn tất" },
                new ChiTietDichVu { MaChiTietDv = "CTDV_DEMO_006", MaPhieuKhamCls = "CLS_DEMO_004", MaDichVu = "DV_XN_NT_01", TrangThai = "da_hoan_tat", GhiChu = "Nước tiểu đã có kết quả" },
                new ChiTietDichVu { MaChiTietDv = "CTDV_DEMO_007", MaPhieuKhamCls = "CLS_DEMO_004", MaDichVu = "DV_XN_CBC_01", TrangThai = "da_hoan_tat", GhiChu = "Công thức máu nhi đã hoàn tất" },
                new ChiTietDichVu { MaChiTietDv = "CTDV_DEMO_008", MaPhieuKhamCls = "CLS_DEMO_005", MaDichVu = "DV_XQ_NGUC_01", TrangThai = "dang_thuc_hien", GhiChu = "Đang chụp X-quang kiểm tra" },
                new ChiTietDichVu { MaChiTietDv = "CTDV_DEMO_009", MaPhieuKhamCls = "CLS_DEMO_005", MaDichVu = "DV_SIEUAM_BUNG_01", TrangThai = "da_hoan_tat", GhiChu = "Đã hoàn tất siêu âm bụng trong ngày" },
                new ChiTietDichVu { MaChiTietDv = "CTDV_DEMO_010", MaPhieuKhamCls = "CLS_DEMO_006", MaDichVu = "DV_SIEUAM_TUYEN_GIAP_01", TrangThai = "da_lap", GhiChu = "Đã tạo chỉ định siêu âm tuyến giáp" },
                new ChiTietDichVu { MaChiTietDv = "CTDV_DEMO_011", MaPhieuKhamCls = "CLS_DEMO_006", MaDichVu = "DV_XQ_XUONG_01", TrangThai = "da_hoan_tat", GhiChu = "Đã hoàn tất X-quang xương chi" },
                new ChiTietDichVu { MaChiTietDv = "CTDV_DEMO_012", MaPhieuKhamCls = "CLS_DEMO_007", MaDichVu = "DV_XN_SH_01", TrangThai = "da_lap", GhiChu = "Phiếu sinh hóa buổi tối đang chờ tiếp nhận" }
            };

        private static List<HangDoi> BuildClsQueues(SeedClock clock) =>
            new()
            {
                new HangDoi { MaHangDoi = "HQ_CLS_001", MaBenhNhan = "BN_DEMO_01", MaPhong = "CLS_XN_01", LoaiHangDoi = "can_lam_sang", Nguon = "service_return", Nhan = "XN001", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Yesterday, 9, 18), DoUuTien = 2, TrangThai = "da_phuc_vu", GhiChu = "Đã lấy mẫu xét nghiệm", MaChiTietDv = "CTDV_DEMO_001", SoLanGoi = 1, ThoiGianGoiGanNhat = At(clock.Yesterday, 9, 20), NgayTao = At(clock.Yesterday, 9, 18), NgayCapNhat = At(clock.Yesterday, 9, 40) },
                new HangDoi { MaHangDoi = "HQ_CLS_002", MaBenhNhan = "BN_DEMO_01", MaPhong = "CLS_CDH_01", LoaiHangDoi = "can_lam_sang", Nguon = "service_return", Nhan = "CD001", CapCuu = false, PhanLoaiDen = "den_som", ThoiGianCheckin = At(clock.Yesterday, 9, 45), DoUuTien = 2, TrangThai = "da_phuc_vu", GhiChu = "Đã chụp X-quang", MaChiTietDv = "CTDV_DEMO_002", SoLanGoi = 1, ThoiGianGoiGanNhat = At(clock.Yesterday, 9, 47), NgayTao = At(clock.Yesterday, 9, 45), NgayCapNhat = At(clock.Yesterday, 10, 15) },
                new HangDoi { MaHangDoi = "HQ_CLS_003", MaBenhNhan = "BN_DEMO_03", MaPhong = "CLS_CDH_01", LoaiHangDoi = "can_lam_sang", Nguon = "service_return", Nhan = "CD002", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Today, 9, 42), DoUuTien = 2, TrangThai = "cho_goi", GhiChu = "Chờ thanh toán để vào phòng CLS", MaChiTietDv = "CTDV_DEMO_003", SoLanGoi = 0, NgayTao = At(clock.Today, 9, 42), NgayCapNhat = At(clock.Today, 9, 42) },
                new HangDoi { MaHangDoi = "HQ_CLS_004", MaBenhNhan = "BN_DEMO_04", MaPhong = "CLS_XN_02", LoaiHangDoi = "can_lam_sang", Nguon = "service_return", Nhan = "XN002", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Today, 10, 32), DoUuTien = 2, TrangThai = "da_phuc_vu", GhiChu = "Sinh hóa máu đã hoàn tất", MaChiTietDv = "CTDV_DEMO_004", SoLanGoi = 1, ThoiGianGoiGanNhat = At(clock.Today, 10, 35), NgayTao = At(clock.Today, 10, 32), NgayCapNhat = At(clock.Today, 10, 56) },
                new HangDoi { MaHangDoi = "HQ_CLS_005", MaBenhNhan = "BN_DEMO_04", MaPhong = "CLS_XN_01", LoaiHangDoi = "can_lam_sang", Nguon = "service_return", Nhan = "XN003", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Today, 10, 34), DoUuTien = 2, TrangThai = "da_phuc_vu", GhiChu = "Công thức máu đã hoàn tất", MaChiTietDv = "CTDV_DEMO_005", SoLanGoi = 1, ThoiGianGoiGanNhat = At(clock.Today, 10, 37), NgayTao = At(clock.Today, 10, 34), NgayCapNhat = At(clock.Today, 10, 58) },
                new HangDoi { MaHangDoi = "HQ_CLS_006", MaBenhNhan = "BN_DEMO_07", MaPhong = "CLS_XN_02", LoaiHangDoi = "can_lam_sang", Nguon = "service_return", Nhan = "XN004", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Today, 9, 8), DoUuTien = 2, TrangThai = "da_phuc_vu", GhiChu = "Nước tiểu đã xử lý xong", MaChiTietDv = "CTDV_DEMO_006", SoLanGoi = 1, ThoiGianGoiGanNhat = At(clock.Today, 9, 12), NgayTao = At(clock.Today, 9, 8), NgayCapNhat = At(clock.Today, 9, 28) },
                new HangDoi { MaHangDoi = "HQ_CLS_007", MaBenhNhan = "BN_DEMO_07", MaPhong = "CLS_XN_01", LoaiHangDoi = "can_lam_sang", Nguon = "service_return", Nhan = "XN005", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Today, 9, 10), DoUuTien = 2, TrangThai = "da_phuc_vu", GhiChu = "Công thức máu nhi đã xong", MaChiTietDv = "CTDV_DEMO_007", SoLanGoi = 1, ThoiGianGoiGanNhat = At(clock.Today, 9, 14), NgayTao = At(clock.Today, 9, 10), NgayCapNhat = At(clock.Today, 9, 32) },
                new HangDoi { MaHangDoi = "HQ_CLS_008", MaBenhNhan = "BN_DEMO_08", MaPhong = "CLS_XQ_01", LoaiHangDoi = "can_lam_sang", Nguon = "service_return", Nhan = "CD003", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Today, 14, 18), DoUuTien = 2, TrangThai = "dang_phuc_vu", GhiChu = "Đang chụp X-quang ngực", MaChiTietDv = "CTDV_DEMO_008", SoLanGoi = 1, ThoiGianGoiGanNhat = At(clock.Today, 14, 22), NgayTao = At(clock.Today, 14, 18), NgayCapNhat = At(clock.Today, 14, 30) },
                new HangDoi { MaHangDoi = "HQ_CLS_009", MaBenhNhan = "BN_DEMO_08", MaPhong = "CLS_SA_01", LoaiHangDoi = "can_lam_sang", Nguon = "service_return", Nhan = "SA001", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Today, 14, 20), DoUuTien = 2, TrangThai = "da_phuc_vu", GhiChu = "Đã hoàn tất siêu âm bụng", MaChiTietDv = "CTDV_DEMO_009", SoLanGoi = 1, ThoiGianGoiGanNhat = At(clock.Today, 14, 26), NgayTao = At(clock.Today, 14, 20), NgayCapNhat = At(clock.Today, 14, 44) },
                new HangDoi { MaHangDoi = "HQ_CLS_010", MaBenhNhan = "BN_DEMO_09", MaPhong = "CLS_SA_01", LoaiHangDoi = "can_lam_sang", Nguon = "service_return", Nhan = "SA002", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Today, 15, 36), DoUuTien = 2, TrangThai = "cho_goi", GhiChu = "Chờ siêu âm tuyến giáp", MaChiTietDv = "CTDV_DEMO_010", SoLanGoi = 0, NgayTao = At(clock.Today, 15, 36), NgayCapNhat = At(clock.Today, 15, 36) },
                new HangDoi { MaHangDoi = "HQ_CLS_011", MaBenhNhan = "BN_DEMO_09", MaPhong = "CLS_XQ_01", LoaiHangDoi = "can_lam_sang", Nguon = "service_return", Nhan = "CD004", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Today, 15, 37), DoUuTien = 2, TrangThai = "da_phuc_vu", GhiChu = "Đã hoàn tất X-quang xương chi", MaChiTietDv = "CTDV_DEMO_011", SoLanGoi = 1, ThoiGianGoiGanNhat = At(clock.Today, 15, 42), NgayTao = At(clock.Today, 15, 37), NgayCapNhat = At(clock.Today, 15, 55) },
                new HangDoi { MaHangDoi = "HQ_CLS_012", MaBenhNhan = "BN_DEMO_10", MaPhong = "CLS_XN_02", LoaiHangDoi = "can_lam_sang", Nguon = "service_return", Nhan = "XN006", CapCuu = false, PhanLoaiDen = "dung_gio", ThoiGianCheckin = At(clock.Today, 18, 46), DoUuTien = 2, TrangThai = "cho_goi", GhiChu = "Phiếu sinh hóa buổi tối đang chờ gọi", MaChiTietDv = "CTDV_DEMO_012", SoLanGoi = 0, NgayTao = At(clock.Today, 18, 46), NgayCapNhat = At(clock.Today, 18, 46) }
            };

        private static List<LuotKhamBenh> BuildClsVisits(SeedClock clock) =>
            new()
            {
                new LuotKhamBenh { MaLuotKham = "LK_CLS_001", MaHangDoi = "HQ_CLS_001", MaNhanSuThucHien = "NV_KTV_XN_01", MaYTaHoTro = "NV_YT_CLS_01", LoaiLuot = "can_lam_sang", ThoiGianBatDau = At(clock.Yesterday, 9, 22), ThoiGianKetThuc = At(clock.Yesterday, 9, 34), TrangThai = "hoan_tat", ThoiGianThucTe = At(clock.Yesterday, 9, 34), GhiChu = "Lấy mẫu thành công", NgayTao = At(clock.Yesterday, 9, 22), NgayCapNhat = At(clock.Yesterday, 9, 34) },
                new LuotKhamBenh { MaLuotKham = "LK_CLS_002", MaHangDoi = "HQ_CLS_002", MaNhanSuThucHien = "NV_KTV_CDH_01", MaYTaHoTro = "NV_YT_CLS_01", LoaiLuot = "can_lam_sang", ThoiGianBatDau = At(clock.Yesterday, 9, 50), ThoiGianKetThuc = At(clock.Yesterday, 10, 5), TrangThai = "hoan_tat", ThoiGianThucTe = At(clock.Yesterday, 10, 5), GhiChu = "Hoàn tất chụp phim", NgayTao = At(clock.Yesterday, 9, 50), NgayCapNhat = At(clock.Yesterday, 10, 5) },
                new LuotKhamBenh { MaLuotKham = "LK_CLS_004", MaHangDoi = "HQ_CLS_004", MaNhanSuThucHien = "NV_KTV_XN_02", MaYTaHoTro = "NV_YT_CLS_01", LoaiLuot = "can_lam_sang", ThoiGianBatDau = At(clock.Today, 10, 35), ThoiGianKetThuc = At(clock.Today, 10, 52), TrangThai = "hoan_tat", ThoiGianThucTe = At(clock.Today, 10, 52), GhiChu = "Hoàn tất sinh hóa máu", NgayTao = At(clock.Today, 10, 35), NgayCapNhat = At(clock.Today, 10, 52) },
                new LuotKhamBenh { MaLuotKham = "LK_CLS_005", MaHangDoi = "HQ_CLS_005", MaNhanSuThucHien = "NV_KTV_XN_01", MaYTaHoTro = "NV_YT_CLS_01", LoaiLuot = "can_lam_sang", ThoiGianBatDau = At(clock.Today, 10, 37), ThoiGianKetThuc = At(clock.Today, 10, 54), TrangThai = "hoan_tat", ThoiGianThucTe = At(clock.Today, 10, 54), GhiChu = "Hoàn tất công thức máu", NgayTao = At(clock.Today, 10, 37), NgayCapNhat = At(clock.Today, 10, 54) },
                new LuotKhamBenh { MaLuotKham = "LK_CLS_006", MaHangDoi = "HQ_CLS_006", MaNhanSuThucHien = "NV_KTV_XN_02", MaYTaHoTro = "NV_YT_CLS_01", LoaiLuot = "can_lam_sang", ThoiGianBatDau = At(clock.Today, 9, 12), ThoiGianKetThuc = At(clock.Today, 9, 24), TrangThai = "hoan_tat", ThoiGianThucTe = At(clock.Today, 9, 24), GhiChu = "Hoàn tất tổng phân tích nước tiểu", NgayTao = At(clock.Today, 9, 12), NgayCapNhat = At(clock.Today, 9, 24) },
                new LuotKhamBenh { MaLuotKham = "LK_CLS_007", MaHangDoi = "HQ_CLS_007", MaNhanSuThucHien = "NV_KTV_XN_01", MaYTaHoTro = "NV_YT_CLS_01", LoaiLuot = "can_lam_sang", ThoiGianBatDau = At(clock.Today, 9, 14), ThoiGianKetThuc = At(clock.Today, 9, 26), TrangThai = "hoan_tat", ThoiGianThucTe = At(clock.Today, 9, 26), GhiChu = "Hoàn tất công thức máu nhi", NgayTao = At(clock.Today, 9, 14), NgayCapNhat = At(clock.Today, 9, 26) },
                new LuotKhamBenh { MaLuotKham = "LK_CLS_008", MaHangDoi = "HQ_CLS_008", MaNhanSuThucHien = "NV_KTV_CDH_01", MaYTaHoTro = "NV_YT_CLS_02", LoaiLuot = "can_lam_sang", ThoiGianBatDau = At(clock.Today, 14, 22), ThoiGianKetThuc = null, TrangThai = "dang_thuc_hien", ThoiGianThucTe = null, GhiChu = "Đang thực hiện X-quang ngực", NgayTao = At(clock.Today, 14, 22), NgayCapNhat = At(clock.Today, 14, 30) },
                new LuotKhamBenh { MaLuotKham = "LK_CLS_009", MaHangDoi = "HQ_CLS_009", MaNhanSuThucHien = "NV_KTV_SA_01", MaYTaHoTro = "NV_YT_CLS_02", LoaiLuot = "can_lam_sang", ThoiGianBatDau = At(clock.Today, 14, 26), ThoiGianKetThuc = At(clock.Today, 14, 42), TrangThai = "hoan_tat", ThoiGianThucTe = At(clock.Today, 14, 42), GhiChu = "Hoàn tất siêu âm bụng", NgayTao = At(clock.Today, 14, 26), NgayCapNhat = At(clock.Today, 14, 42) },
                new LuotKhamBenh { MaLuotKham = "LK_CLS_011", MaHangDoi = "HQ_CLS_011", MaNhanSuThucHien = "NV_KTV_CDH_02", MaYTaHoTro = "NV_YT_CLS_02", LoaiLuot = "can_lam_sang", ThoiGianBatDau = At(clock.Today, 15, 42), ThoiGianKetThuc = At(clock.Today, 15, 54), TrangThai = "hoan_tat", ThoiGianThucTe = At(clock.Today, 15, 54), GhiChu = "Hoàn tất X-quang xương chi", NgayTao = At(clock.Today, 15, 42), NgayCapNhat = At(clock.Today, 15, 54) }
            };

        private static List<KetQuaDichVu> BuildResults(SeedClock clock) =>
            new()
            {
                new KetQuaDichVu
                {
                    MaKetQua = "KQ_DEMO_001",
                    MaChiTietDv = "CTDV_DEMO_001",
                    LoaiKetQua = "xet_nghiem",
                    KetLuanChuyen = "Bạch cầu tăng nhẹ, đề nghị theo dõi viêm",
                    GhiChu = "Không có bất thường nghiêm trọng",
                    TepDinhKem = "[\"cbc-demo-001.pdf\"]",
                    ThoiGianChot = At(clock.Yesterday, 10, 10),
                    TrangThaiChot = "hoan_tat",
                    MaNguoiTao = "NV_KTV_XN_01",
                    ThoiGianTao = At(clock.Yesterday, 9, 55)
                },
                new KetQuaDichVu
                {
                    MaKetQua = "KQ_DEMO_002",
                    MaChiTietDv = "CTDV_DEMO_002",
                    LoaiKetQua = "chan_doan_hinh_anh",
                    KetLuanChuyen = "X-quang ngực chưa thấy tổn thương cấp tính",
                    GhiChu = "Khuyến nghị tái khám nếu triệu chứng tăng",
                    TepDinhKem = "[\"xray-demo-001.jpg\"]",
                    ThoiGianChot = At(clock.Yesterday, 10, 18),
                    TrangThaiChot = "hoan_tat",
                    MaNguoiTao = "NV_KTV_CDH_01",
                    ThoiGianTao = At(clock.Yesterday, 10, 12)
                },
                new KetQuaDichVu
                {
                    MaKetQua = "KQ_DEMO_003",
                    MaChiTietDv = "CTDV_DEMO_004",
                    LoaiKetQua = "xet_nghiem",
                    KetLuanChuyen = "Chỉ số sinh hóa trong giới hạn theo dõi",
                    GhiChu = "Ưu tiên tái khám nếu còn đau bụng",
                    TepDinhKem = "[\"sh-demo-003.pdf\"]",
                    ThoiGianChot = At(clock.Today, 10, 53),
                    TrangThaiChot = "hoan_tat",
                    MaNguoiTao = "NV_KTV_XN_02",
                    ThoiGianTao = At(clock.Today, 10, 49)
                },
                new KetQuaDichVu
                {
                    MaKetQua = "KQ_DEMO_004",
                    MaChiTietDv = "CTDV_DEMO_005",
                    LoaiKetQua = "xet_nghiem",
                    KetLuanChuyen = "Công thức máu chưa ghi nhận bất thường rõ",
                    GhiChu = "Theo dõi thêm triệu chứng tiêu hóa",
                    TepDinhKem = "[\"cbc-demo-004.pdf\"]",
                    ThoiGianChot = At(clock.Today, 10, 55),
                    TrangThaiChot = "hoan_tat",
                    MaNguoiTao = "NV_KTV_XN_01",
                    ThoiGianTao = At(clock.Today, 10, 50)
                },
                new KetQuaDichVu
                {
                    MaKetQua = "KQ_DEMO_005",
                    MaChiTietDv = "CTDV_DEMO_006",
                    LoaiKetQua = "xet_nghiem",
                    KetLuanChuyen = "Nước tiểu chưa thấy dấu hiệu nhiễm trùng",
                    GhiChu = "Khuyến nghị uống đủ nước",
                    TepDinhKem = "[\"urine-demo-005.pdf\"]",
                    ThoiGianChot = At(clock.Today, 9, 27),
                    TrangThaiChot = "hoan_tat",
                    MaNguoiTao = "NV_KTV_XN_02",
                    ThoiGianTao = At(clock.Today, 9, 22)
                },
                new KetQuaDichVu
                {
                    MaKetQua = "KQ_DEMO_006",
                    MaChiTietDv = "CTDV_DEMO_007",
                    LoaiKetQua = "xet_nghiem",
                    KetLuanChuyen = "Công thức máu nhi ổn định, chưa thấy dấu hiệu nặng",
                    GhiChu = "Tiếp tục theo dõi tại nhà",
                    TepDinhKem = "[\"cbc-demo-006.pdf\"]",
                    ThoiGianChot = At(clock.Today, 9, 29),
                    TrangThaiChot = "hoan_tat",
                    MaNguoiTao = "NV_KTV_XN_01",
                    ThoiGianTao = At(clock.Today, 9, 24)
                },
                new KetQuaDichVu
                {
                    MaKetQua = "KQ_DEMO_007",
                    MaChiTietDv = "CTDV_DEMO_009",
                    LoaiKetQua = "chan_doan_hinh_anh",
                    KetLuanChuyen = "Siêu âm bụng chưa ghi nhận bất thường đáng kể",
                    GhiChu = "Đề nghị tiếp tục theo dõi lâm sàng",
                    TepDinhKem = "[\"sa-demo-009.pdf\"]",
                    ThoiGianChot = At(clock.Today, 14, 45),
                    TrangThaiChot = "hoan_tat",
                    MaNguoiTao = "NV_KTV_SA_01",
                    ThoiGianTao = At(clock.Today, 14, 45)
                },
                new KetQuaDichVu
                {
                    MaKetQua = "KQ_DEMO_008",
                    MaChiTietDv = "CTDV_DEMO_011",
                    LoaiKetQua = "chan_doan_hinh_anh",
                    KetLuanChuyen = "X-quang xương chi chưa thấy tổn thương xương cấp tính",
                    GhiChu = "Khuyến nghị theo dõi triệu chứng sau chấn thương",
                    TepDinhKem = "[\"xq-demo-011.pdf\"]",
                    ThoiGianChot = At(clock.Today, 15, 56),
                    TrangThaiChot = "hoan_tat",
                    MaNguoiTao = "NV_KTV_CDH_02",
                    ThoiGianTao = At(clock.Today, 15, 56)
                }
            };

        private static List<PhieuTongHopKetQua> BuildSummaries(SeedClock clock) =>
            new()
            {
                new PhieuTongHopKetQua
                {
                    MaPhieuTongHop = "PTH_DEMO_001",
                    MaPhieuKhamCls = "CLS_DEMO_001",
                    LoaiPhieu = "tong_hop_cls",
                    MaNhanSuXuLy = "NV_BS_NOI_01",
                    TrangThai = "da_hoan_tat",
                    ThoiGianXuLy = At(clock.Yesterday, 10, 25),
                    SnapshotJson = "{\"tong_quan\":\"CLS hoan tat\",\"ket_luan\":\"Theo doi viem ho hap nhe\"}"
                }
            };

        private static List<PhieuChanDoanCuoi> BuildDiagnoses(SeedClock clock) =>
            new()
            {
                new PhieuChanDoanCuoi
                {
                    MaPhieuChanDoan = "CD_DEMO_001",
                    MaPhieuKham = "PK_DEMO_001",
                    MaDonThuoc = "DT_DEMO_001",
                    ChanDoanSoBo = "Theo dõi viêm đường hô hấp trên",
                    ChanDoanCuoi = "Viêm họng cấp, chưa ghi nhận tổn thương phổi",
                    MaICD10 = "J02.9",
                    NoiDungKham = "Ho, rát họng, CLS không thấy tổn thương cấp tính",
                    HuongXuTri = "cho_thuoc",
                    LoiKhuyen = "Uống nhiều nước, tái khám sau 7 ngày nếu không đỡ",
                    PhatDoDieuTri = "Thuốc giảm sốt, kháng histamin, vitamin",
                    NgayTaiKham = clock.Tomorrow.AddDays(7),
                    GhiChuTaiKham = "Tái khám nếu ho không giảm",
                    ThoiGianTao = At(clock.Yesterday, 10, 35),
                    ThoiGianCapNhat = At(clock.Yesterday, 10, 35)
                },
                new PhieuChanDoanCuoi
                {
                    MaPhieuChanDoan = "CD_DEMO_004",
                    MaPhieuKham = "PK_DEMO_004",
                    MaDonThuoc = "DT_DEMO_002",
                    ChanDoanSoBo = "Rối loạn tiêu hóa",
                    ChanDoanCuoi = "Viêm dạ dày cấp",
                    MaICD10 = "K29.0",
                    NoiDungKham = "Đau vùng thượng vị, đầy hơi sau ăn",
                    HuongXuTri = "cho_thuoc",
                    LoiKhuyen = "Ăn nhẹ, tránh đồ cay nóng, tái khám nếu đau tăng",
                    PhatDoDieuTri = "Thuốc bảo vệ dạ dày và giảm buồn nôn",
                    ThoiGianTao = At(clock.Today, 10, 32),
                    ThoiGianCapNhat = At(clock.Today, 10, 32)
                }
            };

        private static List<DonThuoc> BuildPrescriptions(SeedClock clock) =>
            new()
            {
                new DonThuoc
                {
                    MaDonThuoc = "DT_DEMO_001",
                    MaBacSiKeDon = "NV_BS_NOI_01",
                    MaBenhNhan = "BN_DEMO_01",
                    ThoiGianKeDon = At(clock.Yesterday, 10, 36),
                    TrangThai = "da_phat",
                    ThoiGianThanhToan = At(clock.Yesterday, 10, 45),
                    ThoiGianPhat = At(clock.Yesterday, 10, 52),
                    MaNhanSuPhat = "NV_YT_HC_01",
                    NgayTao = At(clock.Yesterday, 10, 36),
                    NgayCapNhat = At(clock.Yesterday, 10, 52)
                },
                new DonThuoc
                {
                    MaDonThuoc = "DT_DEMO_002",
                    MaBacSiKeDon = "NV_BS_NGOAI_01",
                    MaBenhNhan = "BN_DEMO_04",
                    ThoiGianKeDon = At(clock.Today, 10, 33),
                    TrangThai = "cho_phat",
                    NgayTao = At(clock.Today, 10, 33),
                    NgayCapNhat = At(clock.Today, 10, 33)
                }
            };

        private static List<ChiTietDonThuoc> BuildPrescriptionItems(SeedClock clock) =>
            new()
            {
                new ChiTietDonThuoc { MaChiTietDon = "CTDT_001", MaDonThuoc = "DT_DEMO_001", MaThuoc = "THUOC_001", SoLuong = 10, ThanhTien = 25000m, ChiDinhSuDung = "Sau ăn", LieuDung = "1 viên", TanSuatDung = "3 lần/ngày", SoNgayDung = 4, GhiChu = "Uống khi sốt trên 38 độ", NgayTao = At(clock.Yesterday, 10, 36), NgayCapNhat = At(clock.Yesterday, 10, 36) },
                new ChiTietDonThuoc { MaChiTietDon = "CTDT_002", MaDonThuoc = "DT_DEMO_001", MaThuoc = "THUOC_002", SoLuong = 7, ThanhTien = 21000m, ChiDinhSuDung = "Buổi tối", LieuDung = "1 viên", TanSuatDung = "1 lần/ngày", SoNgayDung = 7, GhiChu = "Dùng buổi tối", NgayTao = At(clock.Yesterday, 10, 36), NgayCapNhat = At(clock.Yesterday, 10, 36) },
                new ChiTietDonThuoc { MaChiTietDon = "CTDT_003", MaDonThuoc = "DT_DEMO_001", MaThuoc = "THUOC_005", SoLuong = 10, ThanhTien = 20000m, ChiDinhSuDung = "Sau ăn sáng", LieuDung = "1 viên", TanSuatDung = "1 lần/ngày", SoNgayDung = 10, GhiChu = "Bổ sung sức đề kháng", NgayTao = At(clock.Yesterday, 10, 36), NgayCapNhat = At(clock.Yesterday, 10, 36) },
                new ChiTietDonThuoc { MaChiTietDon = "CTDT_004", MaDonThuoc = "DT_DEMO_002", MaThuoc = "THUOC_003", SoLuong = 14, ThanhTien = 56000m, ChiDinhSuDung = "Trước bữa ăn", LieuDung = "1 viên", TanSuatDung = "2 lần/ngày", SoNgayDung = 7, GhiChu = "Uống trước ăn 30 phút", NgayTao = At(clock.Today, 10, 33), NgayCapNhat = At(clock.Today, 10, 33) },
                new ChiTietDonThuoc { MaChiTietDon = "CTDT_005", MaDonThuoc = "DT_DEMO_002", MaThuoc = "THUOC_004", SoLuong = 10, ThanhTien = 35000m, ChiDinhSuDung = "Khi buồn nôn", LieuDung = "1 viên", TanSuatDung = "2 lần/ngày", SoNgayDung = 5, GhiChu = "Dùng khi cần", NgayTao = At(clock.Today, 10, 33), NgayCapNhat = At(clock.Today, 10, 33) }
            };

        private static void ApplyPrescriptionTotals(List<DonThuoc> prescriptions, List<ChiTietDonThuoc> items)
        {
            foreach (var prescription in prescriptions)
            {
                prescription.TongTienDon = items
                    .Where(item => item.MaDonThuoc == prescription.MaDonThuoc)
                    .Sum(item => item.ThanhTien);
            }
        }

        private static List<HoaDonThanhToan> BuildInvoices(SeedClock clock, List<DonThuoc> prescriptions)
        {
            var historicalDrugTotal = prescriptions.Single(x => x.MaDonThuoc == "DT_DEMO_001").TongTienDon;
            var pendingDrugTotal = prescriptions.Single(x => x.MaDonThuoc == "DT_DEMO_002").TongTienDon;

            var invoices = new List<HoaDonThanhToan>
            {
                // ===== HÓA ĐƠN ĐÃ THU (Historical) =====
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_DEMO_001",
                    MaBenhNhan = "BN_DEMO_01",
                    MaNhanSuThu = "NV_YT_HC_01",
                    MaPhieuKham = "PK_DEMO_001",
                    LoaiDotthu = "kham_lam_sang",
                    SoTien = 150000m,
                    SoTienPhaiTra = 150000m,
                    MaGiaoDich = "GD-DEMO-001",
                    PhuongThucThanhToan = "tien_mat",
                    ThoiGian = At(clock.Yesterday, 8, 42),
                    TrangThai = "da_thu",
                    NoiDung = "Thu tiền khám nội tổng quát"
                },
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_DEMO_002",
                    MaBenhNhan = "BN_DEMO_01",
                    MaNhanSuThu = "NV_YT_HC_01",
                    MaPhieuKhamCls = "CLS_DEMO_001",
                    LoaiDotthu = "can_lam_sang",
                    SoTien = 340000m,
                    SoTienPhaiTra = 340000m,
                    MaGiaoDich = "GD-DEMO-002",
                    PhuongThucThanhToan = "chuyen_khoan",
                    ThoiGian = At(clock.Yesterday, 9, 15),
                    TrangThai = "da_thu",
                    NoiDung = "Thu tiền CLS gồm công thức máu và X-quang"
                },
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_DEMO_003",
                    MaBenhNhan = "BN_DEMO_01",
                    MaNhanSuThu = "NV_YT_HC_01",
                    MaDonThuoc = "DT_DEMO_001",
                    LoaiDotthu = "thuoc",
                    SoTien = historicalDrugTotal,
                    SoTienPhaiTra = historicalDrugTotal,
                    MaGiaoDich = "GD-DEMO-003",
                    PhuongThucThanhToan = "tien_mat",
                    ThoiGian = At(clock.Yesterday, 10, 45),
                    TrangThai = "da_thu",
                    NoiDung = "Thu tiền đơn thuốc đã kê"
                },

                // ===== HÓA ĐƠN BẢO LƯU (Bệnh nhân bỏ về sau thanh toán) =====
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_RESERVE_001",
                    MaBenhNhan = "BN_DEMO_06",
                    MaNhanSuThu = "NV_YT_HC_01",
                    LoaiDotthu = "kham_lam_sang",
                    SoTien = 180000m,
                    SoTienPhaiTra = 180000m,
                    MaGiaoDich = "GD-RES-001",
                    PhuongThucThanhToan = "tien_mat",
                    ThoiGian = clock.Today.AddDays(-2).AddHours(9).AddMinutes(10),
                    TrangThai = "bao_luu",
                    ThoiGianHuy = clock.Today.AddDays(-1).AddHours(10).AddMinutes(5),
                    NoiDung = "Bảo lưu phí khám nội do bệnh nhân bỏ về sau thanh toán"
                },
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_RESERVE_002",
                    MaBenhNhan = "BN_DEMO_08",
                    MaNhanSuThu = "NV_YT_HC_02",
                    LoaiDotthu = "can_lam_sang",
                    SoTien = 620000m,
                    SoTienPhaiTra = 620000m,
                    MaGiaoDich = "GD-RES-002",
                    PhuongThucThanhToan = "chuyen_khoan",
                    ThoiGian = clock.Today.AddDays(-10).AddHours(8).AddMinutes(25),
                    TrangThai = "bao_luu",
                    ThoiGianHuy = clock.Today.AddDays(-8).AddHours(11).AddMinutes(40),
                    NoiDung = "Bảo lưu chi phí CLS do bệnh nhân bỏ về trước khi thực hiện"
                },
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_RESERVE_003",
                    MaBenhNhan = "BN_DEMO_10",
                    MaNhanSuThu = "NV_YT_HC_03",
                    LoaiDotthu = "kham_lam_sang",
                    SoTien = 190000m,
                    SoTienPhaiTra = 190000m,
                    MaGiaoDich = "GD-RES-003",
                    PhuongThucThanhToan = "the",
                    ThoiGian = clock.Today.AddDays(-24).AddHours(14).AddMinutes(20),
                    TrangThai = "bao_luu",
                    ThoiGianHuy = clock.Today.AddDays(-18).AddHours(15).AddMinutes(15),
                    NoiDung = "Bảo lưu phí khám chuyên khoa do bệnh nhân xin dời lịch"
                },

                // ===== HÓA ĐƠN CHƯA THU (Unpaid - Hôm nay) =====
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_DEMO_004",
                    MaBenhNhan = "BN_DEMO_02",
                    MaNhanSuThu = "NV_YT_HC_01",
                    MaPhieuKham = "PK_DEMO_002",
                    LoaiDotthu = "kham_lam_sang",
                    SoTien = 150000m,
                    SoTienPhaiTra = 150000m,
                    PhuongThucThanhToan = "vietqr",
                    ThoiGian = At(clock.Today, 8, 51),
                    TrangThai = "chua_thu",
                    NoiDung = "Chờ thu tiền khám nội tổng quát"
                },
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_DEMO_005",
                    MaBenhNhan = "BN_DEMO_03",
                    MaNhanSuThu = "NV_YT_HC_01",
                    MaPhieuKhamCls = "CLS_DEMO_002",
                    LoaiDotthu = "can_lam_sang",
                    SoTien = 300000m,
                    SoTienPhaiTra = 300000m,
                    PhuongThucThanhToan = "vietqr",
                    ThoiGian = At(clock.Today, 9, 40),
                    TrangThai = "chua_thu",
                    NoiDung = "Chờ thu tiền siêu âm bụng"
                },
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_DEMO_006",
                    MaBenhNhan = "BN_DEMO_04",
                    MaNhanSuThu = "NV_YT_HC_01",
                    MaDonThuoc = "DT_DEMO_002",
                    LoaiDotthu = "thuoc",
                    SoTien = pendingDrugTotal,
                    SoTienPhaiTra = pendingDrugTotal,
                    PhuongThucThanhToan = "tien_mat",
                    ThoiGian = At(clock.Today, 10, 34),
                    TrangThai = "chua_thu",
                    NoiDung = "Chờ thu tiền đơn thuốc viêm dạ dày"
                },

                // ===== HÓA ĐƠN CHƯA THU (5 ngày trước - dưới 7 ngày) =====
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_UNPAID_001",
                    MaBenhNhan = "BN_DEMO_02",
                    MaNhanSuThu = "NV_YT_HC_01",
                    LoaiDotthu = "kham_lam_sang",
                    SoTien = 200000m,
                    SoTienPhaiTra = 200000m,
                    ThoiGian = clock.Today.AddDays(-5).AddHours(9).AddMinutes(15),
                    TrangThai = "chua_thu",
                    NoiDung = "Khám ngoại tổng quát - 5 ngày trước"
                },
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_UNPAID_002",
                    MaBenhNhan = "BN_DEMO_03",
                    MaNhanSuThu = "NV_YT_HC_01",
                    LoaiDotthu = "thuoc",
                    SoTien = 450000m,
                    SoTienPhaiTra = 450000m,
                    ThoiGian = clock.Today.AddDays(-5).AddHours(14).AddMinutes(30),
                    TrangThai = "chua_thu",
                    NoiDung = "Đơn thuốc kháng sinh - 5 ngày trước"
                },

                // ===== HÓA ĐƠN CHƯA THU (10 ngày trước - 7-30 ngày, màu vàng) =====
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_UNPAID_003",
                    MaBenhNhan = "BN_DEMO_01",
                    MaNhanSuThu = "NV_YT_HC_01",
                    LoaiDotthu = "can_lam_sang",
                    SoTien = 850000m,
                    SoTienPhaiTra = 850000m,
                    ThoiGian = clock.Today.AddDays(-10).AddHours(10).AddMinutes(20),
                    TrangThai = "chua_thu",
                    NoiDung = "Xét nghiệm máu tổng quát + sinh hóa - 10 ngày trước"
                },
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_UNPAID_004",
                    MaBenhNhan = "BN_DEMO_04",
                    MaNhanSuThu = "NV_YT_HC_01",
                    LoaiDotthu = "kham_lam_sang",
                    SoTien = 1200000m,
                    SoTienPhaiTra = 1200000m,
                    ThoiGian = clock.Today.AddDays(-10).AddHours(15).AddMinutes(45),
                    TrangThai = "chua_thu",
                    NoiDung = "Khám chuyên khoa tim mạch - 10 ngày trước"
                },

                // ===== HÓA ĐƠN CHƯA THU (15 ngày trước - 7-30 ngày, màu vàng) =====
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_UNPAID_005",
                    MaBenhNhan = "BN_DEMO_02",
                    MaNhanSuThu = "NV_YT_HC_01",
                    LoaiDotthu = "thuoc",
                    SoTien = 2500000m,
                    SoTienPhaiTra = 2500000m,
                    ThoiGian = clock.Today.AddDays(-15).AddHours(11).AddMinutes(10),
                    TrangThai = "chua_thu",
                    NoiDung = "Đơn thuốc điều trị dài ngày - 15 ngày trước"
                },
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_UNPAID_006",
                    MaBenhNhan = "BN_DEMO_03",
                    MaNhanSuThu = "NV_YT_HC_01",
                    LoaiDotthu = "can_lam_sang",
                    SoTien = 3200000m,
                    SoTienPhaiTra = 3200000m,
                    ThoiGian = clock.Today.AddDays(-15).AddHours(16).AddMinutes(25),
                    TrangThai = "chua_thu",
                    NoiDung = "Chụp CT Scanner + MRI - 15 ngày trước"
                },

                // ===== HÓA ĐƠN CHƯA THU (20 ngày trước - 7-30 ngày, màu vàng) =====
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_UNPAID_007",
                    MaBenhNhan = "BN_DEMO_01",
                    MaNhanSuThu = "NV_YT_HC_01",
                    LoaiDotthu = "kham_lam_sang",
                    SoTien = 4500000m,
                    SoTienPhaiTra = 4500000m,
                    ThoiGian = clock.Today.AddDays(-20).AddHours(9).AddMinutes(30),
                    TrangThai = "chua_thu",
                    NoiDung = "Khám và tư vấn chuyên sâu - 20 ngày trước"
                },
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_UNPAID_008",
                    MaBenhNhan = "BN_DEMO_04",
                    MaNhanSuThu = "NV_YT_HC_01",
                    LoaiDotthu = "can_lam_sang",
                    SoTien = 1800000m,
                    SoTienPhaiTra = 1800000m,
                    ThoiGian = clock.Today.AddDays(-20).AddHours(14).AddMinutes(15),
                    TrangThai = "chua_thu",
                    NoiDung = "Siêu âm tim + điện tâm đồ - 20 ngày trước"
                },

                // ===== HÓA ĐƠN CHƯA THU (35 ngày trước - quá 30 ngày, màu đỏ) =====
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_UNPAID_009",
                    MaBenhNhan = "BN_DEMO_02",
                    MaNhanSuThu = "NV_YT_HC_01",
                    LoaiDotthu = "thuoc",
                    SoTien = 6500000m,
                    SoTienPhaiTra = 6500000m,
                    ThoiGian = clock.Today.AddDays(-35).AddHours(10).AddMinutes(45),
                    TrangThai = "chua_thu",
                    NoiDung = "Đơn thuốc đặc trị cao cấp - 35 ngày trước"
                },
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_UNPAID_010",
                    MaBenhNhan = "BN_DEMO_03",
                    MaNhanSuThu = "NV_YT_HC_01",
                    LoaiDotthu = "kham_lam_sang",
                    SoTien = 8200000m,
                    SoTienPhaiTra = 8200000m,
                    ThoiGian = clock.Today.AddDays(-35).AddHours(15).AddMinutes(20),
                    TrangThai = "chua_thu",
                    NoiDung = "Gói khám sức khỏe tổng quát VIP - 35 ngày trước"
                },

                // ===== HÓA ĐƠN CHƯA THU (40 ngày trước - quá 30 ngày, màu đỏ) =====
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_UNPAID_011",
                    MaBenhNhan = "BN_DEMO_01",
                    MaNhanSuThu = "NV_YT_HC_01",
                    LoaiDotthu = "can_lam_sang",
                    SoTien = 5800000m,
                    SoTienPhaiTra = 5800000m,
                    ThoiGian = clock.Today.AddDays(-40).AddHours(11).AddMinutes(30),
                    TrangThai = "chua_thu",
                    NoiDung = "Gói xét nghiệm toàn diện - 40 ngày trước"
                },
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_UNPAID_012",
                    MaBenhNhan = "BN_DEMO_04",
                    MaNhanSuThu = "NV_YT_HC_01",
                    LoaiDotthu = "thuoc",
                    SoTien = 3900000m,
                    SoTienPhaiTra = 3900000m,
                    ThoiGian = clock.Today.AddDays(-40).AddHours(16).AddMinutes(50),
                    TrangThai = "chua_thu",
                    NoiDung = "Thuốc điều trị mãn tính - 40 ngày trước"
                },

                // ===== HÓA ĐƠN CHƯA THU (45 ngày trước - quá 30 ngày, màu đỏ) =====
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_UNPAID_013",
                    MaBenhNhan = "BN_DEMO_02",
                    MaNhanSuThu = "NV_YT_HC_01",
                    LoaiDotthu = "kham_lam_sang",
                    SoTien = 350000m,
                    SoTienPhaiTra = 350000m,
                    ThoiGian = clock.Today.AddDays(-45).AddHours(9).AddMinutes(0),
                    TrangThai = "chua_thu",
                    NoiDung = "Khám tái khám định kỳ - 45 ngày trước"
                },
                new HoaDonThanhToan
                {
                    MaHoaDon = "HD_UNPAID_014",
                    MaBenhNhan = "BN_DEMO_03",
                    MaNhanSuThu = "NV_YT_HC_01",
                    LoaiDotthu = "can_lam_sang",
                    SoTien = 720000m,
                    SoTienPhaiTra = 720000m,
                    ThoiGian = clock.Today.AddDays(-45).AddHours(13).AddMinutes(40),
                    TrangThai = "chua_thu",
                    NoiDung = "X-quang phổi 2 tư thế - 45 ngày trước"
                }
            };

            invoices.AddRange(BuildReportInvoices(clock));
            return invoices;
        }

        private static List<LichSuXuatKho> BuildStockLogs(SeedClock clock) =>
            new()
            {
                new LichSuXuatKho { MaGiaoDich = "XK_DEMO_001", MaThuoc = "THUOC_001", MaDonThuoc = "DT_DEMO_001", MaNhanSuXuat = "NV_YT_HC_01", LoaiGiaoDich = "xuat_ban", SoLuong = 10, SoLuongConLai = 170, ThoiGianXuat = At(clock.Yesterday, 10, 52), GhiChu = "Phát thuốc cho BN_DEMO_01" },
                new LichSuXuatKho { MaGiaoDich = "XK_DEMO_002", MaThuoc = "THUOC_002", MaDonThuoc = "DT_DEMO_001", MaNhanSuXuat = "NV_YT_HC_01", LoaiGiaoDich = "xuat_ban", SoLuong = 7, SoLuongConLai = 103, ThoiGianXuat = At(clock.Yesterday, 10, 52), GhiChu = "Phát thuốc cho BN_DEMO_01" },
                new LichSuXuatKho { MaGiaoDich = "XK_DEMO_003", MaThuoc = "THUOC_005", MaDonThuoc = "DT_DEMO_001", MaNhanSuXuat = "NV_YT_HC_01", LoaiGiaoDich = "xuat_ban", SoLuong = 10, SoLuongConLai = 30, ThoiGianXuat = At(clock.Yesterday, 10, 52), GhiChu = "Phát vitamin cho BN_DEMO_01" }
            };

        private static List<ThongBaoMau> BuildNotificationTemplates(SeedClock clock) =>
            new()
            {
                new ThongBaoMau { MaMau = "TBM_001", TenMau = "Nhắc thanh toán", NoiDungMau = "Bệnh nhân {ten_benh_nhan} đang chờ thanh toán hóa đơn {ma_hoa_don}.", BienDong = "[\"ten_benh_nhan\",\"ma_hoa_don\"]", NgayTao = clock.Today, NgayCapNhat = clock.Today, KichHoat = true },
                new ThongBaoMau { MaMau = "TBM_002", TenMau = "Kết quả CLS sẵn sàng", NoiDungMau = "Kết quả CLS của bệnh nhân {ten_benh_nhan} đã sẵn sàng.", BienDong = "[\"ten_benh_nhan\"]", NgayTao = clock.Today, NgayCapNhat = clock.Today, KichHoat = true },
                new ThongBaoMau { MaMau = "TBM_003", TenMau = "Nhắc tái khám", NoiDungMau = "Bệnh nhân {ten_benh_nhan} có lịch tái khám vào {ngay_hen}.", BienDong = "[\"ten_benh_nhan\",\"ngay_hen\"]", NgayTao = clock.Today, NgayCapNhat = clock.Today, KichHoat = true }
            };

        private static (List<ThongBaoHeThong> notifications, List<ThongBaoNguoiNhan> recipients) BuildNotifications(SeedClock clock)
        {
            var notifications = new List<ThongBaoHeThong>
            {
                new ThongBaoHeThong { MaThongBao = "TB_DEMO_001", TieuDe = "Chờ thanh toán khám", NoiDung = "BN_DEMO_02 đang chờ thanh toán hóa đơn khám.", LoaiThongBao = "thanh_toan", DoUuTien = "trung_binh", ThoiGianGui = At(clock.Today, 8, 52), MaPhieuKham = "PK_DEMO_002", TrangThai = "da_gui" },
                new ThongBaoHeThong { MaThongBao = "TB_DEMO_002", TieuDe = "Chờ thanh toán CLS", NoiDung = "BN_DEMO_03 đang chờ thanh toán phiếu CLS.", LoaiThongBao = "thanh_toan", DoUuTien = "trung_binh", ThoiGianGui = At(clock.Today, 9, 41), MaPhieuKham = "PK_DEMO_003", TrangThai = "da_gui" },
                new ThongBaoHeThong { MaThongBao = "TB_DEMO_003", TieuDe = "Kết quả CLS đã sẵn sàng", NoiDung = "Kết quả CLS của BN_DEMO_01 đã được tổng hợp.", LoaiThongBao = "ket_qua_cls", DoUuTien = "cao", ThoiGianGui = At(clock.Yesterday, 10, 26), MaPhieuKham = "PK_DEMO_001", TrangThai = "da_gui" },
                new ThongBaoHeThong { MaThongBao = "TB_DEMO_004", TieuDe = "Nhắc tái khám", NoiDung = "Bệnh nhân Đỗ Quốc Gia có lịch tái khám vào ngày mai.", LoaiThongBao = "tai_kham", DoUuTien = "trung_binh", ThoiGianGui = At(clock.Today, 16, 0), TrangThai = "da_gui" }
            };

            var recipients = new List<ThongBaoNguoiNhan>
            {
                new ThongBaoNguoiNhan { MaThongBao = "TB_DEMO_001", LoaiNguoiNhan = "nhan_vien_y_te", MaNhanSu = "NV_YT_HC_01", DaDoc = false },
                new ThongBaoNguoiNhan { MaThongBao = "TB_DEMO_001", LoaiNguoiNhan = "benh_nhan", MaBenhNhan = "BN_DEMO_02", DaDoc = false },
                new ThongBaoNguoiNhan { MaThongBao = "TB_DEMO_002", LoaiNguoiNhan = "nhan_vien_y_te", MaNhanSu = "NV_YT_HC_01", DaDoc = false },
                new ThongBaoNguoiNhan { MaThongBao = "TB_DEMO_002", LoaiNguoiNhan = "benh_nhan", MaBenhNhan = "BN_DEMO_03", DaDoc = false },
                new ThongBaoNguoiNhan { MaThongBao = "TB_DEMO_003", LoaiNguoiNhan = "nhan_vien_y_te", MaNhanSu = "NV_BS_NOI_01", DaDoc = true, ThoiGianDoc = At(clock.Yesterday, 10, 30) },
                new ThongBaoNguoiNhan { MaThongBao = "TB_DEMO_003", LoaiNguoiNhan = "benh_nhan", MaBenhNhan = "BN_DEMO_01", DaDoc = true, ThoiGianDoc = At(clock.Yesterday, 10, 40) },
                new ThongBaoNguoiNhan { MaThongBao = "TB_DEMO_004", LoaiNguoiNhan = "benh_nhan", MaBenhNhan = "BN_DEMO_06", DaDoc = false }
            };

            return (notifications, recipients);
        }

        private static IEnumerable<LichHenKham> BuildReportAppointments(SeedClock clock)
        {
            var visitAppointments = GetReportVisitSeeds().Select(seed =>
            {
                var date = clock.Today.AddDays(seed.DayOffset).Date;
                return new LichHenKham
                {
                    MaLichHen = $"LH_RPT_{seed.Suffix}",
                    MaBenhNhan = seed.MaBenhNhan,
                    TenBenhNhan = seed.HoTen,
                    SoDienThoai = seed.DienThoai,
                    LoaiHen = seed.LoaiHen,
                    NgayHen = date,
                    GioHen = Time(seed.Hour, seed.Minute),
                    ThoiLuongPhut = 30,
                    MaLichTruc = ResolveScheduleId(clock, seed.RoomId, date, ResolveShiftByHour(seed.Hour)),
                    GhiChu = seed.GhiChu,
                    TrangThai = seed.TrangThaiLichHen,
                    CoHieuLuc = true
                };
            });

            var extraAppointments = GetReportExtraAppointmentSeeds().Select(seed =>
            {
                var date = clock.Today.AddDays(seed.DayOffset).Date;
                return new LichHenKham
                {
                    MaLichHen = $"LH_RPT_{seed.Suffix}",
                    MaBenhNhan = seed.MaBenhNhan,
                    TenBenhNhan = seed.HoTen,
                    SoDienThoai = seed.DienThoai,
                    LoaiHen = seed.LoaiHen,
                    NgayHen = date,
                    GioHen = Time(seed.Hour, seed.Minute),
                    ThoiLuongPhut = 30,
                    MaLichTruc = ResolveScheduleId(clock, seed.RoomId, date, ResolveShiftByHour(seed.Hour)),
                    GhiChu = seed.GhiChu,
                    TrangThai = seed.TrangThai,
                    CoHieuLuc = true
                };
            });

            return visitAppointments.Concat(extraAppointments);
        }

        private static IEnumerable<PhieuKhamLamSang> BuildReportClinicalForms(SeedClock clock) =>
            GetReportVisitSeeds().Select(seed =>
            {
                var date = clock.Today.AddDays(seed.DayOffset).Date;
                var intakeTime = At(date, seed.Hour, seed.Minute).AddMinutes(6);

                return new PhieuKhamLamSang
                {
                    MaPhieuKham = $"PK_RPT_{seed.Suffix}",
                    MaBacSiKham = seed.DoctorId,
                    MaNguoiLap = seed.ReceptionId,
                    MaBenhNhan = seed.MaBenhNhan,
                    MaLichHen = $"LH_RPT_{seed.Suffix}",
                    MaDichVuKham = seed.ServiceId,
                    HinhThucTiepNhan = "appointment",
                    NgayLap = date,
                    GioLap = intakeTime.TimeOfDay,
                    TrieuChung = seed.TrieuChung,
                    TrangThai = "da_hoan_tat"
                };
            });

        private static IEnumerable<HangDoi> BuildReportClinicalQueues(SeedClock clock) =>
            GetReportVisitSeeds().Select((seed, index) =>
            {
                var date = clock.Today.AddDays(seed.DayOffset).Date;
                var appointmentTime = At(date, seed.Hour, seed.Minute);
                var checkInTime = appointmentTime.AddMinutes(-12);

                return new HangDoi
                {
                    MaHangDoi = $"HQ_RPT_{seed.Suffix}",
                    MaBenhNhan = seed.MaBenhNhan,
                    MaPhong = seed.RoomId,
                    LoaiHangDoi = "kham_lam_sang",
                    Nguon = "appointment",
                    Nhan = $"R{index + 1:000}",
                    CapCuu = false,
                    PhanLoaiDen = "dung_gio",
                    ThoiGianCheckin = checkInTime,
                    ThoiGianLichHen = appointmentTime,
                    DoUuTien = seed.LoaiLuot == "tai_kham" ? 3 : 2,
                    TrangThai = "da_phuc_vu",
                    GhiChu = seed.LoaiLuot == "tai_kham"
                        ? "Tái khám theo lịch hẹn, đã hoàn tất trong ngày"
                        : "Khám mới phục vụ báo cáo lịch sử",
                    MaPhieuKham = $"PK_RPT_{seed.Suffix}",
                    SoLanGoi = 1,
                    ThoiGianGoiGanNhat = checkInTime.AddMinutes(8),
                    NgayTao = checkInTime,
                    NgayCapNhat = checkInTime.AddMinutes(55)
                };
            });

        private static IEnumerable<LuotKhamBenh> BuildReportClinicalVisits(SeedClock clock) =>
            GetReportVisitSeeds().Select((seed, index) =>
            {
                var date = clock.Today.AddDays(seed.DayOffset).Date;
                var startedAt = At(date, seed.Hour, seed.Minute).AddMinutes(18);
                var duration = seed.LoaiLuot == "tai_kham" ? 18 : 24;
                var finishedAt = startedAt.AddMinutes(duration);

                return new LuotKhamBenh
                {
                    MaLuotKham = $"LK_RPT_{seed.Suffix}",
                    MaHangDoi = $"HQ_RPT_{seed.Suffix}",
                    MaNhanSuThucHien = seed.DoctorId,
                    MaYTaHoTro = seed.ClinicalNurseId,
                    LoaiLuot = seed.LoaiLuot,
                    ThoiGianBatDau = startedAt,
                    ThoiGianKetThuc = finishedAt,
                    TrangThai = "hoan_tat",
                    ThoiGianThucTe = finishedAt,
                    SinhHieuTruocKham = $"{{\"nhiet_do\":36.{(index % 4) + 6},\"huyet_ap\":\"11{index % 3 + 5}/7{index % 4 + 1}\",\"mach\":{74 + index % 14}}}",
                    GhiChu = seed.LoaiLuot == "tai_kham"
                        ? "Lượt tái khám đã hoàn tất, ghi nhận đáp ứng điều trị tốt"
                        : "Khám mới đã hoàn tất và cập nhật đủ hồ sơ",
                    NgayTao = startedAt,
                    NgayCapNhat = finishedAt
                };
            });

        private static IEnumerable<HoaDonThanhToan> BuildReportInvoices(SeedClock clock) =>
            GetReportVisitSeeds()
                .Where(seed => seed.TaoHoaDon)
                .Select((seed, index) =>
                {
                    var date = clock.Today.AddDays(seed.DayOffset).Date;
                    var paidAt = At(date, seed.Hour, seed.Minute).AddMinutes(4);
                    var method = (index % 3) switch
                    {
                        1 => "chuyen_khoan",
                        2 => "vietqr",
                        _ => "tien_mat"
                    };

                    return new HoaDonThanhToan
                    {
                        MaHoaDon = $"HD_RPT_{seed.Suffix}",
                        MaBenhNhan = seed.MaBenhNhan,
                        MaNhanSuThu = seed.ReceptionId,
                        MaPhieuKham = $"PK_RPT_{seed.Suffix}",
                        LoaiDotthu = "kham_lam_sang",
                        SoTien = seed.SoTienHoaDon,
                        SoTienPhaiTra = seed.SoTienHoaDon,
                        MaGiaoDich = $"GD-RPT-{seed.Suffix}",
                        PhuongThucThanhToan = method,
                        ThoiGian = paidAt,
                        TrangThai = "da_thu",
                        NoiDung = seed.NoiDungHoaDon
                    };
                });

        private static string ResolveShiftByHour(int hour) =>
            hour >= 17 ? "toi" : hour >= 12 ? "chieu" : "sang";

        private static IReadOnlyList<ReportPatientSeed> GetReportPatientSeeds() =>
            new[]
            {
                new ReportPatientSeed { MaBenhNhan = "BN_RPT_01", HoTen = "Ngô Thảo My", Tuoi = 26, GioiTinh = "Nữ", DienThoai = "0911001011", Email = "thao.my@demo.local", DiaChi = "Quận 2, TP HCM", TieuSuBenh = "Khám sức khỏe định kỳ", NhomMau = "A+", CreatedOffsetDays = -70 },
                new ReportPatientSeed { MaBenhNhan = "BN_RPT_02", HoTen = "Trương Hải Đăng", Tuoi = 33, GioiTinh = "Nam", DienThoai = "0911001012", Email = "hai.dang@demo.local", DiaChi = "Quận 12, TP HCM", TieuSuBenh = "Đau vai gáy kéo dài", NhomMau = "O+", CreatedOffsetDays = -64 },
                new ReportPatientSeed { MaBenhNhan = "BN_RPT_03", HoTen = "Bùi Diệu Linh", Tuoi = 29, GioiTinh = "Nữ", DienThoai = "0911001013", Email = "dieu.linh@demo.local", DiaChi = "Quận 4, TP HCM", TieuSuBenh = "Khó ngủ, đau đầu nhẹ", NhomMau = "B+", CreatedOffsetDays = -58 },
                new ReportPatientSeed { MaBenhNhan = "BN_RPT_04", HoTen = "Đặng Khánh Nguyên", Tuoi = 41, GioiTinh = "Nam", DienThoai = "0911001014", Email = "khanh.nguyen@demo.local", DiaChi = "Phú Nhuận, TP HCM", TieuSuBenh = "Đau răng số 6", NhomMau = "AB+", CreatedOffsetDays = -54 },
                new ReportPatientSeed { MaBenhNhan = "BN_RPT_05", HoTen = "Lý Minh Khang", Tuoi = 37, GioiTinh = "Nam", DienThoai = "0911001015", Email = "minh.khang@demo.local", DiaChi = "Quận 11, TP HCM", TieuSuBenh = "Viêm họng tái phát", NhomMau = "A+", CreatedOffsetDays = -49 },
                new ReportPatientSeed { MaBenhNhan = "BN_RPT_06", HoTen = "Phạm Uyển Nhi", Tuoi = 24, GioiTinh = "Nữ", DienThoai = "0911001016", Email = "uyen.nhi@demo.local", DiaChi = "Bình Tân, TP HCM", TieuSuBenh = "Đầy bụng sau ăn", NhomMau = "O+", CreatedOffsetDays = -44 },
                new ReportPatientSeed { MaBenhNhan = "BN_RPT_07", HoTen = "Vũ Quốc Thịnh", Tuoi = 35, GioiTinh = "Nam", DienThoai = "0911001017", Email = "quoc.thinh@demo.local", DiaChi = "Thủ Đức, TP HCM", TieuSuBenh = "Đau vùng hông lưng", NhomMau = "B+", CreatedOffsetDays = -39 },
                new ReportPatientSeed { MaBenhNhan = "BN_RPT_08", HoTen = "Nguyễn Tường Vi", Tuoi = 31, GioiTinh = "Nữ", DienThoai = "0911001018", Email = "tuong.vi@demo.local", DiaChi = "Quận 6, TP HCM", TieuSuBenh = "Sốt nhẹ kéo dài", NhomMau = "A+", CreatedOffsetDays = -34 },
                new ReportPatientSeed { MaBenhNhan = "BN_RPT_09", HoTen = "Trần Gia Hân", Tuoi = 27, GioiTinh = "Nữ", DienThoai = "0911001019", Email = "gia.han@demo.local", DiaChi = "Quận 8, TP HCM", TieuSuBenh = "Khó tiêu, đau thượng vị", NhomMau = "O+", CreatedOffsetDays = -28 },
                new ReportPatientSeed { MaBenhNhan = "BN_RPT_10", HoTen = "Mai Hoàng Nam", Tuoi = 39, GioiTinh = "Nam", DienThoai = "0911001020", Email = "hoang.nam@demo.local", DiaChi = "Tân Bình, TP HCM", TieuSuBenh = "Nghẹt mũi, đau đầu vùng trán", NhomMau = "B+", CreatedOffsetDays = -22 },
                new ReportPatientSeed { MaBenhNhan = "BN_RPT_11", HoTen = "Đinh Thanh Trúc", Tuoi = 34, GioiTinh = "Nữ", DienThoai = "0911001021", Email = "thanh.truc@demo.local", DiaChi = "Tân Phú, TP HCM", TieuSuBenh = "Mệt mỏi, chóng mặt nhẹ", NhomMau = "AB+", CreatedOffsetDays = -18 },
                new ReportPatientSeed { MaBenhNhan = "BN_RPT_12", HoTen = "Tạ Minh Quân", Tuoi = 42, GioiTinh = "Nam", DienThoai = "0911001022", Email = "minh.quan@demo.local", DiaChi = "Quận 9, TP HCM", TieuSuBenh = "Đau vai, tê tay", NhomMau = "A+", CreatedOffsetDays = -14 },
                new ReportPatientSeed { MaBenhNhan = "BN_RPT_13", HoTen = "Hồ Gia Linh", Tuoi = 22, GioiTinh = "Nữ", DienThoai = "0911001023", Email = "gia.linh@demo.local", DiaChi = "Bình Thạnh, TP HCM", TieuSuBenh = "Đau bụng kinh kèm mệt mỏi", NhomMau = "O+", CreatedOffsetDays = -9 },
                new ReportPatientSeed { MaBenhNhan = "BN_RPT_14", HoTen = "Kiều Anh Tuấn", Tuoi = 45, GioiTinh = "Nam", DienThoai = "0911001024", Email = "anh.tuan@demo.local", DiaChi = "Gò Vấp, TP HCM", TieuSuBenh = "Khám kiểm tra sau giai đoạn căng thẳng", NhomMau = "B+", CreatedOffsetDays = -6 }
            };

        private static IReadOnlyList<ReportVisitSeed> GetReportVisitSeeds() =>
            new[]
            {
                new ReportVisitSeed { Suffix = "101", MaBenhNhan = "BN_RPT_01", HoTen = "Ngô Thảo My", DienThoai = "0911001011", DayOffset = -34, Hour = 8, Minute = 10, LoaiHen = "kham_moi", TrangThaiLichHen = "da_checkin", RoomId = "PK_NOI_01", ServiceId = "DV_KHAM_NOI_01", DoctorId = "NV_BS_NOI_01", ClinicalNurseId = "NV_YT_LS_01", ReceptionId = "NV_YT_HC_01", TrieuChung = "Khám sức khỏe tổng quát, dễ mệt cuối ngày", GhiChu = "Bệnh nhân mới từ chương trình doanh nghiệp", LoaiLuot = "kham_lam_sang", TaoHoaDon = true, SoTienHoaDon = 150000m, NoiDungHoaDon = "Thu tiền khám nội tổng quát - đợt báo cáo" },
                new ReportVisitSeed { Suffix = "102", MaBenhNhan = "BN_RPT_02", HoTen = "Trương Hải Đăng", DienThoai = "0911001012", DayOffset = -29, Hour = 9, Minute = 20, LoaiHen = "kham_moi", TrangThaiLichHen = "da_checkin", RoomId = "PK_NGOAI_01", ServiceId = "DV_KHAM_NGOAI_01", DoctorId = "NV_BS_NGOAI_01", ClinicalNurseId = "NV_YT_LS_02", ReceptionId = "NV_YT_HC_01", TrieuChung = "Đau vai gáy và tê cánh tay phải", GhiChu = "Bệnh nhân mới do đồng nghiệp giới thiệu", LoaiLuot = "kham_lam_sang", TaoHoaDon = true, SoTienHoaDon = 180000m, NoiDungHoaDon = "Thu tiền khám ngoại tổng quát - đợt báo cáo" },
                new ReportVisitSeed { Suffix = "103", MaBenhNhan = "BN_RPT_03", HoTen = "Bùi Diệu Linh", DienThoai = "0911001013", DayOffset = -20, Hour = 8, Minute = 35, LoaiHen = "kham_moi", TrangThaiLichHen = "da_checkin", RoomId = "PK_NOI_02", ServiceId = "DV_KHAM_NOI_02", DoctorId = "NV_BS_NOI_02", ClinicalNurseId = "NV_YT_LS_03", ReceptionId = "NV_YT_HC_02", TrieuChung = "Đau đầu nhẹ và khó ngủ kéo dài 2 tuần", GhiChu = "Khách hàng doanh nghiệp khám định kỳ", LoaiLuot = "kham_lam_sang", TaoHoaDon = true, SoTienHoaDon = 180000m, NoiDungHoaDon = "Thu tiền khám nội nâng cao - đợt báo cáo" },
                new ReportVisitSeed { Suffix = "104", MaBenhNhan = "BN_RPT_04", HoTen = "Đặng Khánh Nguyên", DienThoai = "0911001014", DayOffset = -19, Hour = 10, Minute = 5, LoaiHen = "kham_moi", TrangThaiLichHen = "da_checkin", RoomId = "PK_RHM_01", ServiceId = "DV_KHAM_RHM_01", DoctorId = "NV_BS_RHM_01", ClinicalNurseId = "NV_YT_LS_06", ReceptionId = "NV_YT_HC_02", TrieuChung = "Đau buốt răng hàm và ê khi uống lạnh", GhiChu = "Khách lẻ từ kênh đặt lịch online", LoaiLuot = "kham_lam_sang", TaoHoaDon = true, SoTienHoaDon = 200000m, NoiDungHoaDon = "Thu tiền khám răng hàm mặt - đợt báo cáo" },
                new ReportVisitSeed { Suffix = "105", MaBenhNhan = "BN_RPT_05", HoTen = "Lý Minh Khang", DienThoai = "0911001015", DayOffset = -18, Hour = 13, Minute = 15, LoaiHen = "kham_moi", TrangThaiLichHen = "da_checkin", RoomId = "PK_TMH_01", ServiceId = "DV_KHAM_TMH_01", DoctorId = "NV_BS_TMH_01", ClinicalNurseId = "NV_YT_LS_07", ReceptionId = "NV_YT_HC_03", TrieuChung = "Đau họng và nghẹt mũi lúc sáng sớm", GhiChu = "Bệnh nhân mới đến khám sau khi tự mua thuốc không đỡ", LoaiLuot = "kham_lam_sang", TaoHoaDon = true, SoTienHoaDon = 190000m, NoiDungHoaDon = "Thu tiền khám tai mũi họng - đợt báo cáo" },
                new ReportVisitSeed { Suffix = "106", MaBenhNhan = "BN_RPT_06", HoTen = "Phạm Uyển Nhi", DienThoai = "0911001016", DayOffset = -16, Hour = 14, Minute = 10, LoaiHen = "kham_moi", TrangThaiLichHen = "da_checkin", RoomId = "PK_NOI_01", ServiceId = "DV_KHAM_NOI_01", DoctorId = "NV_BS_NOI_01", ClinicalNurseId = "NV_YT_LS_01", ReceptionId = "NV_YT_HC_01", TrieuChung = "Đầy bụng, ợ chua sau bữa tối", GhiChu = "Khách đặt lịch qua hotline", LoaiLuot = "kham_lam_sang", TaoHoaDon = true, SoTienHoaDon = 150000m, NoiDungHoaDon = "Thu tiền khám nội tổng quát - đợt báo cáo" },
                new ReportVisitSeed { Suffix = "107", MaBenhNhan = "BN_RPT_07", HoTen = "Vũ Quốc Thịnh", DienThoai = "0911001017", DayOffset = -15, Hour = 8, Minute = 25, LoaiHen = "kham_moi", TrangThaiLichHen = "da_checkin", RoomId = "PK_NGOAI_02", ServiceId = "DV_KHAM_NGOAI_02", DoctorId = "NV_BS_NGOAI_02", ClinicalNurseId = "NV_YT_LS_04", ReceptionId = "NV_YT_HC_01", TrieuChung = "Đau vùng hông lưng sau vận động mạnh", GhiChu = "Bệnh nhân mới khám ngoại chuyên sâu", LoaiLuot = "kham_lam_sang", TaoHoaDon = true, SoTienHoaDon = 220000m, NoiDungHoaDon = "Thu tiền khám ngoại tiểu phẫu - đợt báo cáo" },
                new ReportVisitSeed { Suffix = "108", MaBenhNhan = "BN_RPT_08", HoTen = "Nguyễn Tường Vi", DienThoai = "0911001018", DayOffset = -9, Hour = 9, Minute = 40, LoaiHen = "kham_moi", TrangThaiLichHen = "da_checkin", RoomId = "PK_NHI_01", ServiceId = "DV_KHAM_NHI_01", DoctorId = "NV_BS_NHI_01", ClinicalNurseId = "NV_YT_LS_05", ReceptionId = "NV_YT_HC_02", TrieuChung = "Sốt nhẹ, mệt mỏi và chán ăn", GhiChu = "Khám mới trong đợt cao điểm đầu tháng", LoaiLuot = "kham_lam_sang", TaoHoaDon = true, SoTienHoaDon = 160000m, NoiDungHoaDon = "Thu tiền khám nhi tổng quát - đợt báo cáo" },
                new ReportVisitSeed { Suffix = "109", MaBenhNhan = "BN_RPT_09", HoTen = "Trần Gia Hân", DienThoai = "0911001019", DayOffset = -8, Hour = 10, Minute = 5, LoaiHen = "kham_moi", TrangThaiLichHen = "da_checkin", RoomId = "PK_NOI_02", ServiceId = "DV_KHAM_NOI_02", DoctorId = "NV_BS_NOI_02", ClinicalNurseId = "NV_YT_LS_03", ReceptionId = "NV_YT_HC_02", TrieuChung = "Khó tiêu và đau thượng vị sau ăn cay", GhiChu = "Khách đặt hẹn trực tuyến trong tuần", LoaiLuot = "kham_lam_sang", TaoHoaDon = true, SoTienHoaDon = 180000m, NoiDungHoaDon = "Thu tiền khám nội nâng cao - đợt báo cáo" },
                new ReportVisitSeed { Suffix = "110", MaBenhNhan = "BN_RPT_10", HoTen = "Mai Hoàng Nam", DienThoai = "0911001020", DayOffset = -7, Hour = 13, Minute = 35, LoaiHen = "kham_moi", TrangThaiLichHen = "da_checkin", RoomId = "PK_TMH_01", ServiceId = "DV_KHAM_TMH_01", DoctorId = "NV_BS_TMH_01", ClinicalNurseId = "NV_YT_LS_07", ReceptionId = "NV_YT_HC_03", TrieuChung = "Đau đầu vùng trán và nghẹt mũi ban đêm", GhiChu = "Khách hàng quay lại hệ thống sau thời gian dài không khám", LoaiLuot = "kham_lam_sang", TaoHoaDon = true, SoTienHoaDon = 190000m, NoiDungHoaDon = "Thu tiền khám tai mũi họng - đợt báo cáo" },
                new ReportVisitSeed { Suffix = "111", MaBenhNhan = "BN_RPT_11", HoTen = "Đinh Thanh Trúc", DienThoai = "0911001021", DayOffset = -5, Hour = 14, Minute = 20, LoaiHen = "kham_moi", TrangThaiLichHen = "da_checkin", RoomId = "PK_NOI_01", ServiceId = "DV_KHAM_NOI_01", DoctorId = "NV_BS_NOI_01", ClinicalNurseId = "NV_YT_LS_01", ReceptionId = "NV_YT_HC_01", TrieuChung = "Mệt mỏi và chóng mặt khi đứng lên nhanh", GhiChu = "Khách mới từ chương trình khám định kỳ công ty", LoaiLuot = "kham_lam_sang", TaoHoaDon = true, SoTienHoaDon = 150000m, NoiDungHoaDon = "Thu tiền khám nội tổng quát - đợt báo cáo" },
                new ReportVisitSeed { Suffix = "112", MaBenhNhan = "BN_RPT_12", HoTen = "Tạ Minh Quân", DienThoai = "0911001022", DayOffset = -4, Hour = 18, Minute = 5, LoaiHen = "kham_moi", TrangThaiLichHen = "da_checkin", RoomId = "PK_NGOAI_02", ServiceId = "DV_KHAM_NGOAI_02", DoctorId = "NV_BS_NGOAI_02", ClinicalNurseId = "NV_YT_LS_04", ReceptionId = "NV_YT_HC_01", TrieuChung = "Đau vai và tê tay sau nhiều giờ làm việc", GhiChu = "Khung tối phục vụ bệnh nhân đi làm giờ hành chính", LoaiLuot = "kham_lam_sang", TaoHoaDon = true, SoTienHoaDon = 220000m, NoiDungHoaDon = "Thu tiền khám ngoại tiểu phẫu - đợt báo cáo" },
                new ReportVisitSeed { Suffix = "113", MaBenhNhan = "BN_RPT_13", HoTen = "Hồ Gia Linh", DienThoai = "0911001023", DayOffset = -2, Hour = 18, Minute = 25, LoaiHen = "kham_moi", TrangThaiLichHen = "da_checkin", RoomId = "PK_NHI_01", ServiceId = "DV_KHAM_NHI_01", DoctorId = "NV_BS_NHI_01", ClinicalNurseId = "NV_YT_LS_05", ReceptionId = "NV_YT_HC_02", TrieuChung = "Mệt mỏi, đau bụng từng cơn", GhiChu = "Khung tối đầu tuần để tăng dữ liệu check-in", LoaiLuot = "kham_lam_sang", TaoHoaDon = true, SoTienHoaDon = 160000m, NoiDungHoaDon = "Thu tiền khám nhi tổng quát - đợt báo cáo" },
                new ReportVisitSeed { Suffix = "114", MaBenhNhan = "BN_RPT_14", HoTen = "Kiều Anh Tuấn", DienThoai = "0911001024", DayOffset = -1, Hour = 19, Minute = 0, LoaiHen = "kham_moi", TrangThaiLichHen = "da_checkin", RoomId = "PK_NOI_02", ServiceId = "DV_KHAM_NOI_02", DoctorId = "NV_BS_NOI_02", ClinicalNurseId = "NV_YT_LS_03", ReceptionId = "NV_YT_HC_03", TrieuChung = "Mất ngủ và căng thẳng kéo dài", GhiChu = "Khách doanh nghiệp khám cuối ngày", LoaiLuot = "kham_lam_sang", TaoHoaDon = true, SoTienHoaDon = 180000m, NoiDungHoaDon = "Thu tiền khám nội nâng cao - đợt báo cáo" },

                new ReportVisitSeed { Suffix = "201", MaBenhNhan = "BN_DEMO_01", HoTen = "Lê Minh An", DienThoai = "0911000003", DayOffset = -33, Hour = 8, Minute = 15, LoaiHen = "tai_kham", TrangThaiLichHen = "da_checkin", RoomId = "PK_NOI_01", ServiceId = "DV_KHAM_NOI_01", DoctorId = "NV_BS_NOI_01", ClinicalNurseId = "NV_YT_LS_01", ReceptionId = "NV_YT_HC_01", TrieuChung = "Tái khám theo dõi hen phế quản", GhiChu = "Tái khám hô hấp đầu kỳ so sánh", LoaiLuot = "tai_kham", TaoHoaDon = false, SoTienHoaDon = 0m, NoiDungHoaDon = "" },
                new ReportVisitSeed { Suffix = "202", MaBenhNhan = "BN_DEMO_02", HoTen = "Phạm Gia Bảo", DienThoai = "0911000004", DayOffset = -28, Hour = 9, Minute = 0, LoaiHen = "tai_kham", TrangThaiLichHen = "da_checkin", RoomId = "PK_NOI_01", ServiceId = "DV_KHAM_NOI_01", DoctorId = "NV_BS_NOI_01", ClinicalNurseId = "NV_YT_LS_01", ReceptionId = "NV_YT_HC_01", TrieuChung = "Tái khám theo dõi đáp ứng thuốc dạ dày", GhiChu = "Tái khám hệ tiêu hóa", LoaiLuot = "tai_kham", TaoHoaDon = false, SoTienHoaDon = 0m, NoiDungHoaDon = "" },
                new ReportVisitSeed { Suffix = "203", MaBenhNhan = "BN_DEMO_03", HoTen = "Trần Thu Chi", DienThoai = "0911000005", DayOffset = -21, Hour = 14, Minute = 5, LoaiHen = "tai_kham", TrangThaiLichHen = "da_checkin", RoomId = "PK_NGOAI_01", ServiceId = "DV_KHAM_NGOAI_01", DoctorId = "NV_BS_NGOAI_01", ClinicalNurseId = "NV_YT_LS_02", ReceptionId = "NV_YT_HC_01", TrieuChung = "Tái khám sau theo dõi đau bụng cấp", GhiChu = "Tái khám ngoại đầu chu kỳ trước", LoaiLuot = "tai_kham", TaoHoaDon = false, SoTienHoaDon = 0m, NoiDungHoaDon = "" },
                new ReportVisitSeed { Suffix = "204", MaBenhNhan = "BN_DEMO_04", HoTen = "Hoàng Minh Dũng", DienThoai = "0911000006", DayOffset = -17, Hour = 15, Minute = 10, LoaiHen = "tai_kham", TrangThaiLichHen = "da_checkin", RoomId = "PK_NGOAI_02", ServiceId = "DV_KHAM_NGOAI_02", DoctorId = "NV_BS_NGOAI_02", ClinicalNurseId = "NV_YT_LS_04", ReceptionId = "NV_YT_HC_01", TrieuChung = "Tái khám kiểm tra sau đợt điều trị tiêu hóa", GhiChu = "Tái khám ngoại chuyên sâu", LoaiLuot = "tai_kham", TaoHoaDon = false, SoTienHoaDon = 0m, NoiDungHoaDon = "" },
                new ReportVisitSeed { Suffix = "205", MaBenhNhan = "BN_DEMO_06", HoTen = "Đỗ Quốc Gia", DienThoai = "0911000008", DayOffset = -13, Hour = 9, Minute = 50, LoaiHen = "tai_kham", TrangThaiLichHen = "da_checkin", RoomId = "PK_NOI_02", ServiceId = "DV_KHAM_NOI_02", DoctorId = "NV_BS_NOI_02", ClinicalNurseId = "NV_YT_LS_03", ReceptionId = "NV_YT_HC_02", TrieuChung = "Tái khám theo dõi sau điều trị tuần trước", GhiChu = "Tái khám nội có hồ sơ cũ đầy đủ", LoaiLuot = "tai_kham", TaoHoaDon = false, SoTienHoaDon = 0m, NoiDungHoaDon = "" },
                new ReportVisitSeed { Suffix = "206", MaBenhNhan = "BN_DEMO_07", HoTen = "Nguyễn Bảo Châu", DienThoai = "0911000009", DayOffset = -10, Hour = 8, Minute = 45, LoaiHen = "tai_kham", TrangThaiLichHen = "da_checkin", RoomId = "PK_NHI_01", ServiceId = "DV_KHAM_NHI_01", DoctorId = "NV_BS_NHI_01", ClinicalNurseId = "NV_YT_LS_05", ReceptionId = "NV_YT_HC_02", TrieuChung = "Tái khám viêm mũi dị ứng ở trẻ", GhiChu = "Tái khám nhi giữa tháng", LoaiLuot = "tai_kham", TaoHoaDon = false, SoTienHoaDon = 0m, NoiDungHoaDon = "" },
                new ReportVisitSeed { Suffix = "207", MaBenhNhan = "BN_DEMO_08", HoTen = "Trần Đức Huy", DienThoai = "0911000010", DayOffset = -6, Hour = 13, Minute = 50, LoaiHen = "tai_kham", TrangThaiLichHen = "da_checkin", RoomId = "PK_RHM_01", ServiceId = "DV_KHAM_RHM_01", DoctorId = "NV_BS_RHM_01", ClinicalNurseId = "NV_YT_LS_06", ReceptionId = "NV_YT_HC_03", TrieuChung = "Tái khám kiểm tra răng sau xử trí ban đầu", GhiChu = "Tái khám răng hàm mặt", LoaiLuot = "tai_kham", TaoHoaDon = false, SoTienHoaDon = 0m, NoiDungHoaDon = "" },
                new ReportVisitSeed { Suffix = "208", MaBenhNhan = "BN_DEMO_09", HoTen = "Phan Quỳnh Như", DienThoai = "0911000011", DayOffset = -3, Hour = 15, Minute = 10, LoaiHen = "tai_kham", TrangThaiLichHen = "da_checkin", RoomId = "PK_TMH_01", ServiceId = "DV_KHAM_TMH_01", DoctorId = "NV_BS_TMH_01", ClinicalNurseId = "NV_YT_LS_07", ReceptionId = "NV_YT_HC_03", TrieuChung = "Tái khám đau họng tái phát", GhiChu = "Tái khám TMH cuối kỳ hiện tại", LoaiLuot = "tai_kham", TaoHoaDon = false, SoTienHoaDon = 0m, NoiDungHoaDon = "" },
                new ReportVisitSeed { Suffix = "209", MaBenhNhan = "BN_DEMO_10", HoTen = "Lê Hoàng Vy", DienThoai = "0911000012", DayOffset = -1, Hour = 18, Minute = 40, LoaiHen = "tai_kham", TrangThaiLichHen = "da_checkin", RoomId = "PK_NOI_02", ServiceId = "DV_KHAM_NOI_02", DoctorId = "NV_BS_NOI_02", ClinicalNurseId = "NV_YT_LS_03", ReceptionId = "NV_YT_HC_02", TrieuChung = "Tái khám theo dõi đường huyết", GhiChu = "Tái khám nội buổi tối", LoaiLuot = "tai_kham", TaoHoaDon = false, SoTienHoaDon = 0m, NoiDungHoaDon = "" },
                new ReportVisitSeed { Suffix = "210", MaBenhNhan = "BN_DEMO_01", HoTen = "Lê Minh An", DienThoai = "0911000003", DayOffset = 0, Hour = 9, Minute = 25, LoaiHen = "tai_kham", TrangThaiLichHen = "da_checkin", RoomId = "PK_NOI_01", ServiceId = "DV_KHAM_NOI_01", DoctorId = "NV_BS_NOI_01", ClinicalNurseId = "NV_YT_LS_01", ReceptionId = "NV_YT_HC_01", TrieuChung = "Tái khám trong ngày để đánh giá tiếp diễn triệu chứng", GhiChu = "Tái khám hiện tại giúp KPI hôm nay có dữ liệu", LoaiLuot = "tai_kham", TaoHoaDon = false, SoTienHoaDon = 0m, NoiDungHoaDon = "" }
            };

        private static IReadOnlyList<ReportExtraAppointmentSeed> GetReportExtraAppointmentSeeds() =>
            new[]
            {
                new ReportExtraAppointmentSeed { Suffix = "301", MaBenhNhan = "BN_DEMO_05", HoTen = "Võ Thanh Em", DienThoai = "0911000007", DayOffset = -31, Hour = 10, Minute = 15, LoaiHen = "kham_moi", TrangThai = "da_huy", RoomId = "PK_NGOAI_01", GhiChu = "Bệnh nhân hủy vì trùng lịch cá nhân" },
                new ReportExtraAppointmentSeed { Suffix = "302", MaBenhNhan = "BN_DEMO_06", HoTen = "Đỗ Quốc Gia", DienThoai = "0911000008", DayOffset = -24, Hour = 9, Minute = 10, LoaiHen = "tai_kham", TrangThai = "da_huy", RoomId = "PK_NOI_01", GhiChu = "Tái khám lùi lịch do bận công tác" },
                new ReportExtraAppointmentSeed { Suffix = "303", MaBenhNhan = "BN_RPT_02", HoTen = "Trương Hải Đăng", DienThoai = "0911001012", DayOffset = -20, Hour = 15, Minute = 30, LoaiHen = "tai_kham", TrangThai = "da_huy", RoomId = "PK_NOI_02", GhiChu = "Đã xác nhận nhưng hủy vào phút cuối" },
                new ReportExtraAppointmentSeed { Suffix = "304", MaBenhNhan = "BN_DEMO_08", HoTen = "Trần Đức Huy", DienThoai = "0911000010", DayOffset = -14, Hour = 13, Minute = 20, LoaiHen = "kham_moi", TrangThai = "da_xac_nhan", RoomId = "PK_RHM_01", GhiChu = "Lịch hẹn đã xác nhận nhưng chưa đến ngày" },
                new ReportExtraAppointmentSeed { Suffix = "305", MaBenhNhan = "BN_DEMO_09", HoTen = "Phan Quỳnh Như", DienThoai = "0911000011", DayOffset = -12, Hour = 9, Minute = 25, LoaiHen = "kham_moi", TrangThai = "da_huy", RoomId = "PK_TMH_01", GhiChu = "Bệnh nhân hủy do triệu chứng đã giảm" },
                new ReportExtraAppointmentSeed { Suffix = "306", MaBenhNhan = "BN_DEMO_05", HoTen = "Võ Thanh Em", DienThoai = "0911000007", DayOffset = -7, Hour = 10, Minute = 45, LoaiHen = "kham_moi", TrangThai = "dang_cho", RoomId = "PK_NGOAI_01", GhiChu = "Lịch chờ xác nhận lại từ bệnh nhân" },
                new ReportExtraAppointmentSeed { Suffix = "307", MaBenhNhan = "BN_DEMO_06", HoTen = "Đỗ Quốc Gia", DienThoai = "0911000008", DayOffset = -6, Hour = 8, Minute = 55, LoaiHen = "tai_kham", TrangThai = "da_huy", RoomId = "PK_NOI_01", GhiChu = "Tái khám bị hủy do đổi lịch bác sĩ" },
                new ReportExtraAppointmentSeed { Suffix = "308", MaBenhNhan = "BN_RPT_04", HoTen = "Đặng Khánh Nguyên", DienThoai = "0911001014", DayOffset = -4, Hour = 16, Minute = 10, LoaiHen = "kham_moi", TrangThai = "da_xac_nhan", RoomId = "PK_RHM_01", GhiChu = "Lịch theo dõi sau khám đầu tiên" },
                new ReportExtraAppointmentSeed { Suffix = "309", MaBenhNhan = "BN_DEMO_07", HoTen = "Nguyễn Bảo Châu", DienThoai = "0911000009", DayOffset = -2, Hour = 8, Minute = 15, LoaiHen = "kham_moi", TrangThai = "da_huy", RoomId = "PK_NHI_01", GhiChu = "Bệnh nhi hủy do phụ huynh xin đổi ngày" },
                new ReportExtraAppointmentSeed { Suffix = "310", MaBenhNhan = "BN_DEMO_02", HoTen = "Phạm Gia Bảo", DienThoai = "0911000004", DayOffset = 0, Hour = 11, Minute = 5, LoaiHen = "tai_kham", TrangThai = "da_xac_nhan", RoomId = "PK_NOI_01", GhiChu = "Lịch tái khám đã xác nhận trong ngày" }
            };

        private static IEnumerable<DateTime> GetFullWeekDates(SeedClock clock) =>
            Enumerable.Range(-45, 76)
                .Select(offset => clock.Today.AddDays(offset).Date);

        private static void AddGeneratedSchedule(List<LichTruc> schedules, string roomId, DateTime date, string shift)
        {
            if (schedules.Any(x => x.MaPhong == roomId && x.Ngay.Date == date.Date && x.CaTruc == shift))
            {
                return;
            }

            var window = GetShiftWindow(roomId, shift);
            schedules.Add(new LichTruc
            {
                MaLichTruc = BuildGeneratedScheduleId(roomId, date, shift),
                Ngay = date.Date,
                CaTruc = shift,
                GioBatDau = window.start,
                GioKetThuc = window.end,
                MaYTaTruc = ResolveDutyStaff(roomId, date, shift),
                MaPhong = roomId
            });
        }

        private static string BuildGeneratedScheduleId(string roomId, DateTime date, string shift)
        {
            var suffix = shift switch
            {
                "sang" => "AM",
                "chieu" => "PM",
                "toi" => "EV",
                _ => shift.ToUpperInvariant()
            };

            return $"LT_{roomId}_{date:yyyyMMdd}_{suffix}";
        }

        private static string ResolveScheduleId(SeedClock clock, string roomId, DateTime date, string shift)
        {
            var normalizedDate = date.Date;

            if (normalizedDate == clock.Yesterday.Date && roomId == "PK_NOI_01" && shift == "sang")
            {
                return "LT_PK_NOI_YD_AM";
            }

            if (normalizedDate == clock.Yesterday.Date && roomId == "PK_NGOAI_01" && shift == "chieu")
            {
                return "LT_PK_NGOAI_YD_PM";
            }

            if (normalizedDate == clock.Today.Date && roomId == "PK_NOI_01" && shift == "sang")
            {
                return "LT_PK_NOI_TD_AM";
            }

            if (normalizedDate == clock.Today.Date && roomId == "PK_NGOAI_01" && shift == "sang")
            {
                return "LT_PK_NGOAI_TD_AM";
            }

            if (normalizedDate == clock.Today.Date && roomId == "CLS_XN_01" && shift == "sang")
            {
                return "LT_XN_TD_AM";
            }

            if (normalizedDate == clock.Today.Date && roomId == "CLS_CDH_01" && shift == "sang")
            {
                return "LT_CDH_TD_AM";
            }

            if (normalizedDate == clock.Tomorrow.Date && roomId == "PK_NOI_01" && shift == "sang")
            {
                return "LT_PK_NOI_TM_AM";
            }

            if (normalizedDate == clock.Tomorrow.Date && roomId == "PK_NGOAI_01" && shift == "sang")
            {
                return "LT_PK_NGOAI_TM_AM";
            }

            return BuildGeneratedScheduleId(roomId, normalizedDate, shift);
        }

        private static (TimeSpan start, TimeSpan end) GetShiftWindow(string roomId, string shift)
        {
            var isClsRoom = roomId.StartsWith("CLS_", StringComparison.OrdinalIgnoreCase);
            return (isClsRoom, shift) switch
            {
                (true, "sang") => (Time(7, 30), Time(11, 30)),
                (true, "chieu") => (Time(13, 0), Time(17, 0)),
                (true, "toi") => (Time(17, 30), Time(20, 30)),
                (false, "sang") => (Time(8, 0), Time(12, 0)),
                (false, "chieu") => (Time(13, 0), Time(17, 0)),
                (false, "toi") => (Time(17, 30), Time(21, 0)),
                _ => (Time(8, 0), Time(12, 0))
            };
        }

        private static string ResolveDutyStaff(string roomId, DateTime date, string shift)
        {
            string[] candidates = roomId switch
            {
                "PK_NOI_01" => new[] { "NV_YT_LS_01", "NV_YT_LS_03" },
                "PK_NOI_02" => new[] { "NV_YT_LS_03", "NV_YT_LS_01" },
                "PK_NGOAI_01" => new[] { "NV_YT_LS_02", "NV_YT_LS_04" },
                "PK_NGOAI_02" => new[] { "NV_YT_LS_04", "NV_YT_LS_02" },
                "PK_NHI_01" => new[] { "NV_YT_LS_05" },
                "PK_RHM_01" => new[] { "NV_YT_LS_06" },
                "PK_TMH_01" => new[] { "NV_YT_LS_07" },
                "CLS_XN_01" => new[] { "NV_KTV_XN_01", "NV_YT_CLS_01", "NV_KTV_XN_02" },
                "CLS_XN_02" => new[] { "NV_KTV_XN_02", "NV_YT_CLS_01", "NV_KTV_XN_01" },
                "CLS_CDH_01" => new[] { "NV_KTV_CDH_01", "NV_YT_CLS_02", "NV_KTV_CDH_02" },
                "CLS_XQ_01" => new[] { "NV_KTV_CDH_02", "NV_KTV_CDH_01", "NV_YT_CLS_02" },
                "CLS_SA_01" => new[] { "NV_KTV_SA_01", "NV_YT_CLS_02", "NV_KTV_CDH_01" },
                _ => new[] { "NV_YT_HC_02" }
            };

            var offset = shift switch
            {
                "chieu" => 1,
                "toi" => 2,
                _ => 0
            };
            var index = Math.Abs(date.DayOfYear + offset) % candidates.Length;
            return candidates[index];
        }

        private sealed class ReportPatientSeed
        {
            public string MaBenhNhan { get; init; } = string.Empty;
            public string HoTen { get; init; } = string.Empty;
            public int Tuoi { get; init; }
            public string GioiTinh { get; init; } = string.Empty;
            public string DienThoai { get; init; } = string.Empty;
            public string Email { get; init; } = string.Empty;
            public string DiaChi { get; init; } = string.Empty;
            public string? TieuSuBenh { get; init; }
            public string? NhomMau { get; init; }
            public int CreatedOffsetDays { get; init; }
        }

        private sealed class ReportVisitSeed
        {
            public string Suffix { get; init; } = string.Empty;
            public string MaBenhNhan { get; init; } = string.Empty;
            public string HoTen { get; init; } = string.Empty;
            public string DienThoai { get; init; } = string.Empty;
            public int DayOffset { get; init; }
            public int Hour { get; init; }
            public int Minute { get; init; }
            public string LoaiHen { get; init; } = string.Empty;
            public string TrangThaiLichHen { get; init; } = string.Empty;
            public string RoomId { get; init; } = string.Empty;
            public string ServiceId { get; init; } = string.Empty;
            public string DoctorId { get; init; } = string.Empty;
            public string ClinicalNurseId { get; init; } = string.Empty;
            public string ReceptionId { get; init; } = string.Empty;
            public string TrieuChung { get; init; } = string.Empty;
            public string GhiChu { get; init; } = string.Empty;
            public string LoaiLuot { get; init; } = "kham_lam_sang";
            public bool TaoHoaDon { get; init; }
            public decimal SoTienHoaDon { get; init; }
            public string NoiDungHoaDon { get; init; } = string.Empty;
        }

        private sealed class ReportExtraAppointmentSeed
        {
            public string Suffix { get; init; } = string.Empty;
            public string MaBenhNhan { get; init; } = string.Empty;
            public string HoTen { get; init; } = string.Empty;
            public string DienThoai { get; init; } = string.Empty;
            public int DayOffset { get; init; }
            public int Hour { get; init; }
            public int Minute { get; init; }
            public string LoaiHen { get; init; } = string.Empty;
            public string TrangThai { get; init; } = string.Empty;
            public string RoomId { get; init; } = string.Empty;
            public string GhiChu { get; init; } = string.Empty;
        }

        private static DateTime At(DateTime date, int hour, int minute) =>
            date.Date.AddHours(hour).AddMinutes(minute);

        private static TimeSpan Time(int hour, int minute) =>
            new(hour, minute, 0);

        private sealed record SeedClock(DateTime Today)
        {
            public DateTime Yesterday => Today.AddDays(-1);
            public DateTime Tomorrow => Today.AddDays(1);
        }
    }
}
