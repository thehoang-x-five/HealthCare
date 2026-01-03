namespace HealthCare.Enums
{
    /// <summary>
    /// Trạng thái tài khoản bệnh nhân
    /// </summary>
    public static class TrangThaiTaiKhoan
    {
        public const string HoatDong = "hoat_dong";
        public const string KhongHoatDong = "khong_hoat_dong";
        public const string DaXoa = "da_xoa";
    }

    /// <summary>
    /// Trạng thái hôm nay của bệnh nhân (workflow trong ngày)
    /// </summary>
    public static class TrangThaiHomNay
    {
        public const string ChoTiepNhan = "cho_tiep_nhan";  // Mặc định khi mới vào
        public const string ChoKham = "cho_kham";
        public const string DangKham = "dang_kham";  // Đang khám lâm sàng (LS)
        public const string ChoTiepNhanDv = "cho_tiep_nhan_dv";
        public const string ChoKhamDv = "cho_kham_dv";
        public const string DangKhamDv = "dang_kham_dv";  // Đang khám dịch vụ (CLS)
        public const string ChoXuLy = "cho_xu_ly";
        public const string ChoXuLyDv = "cho_xu_ly_dv";
        public const string DaHoanTat = "da_hoan_tat";
        public const string DaHuy = "da_huy";
    }

    /// <summary>
    /// Trạng thái phiếu khám lâm sàng
    /// </summary>
    public static class TrangThaiPhieuKhamLs
    {
        public const string DaLap = "da_lap";
        public const string DangThucHien = "dang_thuc_hien";
        public const string DaLapChanDoan = "da_lap_chan_doan";
        public const string DaHoanTat = "da_hoan_tat";
        public const string DaHuy = "da_huy";
    }

    /// <summary>
    /// Trạng thái phiếu khám cận lâm sàng (CLS)
    /// </summary>
    public static class TrangThaiPhieuKhamCls
    {
        public const string DaLap = "da_lap";
        public const string DangThucHien = "dang_thuc_hien";
        public const string DaHoanTat = "da_hoan_tat";
        public const string DaHuy = "da_huy";
    }

    /// <summary>
    /// Trạng thái chi tiết dịch vụ CLS
    /// </summary>
    public static class TrangThaiChiTietDv
    {
        public const string DaLap = "da_lap";
        public const string DangThucHien = "dang_thuc_hien";
        public const string DaCoKetQua = "da_co_ket_qua";
        public const string ChuaCoKetQua = "chua_co_ket_qua";
        public const string DaHuy = "da_huy";
    }

    /// <summary>
    /// Trạng thái đơn thuốc
    /// </summary>
    public static class TrangThaiDonThuoc
    {
        public const string DaKe = "da_ke";
        public const string ChoPhat = "cho_phat";
        public const string DaPhat = "da_phat";
        public const string DaHuy = "da_huy";
    }

    /// <summary>
    /// Trạng thái lịch hẹn khám
    /// </summary>
    public static class TrangThaiLichHen
    {
        public const string DangCho = "dang_cho";
        public const string DaXacNhan = "da_xac_nhan";
        public const string DaCheckin = "da_checkin";
        public const string DaHuy = "da_huy";
    }

    /// <summary>
    /// Trạng thái hàng đợi
    /// </summary>
    public static class TrangThaiHangDoi
    {
        public const string ChoGoi = "cho_goi";
        public const string DangGoi = "dang_goi";
        public const string DangThucHien = "dang_thuc_hien";
        public const string DaPhucVu = "da_phuc_vu";
    }

    /// <summary>
    /// Trạng thái lượt khám bệnh
    /// </summary>
    public static class TrangThaiLuotKham
    {
        public const string DangThucHien = "dang_thuc_hien";
        public const string HoanTat = "hoan_tat";
    }

    /// <summary>
    /// Trạng thái hóa đơn thanh toán
    /// </summary>
    public static class TrangThaiHoaDon
    {
        public const string ChuaThu = "chua_thu";
        public const string DaThu = "da_thu";
        public const string DaHuy = "da_huy";
    }

    /// <summary>
    /// Trạng thái thông báo
    /// </summary>
    public static class TrangThaiThongBao
    {
        public const string ChuaGui = "chua_gui";
        public const string DaGui = "da_gui";
        public const string DaDoc = "da_doc";
    }

    /// <summary>
    /// Trạng thái phiếu tổng hợp kết quả
    /// </summary>
    public static class TrangThaiPhieuTongHop
    {
        public const string DangThucHien = "dang_thuc_hien";
        public const string ChoXuLy = "cho_xu_ly";
        public const string DangXuLy = "dang_xu_ly";
        public const string DaHoanTat = "da_hoan_tat";
    }

    /// <summary>
    /// Trạng thái kết quả dịch vụ
    /// </summary>
    public static class TrangThaiKetQua
    {
        public const string HoanTat = "hoan_tat";
        public const string ChuaHoanTat = "chua_hoan_tat";
    }

    /// <summary>
    /// Loại hàng đợi
    /// </summary>
    public static class LoaiHangDoi
    {
        public const string KhamLamSang = "kham_lam_sang";
        public const string CanLamSang = "can_lam_sang";
    }

    /// <summary>
    /// Nguồn hàng đợi
    /// </summary>
    public static class NguonHangDoi
    {
        public const string Appointment = "appointment";
        public const string Walkin = "walkin";
        public const string ServiceReturn = "service_return";
    }

    /// <summary>
    /// Phân loại đến (theo lịch hẹn)
    /// </summary>
    public static class PhanLoaiDen
    {
        public const string DungGio = "dung_gio";
        public const string DenSom = "den_som";
        public const string DenMuon = "den_muon";
    }

    /// <summary>
    /// Hình thức tiếp nhận
    /// </summary>
    public static class HinhThucTiepNhan
    {
        public const string Walkin = "walkin";
        public const string Appointment = "appointment";
        public const string ServiceReturn = "service_return";
    }

    /// <summary>
    /// Loại lượt khám
    /// </summary>
    public static class LoaiLuotKham
    {
        public const string KhamLamSang = "kham_lam_sang";
        public const string CanLamSang = "can_lam_sang";
        public const string TaiKham = "tai_kham";
    }

    /// <summary>
    /// Loại phòng
    /// </summary>
    public static class LoaiPhong
    {
        public const string KhamBenh = "kham_benh";
        public const string XetNghiem = "xet_nghiem";
        public const string ChanDoanHinhAnh = "chan_doan_hinh_anh";
        public const string ThamDoChucNang = "tham_do_chuc_nang";
        public const string NhaThuoc = "nha_thuoc";
        public const string ThuNgan = "thu_ngan";
    }

    /// <summary>
    /// Loại dịch vụ y tế
    /// </summary>
    public static class LoaiDichVu
    {
        public const string KhamBenh = "kham_benh";
        public const string XetNghiem = "xet_nghiem";
        public const string ChanDoanHinhAnh = "chan_dan_hinh_anh";
        public const string ThamDoChucNang = "tham_do_chuc_nang";
    }

    /// <summary>
    /// Loại đợt thu (hóa đơn)
    /// </summary>
    public static class LoaiDotThu
    {
        public const string KhamLamSang = "kham_lam_sang";
        public const string CanLamSang = "can_lam_sang";
        public const string Thuoc = "thuoc";
    }

    /// <summary>
    /// Loại y tá
    /// </summary>
    public static class LoaiYTa
    {
        public const string LamSang = "lamsang";
        public const string CanLamSang = "canlamsang";
        public const string HanhChinh = "hanhchinh";
    }

    /// <summary>
    /// Vai trò người dùng
    /// </summary>
    public static class VaiTro
    {
        public const string BacSi = "bac_si";
        public const string YTa = "y_ta";
        public const string ThuNgan = "thu_ngan";
        public const string QuanTri = "quan_tri";
        public const string LeTan = "le_tan";
    }

    /// <summary>
    /// Loại người nhận thông báo
    /// </summary>
    public static class LoaiNguoiNhan
    {
        public const string BenhNhan = "benh_nhan";
        public const string NhanVienYTe = "nhan_vien_y_te";
        public const string Staff = "staff";
        public const string NhanSu = "nhan_su";
    }

    /// <summary>
    /// Mức độ ưu tiên thông báo
    /// </summary>
    public static class MucDoUuTien
    {
        public const string Cao = "cao";
        public const string Thuong = "thuong";
        public const string Thap = "thap";
    }

    /// <summary>
    /// Loại phiếu tổng hợp
    /// </summary>
    public static class LoaiPhieu
    {
        public const string TongHopCls = "tong_hop_cls";
        public const string TongHopLs = "tong_hop_ls";
    }
}
