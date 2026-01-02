using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace HealthCare.Attributes
{
    /// <summary>
    /// Attribute để kiểm tra quyền truy cập dựa trên VaiTro của nhân viên
    /// VaiTro: y_ta, bac_si, ky_thuat_vien, admin
    /// Admin luôn có quyền truy cập
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public RequireRoleAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Lấy ChucVu và VaiTro từ JWT claims
            var chucVu = context.HttpContext.User.FindFirst("ChucVu")?.Value;
            var vaiTro = context.HttpContext.User.FindFirst("VaiTro")?.Value;

            // ✅ Admin luôn có quyền (check ChucVu trước)
            if (chucVu == "admin")
            {
                return; // Bypass kiểm tra
            }

            if (string.IsNullOrEmpty(vaiTro))
            {
                // Không có claim VaiTro -> Unauthorized
                context.Result = new UnauthorizedObjectResult(new
                {
                    message = "Không tìm thấy thông tin vai trò trong token"
                });
                return;
            }

            // Kiểm tra xem VaiTro có trong danh sách được phép không (case-insensitive)
            if (!_allowedRoles.Any(role => string.Equals(role, vaiTro, StringComparison.OrdinalIgnoreCase)))
            {
                // Không có quyền -> Forbidden
                context.Result = new ObjectResult(new
                {
                    message = $"Bạn không có quyền truy cập. Yêu cầu vai trò: {string.Join(", ", _allowedRoles)} hoặc admin"
                })
                {
                    StatusCode = 403
                };
                return;
            }

            // Có quyền -> cho phép tiếp tục
        }
    }
}
