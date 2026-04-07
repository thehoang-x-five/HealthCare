using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace HealthCare.Attributes
{
    /// <summary>
    /// Attribute để kiểm tra quyền truy cập dựa trên VaiTro của nhân viên.
    /// Chỉ những vai trò được khai báo tường minh mới được phép truy cập.
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
            // Lấy VaiTro từ JWT claims
            var vaiTro = context.HttpContext.User.FindFirst("VaiTro")?.Value;

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
                    message = $"Bạn không có quyền truy cập. Yêu cầu vai trò: {string.Join(", ", _allowedRoles)}"
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
