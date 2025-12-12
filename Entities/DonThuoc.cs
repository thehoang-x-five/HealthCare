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
        public string TrangThai { get; set; } = "da_ke"; // da_ke,cho_phat,da_phat
        public decimal TongTienDon { get; set; }
        public NhanVienYTe BacSiKeDon { get; set; } = default!;
        public BenhNhan BenhNhan { get; set; } = default!;

        public HoaDonThanhToan HoaDonThanhToans { get; set; } = default!;

        public ICollection<ChiTietDonThuoc> ChiTietDonThuocs { get; set; } = new List<ChiTietDonThuoc>();
        public PhieuChanDoanCuoi PhieuChanDoanCuoi { get; set; } = default!;

    }
}
