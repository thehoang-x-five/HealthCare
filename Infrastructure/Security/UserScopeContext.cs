using System.Security.Claims;

namespace HealthCare.Infrastructure.Security
{
    public sealed class UserScopeContext
    {
        public UserScopeContext(string vaiTro, string loaiYTa, string? maNhanSu, string? maKhoa)
        {
            VaiTroRaw = vaiTro ?? string.Empty;
            LoaiYTaRaw = loaiYTa ?? string.Empty;
            MaNhanSu = string.IsNullOrWhiteSpace(maNhanSu) ? null : maNhanSu.Trim();
            MaKhoa = string.IsNullOrWhiteSpace(maKhoa) ? null : maKhoa.Trim();
        }

        public string VaiTroRaw { get; }
        public string LoaiYTaRaw { get; }
        public string? MaNhanSu { get; }
        public string? MaKhoa { get; }

        public string VaiTro => Normalize(VaiTroRaw);
        public string LoaiYTa => Normalize(LoaiYTaRaw);

        public bool IsManagement => VaiTro is "admin" or "quantrivien";
        public bool IsReceptionNurse => VaiTro == "yta" && LoaiYTa is "hanhchinh" or "hc";
        public bool IsGlobal => IsManagement || IsReceptionNurse;

        public bool IsDoctor => VaiTro == "bacsi";
        public bool IsClinicalNurse => VaiTro == "yta" && LoaiYTa is "ls" or "lamsang" or "phongkham";
        public bool IsClsNurse => VaiTro == "yta" && LoaiYTa is "cls" or "canlamsang";
        public bool IsTechnician => VaiTro == "kythuatvien";

        public bool IsClinicalCareRole => IsDoctor || IsClinicalNurse;
        public bool IsClsRole => IsTechnician || IsClsNurse;

        public bool HasDepartmentScope => !IsGlobal && !string.IsNullOrWhiteSpace(MaKhoa);
        public string? DepartmentScope => HasDepartmentScope ? MaKhoa : null;

        private static string Normalize(string? value)
        {
            return (value ?? string.Empty)
                .Trim()
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();
        }
    }

    public static class UserScopeExtensions
    {
        public static UserScopeContext GetUserScope(this ClaimsPrincipal user)
        {
            var vaiTro = user.FindFirst("VaiTro")?.Value
                      ?? user.FindFirst(ClaimTypes.Role)?.Value
                      ?? string.Empty;
            var loaiYTa = user.FindFirst("loai_y_ta")?.Value ?? string.Empty;
            var maNhanSu = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? user.FindFirst("sub")?.Value;
            var maKhoa = user.FindFirst("ma_khoa")?.Value;

            return new UserScopeContext(vaiTro, loaiYTa, maNhanSu, maKhoa);
        }
    }
}
