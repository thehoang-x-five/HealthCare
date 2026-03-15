using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HealthCare.Entities
{
    [Table("chi_tiet_don_thuoc")]
    public class ChiTietDonThuoc
    {
        [Key]
        public string MaChiTietDon { get; set; } = default!;
        public string MaDonThuoc { get; set; } = default!;
        public string MaThuoc { get; set; } = default!;
        public string? ChiDinhSuDung { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien { get; set; }
        
        // NEW: Dosage instructions (Week 1 - Task 7)
        public string? LieuDung { get; set; } // e.g., "1 viên"
        public string? TanSuatDung { get; set; } // e.g., "sáng 1, tối 1"
        public int? SoNgayDung { get; set; } // e.g., 7 days
        public string? GhiChu { get; set; }

        public DonThuoc DonThuoc { get; set; } = default!;
        public KhoThuoc KhoThuoc { get; set; } = default!;
    }
}

