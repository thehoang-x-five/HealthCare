using HealthCare.DTOs;

namespace HealthCare.Services.Banking
{
    /// <summary>
    /// Service tạo mã QR thanh toán chuẩn VietQR (Napas 247).
    /// Cấu hình bank info trong appsettings.json → VietQR section.
    /// </summary>
    public class VietQRService
    {
        private readonly string _bankId;
        private readonly string _accountNo;
        private readonly string _accountName;
        private readonly string _bankName;

        public VietQRService(IConfiguration config)
        {
            var section = config.GetSection("VietQR");
            _bankId = section["BankId"] ?? "970422"; // MB Bank default
            _accountNo = section["AccountNo"] ?? "0000000000";
            _accountName = section["AccountName"] ?? "PHONG KHAM DA KHOA";
            _bankName = section["BankName"] ?? "MB Bank";
        }

        /// <summary>
        /// Tạo QR code URL theo chuẩn VietQR.io API.
        /// URL trả về là image link — FE hiển thị trực tiếp bằng &lt;img src&gt;.
        /// </summary>
        public VietQRResponse GenerateQR(string maHoaDon, decimal soTien, string? noiDung = null)
        {
            var memo = noiDung ?? $"TT HD {maHoaDon}";
            var amount = (long)soTien;

            // VietQR image URL — chuẩn VietQR.io API (public, free)
            var qrUrl = $"https://img.vietqr.io/image/{_bankId}-{_accountNo}-compact2.png"
                      + $"?amount={amount}"
                      + $"&addInfo={Uri.EscapeDataString(memo)}"
                      + $"&accountName={Uri.EscapeDataString(_accountName)}";

            return new VietQRResponse
            {
                QrDataUrl = qrUrl,
                BankName = _bankName,
                AccountNo = _accountNo,
                AccountName = _accountName,
                SoTien = soTien,
                NoiDung = memo,
            };
        }
    }
}
