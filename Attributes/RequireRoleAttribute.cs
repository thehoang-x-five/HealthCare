using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace HealthCare.Attributes
{
    /// <summary>
    /// Attribute để kiểm tra quyền truy cập dựa trên ChucVu của nhân viên
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
            // Lấy ChucVu từ JWT claims
            var chucVuClaim = context.HttpContext.User.FindFirst("ChucVu")?.Value;

            if (string.IsNullOrEmpty(chucVuClaim))
            {
                // Không có claim ChucVu -> Unauthorized
                context.Result = new UnauthorizedObjectResult(new
                {
                    message = "Không tìm thấy thông tin chức vụ trong token"
                });
                return;
            }

            // Kiểm tra xem ChucVu có trong danh sách được phép không
            if (!_allowedRoles.Contains(chucVuClaim))
            {
                // Không có quyền -> Forbidden
                context.Result = new ObjectResult(new
                {
                    message = $"Bạn không có quyền truy cập. Yêu cầu chức vụ: {string.Join(", ", _allowedRoles)}"
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
