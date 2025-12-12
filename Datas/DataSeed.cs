using HealthCare.Entities;
using HealthCare.RenderID; // Ensure you have this namespace or the GeneratorID class below
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;

namespace HealthCare.Datas
{
    public static class DataSeed
    {
        private const string DefaultPassword = "P@ssw0rd";

        public static async Task EnsureSeedAsync(DataContext db)
        {
            if (await db.NhanVienYTes.AnyAsync()) return;
            Console.WriteLine(">>> BẮT ĐẦU SEED DATA (FULL FLOW CHUẨN)...");

            // =========================================================================
            // 1. MASTER DATA (Dữ liệu nền)
            // =========================================================================
            var khoaList = SeedKhoa(); db.KhoaChuyenMons.AddRange(khoaList);
            var nhanSuList = SeedNhanSu(khoaList); db.NhanVienYTes.AddRange(nhanSuList);
            var phongList = SeedPhong(); db.Phongs.AddRange(phongList);
            var benhNhanList = SeedBenhNhan(); db.BenhNhans.AddRange(benhNhanList);
            var khoThuocList = SeedKhoThuoc(); db.KhoThuocs.AddRange(khoThuocList);
            var dichVuList = SeedDichVuYTe(phongList); db.DichVuYTes.AddRange(dichVuList);

            await db.SaveChangesAsync(); // Save để lấy ID tham chiếu

            // =========================================================================
            // 2. LỊCH TRỰC & LỊCH HẸN
            // =========================================================================
            var lichTrucList = SeedLichTruc(phongList, nhanSuList);
            db.LichTrucs.AddRange(lichTrucList);
            var lichHenList = SeedLichHen(lichTrucList, benhNhanList); db.LichHenKhams.AddRange(lichHenList);

            // =========================================================================
            // 3. QUY TRÌNH KHÁM LÂM SÀNG (BƯỚC 1: TIẾP NHẬN & KHÁM BAN ĐẦU)
            // =========================================================================

            // Lập phiếu khám LS (Từ lịch hẹn hoặc vãng lai)
            var phieuKhamList = SeedPhieuKham(benhNhanList, nhanSuList, lichHenList, dichVuList);
            db.PhieuKhamLamSangs.AddRange(phieuKhamList);

            // Xác định ai sẽ phải đi làm CLS (Logic: 70% phiếu khám sẽ có chỉ định CLS)
            var phieuKhamClsList = SeedPhieuKhamCls(phieuKhamList);
            db.PhieuKhamCanLamSangs.AddRange(phieuKhamClsList);

            // TẠO HÓA ĐƠN KHÁM LS (Logic: Check miễn phí nếu tái khám đúng giờ)
            var hoaDonKhamList = SeedHoaDonKham(benhNhanList, nhanSuList, phieuKhamList, lichHenList, dichVuList);
            db.HoaDonThanhToans.AddRange(hoaDonKhamList);

            // TẠO QUEUE & LƯỢT KHÁM LS (LẦN 1 & LẦN 2 - QUAY LẠI SAU CLS)
            // Hàm này giờ đây sẽ sinh ra cả Queue ban đầu VÀ Queue quay lại (service_return) nếu có phiếu CLS
            var hangDoiLsList = SeedHangDoiTuPhieuKham(phieuKhamList, dichVuList, phieuKhamClsList);
            db.HangDois.AddRange(hangDoiLsList);

            var luotKhamLsList = SeedLuotKhamTuHangDoi(hangDoiLsList, nhanSuList, phieuKhamList);
            db.LuotKhamBenhs.AddRange(luotKhamLsList);

            // =========================================================================
            // 4. QUY TRÌNH CẬN LÂM SÀNG (CLS) - NẾU CÓ CHỈ ĐỊNH
            // =========================================================================

            // Chi tiết dịch vụ con (Máu, Siêu âm...)
            var chiTietDvClsList = SeedChiTietDichVuCls(phieuKhamClsList, dichVuList);
            db.ChiTietDichVus.AddRange(chiTietDvClsList);

            // Tạo Hóa đơn cho CLS (Luôn thu tiền)
            var hoaDonClsList = SeedHoaDonCls(phieuKhamClsList, phieuKhamList, benhNhanList, nhanSuList);
            db.HoaDonThanhToans.AddRange(hoaDonClsList);

            // Tạo Queue & Lượt thực hiện tại phòng CLS
            var (hangDoiCls, luotKhamCls) = SeedHangDoiVaLuotKhamCLS(chiTietDvClsList, phieuKhamClsList, phieuKhamList, dichVuList, nhanSuList);
            db.HangDois.AddRange(hangDoiCls);
            db.LuotKhamBenhs.AddRange(luotKhamCls);

            // Nhập kết quả
            var ketQuaDvList = SeedKetQuaDichVu(chiTietDvClsList, nhanSuList);
            db.KetQuaDichVus.AddRange(ketQuaDvList);

            // Tổng hợp kết quả (Để bác sĩ xem khi bệnh nhân quay lại Queue Lần 2)
            var phieuTongHopList = SeedPhieuTongHop(phieuKhamClsList, nhanSuList);
            db.PhieuTongHopKetQuas.AddRange(phieuTongHopList);

            // =========================================================================
            // 5. KẾT THÚC (CHẨN ĐOÁN & THUỐC)
            // =========================================================================

            // Chẩn đoán cuối cùng (1 Phiếu khám -> 1 Chẩn đoán)
            var phieuChanDoanList = SeedPhieuChanDoan(phieuKhamList, phieuKhamClsList);
            db.PhieuChanDoanCuois.AddRange(phieuChanDoanList);

            // Kê đơn & Hóa đơn thuốc (Dựa trên chẩn đoán: có thể có hoặc không)
            var (donThuocList, chiTietDonList, hoaDonThuocList) = SeedDonThuocVaHoaDon(phieuChanDoanList, phieuKhamList, khoThuocList, nhanSuList);

            db.DonThuocs.AddRange(donThuocList);
            db.ChiTietDonThuocs.AddRange(chiTietDonList);
            db.HoaDonThanhToans.AddRange(hoaDonThuocList);

            // Link đơn thuốc vào chẩn đoán
            LinkDonThuocVaoChanDoan(phieuChanDoanList, donThuocList);

            // =========================================================================
            // 6. THÔNG BÁO & SAVE
            // =========================================================================
            var allLuotKham = luotKhamLsList.Concat(luotKhamCls).ToList();
            var (tbHeThongList, tbNguoiNhanList) = SeedThongBao(allLuotKham, benhNhanList, nhanSuList);
            db.ThongBaoHeThongs.AddRange(tbHeThongList);
            db.ThongBaoNguoiNhans.AddRange(tbNguoiNhanList);

            try
            {
                await db.SaveChangesAsync();
                Console.WriteLine(">>> SEED DATA FINAL SUCCESS!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($">>> ERROR: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($">>> INNER: {ex.InnerException.Message}");
                throw;
            }
        }
        // ================== SEEDING METHODS ==================

        private static List<HangDoi> SeedHangDoiTuPhieuKham(
     List<PhieuKhamLamSang> phieuKhamList,
     List<DichVuYTe> dichVuList,
     List<PhieuKhamCanLamSang> phieuKhamClsList)
        {
            var list = new List<HangDoi>();
            if (phieuKhamList == null || phieuKhamList.Count == 0)
                return list;

            var today = DateTime.Today;

            // Map Dịch vụ -> Phòng thực hiện
            var dicPhongTheoDv = dichVuList
                .Where(d => !string.IsNullOrEmpty(d.MaPhongThucHien))
                .ToDictionary(d => d.MaDichVu, d => d.MaPhongThucHien!);

            // Map Phiếu Khám -> Phiếu CLS để biết ai cần quay lại
            var clsMap = (phieuKhamClsList ?? new List<PhieuKhamCanLamSang>())
                .ToDictionary(c => c.MaPhieuKhamLs, c => c);

            // ====== Chuẩn hoá ngày về TODAY và các ngày sau đó ======
            // Ngày nhỏ nhất trong danh sách phiếu khám
            var minNgayKham = phieuKhamList.Min(pk => pk.NgayLap.Date);

            // Ngày nhỏ nhất trong danh sách phiếu CLS (nếu có)
            DateTime? minNgayCls = null;
            if (phieuKhamClsList != null && phieuKhamClsList.Any())
            {
                minNgayCls = phieuKhamClsList.Min(c => c.NgayGioLap.Date);
            }

            foreach (var pk in phieuKhamList)
            {
                // --- Chuẩn hoá thời gian khám (lần 1) về hôm nay + offset ---
                var offsetKhamDays = (pk.NgayLap.Date - minNgayKham).Days;   // lệch bao nhiêu ngày so với min
                var baseDateKham = today.AddDays(offsetKhamDays);            // dời về hôm nay + offset
                var thoiDiemKham = baseDateKham.Add(pk.GioLap);              // giữ nguyên giờ khám cũ

                // --- 1. QUEUE LẦN 1: KHÁM MỚI / TÁI KHÁM BAN ĐẦU ---
                var checkinTime = thoiDiemKham.AddMinutes(-10); // Checkin sớm 10p
                DateTime? lichHen = pk.HinhThucTiepNhan == "appointment"
                    ? thoiDiemKham
                    : null;

                // Xác định phòng khám
                string maPhong = "PK_TQ_01";
                if (dicPhongTheoDv.TryGetValue(pk.MaDichVuKham, out var mappedPhong))
                    maPhong = mappedPhong;

                // Logic phân loại đến
                string phanLoaiDen = "thuong";
                if (lichHen.HasValue)
                {
                    if (checkinTime <= lichHen.Value)
                        phanLoaiDen = "den_som";
                    else if (checkinTime <= lichHen.Value.AddMinutes(15))
                        phanLoaiDen = "dung_gio";
                    else
                        phanLoaiDen = "den_muon";
                }

                list.Add(new HangDoi
                {
                    MaHangDoi = GeneratorID.NewHangDoiId(),
                    MaBenhNhan = pk.MaBenhNhan,
                    MaPhong = maPhong,
                    LoaiHangDoi = "kham_ls",
                    Nguon = pk.HinhThucTiepNhan, // 'appointment' hoặc 'walkin'
                    Nhan = null,
                    CapCuu = false,
                    PhanLoaiDen = phanLoaiDen,
                    ThoiGianCheckin = checkinTime,
                    ThoiGianLichHen = lichHen,
                    DoUuTien = 0,
                    TrangThai = "cho_goi",
                    GhiChu = "Đăng ký khám ban đầu",
                    MaPhieuKham = pk.MaPhieuKham
                });

                // --- 2. QUEUE LẦN 2: QUAY LẠI SAU KHI LÀM CLS (NẾU CÓ) ---
                if (clsMap.TryGetValue(pk.MaPhieuKham, out var phieuCls))
                {
                    DateTime ngayGioCls;

                    if (minNgayCls.HasValue)
                    {
                        // Dời thời điểm lập phiếu CLS về today + offset (giữ nguyên giờ)
                        var offsetClsDays = (phieuCls.NgayGioLap.Date - minNgayCls.Value).Days;
                        var baseDateCls = today.AddDays(offsetClsDays);
                        ngayGioCls = baseDateCls.Add(phieuCls.NgayGioLap.TimeOfDay);
                    }
                    else
                    {
                        // Fallback: nếu không có minNgayCls, cho CLS xảy ra sau khám 1h
                        ngayGioCls = thoiDiemKham.AddHours(1);
                    }

                    // Thời gian quay lại = Thời gian làm phiếu CLS + 2 tiếng (chờ kết quả)
                    var returnTime = ngayGioCls.AddHours(2);

                    list.Add(new HangDoi
                    {
                        MaHangDoi = GeneratorID.NewHangDoiId(),
                        MaBenhNhan = pk.MaBenhNhan,
                        MaPhong = maPhong, // Quay về đúng phòng bác sĩ ban đầu
                        LoaiHangDoi = "kham_ls",
                        Nguon = "service_return", // quay lại sau CLS
                        Nhan = "tai_kham",
                        CapCuu = false,
                        PhanLoaiDen = null,
                        ThoiGianCheckin = returnTime,
                        ThoiGianLichHen = null,
                        DoUuTien = 10, // Ưu tiên hơn khách mới đến
                        TrangThai = "cho_goi",
                        GhiChu = "Đã có đủ kết quả CLS",
                        MaPhieuKham = pk.MaPhieuKham // Vẫn dùng phiếu khám cũ
                    });
                }
            }

            return list;
        }

        private static List<HoaDonThanhToan> SeedHoaDonKham(
    List<BenhNhan> benhNhanList,
    List<NhanVienYTe> nhanSuList,
    List<PhieuKhamLamSang> phieuKhamList,
    List<LichHenKham> lichHenList,
    List<DichVuYTe> dichVuList)
        {
            var list = new List<HoaDonThanhToan>();
            var thuNgan = nhanSuList.First();
            var lichHenMap = lichHenList.ToDictionary(l => l.MaLichHen, l => l);
            var dvMap = dichVuList.ToDictionary(d => d.MaDichVu, d => d.DonGia);

            foreach (var pk in phieuKhamList)
            {
                decimal donGia = dvMap.ContainsKey(pk.MaDichVuKham) ? dvMap[pk.MaDichVuKham] : 150000m;
                string note = "Thu phí khám lâm sàng";

                // --- LOGIC MIỄN PHÍ ---
                // Nếu có lịch hẹn LÀ TÁI KHÁM và ĐẾN ĐÚNG GIỜ/SỚM
                if (!string.IsNullOrEmpty(pk.MaLichHen) && lichHenMap.TryGetValue(pk.MaLichHen, out var lh))
                {
                    var checkInTime = pk.NgayLap.Add(pk.GioLap);
                    var apptTime = lh.NgayHen.Add(lh.GioHen);
                    bool isOnTime = checkInTime <= apptTime.AddMinutes(15); // Cho phép trễ 15p

                    if (lh.LoaiHen == "tai_kham" && isOnTime)
                    {
                        donGia = 0;
                        note = "Miễn phí (Tái khám đúng giờ)";
                    }
                }

                list.Add(new HoaDonThanhToan
                {
                    MaHoaDon = GeneratorID.NewHoaDonId(),
                    MaBenhNhan = pk.MaBenhNhan,
                    MaNhanSuThu = thuNgan.MaNhanVien,
                    MaPhieuKham = pk.MaPhieuKham,
                    MaPhieuKhamCls = null,
                    MaDonThuoc = null,
                    LoaiDotthu = "kham_lam_sang",
                    SoTien = donGia,
                    ThoiGian = pk.NgayLap.Add(pk.GioLap).AddMinutes(5),
                    TrangThai = "da_thu",
                    NoiDung = note
                });
            }
            return list;
        }
        private static List<HoaDonThanhToan> SeedHoaDonCls(
    List<PhieuKhamCanLamSang> phieuKhamClsList,
    List<PhieuKhamLamSang> phieuKhamList,
    List<BenhNhan> benhNhanList,
    List<NhanVienYTe> nhanSuList)
        {
            var list = new List<HoaDonThanhToan>();
            var thuNgan = nhanSuList.First();
            var pkMap = phieuKhamList.ToDictionary(p => p.MaPhieuKham, p => p);
            var bnMap = benhNhanList.ToDictionary(b => b.MaBenhNhan, b => b);

            foreach (var pkCls in phieuKhamClsList)
            {
                if (!pkMap.TryGetValue(pkCls.MaPhieuKhamLs, out var pk)) continue;

                list.Add(new HoaDonThanhToan
                {
                    MaHoaDon = GeneratorID.NewHoaDonId(),
                    MaBenhNhan = pk.MaBenhNhan,
                    MaNhanSuThu = thuNgan.MaNhanVien,
                    MaPhieuKham = pk.MaPhieuKham,
                    MaPhieuKhamCls = pkCls.MaPhieuKhamCls,
                    MaDonThuoc = null,
                    LoaiDotthu = "can_lam_sang",
                    SoTien = 500000m, // Giả định thu trọn gói CLS
                    ThoiGian = pkCls.NgayGioLap.AddMinutes(-5), // Thu trước khi làm
                    TrangThai = "da_thu",
                    NoiDung = "Thu phí chỉ định Cận lâm sàng"
                });
            }
            return list;
        }
        private static List<LuotKhamBenh> SeedLuotKhamTuHangDoi(
     List<HangDoi> hangDoiList,
     List<NhanVienYTe> nhanSuList,
     List<PhieuKhamLamSang> phieuKhamList)
        {
            var list = new List<LuotKhamBenh>();

            if (hangDoiList == null || hangDoiList.Count == 0)
                return list;

            var bacSi = nhanSuList.Where(n => n.VaiTro == "bac_si").ToArray();
            var yTa = nhanSuList.Where(n => n.VaiTro == "y_ta").ToArray();

            if (!bacSi.Any() || !yTa.Any())
                return list;

            var dicPhieuKham = phieuKhamList
                .ToDictionary(x => x.MaPhieuKham, x => x);

            // ====== Chuẩn hoá thời gian CHECKIN về hôm nay + offset ======
            var today = DateTime.Today;
            var minCheckinDate = hangDoiList.Min(h => h.ThoiGianCheckin.Date);

            for (int i = 0; i < hangDoiList.Count; i++)
            {
                var hd = hangDoiList[i];

                // Dời thời gian checkin về TODAY + offset (giữ nguyên giờ)
                var offsetDays = (hd.ThoiGianCheckin.Date - minCheckinDate).Days;
                var normalizedCheckin = today
                    .AddDays(offsetDays)
                    .Add(hd.ThoiGianCheckin.TimeOfDay);

                // Chọn bác sĩ
                NhanVienYTe bs;
                if (!string.IsNullOrEmpty(hd.MaPhieuKham)
                    && dicPhieuKham.TryGetValue(hd.MaPhieuKham, out var pk))
                {
                    // Bác sĩ theo phiếu khám, nếu không có thì Round-Robin
                    bs = bacSi.FirstOrDefault(b => b.MaNhanVien == pk.MaBacSiKham)
                         ?? bacSi[i % bacSi.Length];
                }
                else
                {
                    bs = bacSi[i % bacSi.Length];
                }

                // Chọn y tá Round-Robin
                var yt = yTa[i % yTa.Length];

                // Lượt khám bắt đầu sau checkin 3 phút
                var start = normalizedCheckin.AddMinutes(3);

                list.Add(new LuotKhamBenh
                {
                    MaLuotKham = GeneratorID.NewLuotKhamId(),
                    MaHangDoi = hd.MaHangDoi,
                    MaNhanSuThucHien = bs.MaNhanVien,
                    MaYTaHoTro = yt.MaNhanVien,
                    LoaiLuot = string.Equals(hd.LoaiHangDoi, "can_lam_sang", StringComparison.OrdinalIgnoreCase)
                        ? "can_lam_sang"
                        : "kham_lam_sang",
                    ThoiGianBatDau = start,
                    ThoiGianKetThuc = start.AddMinutes(12),
                    TrangThai = "da_kham"
                });
            }

            return list;
        }

      

        private static List<BenhNhan> SeedBenhNhan()
        {
            var list = new List<BenhNhan>();
            var random = new Random(42);

            // Danh sách họ và tên đệm để random
            var hos = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý" };
            var dems = new[] { "Văn", "Thị", "Hữu", "Minh", "Ngọc", "Thanh", "Quốc", "Gia", "Hải", "Khánh", "Thu", "Xuân", "Đức" };
            var tens = new[] { "An", "Bình", "Châu", "Dũng", "Em", "Giang", "Hân", "Hoa", "Khôi", "Long", "Nam", "Nga", "Phúc", "Quân", "Sơn", "Tài", "Thảo", "Trang", "Tùng", "Uyên", "Vân", "Yến" };

            // CHỈ TẠO 60 BỆNH NHÂN
            for (int i = 0; i < 60; i++)
            {
                var ho = hos[random.Next(hos.Length)];
                var dem = dems[random.Next(dems.Length)];
                var ten = tens[random.Next(tens.Length)];

                var id = GeneratorID.NewBenhNhanId();
                var age = random.Next(3, 85); // Tuổi từ 3 đến 85
                var gioiTinh = (i % 2 == 0) ? "nam" : "nu";

                // Random trạng thái bệnh nền
                string? benhManTinh = null;
                if (age > 50) benhManTinh = (i % 3 == 0) ? "Tăng huyết áp" : (i % 3 == 1 ? "Đái tháo đường" : "Tim mạch");

                list.Add(new BenhNhan
                {
                    MaBenhNhan = id,
                    HoTen = $"{ho} {dem} {ten}",
                    NgaySinh = DateTime.Today.AddYears(-age).AddDays(-random.Next(0, 365)),
                    GioiTinh = gioiTinh,
                    DienThoai = "09" + random.Next(10000000, 99999999),
                    Email = $"bn{i + 1:000}@example.com",
                    DiaChi = (i % 3 == 0) ? "TP. Hồ Chí Minh" : (i % 3 == 1) ? "TP. Thủ Đức" : "Bình Dương",
                    DiUng = (i % 10 == 0) ? "Dị ứng hải sản" : null,
                    ChongChiDinh = null,
                    ThuocDangDung = benhManTinh != null ? "Thuốc định kỳ BHYT" : null,
                    TieuSuBenh = benhManTinh ?? "Khỏe mạnh",
                    TienSuPhauThuat = (i % 15 == 0) ? "Mổ ruột thừa" : null,
                    NhomMau = new[] { "A", "B", "AB", "O" }[random.Next(0, 4)],
                    BenhManTinh = benhManTinh,
                    SinhHieu = "HA 120/80, M 80, N 18, T 37.0",
                    TrangThaiTaiKhoan = "hoat_dong",
                    TrangThaiHomNay = null,
                    NgayTrangThai = DateTime.Today
                });
            }
            return list;
        }
        private static List<KhoThuoc> SeedKhoThuoc()
        {
            var today = DateTime.Today;
            return new List<KhoThuoc>
            {
                new KhoThuoc { MaThuoc = "THUOC_PARACETAMOL_500", TenThuoc = "Paracetamol 500mg", DonViTinh = "viên", CongDung = "Giảm đau, hạ sốt", GiaNiemYet = 1500m, SoLuongTon = 500, HanSuDung = today.AddYears(2), SoLo = "P500-01", TrangThai = "hoat_dong" },
                new KhoThuoc { MaThuoc = "THUOC_IBUPROFEN_400", TenThuoc = "Ibuprofen 400mg", DonViTinh = "viên", CongDung = "Giảm đau, kháng viêm", GiaNiemYet = 2500m, SoLuongTon = 300, HanSuDung = today.AddYears(1), SoLo = "IBU400-01", TrangThai = "hoat_dong" },
                new KhoThuoc { MaThuoc = "THUOC_AMOX_500", TenThuoc = "Amoxicillin 500mg", DonViTinh = "viên", CongDung = "Kháng sinh", GiaNiemYet = 2000m, SoLuongTon = 400, HanSuDung = today.AddYears(1), SoLo = "AMOX500-01", TrangThai = "hoat_dong" },
                new KhoThuoc { MaThuoc = "THUOC_METFORMIN_500", TenThuoc = "Metformin 500mg", DonViTinh = "viên", CongDung = "Đái tháo đường type 2", GiaNiemYet = 1800m, SoLuongTon = 200, HanSuDung = today.AddYears(2), SoLo = "MET500-01", TrangThai = "hoat_dong" },
                new KhoThuoc { MaThuoc = "THUOC_ATORVASTATIN_10", TenThuoc = "Atorvastatin 10mg", DonViTinh = "viên", CongDung = "Giảm mỡ máu", GiaNiemYet = 3000m, SoLuongTon = 150, HanSuDung = today.AddYears(2), SoLo = "ATOR10-01", TrangThai = "hoat_dong" },
                new KhoThuoc { MaThuoc = "THUOC_OMEPRAZOLE_20", TenThuoc = "Omeprazole 20mg", DonViTinh = "viên", CongDung = "Giảm tiết acid dạ dày", GiaNiemYet = 2200m, SoLuongTon = 250, HanSuDung = today.AddYears(1), SoLo = "OME20-01", TrangThai = "hoat_dong" },
                new KhoThuoc { MaThuoc = "THUOC_CEFTRIAXONE_1G", TenThuoc = "Ceftriaxone 1g", DonViTinh = "lọ", CongDung = "Kháng sinh tiêm", GiaNiemYet = 35000m, SoLuongTon = 100, HanSuDung = today.AddMonths(18), SoLo = "CEF1G-01", TrangThai = "hoat_dong" },
                new KhoThuoc { MaThuoc = "THUOC_SODIUM_CHLORIDE_0_9", TenThuoc = "NaCl 0.9%", DonViTinh = "chai", CongDung = "Dịch truyền", GiaNiemYet = 8000m, SoLuongTon = 200, HanSuDung = today.AddMonths(6), SoLo = "NACL09-01", TrangThai = "sap_het_ton" },
                new KhoThuoc { MaThuoc = "THUOC_PARACETAMOL_250_SIRO", TenThuoc = "Paracetamol 250mg/5ml", DonViTinh = "chai", CongDung = "Giảm đau, hạ sốt trẻ em", GiaNiemYet = 25000m, SoLuongTon = 80, HanSuDung = today.AddYears(1), SoLo = "P250S-01", TrangThai = "hoat_dong" },
                new KhoThuoc { MaThuoc = "THUOC_VITAMIN_C_500", TenThuoc = "Vitamin C 500mg", DonViTinh = "viên", CongDung = "Bổ sung vitamin C", GiaNiemYet = 1200m, SoLuongTon = 600, HanSuDung = today.AddMonths(10), SoLo = "VC500-01", TrangThai = "hoat_dong" },
            };
        }
        private static List<KhoaChuyenMon> SeedKhoa()
        {
            return new List<KhoaChuyenMon>
            {
                new KhoaChuyenMon { MaKhoa = "KHOA_TQ", TenKhoa = "Khám tổng quát", MoTa = "Khám ban đầu, sàng lọc tổng quát", DiaDiem = "Tầng 1", DienThoai = "02839100001", Email = "khamtq@hospital.local" , TrangThai = "hoat_dong"},
                new KhoaChuyenMon { MaKhoa = "KHOA_NOI", TenKhoa = "Nội tổng quát", MoTa = "Điều trị bệnh lý nội khoa", DiaDiem = "Tầng 2", DienThoai = "02839100002", Email = "noi@hospital.local" , TrangThai = "hoat_dong"},
                new KhoaChuyenMon { MaKhoa = "KHOA_NGOAI", TenKhoa = "Ngoại tổng quát", MoTa = "Phẫu thuật và ngoại khoa", DiaDiem = "Tầng 3", DienThoai = "02839100003", Email = "ngoai@hospital.local" , TrangThai = "hoat_dong"},
                new KhoaChuyenMon { MaKhoa = "KHOA_NHI", TenKhoa = "Nhi", MoTa = "Khám cho trẻ em", DiaDiem = "Tầng 4", DienThoai = "02839100004", Email = "nhi@hospital.local" , TrangThai = "hoat_dong"},
                new KhoaChuyenMon { MaKhoa = "KHOA_SAN", TenKhoa = "Sản", MoTa = "Sản phụ khoa", DiaDiem = "Tầng 5", DienThoai = "02839100005", Email = "san@hospital.local" , TrangThai = "hoat_dong"},
                new KhoaChuyenMon { MaKhoa = "KHOA_TMH", TenKhoa = "Tai Mũi Họng", MoTa = "Các bệnh lý tai mũi họng", DiaDiem = "Tầng 2", DienThoai = "02839100006", Email = "tmh@hospital.local" , TrangThai = "hoat_dong"},
                new KhoaChuyenMon { MaKhoa = "KHOA_MAT", TenKhoa = "Mắt", MoTa = "Khám và điều trị mắt", DiaDiem = "Tầng 2", DienThoai = "02839100007", Email = "mat@hospital.local" , TrangThai = "hoat_dong"},
                new KhoaChuyenMon { MaKhoa = "KHOA_RHM", TenKhoa = "Răng Hàm Mặt", MoTa = "Nha khoa tổng quát", DiaDiem = "Tầng 3", DienThoai = "02839100008", Email = "rhm@hospital.local" , TrangThai = "hoat_dong"},
                new KhoaChuyenMon { MaKhoa = "KHOA_XN", TenKhoa = "Xét nghiệm", MoTa = "Xét nghiệm huyết học, sinh hóa", DiaDiem = "Tầng 1", DienThoai = "02839100009", Email = "xetnghiem@hospital.local" , TrangThai = "hoat_dong"},
                new KhoaChuyenMon { MaKhoa = "KHOA_CDHA", TenKhoa = "Chẩn đoán hình ảnh", MoTa = "Siêu âm, X-quang, CT", DiaDiem = "Tầng 1", DienThoai = "02839100010", Email = "cdha@hospital.local" , TrangThai = "hoat_dong"},
            };
        }

        private static List<Phong> SeedPhong()
        {
            return new List<Phong>
    {
        // ================== PHÒNG KHÁM (BẮT BUỘC CÓ BÁC SĨ PHỤ TRÁCH) ==================
        
        // 1. Khám tổng quát 1 - Phụ trách: BS Nguyễn Văn A (NV_BS_TQ_01)
        new Phong {
            MaPhong = "PK_TQ_01",
            TenPhong = "Phòng khám tổng quát 01",
            MaKhoa = "KHOA_TQ",
            LoaiPhong = "phong_kham",
            SucChua = 10,
            ViTri = "Tầng 1 - A101",
            Email = "pk_tq_01@hospital.local",
            DienThoai = "02839110001",
            ThietBi = new List<string>{"Giường khám","Máy đo huyết áp"},
            TrangThai = "hoat_dong",
            MaBacSiPhuTrach = "NV_BS_TQ_01" // <--- Đã thêm
        },

        // 2. Khám tổng quát 2 - Phụ trách: BS Trần Thị B (NV_BS_TQ_02)
        new Phong {
            MaPhong = "PK_TQ_02",
            TenPhong = "Phòng khám tổng quát 02",
            MaKhoa = "KHOA_TQ",
            LoaiPhong = "phong_kham",
            SucChua = 8,
            ViTri = "Tầng 1 - A102",
            Email = "pk_tq_02@hospital.local",
            DienThoai = "02839110002",
            ThietBi = new List<string>{"Giường khám","Máy đo đường huyết"},
            TrangThai = "hoat_dong",
            MaBacSiPhuTrach = "NV_BS_TQ_02" // <--- Đã thêm
        },

        // 3. Khám Nội - Phụ trách: BS Phạm Văn C (NV_BS_NOI_01)
        new Phong {
            MaPhong = "PK_NOI_01",
            TenPhong = "Phòng khám Nội 01",
            MaKhoa = "KHOA_NOI",
            LoaiPhong = "phong_kham",
            SucChua = 10,
            ViTri = "Tầng 2 - B201",
            Email = "pk_noi_01@hospital.local",
            DienThoai = "02839110003",
            ThietBi = new List<string>{"Monitor","Máy điện tim"},
            TrangThai = "hoat_dong",
            MaBacSiPhuTrach = "NV_BS_NOI_01" // <--- Đã thêm
        },

        // 4. Khám Ngoại - Phụ trách: BS Lê Thị D (NV_BS_NGOAI_01)
        new Phong {
            MaPhong = "PK_NGOAI_01",
            TenPhong = "Phòng khám Ngoại 01",
            MaKhoa = "KHOA_NGOAI",
            LoaiPhong = "phong_kham",
            SucChua = 10,
            ViTri = "Tầng 3 - C301",
            Email = "pk_ngoai_01@hospital.local",
            DienThoai = "02839110004",
            ThietBi = new List<string>{"Giường khám","Đèn tiểu phẫu"},
            TrangThai = "hoat_dong",
            MaBacSiPhuTrach = "NV_BS_NGOAI_01" // <--- Đã thêm
        },

        // 5. Khám Nhi - Phụ trách: BS Nguyễn Thị E (NV_BS_NHI_01)
        new Phong {
            MaPhong = "PK_NHI_01",
            TenPhong = "Phòng khám Nhi 01",
            MaKhoa = "KHOA_NHI",
            LoaiPhong = "phong_kham",
            SucChua = 12,
            ViTri = "Tầng 4 - D401",
            Email = "pk_nhi_01@hospital.local",
            DienThoai = "02839110005",
            ThietBi = new List<string>{"Cân điện tử","Máy đo chiều cao"},
            TrangThai = "hoat_dong",
            MaBacSiPhuTrach = "NV_BS_NHI_01" // <--- Đã thêm
        },

        // 6. Khám Sản - Phụ trách: BS Đỗ Văn F (NV_BS_SAN_01)
        new Phong {
            MaPhong = "PK_SAN_01",
            TenPhong = "Phòng khám Sản 01",
            MaKhoa = "KHOA_SAN",
            LoaiPhong = "phong_kham",
            SucChua = 10,
            ViTri = "Tầng 5 - E501",
            Email = "pk_san_01@hospital.local",
            DienThoai = "02839110006",
            ThietBi = new List<string>{"Bàn khám sản","Máy siêu âm sản"},
            TrangThai = "hoat_dong",
            MaBacSiPhuTrach = "NV_BS_SAN_01" // <--- Đã thêm
        },


        // ================== PHÒNG DỊCH VỤ (KHÔNG CẦN BÁC SĨ PHỤ TRÁCH) ==================
        
        new Phong { MaPhong = "CLS_XN_01", TenPhong = "Phòng xét nghiệm 01", MaKhoa = "KHOA_XN", LoaiPhong = "phong_dich_vu", SucChua = 6, ViTri = "Tầng 1 - X101", Email = "cls_xn_01@hospital.local", DienThoai = "02839110007", ThietBi = new List<string>{"Máy huyết học","Máy sinh hóa"}, TrangThai = "hoat_dong", MaBacSiPhuTrach = null },

        new Phong { MaPhong = "CLS_CDHA_01", TenPhong = "Phòng siêu âm 01", MaKhoa = "KHOA_CDHA", LoaiPhong = "phong_dich_vu", SucChua = 5, ViTri = "Tầng 1 - H101", Email = "cls_cdha_01@hospital.local", DienThoai = "02839110008", ThietBi = new List<string>{"Máy siêu âm 4D"}, TrangThai = "hoat_dong", MaBacSiPhuTrach = null },

        new Phong { MaPhong = "CLS_CDHA_02", TenPhong = "Phòng X-quang 01", MaKhoa = "KHOA_CDHA", LoaiPhong = "phong_dich_vu", SucChua = 4, ViTri = "Tầng 1 - H102", Email = "cls_cdha_02@hospital.local", DienThoai = "02839110009", ThietBi = new List<string>{"Máy X-quang kỹ thuật số"}, TrangThai = "hoat_dong", MaBacSiPhuTrach = null },

        new Phong { MaPhong = "CLS_XN_02", TenPhong = "Phòng xét nghiệm 02", MaKhoa = "KHOA_XN", LoaiPhong = "phong_dich_vu", SucChua = 6, ViTri = "Tầng 1 - X102", Email = "cls_xn_02@hospital.local", DienThoai = "02839110010", ThietBi = new List<string>{"Máy nước tiểu","Máy miễn dịch"}, TrangThai = "hoat_dong", MaBacSiPhuTrach = null },
    };
        }

        private static List<NhanVienYTe> SeedNhanSu(List<KhoaChuyenMon> khoaList)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);
            return new List<NhanVienYTe>
            {
                new NhanVienYTe { MaNhanVien = "NV_BS_TQ_01", TenDangNhap = "bs_tq01", MatKhauHash = hash, HoTen = "BS. Nguyễn Văn A", VaiTro = "bac_si", HocVi = "BS CKI", ChuyenMon = "Khám tổng quát", SoNamKinhNghiem = 5, Email = "bs_tq01@hospital.local", DienThoai = "0903000001", MaKhoa = "KHOA_TQ", TrangThaiCongTac = "dang_cong_tac" },
                new NhanVienYTe { MaNhanVien = "NV_BS_TQ_02", TenDangNhap = "bs_tq02", MatKhauHash = hash, HoTen = "BS. Trần Thị B", VaiTro = "bac_si", HocVi = "BS CKI", ChuyenMon = "Khám tổng quát", SoNamKinhNghiem = 7, Email = "bs_tq02@hospital.local", DienThoai = "0903000002", MaKhoa = "KHOA_TQ", TrangThaiCongTac = "dang_cong_tac" },
                new NhanVienYTe { MaNhanVien = "NV_BS_NOI_01", TenDangNhap = "bs_noi01", MatKhauHash = hash, HoTen = "BS. Phạm Văn C", VaiTro = "bac_si", HocVi = "BS CKII", ChuyenMon = "Nội tổng quát", SoNamKinhNghiem = 10, Email = "bs_noi01@hospital.local", DienThoai = "0903000003", MaKhoa = "KHOA_NOI", TrangThaiCongTac = "dang_cong_tac" },
                new NhanVienYTe { MaNhanVien = "NV_BS_NGOAI_01", TenDangNhap = "bs_ngoai01", MatKhauHash = hash, HoTen = "BS. Lê Thị D", VaiTro = "bac_si", HocVi = "BS CKI", ChuyenMon = "Ngoại tổng quát", SoNamKinhNghiem = 8, Email = "bs_ngoai01@hospital.local", DienThoai = "0903000004", MaKhoa = "KHOA_NGOAI", TrangThaiCongTac = "dang_cong_tac" },
                new NhanVienYTe { MaNhanVien = "NV_BS_NHI_01", TenDangNhap = "bs_nhi01", MatKhauHash = hash, HoTen = "BS. Nguyễn Thị E", VaiTro = "bac_si", HocVi = "BS CKI", ChuyenMon = "Nhi", SoNamKinhNghiem = 6, Email = "bs_nhi01@hospital.local", DienThoai = "0903000005", MaKhoa = "KHOA_NHI", TrangThaiCongTac = "dang_cong_tac" },
                new NhanVienYTe { MaNhanVien = "NV_BS_SAN_01", TenDangNhap = "bs_san01", MatKhauHash = hash, HoTen = "BS. Đỗ Văn F", VaiTro = "bac_si", HocVi = "BS CKI", ChuyenMon = "Sản", SoNamKinhNghiem = 9, Email = "bs_san01@hospital.local", DienThoai = "0903000006", MaKhoa = "KHOA_SAN", TrangThaiCongTac = "dang_cong_tac" },

                new NhanVienYTe { MaNhanVien = "NV_YT_HC_01", TenDangNhap = "yt_hc01", MatKhauHash = hash, HoTen = "ĐD. Hồ Thị G", VaiTro = "y_ta", LoaiYTa = "hanhchinh", SoNamKinhNghiem = 4, Email = "yt_hc01@hospital.local", DienThoai = "0903000011", MaKhoa = "KHOA_TQ", TrangThaiCongTac = "dang_cong_tac" },
                new NhanVienYTe { MaNhanVien = "NV_YT_LS_01", TenDangNhap = "yt_ls01", MatKhauHash = hash, HoTen = "ĐD. Bùi Văn H", VaiTro = "y_ta", LoaiYTa = "ls", SoNamKinhNghiem = 5, Email = "yt_ls01@hospital.local", DienThoai = "0903000012", MaKhoa = "KHOA_NOI", TrangThaiCongTac = "dang_cong_tac" },
                new NhanVienYTe { MaNhanVien = "NV_YT_CLS_01", TenDangNhap = "yt_cls01", MatKhauHash = hash, HoTen = "ĐD. Lý Thị I", VaiTro = "y_ta", LoaiYTa = "cls", SoNamKinhNghiem = 3, Email = "yt_cls01@hospital.local", DienThoai = "0903000013", MaKhoa = "KHOA_XN", TrangThaiCongTac = "dang_cong_tac" },
                new NhanVienYTe { MaNhanVien = "NV_YT_LS_02", TenDangNhap = "yt_ls02", MatKhauHash = hash, HoTen = "ĐD. Trương Văn K", VaiTro = "y_ta", LoaiYTa = "ls", SoNamKinhNghiem = 2, Email = "yt_ls02@hospital.local", DienThoai = "0903000014", MaKhoa = "KHOA_CDHA", TrangThaiCongTac = "dang_cong_tac" },
                new NhanVienYTe { MaNhanVien = "NV_YT_HC_02", TenDangNhap = "yt_hc02", MatKhauHash = hash, HoTen = "ĐD. Mai Thị L", VaiTro = "y_ta", LoaiYTa = "hanhchinh", SoNamKinhNghiem = 6, Email = "yt_hc02@hospital.local", DienThoai = "0903000015", MaKhoa = "KHOA_TQ", TrangThaiCongTac = "dang_cong_tac" },
                new NhanVienYTe { MaNhanVien = "NV_YT_CLS_02", TenDangNhap = "yt_cls02", MatKhauHash = hash, HoTen = "ĐD. Phan Văn M", VaiTro = "y_ta", LoaiYTa = "cls", SoNamKinhNghiem = 4, Email = "yt_cls02@hospital.local", DienThoai = "0903000016", MaKhoa = "KHOA_CDHA", TrangThaiCongTac = "dang_cong_tac" },
                new NhanVienYTe { MaNhanVien = "NV_BS_CDHA_01", TenDangNhap = "bs_cdha01", MatKhauHash = hash, HoTen = "BS. Vũ Thị N", VaiTro = "bac_si", HocVi = "BS CKI", ChuyenMon = "Chẩn đoán hình ảnh", SoNamKinhNghiem = 8, Email = "bs_cdha01@hospital.local", DienThoai = "0903000017", MaKhoa = "KHOA_CDHA", TrangThaiCongTac = "dang_cong_tac" },
                new NhanVienYTe { MaNhanVien = "NV_BS_XN_01", TenDangNhap = "bs_xn01", MatKhauHash = hash, HoTen = "BS. Đặng Văn O", VaiTro = "bac_si", HocVi = "BS CKI", ChuyenMon = "Xét nghiệm", SoNamKinhNghiem = 7, Email = "bs_xn01@hospital.local", DienThoai = "0903000018", MaKhoa = "KHOA_XN", TrangThaiCongTac = "dang_cong_tac" }
            };
        }
        private static List<DichVuYTe> SeedDichVuYTe(List<Phong> phongList)
        {
            return new List<DichVuYTe>
    {
        // ================== DỊCH VỤ KHÁM LÂM SÀNG ==================
        new DichVuYTe { MaDichVu = "DV_KHAM_TQ", LoaiDichVu = "kham_lam_sang", TenDichVu = "Khám tổng quát", DonGia = 150000m, ThoiGianDuKienPhut = 20, MaPhongThucHien = "PK_TQ_01" },
        new DichVuYTe { MaDichVu = "DV_KHAM_NOI", LoaiDichVu = "kham_lam_sang", TenDichVu = "Khám Nội tổng quát", DonGia = 200000m, ThoiGianDuKienPhut = 25, MaPhongThucHien = "PK_NOI_01" },
        new DichVuYTe { MaDichVu = "DV_KHAM_NGOAI", LoaiDichVu = "kham_lam_sang", TenDichVu = "Khám Ngoại tổng quát", DonGia = 220000m, ThoiGianDuKienPhut = 25, MaPhongThucHien = "PK_NGOAI_01" },
        new DichVuYTe { MaDichVu = "DV_KHAM_NHI", LoaiDichVu = "kham_lam_sang", TenDichVu = "Khám Nhi", DonGia = 180000m, ThoiGianDuKienPhut = 20, MaPhongThucHien = "PK_NHI_01" },
        new DichVuYTe { MaDichVu = "DV_KHAM_SAN", LoaiDichVu = "kham_lam_sang", TenDichVu = "Khám Sản", DonGia = 220000m, ThoiGianDuKienPhut = 20, MaPhongThucHien = "PK_SAN_01" },

        // ================== DỊCH VỤ CẬN LÂM SÀNG – XÉT NGHIỆM ==================
        new DichVuYTe { MaDichVu = "DV_XN_TONG_QUAT", LoaiDichVu = "can_lam_sang", TenDichVu = "Xét nghiệm tổng quát", DonGia = 400000m, ThoiGianDuKienPhut = 45, MaPhongThucHien = "CLS_XN_01" },
        new DichVuYTe { MaDichVu = "DV_XN_CHUYEN_SAU", LoaiDichVu = "can_lam_sang", TenDichVu = "Xét nghiệm chuyên sâu", DonGia = 600000m, ThoiGianDuKienPhut = 60, MaPhongThucHien = "CLS_XN_02" },

        new DichVuYTe { MaDichVu = "DV_XN_MAU_TONG_QUAT", LoaiDichVu = "can_lam_sang", TenDichVu = "Xét nghiệm máu tổng quát", DonGia = 250000m, ThoiGianDuKienPhut = 30, MaPhongThucHien = "CLS_XN_01" },
        new DichVuYTe { MaDichVu = "DV_XN_SINH_HOA_MAU", LoaiDichVu = "can_lam_sang", TenDichVu = "Xét nghiệm sinh hóa máu", DonGia = 320000m, ThoiGianDuKienPhut = 35, MaPhongThucHien = "CLS_XN_01" },
        new DichVuYTe { MaDichVu = "DV_XN_DONG_MAU", LoaiDichVu = "can_lam_sang", TenDichVu = "Xét nghiệm đông máu", DonGia = 280000m, ThoiGianDuKienPhut = 30, MaPhongThucHien = "CLS_XN_02" },
        new DichVuYTe { MaDichVu = "DV_XN_NUOC_TIEU", LoaiDichVu = "can_lam_sang", TenDichVu = "Xét nghiệm nước tiểu tổng quát", DonGia = 180000m, ThoiGianDuKienPhut = 20, MaPhongThucHien = "CLS_XN_02" },
        new DichVuYTe { MaDichVu = "DV_XN_MIEN_DICH", LoaiDichVu = "can_lam_sang", TenDichVu = "Xét nghiệm miễn dịch – hormon", DonGia = 550000m, ThoiGianDuKienPhut = 50, MaPhongThucHien = "CLS_XN_02" },
        new DichVuYTe { MaDichVu = "DV_XN_VIRUS", LoaiDichVu = "can_lam_sang", TenDichVu = "Xét nghiệm virus (HBV/HCV nhanh)", DonGia = 420000m, ThoiGianDuKienPhut = 45, MaPhongThucHien = "CLS_XN_02" },

        // ================== DỊCH VỤ CẬN LÂM SÀNG – CHẨN ĐOÁN HÌNH ẢNH ==================
        new DichVuYTe { MaDichVu = "DV_SIEU_AM_BUNG", LoaiDichVu = "can_lam_sang", TenDichVu = "Siêu âm bụng tổng quát", DonGia = 300000m, ThoiGianDuKienPhut = 20, MaPhongThucHien = "CLS_CDHA_01" },
        new DichVuYTe { MaDichVu = "DV_SIEU_AM_THAI", LoaiDichVu = "can_lam_sang", TenDichVu = "Siêu âm thai", DonGia = 350000m, ThoiGianDuKienPhut = 20, MaPhongThucHien = "CLS_CDHA_01" },
        new DichVuYTe { MaDichVu = "DV_X_QUANG_NGUC", LoaiDichVu = "can_lam_sang", TenDichVu = "X-quang ngực thẳng", DonGia = 250000m, ThoiGianDuKienPhut = 15, MaPhongThucHien = "CLS_CDHA_02" },

        new DichVuYTe { MaDichVu = "DV_X_QUANG_COT_SONG", LoaiDichVu = "can_lam_sang", TenDichVu = "X-quang cột sống thắt lưng", DonGia = 280000m, ThoiGianDuKienPhut = 20, MaPhongThucHien = "CLS_CDHA_02" },
        new DichVuYTe { MaDichVu = "DV_X_QUANG_XOANG", LoaiDichVu = "can_lam_sang", TenDichVu = "X-quang xoang", DonGia = 260000m, ThoiGianDuKienPhut = 15, MaPhongThucHien = "CLS_CDHA_02" },
        new DichVuYTe { MaDichVu = "DV_CT_SCAN_NAO", LoaiDichVu = "can_lam_sang", TenDichVu = "CT-Scan não không cản quang", DonGia = 900000m, ThoiGianDuKienPhut = 40, MaPhongThucHien = "CLS_CDHA_02" },
        new DichVuYTe { MaDichVu = "DV_CT_SCAN_BUNG", LoaiDichVu = "can_lam_sang", TenDichVu = "CT-Scan bụng tổng quát", DonGia = 1200000m, ThoiGianDuKienPhut = 45, MaPhongThucHien = "CLS_CDHA_02" },
        new DichVuYTe { MaDichVu = "DV_SIEU_AM_TIM", LoaiDichVu = "can_lam_sang", TenDichVu = "Siêu âm tim Doppler màu", DonGia = 650000m, ThoiGianDuKienPhut = 30, MaPhongThucHien = "CLS_CDHA_01" },
        new DichVuYTe { MaDichVu = "DV_MRI_NAO", LoaiDichVu = "can_lam_sang", TenDichVu = "MRI não không tiêm thuốc", DonGia = 1800000m, ThoiGianDuKienPhut = 50, MaPhongThucHien = "CLS_CDHA_02" },
        new DichVuYTe { MaDichVu = "DV_SIEU_AM_MACH_MAU", LoaiDichVu = "can_lam_sang", TenDichVu = "Siêu âm mạch máu", DonGia = 500000m, ThoiGianDuKienPhut = 25, MaPhongThucHien = "CLS_CDHA_01" },
        new DichVuYTe { MaDichVu = "DV_XN_HIV", LoaiDichVu = "can_lam_sang", TenDichVu = "Xét nghiệm HIV nhanh", DonGia = 350000m, ThoiGianDuKienPhut = 30, MaPhongThucHien = "CLS_XN_02" },
        new DichVuYTe { MaDichVu = "DV_XN_PCR_CUM", LoaiDichVu = "can_lam_sang", TenDichVu = "Xét nghiệm PCR cúm/RSV", DonGia = 750000m, ThoiGianDuKienPhut = 60, MaPhongThucHien = "CLS_XN_02" }
    };
        }

