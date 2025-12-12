using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCare.Entities
{
    [Table("lich_truc")]
    public class LichTruc
    {
        [Key]
        public string MaLichTruc { get; set; } = default!;
        public bool NghiTruc { get; set; } = false;
        public DateTime Ngay { get; set; } 
        public string CaTruc { get; set; } = default!; 
        public TimeSpan  GioBatDau { get; set; }
        public TimeSpan GioKetThuc { get; set; }
        public string MaYTaTruc { get; set; } = default!;
        public string MaPhong { get; set; } = default!;
        public NhanVienYTe YTaTruc { get; set; } = default!;
        public Phong Phong { get; set; } = default!;

        public ICollection<LichHenKham> LichHenKhams { get; set; } = new List<LichHenKham>(); 
    }
}


   