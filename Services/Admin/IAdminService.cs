using HealthCare.DTOs;

namespace HealthCare.Services.Admin
{
    public interface IAdminService
    {
        Task<PagedResult<AdminUserDto>> GetUsersAsync(AdminUserFilter filter);
        Task<AdminUserDto> GetUserByIdAsync(string maNhanVien);
        Task<AdminUserDto> CreateUserAsync(AdminUserCreateRequest request);
        Task<AdminUserDto> UpdateUserAsync(string maNhanVien, AdminUserUpdateRequest request);
        Task UpdateStatusAsync(string maNhanVien, AdminStatusUpdateRequest request);
        Task LockUnlockAsync(string maNhanVien, AdminAccountStatusRequest request);
        Task ResetPasswordAsync(string maNhanVien, AdminResetPasswordRequest request);
    }
}
