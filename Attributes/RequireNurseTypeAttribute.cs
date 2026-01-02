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

        // Mapping giữa tên mới và tên cũ trong database
        private static readonly Dictionary<string, string[]> NurseTypeAliases = new()
        {
            { "hanhchinh", new[] { "hanhchinh", "hanh_chinh", "y_ta_hanh_chinh" } },
            { "phong_kham", new[] { "phong_kham", "ls", "lam_sang", "y_ta_lam_sang" } },
            { "can_lam_sang", new[] { "can_lam_sang", "cls", "y_ta_can_lam_sang" } }
        };

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

            // ✅ Kiểm tra xem LoaiYTa có khớp với bất kỳ alias nào không (case-insensitive)
            bool isAllowed = false;
            foreach (var allowedType in _allowedNurseTypes)
            {
                if (NurseTypeAliases.TryGetValue(allowedType, out var aliases))
                {
                    // Kiểm tra với tất cả các alias (case-insensitive)
                    if (aliases.Any(alias => string.Equals(alias, loaiYTa, StringComparison.OrdinalIgnoreCase)))
                    {
                        isAllowed = true;
                        break;
                    }
                }
                else
                {
                    // Fallback: so sánh trực tiếp (case-insensitive)
                    if (string.Equals(allowedType, loaiYTa, StringComparison.OrdinalIgnoreCase))
                    {
                        isAllowed = true;
                        break;
                    }
                }
            }

            if (!isAllowed)
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
