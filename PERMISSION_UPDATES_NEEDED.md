# Cập nhật phân quyền cần thiết

## ✅ Đã hoàn thành:

### ClinicalController.cs
- ✅ Tạo phiếu khám: `y_ta` (hanhchinh, phong_kham) + `bac_si` + `admin`
- ✅ Cập nhật trạng thái: `y_ta` (hanhchinh, phong_kham) + `bac_si` + `admin`
- ✅ Tạo chẩn đoán: `bac_si` + `admin`
- ✅ Hoàn tất khám: `bac_si` + `admin`

---

## ❌ Cần cập nhật:

### 1. AppointmentsController.cs
**Hiện tại:** Chỉ có `[Authorize]` - tất cả user đã đăng nhập đều có quyền

**Cần sửa:**
```csharp
// Tạo lịch hẹn - CHỈ Y tá hành chính + Admin
[HttpPost]
[Authorize]
[RequireRole("y_ta", "admin")]
[RequireNurseType("hanhchinh")]

// Cập nhật lịch hẹn - CHỈ Y tá hành chính + Admin
[HttpPut("{maLichHen}")]
[Authorize]
[RequireRole("y_ta", "admin")]
[RequireNurseType("hanhchinh")]

// Check-in - CHỈ Y tá hành chính + Admin
[HttpPut("{maLichHen}/status")]
[Authorize]
[RequireRole("y_ta", "admin")]
[RequireNurseType("hanhchinh")]
```

---

### 2. PatientsController.cs
**Hiện tại:** Chỉ có `[Authorize]` - tất cả user đã đăng nhập đều có quyền

**Cần sửa:**
```csharp
// Tạo/cập nhật bệnh nhân - CHỈ Y tá hành chính + Admin
[HttpPost]
[Authorize]
[RequireRole("y_ta", "admin")]
[RequireNurseType("hanhchinh")]

// Cập nhật trạng thái - CHỈ Y tá hành chính + Admin
[HttpPut("{maBenhNhan}/status")]
[Authorize]
[RequireRole("y_ta", "admin")]
[RequireNurseType("hanhchinh")]

// Xem thông tin - Tất cả (giữ nguyên [Authorize])
```

---

### 3. ClsController.cs
**Hiện tại:** 
- Tạo phiếu CLS: `[RequireRole("bac_si")]` ❌ SAI
- Tạo kết quả: `[RequireRole("ky_thuat_vien")]` ❌ SAI

**Cần sửa:**
```csharp
// Tạo phiếu CLS (chỉ định) - Bác sĩ + Y tá lâm sàng + Admin
[HttpPost("orders")]
[RequireRole("bac_si", "y_ta", "admin")]
[RequireNurseType("phong_kham")]

// Cập nhật trạng thái phiếu CLS - Kỹ thuật viên + Y tá CLS + Admin
[HttpPut("orders/{maPhieuKhamCls}/status")]
[RequireRole("ky_thuat_vien", "y_ta", "admin")]
[RequireNurseType("can_lam_sang")]

// Tạo kết quả CLS - Kỹ thuật viên + Y tá CLS + Admin
[HttpPost("results")]
[RequireRole("ky_thuat_vien", "y_ta", "admin")]
[RequireNurseType("can_lam_sang")]

// Tạo tổng hợp - Kỹ thuật viên + Y tá CLS + Admin
[HttpPost("summary/{maPhieuKhamCls}")]
[RequireRole("ky_thuat_vien", "y_ta", "admin")]
[RequireNurseType("can_lam_sang")]

// Cập nhật tổng hợp - Kỹ thuật viên + Y tá CLS + Admin
[HttpPut("summary/{maPhieuTongHop}")]
[RequireRole("ky_thuat_vien", "y_ta", "admin")]
[RequireNurseType("can_lam_sang")]
```

---

## 🔧 Cần tạo thêm:

### RequireAdminOrRoleAttribute.cs
Attribute mới để Admin luôn bypass kiểm tra:

```csharp
public class RequireAdminOrRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _allowedRoles;

    public RequireAdminOrRoleAttribute(params string[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var chucVu = context.HttpContext.User.FindFirst("ChucVu")?.Value;
        
        // Admin bypass tất cả
        if (chucVu == "admin") return;
        
        // Kiểm tra role thường
        if (!_allowedRoles.Contains(chucVu))
        {
            context.Result = new ObjectResult(new
            {
                message = $"Bạn không có quyền truy cập. Yêu cầu: {string.Join(", ", _allowedRoles)} hoặc admin"
            })
            {
                StatusCode = 403
            };
        }
    }
}
```

---

## 📝 Lưu ý:

1. **Admin luôn có quyền**: Cần sửa tất cả attribute để Admin bypass
2. **RequireNurseType**: Chỉ áp dụng khi user là Y tá, các vai trò khác bỏ qua
3. **Frontend**: Cần ẩn/hiện UI dựa trên vai trò user
4. **Testing**: Test kỹ từng vai trò sau khi cập nhật
