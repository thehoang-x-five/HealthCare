using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCare.Entities
{
    [Table("phieu_kham_lam_sang")]
    public class PhieuKhamLamSang
    {
        [Key]
        public string MaPhieuKham { get; set; } = default!;
        public string MaBacSiKham { get; set; } = default!;
        public string MaNguoiLap { get; set; } = default!;
        public string MaBenhNhan { get; set; } = default!;
        public string? MaLichHen { get; set; }
        public string MaDichVuKham { get; set; } = default!;

        public string? MaPhieuKqKhamCls { get; set; }

        public string HinhThucTiepNhan { get; set; } = default!; // walkin,appointment,service_return
        public DateTime NgayLap { get; set; }
        public TimeSpan GioLap { get; set; }
        public string? TrieuChung { get; set; }
        public string TrangThai { get; set; } = "da_lap"; // da_lap,dang_thuc_hien,da_lap_chan_doan,da_hoan_tat,da_huy


        public BenhNhan BenhNhan { get; set; } = default!;
        public NhanVienYTe BacSiKham { get; set; } = default!;
        public NhanVienYTe NguoiLap { get; set; } = default!;
        public LichHenKham? LichHenKham { get; set; }
        public DichVuYTe DichVuKham { get; set; } = default!;
        public PhieuKhamCanLamSang? PhieuKhamCanLamSang { get; set; }
        public PhieuTongHopKetQua? PhieuTongHopKetQua { get; set; }

        public PhieuChanDoanCuoi PhieuChanDoanCuoi { get; set; } = default!;

        public HangDoi HangDois { get; set; } = default!;
        
        public HoaDonThanhToan? HoaDonThanhToans { get; set; }
        public ICollection<ThongBaoHeThong> ThongBaoHeThongs { get; set; } = new List<ThongBaoHeThong>();
    }
}
