using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCare.Entities
{
    [Table("thong_bao_nguoi_nhan")]
    public class ThongBaoNguoiNhan
    {
        [Key]
        public long MaTbNguoiNhan { get; set; }
        public string MaThongBao { get; set; } = default!;
        public string LoaiNguoiNhan { get; set; } = default!; // benh_nhan,nhan_vien_y_te
        public string? MaBenhNhan { get; set; } 
        public string? MaNhanSu{ get; set; } 

        public bool DaDoc { get; set; }
        public DateTime? ThoiGianDoc { get; set; }


        public ThongBaoHeThong ThongBaoHeThong { get; set; } = default!;
        public BenhNhan? BenhNhan { get; set; }
        public NhanVienYTe? NhanVienYTe { get; set; }
    }
}
