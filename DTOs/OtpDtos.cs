using System;

namespace HealthCare.DTOs
{
    // ==== Cấu hình SMTP (gắn vào appsettings: "Smtp") ====
    public class SmtpOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string DisplayName { get; set; } = "HealthCare HIS";
    }

    // ==== Cấu hình OTP (gắn vào appsettings: "Otp") ====
    public class OtpOptions
    {
        public int CodeLength { get; set; } = 6;
        public int ExpireMinutes { get; set; } = 5;
        public int ResendCooldownSeconds { get; set; } = 60;
        public int MaxVerifyAttempts { get; set; } = 5;
    }

    // ==== DTO request/verify OTP ====
    public record class OtpRequestDto
    {
        public string Email { get; set; } = default!;
        /// <summary>Id ngữ cảnh OTP. FE tự sinh (vd: GUID) và dùng lại cho verify / change / forgot.</summary>
        public string IntentId { get; set; } = default!;
    }

    public record class OtpVerifyDto
    {
        public string IntentId { get; set; } = default!;
        public string Code { get; set; } = default!;
    }

    public record class OtpVerifyResultDto
    {
        public bool Success { get; set; }
        public int? AttemptsLeft { get; set; }
        public bool LockedOrExpired { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ==== Entry lưu trong cache ====
    public class OtpEntry
    {
        public string Code { get; set; } = default!;
        public DateTimeOffset ExpiresAt { get; set; }

        public string Email { get; set; } = default!;
    }

    public class CooldownEntry
    {
        public DateTimeOffset Until { get; set; }
    }
}
