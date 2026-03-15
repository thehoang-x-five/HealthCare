using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCare.Entities
{
    [Table("thong_bao_mau")]
    public class ThongBaoMau
    {
        [Key]
        public string MaMau { get; set; } = default!;
        
        public string TenMau { get; set; } = default!;
        public string NoiDungMau { get; set; } = default!;
        
        [Column(TypeName = "json")]
        public string? BienDong { get; set; } // JSON array of dynamic placeholders: ["ten_benh_nhan", "gio_hen", ...]
        
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public bool KichHoat { get; set; } = true;
    }
}
