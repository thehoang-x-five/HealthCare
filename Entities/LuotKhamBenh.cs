using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCare.Entities
{
    [Table("luot_kham_benh")]
    public class LuotKhamBenh
    {
        [Key]
        public string MaLuotKham { get; set; } = default!;
        public string MaHangDoi { get; set; } = default!;
        public string? MaNhanSuThucHien { get; set; }
        public string? MaYTaHoTro { get; set; }
        public string LoaiLuot { get; set; } = "kham_lam_sang"; // kham_lam_sang,can_lam_sang
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
        public string TrangThai { get; set; } = "dang_thuc_hien"; // dang_thuc_hien,hoan_tat

        public HangDoi HangDoi { get; set; } = default!;

        public NhanVienYTe? NhanSuThucHien { get; set; }
        public NhanVienYTe YTaHoTro { get; set; } = default!;

        public ICollection<ThongBaoHeThong> ThongBaoHeThongs { get; set; } = new List<ThongBaoHeThong>();
    }
}

