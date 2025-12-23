using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.DTOs;

namespace HealthCare.Services.UserInteraction
{
    /// <summary>
    /// UserInteraction Service Module - Quản lý auth, users, notifications, realtime
    /// Tables: Users, Roles, Notifications, OTP
    /// </summary>
    
    public interface IAuthService
    {
        Task<AuthTokenResponse> LoginAsync(AuthLoginRequest request, string? ipAddress);
        Task<AuthTokenResponse> RefreshAsync(string refreshToken, string? ipAddress, string? currentUserId = null);
        Task LogoutAsync(string userId, string? refreshToken, string? ipAddress);
        Task ForgotPasswordAsync(AuthForgotPasswordRequest request);
        Task ChangePasswordAsync(string userId, AuthChangePasswordRequest request, string? ipAddress = null);
    }

    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default);
    }

    public interface INotificationService
    {
        Task<NotificationDto> TaoThongBaoAsync(NotificationCreateRequest request);
        Task<PagedResult<NotificationDto>> LayThongBaoNguoiNhanAsync(NotificationFilterRequest filter);
        Task<NotificationDto?> DanhDauDaDocAsync(long maTbNguoiNhan);
        Task<PagedResult<NotificationDto>> TimKiemThongBaoAsync(NotificationSearchFilter filter);
        Task<NotificationDto?> CapNhatTrangThaiThongBaoAsync(
            string maThongBao,
            NotificationStatusUpdateRequest request);
    }

    // TODO: Thêm IRealtimeService interface sau khi implement SignalR
}
