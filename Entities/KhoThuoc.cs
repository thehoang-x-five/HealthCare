using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HealthCare.Entities
{
    [Table("kho_thuoc")]
    public class KhoThuoc
    {
        [Key]
        public string MaThuoc { get; set; } = default!;
        public string TenThuoc { get; set; } = default!;
        public string DonViTinh { get; set; } = default!;
        public string? CongDung { get; set; }
        public decimal GiaNiemYet { get; set; }
        public int SoLuongTon { get; set; }
        public DateTime HanSuDung { get; set; }
        public string? SoLo { get; set; } 
        public string TrangThai { get; set; } = "hoat_dong"; // hoat_dong,tam_dung,sap_het_han,sap_het_ton

        public ICollection<ChiTietDonThuoc> ChiTietDonThuocs { get; set; } = new List<ChiTietDonThuoc>();
    }
}
