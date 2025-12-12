using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HealthCare.Entities
{
    [Table("phieu_kham_can_lam_sang")]
    public class PhieuKhamCanLamSang
    {
        [Key]
        public string MaPhieuKhamCls { get; set; } = default!;
        public string MaPhieuKhamLs { get; set; } = default!;
        public DateTime NgayGioLap { get; set; }
        public bool AutoPublishEnabled { get; set; } = true;
        public string TrangThai { get; set; } = "da_lap"; // da_lap,dang_thuc_hien,da_hoan_tat
        public string? GhiChu { get; set; }

        public PhieuKhamLamSang PhieuKhamLamSang { get; set; } = default!;

        public ICollection<ChiTietDichVu> ChiTietDichVus { get; set; } = new List<ChiTietDichVu>();
        public PhieuTongHopKetQua PhieuTongHopKetQua { get; set; } = default!;

        public HoaDonThanhToan HoaDonThanhToans { get; set; }= default!;
    }
}
