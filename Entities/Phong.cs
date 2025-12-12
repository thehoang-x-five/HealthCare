using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCare.Entities
{
    [Table("phong")]
    public class Phong
    {
        [Key]
        public string MaPhong { get; set; } = default!;
        public string TenPhong { get; set; } = default!;
        public string MaKhoa { get; set; } = default!;
        public string LoaiPhong { get; set; } = default!; // phong_kham,phong_dich_vu
        public int? SucChua { get; set; }
        public string? ViTri { get; set; }
        public string? Email { get; set; }
        public TimeSpan? GioMoCua { get; set; }
        public TimeSpan? GioDongCua { get; set; }
        public List<string> ThietBi { get; set; } = [];
        public string? DienThoai { get; set; }
        public string TrangThai { get; set; } = "hoat_dong"; // hoat_dong,tam_dung
        public string? MaBacSiPhuTrach { get; set; }

        public KhoaChuyenMon KhoaChuyenMon { get; set; } = default!;

        public NhanVienYTe? BacSiPhuTrach { get; set; }

        public ICollection<LichTruc> LichTrucs { get; set; } = new List<LichTruc>();
        public ICollection<DichVuYTe> DichVuYTes { get; set; } = new List<DichVuYTe>();
        public ICollection<HangDoi> HangDois { get; set; } = new List<HangDoi>();
    }
}
