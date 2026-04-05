using HealthCare.DTOs;

namespace HealthCare.Services.Admin
{
    public interface IAdminService
    {
        Task<PagedResult<AdminUserDto>> GetUsersAsync(AdminUserFilter filter);
        Task<AdminUserDto> GetUserByIdAsync(string maUser);
        Task<AdminUserDto> CreateUserAsync(AdminUserCreateRequest request);
        Task<AdminUserDto> UpdateUserAsync(string maUser, AdminUserUpdateRequest request);
        Task LockAccountAsync(string maUser, string adminUserId);
        Task UnlockAccountAsync(string maUser);
        Task<string> ResetPasswordAsync(string maUser);
    }
}
