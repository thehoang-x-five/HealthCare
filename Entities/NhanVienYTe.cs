using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HealthCare.Entities
{
    [Table("nhan_vien_y_te")]
    public class NhanVienYTe
    {
        [Key]
        public string MaNhanVien { get; set; } = default!;
        
        // Personnel information (RETAINED)
        public string HoTen { get; set; } = default!;
        public string? AnhDaiDien { get; set; }
        public int SoNamKinhNghiem { get; set; }
        public string? ChuyenMon { get; set; }
        public string? HocVi { get; set; }
        public string? Email { get; set; }
        public string? DienThoai { get; set; }
        public string TrangThaiCongTac { get; set; } = "dang_cong_tac"; // dang_cong_tac,tam_nghi,nghi_viec
        public string? MoTa { get; set; }
        public string MaKhoa { get; set; } = default!;

        // Navigation properties
        public UserAccount? UserAccount { get; set; }
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
    }
}

 