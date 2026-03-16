using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCare.Entities
{
    [Table("lich_su_xuat_kho")]
    public class LichSuXuatKho
    {
        [Key]
        public string MaGiaoDich { get; set; } = default!;
        
        public string MaThuoc { get; set; } = default!;
        public string? MaDonThuoc { get; set; }
        public string MaNhanSuXuat { get; set; } = default!;
        
        public string LoaiGiaoDich { get; set; } = default!; // xuat_ban, hoan_tra, dieu_chinh
        public int SoLuong { get; set; }
        public int SoLuongConLai { get; set; }
        public DateTime ThoiGianXuat { get; set; }
        public string? GhiChu { get; set; }
        
        // Navigation properties
        public KhoThuoc KhoThuoc { get; set; } = default!;
        public DonThuoc? DonThuoc { get; set; }
        public NhanVienYTe NhanSuXuat { get; set; } = default!;
    }
}
