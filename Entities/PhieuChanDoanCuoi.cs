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
        public string? NoiDungKham { get; set; }
        public string? HuongXuTri { get; set; }
        public string? LoiKhuyen { get; set; }
        public string? PhatDoDieuTri { get; set; }

        public PhieuKhamLamSang PhieuKhamLamSang { get; set; } = default!;
        public DonThuoc? DonThuoc { get; set; }
    }
}

