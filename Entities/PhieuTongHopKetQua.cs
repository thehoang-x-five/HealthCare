using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HealthCare.Entities
{
    [Table("phieu_tong_hop_ket_qua")]
    public class PhieuTongHopKetQua
    {
        [Key]
        public string MaPhieuTongHop { get; set; } = default!;
        public string MaPhieuKhamCls { get; set; } = default!;
        public string LoaiPhieu { get; set; } = "tong_hop_cls";
        public string MaNhanSuXuLy { get; set; } = default!;
        public string TrangThai { get; set; } = "cho_xu_ly"; // cho_xu_ly,dang_xu_ly,da_hoan_tat
        public DateTime ThoiGianXuLy { get; set; }
        public string SnapshotJson { get; set; } = default!;

        public PhieuKhamLamSang PhieuKhamLamSang { get; set; } = default!;
        public PhieuKhamCanLamSang PhieuKhamCanLamSang { get; set; } = default!;
        public NhanVienYTe NhanSuXuLy { get; set; } = default!;
    }
}
