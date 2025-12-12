
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCare.Entities
{
    [Table("refresh_token")]
    public class RefreshToken
    {
        [Key]
        public string Id { get; set; } = default!;

        public string MaNhanVien { get; set; } = default!;

        public string Token { get; set; } = default!;


        public DateTime ThoiGianTao { get; set; }

        public DateTime ThoiGianHetHan { get; set; }

        /// <summary>
        /// Còn hiệu lực hay không (chưa bị thu hồi, chưa hết hạn).
        /// </summary>

        public bool IsTrangThai { get; set; } = true;


        public string? CreatedIp { get; set; }

     
        public DateTime? ThoiGianThuHoi { get; set; }

        public string? RevokedIp { get; set; }


        public string? ReplacedToken { get; set; }


        public NhanVienYTe NhanVien { get; set; } = default!;
    }
}
