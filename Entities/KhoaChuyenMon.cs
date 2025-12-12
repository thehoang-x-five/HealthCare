using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HealthCare.Entities
{
    [Table("khoa_chuyen_mon")]
    public class KhoaChuyenMon
    {
        [Key]
        public string MaKhoa { get; set; } = default!;
        public string TenKhoa { get; set; } = default!;
        public string? MoTa { get; set; }
        public string? DienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaDiem { get; set; }
        public string TrangThai { get; set; } = "hoat_dong"; // hoat_dong,tam_dung
        public string? GhiChu { get; set; }
        public ICollection<Phong> Phongs { get; set; } = new List<Phong>();
        public ICollection<NhanVienYTe> NhanVienYTes { get; set; } = new List<NhanVienYTe>();
    }
}
