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
        
        // NEW: Result type enum
        public string LoaiKetQua { get; set; } = "xet_nghiem"; // xet_nghiem, chan_doan_hinh_anh
        
        // DEPRECATED: Will be removed after MongoDB migration
        // Data will move to MongoDB medical_histories collection
        [Obsolete("Sẽ xóa sau khi MongoDB migration hoàn tất")]
        public string NoiDungKetQua { get; set; } = "";
        
        // NEW: Specialist conclusion
        public string? KetLuanChuyen { get; set; }
        
        // NEW: Additional notes
        public string? GhiChu { get; set; }
        
        // NEW: Attached files as JSON array
        [Column(TypeName = "json")]
        public string? TepDinhKem { get; set; } 
        
        // NEW: Finalization timestamp
        public DateTime? ThoiGianChot { get; set; }
        
        public string TrangThaiChot { get; set; } = "hoan_tat";
        public string MaNguoiTao { get; set; } = default!;
        public DateTime ThoiGianTao { get; set; }

        public ChiTietDichVu ChiTietDichVu { get; set; } = default!;
        public NhanVienYTe NhanVienYTes { get; set; } = default!;
    }
}
