using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HealthCare.Entities
{
    [Table("don_thuoc")]
    public class DonThuoc
    {
        [Key]
        public string MaDonThuoc { get; set; } = default!;
        public string MaBacSiKeDon { get; set; } = default!;
        public string MaBenhNhan { get; set; } = default!;
        public DateTime ThoiGianKeDon { get; set; }
        public string TrangThai { get; set; } = "da_ke"; // da_ke, cho_phat, da_phat, da_huy
        public decimal TongTienDon { get; set; }
        
        // NEW: Payment and dispensing timestamps
        public DateTime? ThoiGianThanhToan { get; set; }
        public DateTime? ThoiGianPhat { get; set; }
        public string? MaNhanSuPhat { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;
        public DateTime NgayCapNhat { get; set; } = DateTime.Now;

        public NhanVienYTe BacSiKeDon { get; set; } = default!;
        public BenhNhan BenhNhan { get; set; } = default!;

        public HoaDonThanhToan HoaDonThanhToans { get; set; } = default!;

        public ICollection<ChiTietDonThuoc> ChiTietDonThuocs { get; set; } = new List<ChiTietDonThuoc>();
        public PhieuChanDoanCuoi PhieuChanDoanCuoi { get; set; } = default!;

    }
}
