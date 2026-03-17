
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCare.Entities
{
    [Table("benh_nhan")]
    public class BenhNhan
    {
        [Key]
        public string MaBenhNhan { get; set; } = default!;
        public string HoTen { get; set; } = default!;
        public DateTime NgaySinh { get; set; }
        public string GioiTinh { get; set; } = "";
        public string? DienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChi { get; set; }
        
        // ===== NEW: Genealogy Tracking =====
        [ForeignKey(nameof(Cha))]
        [MaxLength(20)]
        public string? MaCha { get; set; }
        
        [ForeignKey(nameof(Me))]
        [MaxLength(20)]
        public string? MaMe { get; set; }
        
        // NEW: National ID with unique constraint
        [MaxLength(12)]
        public string? CCCD { get; set; }
        
        // NEW: Audit timestamps
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public DateTime NgayCapNhat { get; set; } = DateTime.Now;
        
        // ===== Medical History =====
        public string? DiUng { get; set; }
        public string? ChongChiDinh { get; set; }
        public string? ThuocDangDung { get; set; }
        public string? TieuSuBenh { get; set; }
        public string? TienSuPhauThuat { get; set; }
        public string? NhomMau { get; set; }
        public string? BenhManTinh { get; set; }
        public string? SinhHieu { get; set; }
        
        public string TrangThaiTaiKhoan { get; set; } = "hoat_dong"; // hoat_dong,khong_hoat_dong,da_xoa
        public string? TrangThaiHomNay { get; set; } // enum in DB
        public DateTime NgayTrangThai { get; set; }

        // ===== Navigation Properties =====
        // Genealogy relationships
        public BenhNhan? Cha { get; set; }
        public BenhNhan? Me { get; set; }
        public ICollection<BenhNhan> ConCai { get; set; } = new List<BenhNhan>();
        
        // Existing relationships
        public ICollection<LichHenKham> LichHenKhams { get; set; } = new List<LichHenKham>();
        public ICollection<PhieuKhamLamSang> PhieuKhamLamSangs { get; set; } = new List<PhieuKhamLamSang>();
        public ICollection<HangDoi> HangDois { get; set; } = new List<HangDoi>();
        public ICollection<DonThuoc> DonThuocs { get; set; } = new List<DonThuoc>();
        public ICollection<HoaDonThanhToan> HoaDonThanhToans { get; set; } = new List<HoaDonThanhToan>();
        public ICollection<ThongBaoNguoiNhan> ThongBaoNguoiNhans { get; set; } = new List<ThongBaoNguoiNhan>();
    }
}