        private static List<LichTruc> SeedLichTruc(List<Phong> phongList, List<NhanVienYTe> nhanSuList)
        {
            var list = new List<LichTruc>();

            // Phan cong lich truc day du cho tat ca phong va ca
            var yTaAll = nhanSuList.Where(n => n.VaiTro == "y_ta").ToArray();
            if (yTaAll.Length == 0) return list;

            var yTaLs = yTaAll.Where(n => string.Equals(n.LoaiYTa, "ls", StringComparison.OrdinalIgnoreCase)).ToArray();
            var yTaCls = yTaAll.Where(n => string.Equals(n.LoaiYTa, "cls", StringComparison.OrdinalIgnoreCase)).ToArray();
            var yTaHc = yTaAll.Where(n => string.Equals(n.LoaiYTa, "hanhchinh", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (yTaLs.Length == 0) yTaLs = yTaAll;
            if (yTaCls.Length == 0) yTaCls = yTaAll;
            if (yTaHc.Length == 0) yTaHc = yTaAll;

            var roomGroups = phongList
                .Select(p => new
                {
                    Phong = p,
                    Pool = p.LoaiPhong == "phong_dich_vu" ? yTaCls : yTaLs.Concat(yTaHc).ToArray()
                })
                .ToArray();

            if (roomGroups.Length == 0) return list;

            var startDate = DateTime.Today.AddDays(-30).Date;
            var endDate = DateTime.Today.AddDays(60).Date;
            var caList = new[]
            {
                new { Code = "sang",  Start = new TimeSpan(7, 0, 0),   End = new TimeSpan(11, 30, 0) },
                new { Code = "chieu", Start = new TimeSpan(13, 0, 0),  End = new TimeSpan(17, 0, 0) },
                new { Code = "toi",   Start = new TimeSpan(17, 30, 0), End = new TimeSpan(21, 0, 0) },
            };

            int idxLs = 0, idxCls = 0, idxAll = 0;
            var roomKham = roomGroups.Where(g => g.Phong.LoaiPhong == "phong_kham").ToArray();
            var roomDichVu = roomGroups.Where(g => g.Phong.LoaiPhong == "phong_dich_vu").ToArray();
            var suffixCounter = new Dictionary<string, int>();

            for (var ngay = startDate; ngay <= endDate; ngay = ngay.AddDays(1))
            {
                foreach (var ca in caList)
                {
                    var assignedNurses = new HashSet<string>();
                    int spreadCls = 0, spreadLs = 0, spreadAny = 0;

                    // 1) Đảm bảo mỗi phòng đều có lịch trong ca này (phòng dịch vụ lấy CLS, phòng khám lấy LS)
                    foreach (var group in roomDichVu)
                    {
                        var phong = group.Phong;
                        var pool = group.Pool.Length > 0 ? group.Pool : yTaAll;
                        var yta = pool.Length > 0
                            ? pool[idxCls++ % pool.Length]
                            : yTaAll[idxAll++ % yTaAll.Length];

                        var key = $"{phong.MaPhong}_{ngay:yyyyMMdd}_{ca.Code}";
                        suffixCounter.TryGetValue(key, out var count);
                        count++;
                        suffixCounter[key] = count;

                        list.Add(new LichTruc
                        {
                            MaLichTruc = $"LT_{phong.MaPhong}_{ngay:yyyyMMdd}_{ca.Code.ToUpper()}_{count:00}",
                            Ngay = ngay,
                            CaTruc = ca.Code,
                            GioBatDau = ca.Start,
                            GioKetThuc = ca.End,
                            MaYTaTruc = yta.MaNhanVien,
                            MaPhong = phong.MaPhong,
                            NghiTruc = false
                        });

                        assignedNurses.Add(yta.MaNhanVien);
                    }

                    foreach (var group in roomKham)
                    {
                        var phong = group.Phong;
                        var pool = group.Pool.Length > 0 ? group.Pool : yTaAll;
                        var yta = pool.Length > 0
                            ? pool[idxLs++ % pool.Length]
                            : yTaAll[idxAll++ % yTaAll.Length];

                        var key = $"{phong.MaPhong}_{ngay:yyyyMMdd}_{ca.Code}";
                        suffixCounter.TryGetValue(key, out var count);
                        count++;
                        suffixCounter[key] = count;

                        list.Add(new LichTruc
                        {
                            MaLichTruc = $"LT_{phong.MaPhong}_{ngay:yyyyMMdd}_{ca.Code.ToUpper()}_{count:00}",
                            Ngay = ngay,
                            CaTruc = ca.Code,
                            GioBatDau = ca.Start,
                            GioKetThuc = ca.End,
                            MaYTaTruc = yta.MaNhanVien,
                            MaPhong = phong.MaPhong,
                            NghiTruc = false
                        });

                        assignedNurses.Add(yta.MaNhanVien);
                    }

                    // 2) Gán thêm cho tất cả y tá còn lại trong ca này (trải đều vào các phòng)
                    foreach (var yta in yTaAll.Where(y => !assignedNurses.Contains(y.MaNhanVien)))
                    {
                        var targetRooms = (string.Equals(yta.LoaiYTa, "cls", StringComparison.OrdinalIgnoreCase) && roomDichVu.Length > 0)
                            ? roomDichVu
                            : (roomKham.Length > 0 ? roomKham : roomGroups);

                        if (targetRooms.Length == 0) break;

                        int pickIndex;
                        if (ReferenceEquals(targetRooms, roomDichVu))
                        {
                            pickIndex = spreadCls++ % targetRooms.Length;
                        }
                        else if (ReferenceEquals(targetRooms, roomKham))
                        {
                            pickIndex = spreadLs++ % targetRooms.Length;
                        }
                        else
                        {
                            pickIndex = spreadAny++ % targetRooms.Length;
                        }

                        var phong = targetRooms[pickIndex].Phong;
                        var key = $"{phong.MaPhong}_{ngay:yyyyMMdd}_{ca.Code}";
                        suffixCounter.TryGetValue(key, out var count);
                        count++;
                        suffixCounter[key] = count;

                        list.Add(new LichTruc
                        {
                            MaLichTruc = $"LT_{phong.MaPhong}_{ngay:yyyyMMdd}_{ca.Code.ToUpper()}_{count:00}",
                            Ngay = ngay,
                            CaTruc = ca.Code,
                            GioBatDau = ca.Start,
                            GioKetThuc = ca.End,
                            MaYTaTruc = yta.MaNhanVien,
                            MaPhong = phong.MaPhong,
                            NghiTruc = false
                        });
                        assignedNurses.Add(yta.MaNhanVien);
                    }
                }
            }

            return list;
        }

        private static List<LichHenKham> SeedLichHen(List<LichTruc> lichTrucList, List<BenhNhan> benhNhanList)
        {
            var list = new List<LichHenKham>();
            var random = new Random(999);

            if (!benhNhanList.Any() || !lichTrucList.Any()) return list;

            var lichTrucSorted = lichTrucList
                .OrderBy(l => l.Ngay)
                .ThenBy(l => l.CaTruc)
                .ToList();

            int patientIndex = 0;
            int maxAppointments = 60;     // TỔNG LỊCH HẸN TOÀN HỆ THỐNG
            int created = 0;

            foreach (var lt in lichTrucSorted)
            {
                if (created >= maxAppointments) break;

                // Số lượng lịch hẹn cho ca này, nhưng không vượt quá remaining
                int remaining = maxAppointments - created;
                int soLuongHen = random.Next(0, Math.Min(4, remaining + 1)); // 0–3 mỗi ca

                // Slot đầu tiên trong ca
                var currentTime = lt.GioBatDau.Add(TimeSpan.FromMinutes(15));

                for (int k = 0; k < soLuongHen; k++)
                {
                    if (created >= maxAppointments) break;

                    var bn = benhNhanList[patientIndex % benhNhanList.Count];
                    patientIndex++;

                    if (lt.Ngay == DateTime.Today)
                    {
                        bn.TrangThaiHomNay = "cho_kham";
                    }

                    string trangThaiHen;
                    if (lt.Ngay < DateTime.Today)
                    {
                        trangThaiHen = "da_hoan_tat";
                    }
                    else if (lt.Ngay == DateTime.Today)
                    {
                        trangThaiHen = (random.Next(2) == 0) ? "dang_cho" : "da_checkin";
                    }
                    else
                    {
                        trangThaiHen = "da_xac_nhan";
                    }

                    if (random.Next(10) == 0) trangThaiHen = "da_huy";

                    list.Add(new LichHenKham
                    {
                        MaLichHen = $"LH_{lt.MaLichTruc}_{k + 1:00}",
                        CoHieuLuc = true,
                        NgayHen = lt.Ngay,
                        GioHen = currentTime,
                        ThoiLuongPhut = 30,
                        MaBenhNhan = bn.MaBenhNhan,
                        LoaiHen = (random.Next(3) == 0) ? "kham_moi" : "tai_kham",
                        TenBenhNhan = bn.HoTen,
                        SoDienThoai = bn.DienThoai ?? "0900000000",
                        MaLichTruc = lt.MaLichTruc,
                        GhiChu = (random.Next(5) == 0) ? "Bệnh nhân yêu cầu khám nhanh" : null,
                        TrangThai = trangThaiHen
                    });

                    created++;

                    // Thời gian slot tiếp theo (20–30 phút)
                    currentTime = currentTime.Add(TimeSpan.FromMinutes(random.Next(20, 31)));
                    if (currentTime >= lt.GioKetThuc) break;
                }

                if (created >= maxAppointments) break;
            }

            return list;
        }



        private static List<PhieuKhamLamSang> SeedPhieuKham(
    List<BenhNhan> benhNhanList,
    List<NhanVienYTe> nhanSuList,
    List<LichHenKham> lichHenList,
    List<DichVuYTe> dichVuList)
        {
            var list = new List<PhieuKhamLamSang>();
            var random = new Random(555);

            if (!benhNhanList.Any() || !nhanSuList.Any()) return list;

            var bacSiMap = nhanSuList
                .Where(n => n.VaiTro == "bac_si")
                .GroupBy(n => n.MaKhoa)
                .ToDictionary(g => g.Key, g => g.ToList());

            var nguoiLap = nhanSuList.FirstOrDefault(n => n.VaiTro == "y_ta") ?? nhanSuList.First();

            var trieuChungMap = new Dictionary<string, string[]>
    {
        { "KHOA_NOI", new[] { "Đau bụng vùng thượng vị", "Chóng mặt, buồn nôn", "Ho khan kéo dài", "Mệt mỏi, chán ăn", "Đau tức ngực trái" } },
        { "KHOA_NGOAI", new[] { "Đau vết mổ cũ", "Sưng đau khớp gối", "Chấn thương phần mềm tay phải", "Đau lưng lan xuống chân" } },
        { "KHOA_NHI", new[] { "Sốt cao 39 độ", "Ho đờm, sổ mũi", "Nổi ban đỏ toàn thân", "Quấy khóc, bỏ bú", "Tiêu chảy 3 ngày" } },
        { "KHOA_SAN", new[] { "Trễ kinh 2 tuần", "Đau bụng dưới", "Ra huyết bất thường", "Khám thai định kỳ" } },
        { "KHOA_MAT", new[] { "Mắt nhìn mờ", "Đỏ mắt, chảy nước mắt", "Cộm mắt phải", "Giảm thị lực đột ngột" } },
        { "KHOA_TMH", new[] { "Đau họng, nuốt vướng", "Ù tai trái", "Ngạt mũi, chảy mũi xanh", "Khàn tiếng" } },
        { "KHOA_RHM", new[] { "Đau răng hàm dưới", "Sưng nướu", "Ê buốt khi uống lạnh", "Lung lay răng cửa" } }
    };
            var defaultSymptoms = new[] { "Khám sức khỏe tổng quát", "Mệt mỏi trong người", "Đau đầu nhẹ" };

            var today = DateTime.Today;

            // 1. TẠO PHIẾU KHÁM TỪ LỊCH HẸN (dời về hôm nay + các ngày sau)
            var lichHenDaKham = lichHenList
                .Where(lh => lh.NgayHen <= DateTime.Now)
                .ToList();

            // Ngày hẹn nhỏ nhất -> map về hôm nay
            DateTime minNgayHen = today;
            if (lichHenDaKham.Any())
            {
                minNgayHen = lichHenDaKham.Min(lh => lh.NgayHen.Date);
            }

            foreach (var lh in lichHenDaKham)
            {
                // 90% lịch hẹn đã khám
                if (random.Next(100) < 10) continue;

                var khoaKeys = bacSiMap.Keys.ToList();
                if (!khoaKeys.Any()) continue;

                var maKhoa = khoaKeys[random.Next(khoaKeys.Count)];

                if (!bacSiMap.TryGetValue(maKhoa, out var listBS) || !listBS.Any()) continue;
                var bs = listBS[random.Next(listBS.Count)];

                var symptoms = trieuChungMap.ContainsKey(maKhoa) ? trieuChungMap[maKhoa] : defaultSymptoms;
                var trieuChung = symptoms[random.Next(symptoms.Length)];

                var dichVuKham = dichVuList.FirstOrDefault(d =>
                                        d.LoaiDichVu == "kham_lam_sang" &&
                                        d.TenDichVu.Contains(maKhoa.Replace("KHOA_", ""), StringComparison.OrdinalIgnoreCase))
                                 ?? dichVuList.FirstOrDefault(d => d.LoaiDichVu == "kham_lam_sang");

                // Chuẩn hoá ngày lập: dời khoảng cách từ minNgayHen sang today
                var offsetDays = (lh.NgayHen.Date - minNgayHen).Days;
                var ngayLap = today.AddDays(offsetDays);
                var gioLap = lh.GioHen;

                list.Add(new PhieuKhamLamSang
                {
                    MaPhieuKham = GeneratorID.NewLuotKhamId().Replace("LK", "PK"),
                    MaBacSiKham = bs.MaNhanVien,
                    MaNguoiLap = nguoiLap.MaNhanVien,
                    MaBenhNhan = lh.MaBenhNhan,
                    MaLichHen = lh.MaLichHen,
                    MaDichVuKham = dichVuKham?.MaDichVu ?? "DV_KHAM_TQ",
                    HinhThucTiepNhan = "appointment",
                    NgayLap = ngayLap,
                    GioLap = gioLap,
                    TrieuChung = trieuChung,
                    TrangThai = (ngayLap.Date == today) ? "dang_kham" : "da_hoan_tat"
                });
            }

            // 2. PHIẾU KHÁM VÃNG LAI: 40 ca, rải từ hôm nay đến hôm nay + 7 ngày
            int walkInCount = 40;
            int walkInRangeDays = 8; // today -> today + 7

            var khoaKeysAll = bacSiMap.Keys.ToList();
            if (!khoaKeysAll.Any()) return list;

            for (int i = 0; i < walkInCount; i++)
            {
                // random trong 8 ngày: hôm nay → hôm nay + 7
                var ngay = today.AddDays(random.Next(walkInRangeDays));

                var bn = benhNhanList[random.Next(benhNhanList.Count)];

                var maKhoa = khoaKeysAll[random.Next(khoaKeysAll.Count)];
                var listBS2 = bacSiMap[maKhoa];
                if (!listBS2.Any()) continue;

                var bs2 = listBS2[random.Next(listBS2.Count)];

                var symptoms2 = trieuChungMap.ContainsKey(maKhoa) ? trieuChungMap[maKhoa] : defaultSymptoms;

                list.Add(new PhieuKhamLamSang
                {
                    MaPhieuKham = GeneratorID.NewLuotKhamId().Replace("LK", "PKW"),
                    MaBacSiKham = bs2.MaNhanVien,
                    MaNguoiLap = nguoiLap.MaNhanVien,
                    MaBenhNhan = bn.MaBenhNhan,
                    MaLichHen = null,
                    MaDichVuKham = "DV_KHAM_TQ",
                    HinhThucTiepNhan = "walkin",
                    NgayLap = ngay,
                    GioLap = new TimeSpan(random.Next(7, 16), random.Next(0, 59), 0),
                    TrieuChung = symptoms2[random.Next(symptoms2.Length)] + " (Khám gấp)",
                    TrangThai = "da_hoan_tat"
                });
            }

            return list;
        }



        private static List<PhieuKhamCanLamSang> SeedPhieuKhamCls(List<PhieuKhamLamSang> phieuKhamList)
        {
            var list = new List<PhieuKhamCanLamSang>();
            var random = new Random(666);

            var validPhieuKham = phieuKhamList.Where(p => p.TrangThai == "da_hoan_tat").ToList();

            foreach (var pk in validPhieuKham)
            {
                // Giảm xuống ~30% bệnh nhân có CLS
                if (random.Next(100) >= 30) continue;

                list.Add(new PhieuKhamCanLamSang
                {
                    MaPhieuKhamCls = pk.MaPhieuKham.Replace("PK", "CLS"),
                    MaPhieuKhamLs = pk.MaPhieuKham,
                    NgayGioLap = pk.NgayLap.Add(pk.GioLap).AddMinutes(random.Next(15, 45)),
                    AutoPublishEnabled = true,
                    TrangThai = "da_hoan_tat",
                    GhiChu = "Chỉ định cận lâm sàng thường quy"
                });
            }

            // Bảo đảm luôn có một ít phiếu CLS để sinh dữ liệu hàng đợi CLS
            if (list.Count == 0 && validPhieuKham.Any())
            {
                foreach (var pk in validPhieuKham.Take(5))
                {
                    list.Add(new PhieuKhamCanLamSang
                    {
                        MaPhieuKhamCls = pk.MaPhieuKham.Replace("PK", "CLS"),
                        MaPhieuKhamLs = pk.MaPhieuKham,
                        NgayGioLap = pk.NgayLap.Add(pk.GioLap).AddMinutes(random.Next(15, 45)),
                        AutoPublishEnabled = true,
                        TrangThai = "da_hoan_tat",
                        GhiChu = "Fallback seed CLS"
                    });
                }
            }
            return list;
        }


        private static List<ChiTietDichVu> SeedChiTietDichVuCls(
     List<PhieuKhamCanLamSang> phieuKhamClsList,
     List<DichVuYTe> dichVuList)
        {
            var list = new List<ChiTietDichVu>();
            var random = new Random(777);

            // Lấy danh sách dịch vụ là CLS
            var dvCls = dichVuList.Where(d => d.LoaiDichVu == "can_lam_sang").ToList();
            if (!dvCls.Any()) return list;

            foreach (var pkCls in phieuKhamClsList)
            {
                // Mỗi phiếu chỉ định 1 đến 3 dịch vụ
                int serviceCount = random.Next(1, 4);

                // Shuffle dịch vụ để lấy ngẫu nhiên không trùng
                var selectedServices = dvCls.OrderBy(x => random.Next()).Take(serviceCount).ToList();

                foreach (var dv in selectedServices)
                {
                    list.Add(new ChiTietDichVu
                    {
                        MaChiTietDv = GeneratorID.NewLuotKhamId().Replace("LK", "CTDV"),
                        MaPhieuKhamCls = pkCls.MaPhieuKhamCls,
                        MaDichVu = dv.MaDichVu,
                        TrangThai = "da_co_ket_qua",
                        GhiChu = "Đã thực hiện xong"
                    });
                }
            }
            return list;
        }

        private static List<KetQuaDichVu> SeedKetQuaDichVu(
     List<ChiTietDichVu> chiTietDichVuList,
     List<NhanVienYTe> nhanSuList)
        {
            var list = new List<KetQuaDichVu>();
            var random = new Random(888);
            var nhanSuNhapKq = nhanSuList.FirstOrDefault(n => n.VaiTro == "y_ta" && n.MaKhoa == "KHOA_XN") ?? nhanSuList.First();

            foreach (var ct in chiTietDichVuList)
            {
                string ketQuaText;

                // Giả lập kết quả dựa trên mã dịch vụ (lấy từ các bước trước hoặc đoán)
                if (ct.MaDichVu.Contains("XN"))
                    ketQuaText = "Bạch cầu: 7.5 G/L (Bình thường)\nHồng cầu: 4.8 T/L\nTiểu cầu: 250 G/L\nĐường huyết: 5.5 mmol/L";
                else if (ct.MaDichVu.Contains("SIEU_AM"))
                    ketQuaText = "Gan: Nhu mô đều, không có khối khu trú.\nMật: Túi mật không sỏi, thành mỏng.\nThận: Hai thận kích thước bình thường, không ứ nước.";
                else if (ct.MaDichVu.Contains("X_QUANG"))
                    ketQuaText = "Tim phổi bình thường. Không thấy hình ảnh tổn thương như viêm, u.";
                else
                    ketQuaText = "Kết quả trong giới hạn bình thường.";

                // 20% có kết quả bất thường
                if (random.Next(100) < 20)
                {
                    ketQuaText = "CẢNH BÁO: Chỉ số cao hơn mức cho phép. Đề nghị bác sĩ lâm sàng kiểm tra lại.";
                }

                list.Add(new KetQuaDichVu
                {
                    MaKetQua = GeneratorID.NewLuotKhamId().Replace("LK", "KQ"),
                    MaChiTietDv = ct.MaChiTietDv,
                    TrangThaiChot = "hoan_tat",
                    NoiDungKetQua = ketQuaText,
                    MaNguoiTao = nhanSuNhapKq.MaNhanVien,
                    ThoiGianTao = DateTime.Now.AddMinutes(-random.Next(10, 100)), // Đã có kết quả gần đây
                    TepDinhKem = null // Có thể thêm link ảnh giả nếu cần
                });
            }
            return list;
        }


        private static List<PhieuTongHopKetQua> SeedPhieuTongHop(
    List<PhieuKhamCanLamSang> phieuKhamClsList,
    List<NhanVienYTe> nhanSuList)
        {
            var list = new List<PhieuTongHopKetQua>();
            // Lấy bác sĩ để gán người xử lý
            var bacSi = nhanSuList.Where(n => n.VaiTro == "bac_si").ToArray();
            var random = new Random(404);

            // Chỉ tạo phiếu tổng hợp cho các phiếu CLS đã hoàn tất
            var completedCls = phieuKhamClsList.Where(p => p.TrangThai == "da_hoan_tat").ToList();

            foreach (var cls in completedCls)
            {
                var bs = bacSi[random.Next(bacSi.Length)];

                list.Add(new PhieuTongHopKetQua
                {
                    MaPhieuTongHop = GeneratorID.NewLuotKhamId().Replace("LK", "PTH"),
                    MaPhieuKhamCls = cls.MaPhieuKhamCls,
                    LoaiPhieu = "tong_hop_cls",
                    MaNhanSuXuLy = bs.MaNhanVien, // Bác sĩ đọc kết quả
                    TrangThai = "da_hoan_tat",
                    ThoiGianXuLy = cls.NgayGioLap.AddHours(2), // Xử lý sau khi có kết quả CLS khoảng 2h
                    SnapshotJson = "{}" // Giả lập JSON kết quả
                });
            }

            return list;
        }


        private static List<PhieuChanDoanCuoi> SeedPhieuChanDoan(
    List<PhieuKhamLamSang> phieuKhamList,
    List<PhieuKhamCanLamSang> phieuKhamClsList)
        {
            var list = new List<PhieuChanDoanCuoi>();
            var clsMap = phieuKhamClsList.ToDictionary(c => c.MaPhieuKhamLs, c => c);
            var completedPk = phieuKhamList.Where(p => p.TrangThai == "da_hoan_tat").ToList();

            foreach (var pk in completedPk)
            {
                bool hasCls = clsMap.TryGetValue(pk.MaPhieuKham, out var cls);

                list.Add(new PhieuChanDoanCuoi
                {
                    MaPhieuChanDoan = GeneratorID.NewLuotKhamId().Replace("LK", "CD"),
                    MaPhieuKham = pk.MaPhieuKham,
                    MaDonThuoc = null,
                    ChanDoanSoBo = "Theo dõi sức khỏe",
                    ChanDoanCuoi = "Viêm họng / Rối loạn tiêu hóa",
                    NoiDungKham = hasCls ? "Đã xem kết quả CLS: Bình thường." : "Khám lâm sàng: Phổi trong, tim đều.",
                    HuongXuTri = "Điều trị ngoại trú",
                    LoiKhuyen = "Nghỉ ngơi, uống nhiều nước.",
                    PhatDoDieuTri = "Theo phác đồ."
                });
            }
            return list;
        }
        private static (List<HangDoi>, List<LuotKhamBenh>) SeedHangDoiVaLuotKhamCLS(
    List<ChiTietDichVu> chiTietDvList,
    List<PhieuKhamCanLamSang> phieuKhamClsList,
    List<PhieuKhamLamSang> phieuKhamList,
    List<DichVuYTe> dichVuList,
    List<NhanVienYTe> nhanSuList)
        {
            var hangDoiList = new List<HangDoi>();
            var luotKhamList = new List<LuotKhamBenh>();

            var random = new Random(505);
            var ktvXN = nhanSuList.Where(n => n.MaKhoa == "KHOA_XN").ToList();
            var ktvCDHA = nhanSuList.Where(n => n.MaKhoa == "KHOA_CDHA").ToList();

            // Map nhanh để tra cứu thông tin cha
            var pkClsMap = phieuKhamClsList.ToDictionary(p => p.MaPhieuKhamCls, p => p);
            var pkLsMap = phieuKhamList.ToDictionary(p => p.MaPhieuKham, p => p);
            var dvMap = dichVuList.ToDictionary(d => d.MaDichVu, d => d);

            foreach (var ct in chiTietDvList)
            {
                // 1. Validate dữ liệu cha
                if (!pkClsMap.TryGetValue(ct.MaPhieuKhamCls, out var pkCls)) continue;
                if (!pkLsMap.TryGetValue(pkCls.MaPhieuKhamLs, out var pkLs)) continue;
                if (!dvMap.TryGetValue(ct.MaDichVu, out var dvInfo)) continue;

                // 2. Xác định phòng thực hiện & KTV
                string maPhong = dvInfo.MaPhongThucHien ?? "CLS_XN_01";
                var listNhanSu = (dvInfo.MaDichVu.Contains("XN")) ? ktvXN : ktvCDHA;
                var nsThucHien = listNhanSu.Any() ? listNhanSu[random.Next(listNhanSu.Count)] : nhanSuList.First();

                // 3. Tạo Hàng Đợi (Queue) cho dịch vụ này
                // Nếu chi tiết dịch vụ đã có kết quả -> Queue phải là "da_phuc_vu"
                string trangThaiQueue = (ct.TrangThai == "da_co_ket_qua") ? "da_phuc_vu" : "cho_goi";

                var hd = new HangDoi
                {
                    MaHangDoi = GeneratorID.NewHangDoiId(),
                    MaBenhNhan = pkLs.MaBenhNhan,
                    MaPhong = maPhong,
                    LoaiHangDoi = "can_lam_sang", // Queue CLS
                    Nguon = null, // Từ chỉ định bác sĩ
                    Nhan = null,
                    CapCuu = false,
                    PhanLoaiDen = null,
                    ThoiGianCheckin = pkCls.NgayGioLap, // Checkin lúc lập phiếu CLS
                    ThoiGianLichHen = null,
                    DoUuTien = 0,
                    TrangThai = trangThaiQueue,
                    GhiChu = dvInfo.TenDichVu,
                    MaPhieuKham = null,
                    MaChiTietDv = ct.MaChiTietDv // Link chặt chẽ với Chi tiết DV
                };
                hangDoiList.Add(hd);

                // 4. Tạo Lượt Khám (Visit) nếu đã phục vụ
                if (trangThaiQueue == "da_phuc_vu")
                {
                    var start = hd.ThoiGianCheckin.AddMinutes(random.Next(5, 30));
                    luotKhamList.Add(new LuotKhamBenh
                    {
                        MaLuotKham = GeneratorID.NewLuotKhamId(),
                        MaHangDoi = hd.MaHangDoi,
                        MaNhanSuThucHien = nsThucHien.MaNhanVien,
                        MaYTaHoTro = nsThucHien.MaNhanVien, // KTV tự làm hoặc y tá hỗ trợ
                        LoaiLuot = "can_lam_sang",
                        ThoiGianBatDau = start,
                        ThoiGianKetThuc = start.AddMinutes(dvInfo.ThoiGianDuKienPhut),
                        TrangThai = "hoan_tat"
                    });
                }
            }

            return (hangDoiList, luotKhamList);
        }
        private static (List<DonThuoc>, List<ChiTietDonThuoc>, List<HoaDonThanhToan>) SeedDonThuocVaHoaDon(
     List<PhieuChanDoanCuoi> phieuChanDoanList,
     List<PhieuKhamLamSang> phieuKhamList,
     List<KhoThuoc> khoThuocList,
     List<NhanVienYTe> nhanSuList)
        {
            var donThuocList = new List<DonThuoc>();
            var chiTietList = new List<ChiTietDonThuoc>();
            var hoaDonList = new List<HoaDonThanhToan>();
            var random = new Random(222);
            var thuNgan = nhanSuList.First();
            var pkMap = phieuKhamList.ToDictionary(p => p.MaPhieuKham, p => p);

            foreach (var cd in phieuChanDoanList)
            {
                // 20% Bệnh nhân không cần thuốc, chỉ dặn dò
                if (random.Next(100) < 20) continue;

                if (!pkMap.TryGetValue(cd.MaPhieuKham, out var pk)) continue;

                var dtId = GeneratorID.NewDonThuocId();

                // Tạo Đơn thuốc
                var donThuoc = new DonThuoc
                {
                    MaDonThuoc = dtId,
                    MaBacSiKeDon = pk.MaBacSiKham,
                    MaBenhNhan = pk.MaBenhNhan,
                    ThoiGianKeDon = pk.NgayLap.Add(pk.GioLap).AddHours(2), // Sau khi khám xong
                    TrangThai = "da_phat",
                    TongTienDon = 0
                };
                donThuocList.Add(donThuoc);
                cd.MaDonThuoc = dtId; // Link vào chẩn đoán

                // Chi tiết thuốc & Tiền
                decimal tongTien = 0;
                int soLoai = random.Next(1, 4);
                for (int k = 0; k < soLoai; k++)
                {
                    var thuoc = khoThuocList[random.Next(khoThuocList.Count)];
                    var sl = random.Next(5, 15);
                    var tien = sl * thuoc.GiaNiemYet;
                    tongTien += tien;
                    chiTietList.Add(new ChiTietDonThuoc
                    {
                        MaChiTietDon = GeneratorID.NewLuotKhamId().Replace("LK", "CTDT"),
                        MaDonThuoc = dtId,
                        MaThuoc = thuoc.MaThuoc,
                        SoLuong = sl,
                        ThanhTien = tien,
                        ChiDinhSuDung = "Uống sau ăn"
                    });
                }
                donThuoc.TongTienDon = tongTien;

                // Hóa đơn thuốc
                hoaDonList.Add(new HoaDonThanhToan
                {
                    MaHoaDon = GeneratorID.NewHoaDonId(),
                    MaBenhNhan = pk.MaBenhNhan,
                    MaNhanSuThu = thuNgan.MaNhanVien,
                    MaDonThuoc = dtId,
                    LoaiDotthu = "thuoc",
                    SoTien = tongTien,
                    ThoiGian = donThuoc.ThoiGianKeDon.AddMinutes(10),
                    TrangThai = "da_thu",
                    NoiDung = "Tiền thuốc"
                });
            }
            return (donThuocList, chiTietList, hoaDonList);
        }

        private static (List<ThongBaoHeThong>, List<ThongBaoNguoiNhan>) SeedThongBao(List<LuotKhamBenh> luotKhamList, List<BenhNhan> benhNhanList, List<NhanVienYTe> nhanSuList)
        {
            var tbHeThongList = new List<ThongBaoHeThong>();
            var tbNguoiNhanList = new List<ThongBaoNguoiNhan>();
            var luotKhamUse = luotKhamList.Take(10).ToArray();
            var today = DateTime.Today;

            for (int i = 0; i < 20; i++)
            {
                var lk = luotKhamUse[i % luotKhamUse.Length];
                var bn = benhNhanList[i % benhNhanList.Count];
                var tbId = GeneratorID.NewThongBaoId();

                var tb = new ThongBaoHeThong
                {
                    MaThongBao = tbId,
                    TieuDe = (i % 2 == 0) ? "Nhắc tái khám" : "Thông báo kết quả khám",
                    NoiDung = (i % 2 == 0) ? "Quý khách vui lòng tái khám theo lịch hẹn." : "Quý khách vui lòng xem lại kết quả khám và tuân thủ hướng dẫn điều trị.",
                    LoaiThongBao = (i % 2 == 0) ? "reminder" : "result",
                    DoUuTien = (i % 3 == 0) ? "cao" : "binh_thuong",
                    ThoiGianGui = today.AddDays(-i).AddHours(15),
                    MaLuotKham = lk.MaLuotKham,
                    TrangThai = "da_gui"
                };
                tbHeThongList.Add(tb);

                tbNguoiNhanList.Add(new ThongBaoNguoiNhan
                {
                    MaThongBao = tbId,
                    LoaiNguoiNhan = "benh_nhan",
                    MaBenhNhan = bn.MaBenhNhan,
                    DaDoc = (i % 3 == 0),
                    ThoiGianDoc = (i % 3 == 0) ? tb.ThoiGianGui.AddHours(1) : null
                });

                var staff = nhanSuList.First();
                tbNguoiNhanList.Add(new ThongBaoNguoiNhan
                {
                    MaThongBao = tbId,
                    LoaiNguoiNhan = "nhan_vien_y_te",
                    MaNhanSu = staff.MaNhanVien,
                    DaDoc = false,
                    ThoiGianDoc = null
                });
            }
            return (tbHeThongList, tbNguoiNhanList);
        }

        private static void LinkDonThuocVaoChanDoan(List<PhieuChanDoanCuoi> phieuChanDoanList, List<DonThuoc> donThuocList)
        {
            for (int i = 0; i < phieuChanDoanList.Count && i < donThuocList.Count; i++)
            {
                phieuChanDoanList[i].MaDonThuoc = donThuocList[i].MaDonThuoc;
            }
        }
    }
}
