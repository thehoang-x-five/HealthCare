using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCare.Entities
{
    [Table("thong_bao_he_thong")]
    public class ThongBaoHeThong
    {
        [Key]
        public string MaThongBao { get; set; } = default!;
        public string TieuDe { get; set; } = default!;
        public string NoiDung { get; set; } = default!;
        public string LoaiThongBao { get; set; } = default!;
        public string DoUuTien { get; set; } = default!;
        public DateTime ThoiGianGui { get; set; }
        public string? MaLuotKham { get; set; }
        public string? MaPhieuKham { get; set; }
        public string TrangThai { get; set; } = "cho_gui"; // cho_gui,da_gui,da_doc

        public ICollection<ThongBaoNguoiNhan> ThongBaoNguoiNhans { get; set; } = new List<ThongBaoNguoiNhan>();

        public LuotKhamBenh? LuotKhamBenh { get; set; }
        public PhieuKhamLamSang? PhieuKhamLamSang { get; set; }
    }
}

