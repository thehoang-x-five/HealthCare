using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCare.Entities
{
    [Table("hoa_don_thanh_toan")]
    public class HoaDonThanhToan
    {
        [Key]
        public string MaHoaDon { get; set; } = default!;
        public string MaBenhNhan { get; set; } = default!;
        public string MaNhanSuThu { get; set; } = default!;
        public string? MaPhieuKhamCls { get; set; }
        public string? MaPhieuKham { get; set; }
        public string? MaDonThuoc { get; set; }
        public string LoaiDotthu { get; set; } = default!; // kham_lam_sang,can_lam_sang,thuoc
        public decimal SoTien { get; set; }
        
        // NEW: Amount to be paid
        public decimal SoTienPhaiTra { get; set; }
        
        // NEW: Transaction ID
        public string? MaGiaoDich { get; set; }
        
        // UPDATED: Expanded payment method enum
        public string PhuongThucThanhToan { get; set; } = "tien_mat"; // tien_mat, the, chuyen_khoan, vietqr
        
        public DateTime ThoiGian { get; set; } = DateTime.Now;
        public string TrangThai { get; set; } = "da_thu"; // chua_thu, da_thu, da_huy
        public string NoiDung { get; set; } = default!;
        
        // NEW: Cancellation tracking
        public DateTime? ThoiGianHuy { get; set; }
        public string? MaNhanSuHuy { get; set; }

        public BenhNhan BenhNhan { get; set; } = default!;
        public NhanVienYTe NhanSuThu { get; set; } = default!;

        public PhieuKhamCanLamSang? PhieuKhamCanLamSang { get; set; }
        public PhieuKhamLamSang? PhieuKhamLamSang { get; set; }
        public DonThuoc? DonThuoc { get; set; }

    }
}

