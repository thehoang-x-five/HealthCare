using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using HealthCare.DTOs;
using HealthCare.Services.UserInteraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HealthCare.Controllers
{
    [ApiController]
    [Route("api/otp")]
    public class OtpController(
        IEmailService email,
        IMemoryCache cache,
        IOptions<OtpOptions> opt) : ControllerBase
    {
        private readonly IEmailService _email = email;
        private readonly IMemoryCache _cache = cache;
        private readonly OtpOptions _opt = opt.Value;

        private static readonly ConcurrentDictionary<string, object> _locks = new();


        // ============= REQUEST OTP =============

        [HttpPost("request")]
        public async Task<IActionResult> RequestOtp(
                        [FromBody] OtpRequestDto dto,
                        CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "Thiếu email." });

            if (string.IsNullOrWhiteSpace(dto.IntentId))
                return BadRequest(new { message = "Thiếu intentId." });

            // NEW: chuẩn hoá email + key cooldown theo email
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            var cooldownEmailKey = $"otp:cooldown:email:{normalizedEmail}";

            var codeKey = $"otp:code:intent:{dto.IntentId}";
            var attemptsKey = $"otp:attempts:intent:{dto.IntentId}";
            var cooldownKey = $"otp:cooldown:intent:{dto.IntentId}";
            var verifiedKey = $"otp:verified:intent:{dto.IntentId}";

            var now = DateTimeOffset.UtcNow;

           

            // Nếu đã có OTP còn hiệu lực cho intent này?
            if (_cache.TryGetValue(codeKey, out OtpEntry exist) && exist is not null)
            {
                var left = (int)Math.Max(0, (exist.ExpiresAt - now).TotalSeconds);
                if (left > 0)
                {
                    var halfSec = (int)Math.Round(_opt.ExpireMinutes * 60 / 2.0);

                    if (left > halfSec)
                    {
                        // > 1/2 thời gian: không gửi lại
                        return Ok(new
                        {
                            message = "OTP đã được gửi, vui lòng dùng mã đã nhận.",
                            alreadySent = true,
                            expiresLeft = left,
                            cooldownLeft = left - halfSec
                        });
                    }

                    // <= 1/2 thời gian: cho phép gửi mã mới → reset
                    _cache.Remove(codeKey);
                    _cache.Remove(attemptsKey);
                    _cache.Remove(verifiedKey);
                }
                else
                {
                    // Hết hạn
                    _cache.Remove(codeKey);
                    _cache.Remove(attemptsKey);
                    _cache.Remove(verifiedKey);
                }
            }
            // NEW: check cooldown theo email trước
            if (_cache.TryGetValue<CooldownEntry>(cooldownEmailKey, out var emailCooldown) &&
                emailCooldown is not null &&
                emailCooldown.Until > now)
            {
                var left = (int)Math.Max(0, (emailCooldown.Until - now).TotalSeconds);
                return BadRequest(new
                {
                    message = $"Bạn yêu cầu OTP quá nhiều lần. Vui lòng thử lại sau {left}s.",
                    cooldownLeft = left
                });
            }
            // Kiểm tra cooldown theo intent (giữ nguyên logic cũ)
            if (_cache.TryGetValue(cooldownKey, out CooldownEntry cd) && cd is not null)
            {
                var leftCd = (int)Math.Max(0, (cd.Until - now).TotalSeconds);
                if (leftCd > 0)
                    return BadRequest(new
                    {
                        message = $"Vui lòng thử lại sau {leftCd}s.",
                        cooldownLeft = leftCd
                    });

                _cache.Remove(cooldownKey);
            }

            // Sinh mã mới
            var code = GenerateOtp(_opt.CodeLength);
            var expiresAt = now.AddMinutes(_opt.ExpireMinutes);

            _cache.Set(
                codeKey,
                new OtpEntry
                {
                    Code = code,
                    ExpiresAt = expiresAt,
                    // nếu bạn đã thêm Email vào OtpEntry thì set luôn:
                    Email = normalizedEmail
                },
                expiresAt - now);

            _cache.Remove(attemptsKey);
            _cache.Remove(verifiedKey);

            // Tạo cooldown mới
            var cdNewUntil = now.AddSeconds(_opt.ResendCooldownSeconds);

            // cooldown theo intent (cũ)
            _cache.Set(
                cooldownKey,
                new CooldownEntry { Until = cdNewUntil },
                cdNewUntil - now);

            // NEW: cooldown theo email (ngăn spam tạo IntentId mới)
            var emailCdEntry = new CooldownEntry { Until = cdNewUntil };
            _cache.Set(
                cooldownEmailKey,
                emailCdEntry,
                cdNewUntil - now);

            var subject = "Mã OTP xác thực tài khoản HealthCare";
            var html = $@"
                        <div style='font-family:Segoe UI,Roboto,Arial,sans-serif;font-size:14px'>
                          <p>Xin chào,</p>
                          <p>Mã OTP xác thực của bạn là:</p>
                          <div style='font-size:22px;font-weight:700;letter-spacing:4px'>{code}</div>
                          <p>Mã có hiệu lực trong <b>{_opt.ExpireMinutes} phút</b>. Vui lòng không chia sẻ mã cho bất kỳ ai.</p>
                          <p>— HealthCare HIS</p>
                        </div>";

            await _email.SendEmailAsync(dto.Email, subject, html, ct);

            return Ok(new
            {
                message = "Đã gửi OTP.",
                alreadySent = false,
                expiresLeft = (int)Math.Max(0, (expiresAt - now).TotalSeconds),
                cooldownLeft = (int)Math.Max(0, (cdNewUntil - now).TotalSeconds)
            });
        }

       

        // ============= VERIFY OTP =============
        [HttpPost("verify")]
        public IActionResult VerifyOtp([FromBody] OtpVerifyDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.IntentId) ||
                string.IsNullOrWhiteSpace(dto.Code))
            {
                return BadRequest(new OtpVerifyResultDto
                {
                    Success = false,
                    LockedOrExpired = true,
                    Message = "Thiếu tham số."
                });
            }

            var codeKey = $"otp:code:intent:{dto.IntentId}";
            var attemptsKey = $"otp:attempts:intent:{dto.IntentId}";
            var lockKey = $"otp:lock:intent:{dto.IntentId}";
            var verifiedKey = $"otp:verified:intent:{dto.IntentId}";

            if (!_cache.TryGetValue(codeKey, out OtpEntry entry) || entry is null)
            {
                return Unauthorized(new OtpVerifyResultDto
                {
                    Success = false,
                    LockedOrExpired = true,
                    Message = "OTP hết hạn hoặc chưa được yêu cầu."
                });
            }

            var now = DateTimeOffset.UtcNow;
            if (now >= entry.ExpiresAt)
            {
                _cache.Remove(codeKey);
                _cache.Remove(attemptsKey);
                _cache.Remove(verifiedKey);

                return Unauthorized(new OtpVerifyResultDto
                {
                    Success = false,
                    LockedOrExpired = true,
                    Message = "OTP đã hết hạn, vui lòng yêu cầu mã mới."
                });
            }

            var locker = _locks.GetOrAdd(lockKey, _ => new object());

            lock (locker)
            {
                var attempts = _cache.GetOrCreate<int>(attemptsKey, _ => 0);

                if (!string.Equals(dto.Code, entry.Code, StringComparison.Ordinal))
                {
                    attempts++;
                    if (attempts >= _opt.MaxVerifyAttempts)
                    {
                        _cache.Remove(codeKey);
                        _cache.Remove(attemptsKey);
                        _cache.Remove(verifiedKey);

                        return Unauthorized(new OtpVerifyResultDto
                        {
                            Success = false,
                            AttemptsLeft = 0,
                            LockedOrExpired = true,
                            Message = "Sai quá số lần cho phép. Vui lòng yêu cầu mã mới."
                        });
                    }

                    var ttl = entry.ExpiresAt - now;
                    _cache.Set(
                        attemptsKey,
                        attempts,
                        ttl > TimeSpan.Zero ? ttl : TimeSpan.FromMinutes(1));

                    return Unauthorized(new OtpVerifyResultDto
                    {
                        Success = false,
                        AttemptsLeft = _opt.MaxVerifyAttempts - attempts,
                        LockedOrExpired = false,
                        Message = $"OTP không đúng. Còn {_opt.MaxVerifyAttempts - attempts} lần thử."
                    });
                }

                // Đúng → consume code, set cờ verified cho BE Auth sử dụng
                _cache.Remove(codeKey);
                _cache.Remove(attemptsKey);

                var ttlVerified = entry.ExpiresAt - now;
                if (ttlVerified <= TimeSpan.Zero)
                    ttlVerified = TimeSpan.FromMinutes(_opt.ExpireMinutes);


                // entry là OtpEntry vừa lấy ra từ codeKey
                var email = entry.Email?.Trim().ToLowerInvariant() ?? string.Empty;
                if (string.IsNullOrEmpty(email))
                {
                    // fallback: không chấp nhận nếu không có email
                    return Unauthorized(new OtpVerifyResultDto
                    {
                        Success = false,
                        LockedOrExpired = true,
                        Message = "OTP không hợp lệ."
                    });
                }

                _cache.Set(verifiedKey, email, ttlVerified);

                return Ok(new OtpVerifyResultDto
                {
                    Success = true,
                    AttemptsLeft = null,
                    LockedOrExpired = false,
                    Message = "OTP hợp lệ."
                });

            }
        }

        private static string GenerateOtp(int length)
        {
            Span<byte> buf = stackalloc byte[length];
            RandomNumberGenerator.Fill(buf);
            var chars = new char[length];
            for (var i = 0; i < length; i++)
            {
                chars[i] = (char)('0' + (buf[i] % 10));
            }

            return new string(chars);
        }
    }
}
