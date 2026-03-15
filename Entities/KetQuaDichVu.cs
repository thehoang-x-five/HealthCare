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
        
        // NEW: Result type enum (Week 1 - Task 3.1)
        public string LoaiKetQua { get; set; } = "xet_nghiem"; // xet_nghiem, chan_doan_hinh_anh
        
        // REMOVED: NoiDungKetQua (migrated to MongoDB in Week 2)
        // public string NoiDungKetQua { get; set; } = "";
        
        // NEW: Specialist conclusion (Week 1 - Task 3.1)
        public string? KetLuanChuyen { get; set; }
        
        // NEW: Additional notes (Week 1 - Task 3.1)
        public string? GhiChu { get; set; }
        
        // NEW: Attached files as JSON array (Week 1 - Task 3.2)
        [Column(TypeName = "json")]
        public string? TepDinhKem { get; set; } // ["file1.pdf", "file2.jpg"]
        
        // NEW: Finalization timestamp (Week 1 - Task 3.3)
        public DateTime? ThoiGianChot { get; set; }
        
        public string TrangThaiChot { get; set; } = "hoan_tat";
        public string MaNguoiTao { get; set; } = default!;
        public DateTime ThoiGianTao { get; set; }

        public ChiTietDichVu ChiTietDichVu { get; set; } = default!;
        public NhanVienYTe NhanVienYTes { get; set; } = default!;
    }
}
