using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCare.Entities
{
    [Table("hang_doi")]
    public class HangDoi
    {
        [Key]
        public string MaHangDoi { get; set; } = default!;
        public string MaBenhNhan { get; set; } = default!;
        public string MaPhong { get; set; } = default!;
        public string LoaiHangDoi { get; set; } = default!; // kham_lam_sang,can_lam_sang
        public string? Nguon { get; set; } // appointment,walkin,service_return
        public string? Nhan { get; set; } //  nullable
        public bool CapCuu { get; set; }
        public string? PhanLoaiDen { get; set; } // dung_gio,den_som,den_muon
        public DateTime ThoiGianCheckin { get; set; }
        public DateTime? ThoiGianLichHen { get; set; }
        public int DoUuTien { get; set; }
        public string TrangThai { get; set; } = "cho_goi"; // cho_goi,dang_goi,da_phuc_vu
        public string? GhiChu { get; set; }
        public string? MaPhieuKham { get; set; }
        public string? MaChiTietDv { get; set; }

        public BenhNhan BenhNhan { get; set; } = default!;
        public Phong Phong { get; set; } = default!;

        public PhieuKhamLamSang? PhieuKhamLamSang { get; set; }
        public ChiTietDichVu? ChiTietDichVu { get; set; }

        public LuotKhamBenh LuotKhamBenh { get; set; } = default!;
    }
}

