using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HealthCare.Entities
{
    [Table("ket_qua_dich_vu")]
    public class KetQuaDichVu
    {
        [Key]
        public string MaKetQua { get; set; } = default!;
        public string MaChiTietDv { get; set; } = default!;
        public string TrangThaiChot { get; set; } = "hoan_tat";
        public string NoiDungKetQua { get; set; } = "";
        public string MaNguoiTao { get; set; } = default!;
        public DateTime ThoiGianTao { get; set; }
        public string? TepDinhKem { get; set; }

        public ChiTietDichVu ChiTietDichVu { get; set; } = default!;
        public NhanVienYTe NhanVienYTes { get; set; } = default!;
    }
}
