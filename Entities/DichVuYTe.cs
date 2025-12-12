using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HealthCare.Entities
{
    [Table("dich_vu_y_te")]
    public class DichVuYTe
    {
        [Key]
        public string MaDichVu { get; set; } = default!;
        public string LoaiDichVu { get; set; } = default!;  // kham_lam_sang,can_lam_sang
        public string TenDichVu { get; set; } = default!;
        public decimal DonGia { get; set; }
        public int ThoiGianDuKienPhut { get; set; }
        public string MaPhongThucHien { get; set; } = default!;

        public Phong PhongThucHien { get; set; } = default!;
        public ICollection<PhieuKhamLamSang> PhieuKhamLamSangs { get; set; } = new List<PhieuKhamLamSang>();
        public ICollection<ChiTietDichVu> ChiTietDichVus { get; set; } = new List<ChiTietDichVu>();
    }
}

