using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HealthCare.Entities
{
    [Table("chi_tiet_don_thuoc")]
    public class ChiTietDonThuoc
    {
        [Key]
        public string MaChiTietDon { get; set; } = default!;
        public string MaDonThuoc { get; set; } = default!;
        public string MaThuoc { get; set; } = default!;
        public string? ChiDinhSuDung { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien { get; set; }

        public DonThuoc DonThuoc { get; set; } = default!;
        public KhoThuoc KhoThuoc { get; set; } = default!;
    }
}

