using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HealthCare.Entities
{
    [Table("phieu_chan_doan_cuoi")]
    public class PhieuChanDoanCuoi
    {
        [Key]
        public string MaPhieuChanDoan { get; set; } = default!;
        public string MaPhieuKham { get; set; } = default!;
        public string? MaDonThuoc { get; set; }
        public string? ChanDoanSoBo { get; set; }
        public string? ChanDoanCuoi { get; set; }
        
        // NEW: ICD-10 code (Week 1 - Task 4)
        public string? MaICD10 { get; set; }
        
        public string? NoiDungKham { get; set; }
        public string? HuongXuTri { get; set; }
        public string? LoiKhuyen { get; set; }
        public string? PhatDoDieuTri { get; set; }
        
        // NEW: Follow-up appointment (Week 1 - Task 4)
        public DateTime? NgayTaiKham { get; set; }
        public string? GhiChuTaiKham { get; set; }
        
        // NEW: Audit timestamps (Week 1 - Task 4)
        public DateTime ThoiGianTao { get; set; } = DateTime.Now;
        public DateTime ThoiGianCapNhat { get; set; } = DateTime.Now;

        public PhieuKhamLamSang PhieuKhamLamSang { get; set; } = default!;
        public DonThuoc? DonThuoc { get; set; }
    }
}

