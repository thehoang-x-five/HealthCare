using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HealthCare.Entities
{
    [Table("lich_hen_kham")]
    public class LichHenKham
    {
        [Key]
        public string MaLichHen { get; set; } = default!;
        public bool CoHieuLuc { get; set; } = true;
        public DateTime NgayHen { get; set; } 
        public TimeSpan GioHen { get; set; }
        public int ThoiLuongPhut { get; set; } = 30;
        public string? MaBenhNhan { get; set; }
        public string? LoaiHen { get; set; } 
        public string TenBenhNhan { get; set; } = default!;
        public string SoDienThoai { get; set; } = default!;
        public string MaLichTruc { get; set; } = default!;
        public string? GhiChu { get; set; }
        public string TrangThai { get; set; } = "dang_cho"; // dang_cho,da_xac_nhan,da_checkin,da_huy

        public BenhNhan? BenhNhan { get; set; }
        public LichTruc LichTruc { get; set; } = default!;

        public PhieuKhamLamSang? PhieuKhamLamSangs { get; set; }
    }
}
