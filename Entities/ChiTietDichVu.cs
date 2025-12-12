using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HealthCare.Entities
{
    [Table("chi_tiet_dich_vu")]
    public class ChiTietDichVu
    {
        [Key]
        public string MaChiTietDv { get; set; } = default!;
        public string MaPhieuKhamCls { get; set; } = default!;
        public string MaDichVu { get; set; } = default!;
        public string TrangThai { get; set; } = "da_lap"; // da_lap,dang_thuc_hien,da_hoan_tat
        public string? GhiChu { get; set; }

        public PhieuKhamCanLamSang PhieuKhamCanLamSang { get; set; } = default!;
        public DichVuYTe DichVuYTe { get; set; } = default!;

        public KetQuaDichVu KetQuaDichVu { get; set; }= default!;

        public HangDoi HangDois { get; set; } = default!;
       

    }
}

