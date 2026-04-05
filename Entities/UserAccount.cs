using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCare.Entities
{
    [Table("user_accounts")]
    public class UserAccount
    {
        [Key]
        public string MaUser { get; set; } = default!;

        [Required]
        [MaxLength(50)]
        public string TenDangNhap { get; set; } = default!;

        [Required]
        public string MatKhauHash { get; set; } = default!;

        [Required]
        [MaxLength(50)]
        public string VaiTro { get; set; } = default!; // admin, bac_si, y_ta, ky_thuat_vien

        [MaxLength(50)]
        public string? LoaiYTa { get; set; } // hanhchinh, ls, cls (required if VaiTro = y_ta)

        [Required]
        [MaxLength(50)]
        public string TrangThaiTaiKhoan { get; set; } = "hoat_dong"; // hoat_dong, khoa, tam_ngung

        public DateTime? LanDangNhapCuoi { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime NgayCapNhat { get; set; } = DateTime.Now;

        // Foreign key to staff record (optional for future non-staff users)
        [ForeignKey(nameof(NhanVienYTe))]
        public string? MaNhanVien { get; set; }

        // Navigation properties
        public NhanVienYTe? NhanVienYTe { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
