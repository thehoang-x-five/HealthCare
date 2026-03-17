namespace HealthCare.DTOs
{
    // ===== REQUEST DTOs =====

    public class LinkParentsRequest
    {
        public string? MaCha { get; set; }
        public string? MaMe { get; set; }
    }

    // ===== RESPONSE DTOs =====

    /// <summary>
    /// Một node trong cây pha hệ
    /// </summary>
    public class GenealogyNodeDto
    {
        public string MaBenhNhan { get; set; } = default!;
        public string HoTen { get; set; } = default!;
        public DateTime NgaySinh { get; set; }
        public string GioiTinh { get; set; } = "";
        public string? NhomMau { get; set; }
        public string? BenhManTinh { get; set; }
        public string? MaCha { get; set; }
        public string? MaMe { get; set; }

        /// <summary>
        /// Quan hệ so với BN gốc: "self", "cha", "me", "con", "anh_chi_em", "ong_ba", "chau"
        /// </summary>
        public string QuanHe { get; set; } = "self";

        /// <summary>
        /// Khoảng cách đời so với BN gốc (0 = chính mình, 1 = cha/mẹ/con, 2 = ông bà/cháu...)
        /// </summary>
        public int DoiThu { get; set; }
    }

    /// <summary>
    /// Toàn bộ cây pha hệ của 1 BN
    /// </summary>
    public class GenealogyTreeDto
    {
        public string MaBenhNhanGoc { get; set; } = default!;
        public List<GenealogyNodeDto> Nodes { get; set; } = new();
    }

    /// <summary>
    /// Tiền sử bệnh gia đình — tổng hợp bệnh của các thành viên gia phả
    /// </summary>
    public class FamilyDiseaseDto
    {
        public string MaBenhNhan { get; set; } = default!;
        public string HoTen { get; set; } = default!;
        public string QuanHe { get; set; } = "";
        public string? BenhManTinh { get; set; }
        public string? TieuSuBenh { get; set; }
        public string? DiUng { get; set; }
    }

    public class FamilyDiseaseSummaryDto
    {
        public string MaBenhNhanGoc { get; set; } = default!;
        public List<FamilyDiseaseDto> ThanhVien { get; set; } = new();

        /// <summary>
        /// Thống kê nhóm bệnh phổ biến trong gia đình
        /// </summary>
        public Dictionary<string, int> ThongKeBenhGiaDinh { get; set; } = new();
    }
}
