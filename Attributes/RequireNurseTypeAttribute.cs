using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HealthCare.Attributes
{
    /// <summary>
    /// Attribute để kiểm tra loại Y tá (LoaiYTa) từ JWT claims
    /// Chỉ áp dụng cho user có VaiTro = "y_ta"
    /// Admin và các vai trò khác luôn được phép
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireNurseTypeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedNurseTypes;

        public RequireNurseTypeAttribute(params string[] allowedNurseTypes)
        {
            _allowedNurseTypes = allowedNurseTypes;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Lấy VaiTro và LoaiYTa từ JWT claims
            var chucVu = context.HttpContext.User.FindFirst("ChucVu")?.Value;
            var vaiTro = context.HttpContext.User.FindFirst("VaiTro")?.Value;
            var loaiYTa = context.HttpContext.User.FindFirst("loai_y_ta")?.Value;

            // ✅ Admin luôn có quyền
            if (chucVu == "admin")
            {
                return; // Bypass kiểm tra
            }

            // ✅ Nếu không phải Y tá, bỏ qua kiểm tra (cho phép)
            if (vaiTro != "y_ta")
            {
                return;
            }

            // Nếu là Y tá nhưng không có LoaiYTa -> Unauthorized
            if (string.IsNullOrEmpty(loaiYTa))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    message = "Không tìm thấy thông tin loại Y tá trong token"
                });
                return;
            }

            // Kiểm tra xem LoaiYTa có trong danh sách được phép không
            if (!_allowedNurseTypes.Contains(loaiYTa))
            {
                context.Result = new ObjectResult(new
                {
                    message = $"Bạn không có quyền truy cập. Yêu cầu loại Y tá: {string.Join(", ", _allowedNurseTypes)}"
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
