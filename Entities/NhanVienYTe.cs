using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HealthCare.Entities
{
    [Table("nhan_vien_y_te")]
    public class NhanVienYTe
    {
        [Key]
        public string MaNhanVien { get; set; } = default!;
        public string TenDangNhap { get; set; } = default!;
        public string MatKhauHash { get; set; } = default!;
        public string? AnhDaiDien { get; set; }
        public int SoNamKinhNghiem { get; set; }
        public string? ChuyenMon { get; set; }
        public string? HocVi { get; set; }
        public string HoTen { get; set; } = default!;
        public string VaiTro { get; set; } = "bac_si"; // bac_si,y_ta
        public string? LoaiYTa { get; set; } // hanhchinh,ls,cls
        public string ChucVu { get; set; } = "bac_si"; // bac_si, y_ta_hanh_chinh, y_ta_phong_kham, ky_thuat_vien, admin
        public string? Email { get; set; }
        public string? DienThoai { get; set; }
        public string TrangThaiCongTac { get; set; } = "dang_cong_tac"; // dang_cong_tac,tam_nghi,nghi_viec
        public string? MoTa { get; set; }
        public string MaKhoa { get; set; } = default!;

        public KhoaChuyenMon KhoaChuyenMon { get; set; } = default!;
        public Phong? PhongsPhuTrach { get; set; }
        public ICollection<LichTruc> LichTrucsYTa { get; set; } = new List<LichTruc>();
        public ICollection<ThongBaoNguoiNhan> ThongBaoNguoiNhans { get; set; } = new List<ThongBaoNguoiNhan>();
        public ICollection<KetQuaDichVu> KetQuaDichVus { get; set; } = new List<KetQuaDichVu>();
        public ICollection<PhieuKhamLamSang> PhieuKhamLamSangLap { get; set; } = new List<PhieuKhamLamSang>();
        public ICollection<PhieuKhamLamSang> PhieuKhamLamSangKham { get; set; } = new List<PhieuKhamLamSang>();
        public ICollection<LuotKhamBenh> LuotKhamThucHien { get; set; } = new List<LuotKhamBenh>();
        public ICollection<LuotKhamBenh> LuotKhamYTaHoTro { get; set; } = new List<LuotKhamBenh>();

        public ICollection<HoaDonThanhToan> HoaDonThu { get; set; } = new List<HoaDonThanhToan>();
        public ICollection<PhieuTongHopKetQua> PhieuTongHopXuLy { get; set; } = new List<PhieuTongHopKetQua>();

        public ICollection<DonThuoc> DonThuocKe { get; set; } = new List<DonThuoc>();

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}

 