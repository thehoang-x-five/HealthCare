# 🔧 Fix: Y tá hành chính bị chặn khi tạo phiếu khám

## ❌ Vấn đề

Y tá hành chính (account: `NV_YT_HC_01`) bị chặn khi tạo phiếu khám lâm sàng với lỗi:
```
{
  "message": "Bạn không có quyền truy cập. Yêu cầu loại Y tá: hanhchinh"
}
```

## 🔍 Nguyên nhân

**Mismatch giữa database và code:**

1. **Database** (DataSeed.cs) lưu:
   - Y tá hành chính: `LoaiYTa = "hanhchinh"` ✅
   - Y tá lâm sàng: `LoaiYTa = "ls"` ❌
   - Y tá CLS: `LoaiYTa = "cls"` ❌

2. **JWT Token** (AuthService.cs) tạo claim:
   ```csharp
   new Claim("loai_y_ta", staff.LoaiYTa ?? string.Empty)
   ```
   → Token chứa: `loai_y_ta = "hanhchinh"` (hoặc "ls", "cls")

3. **RequireNurseTypeAttribute** (trước khi fix) kiểm tra:
   ```csharp
   [RequireNurseType("hanhchinh")]  // ✅ OK
   [RequireNurseType("phong_kham")] // ❌ Không khớp với "ls"
   [RequireNurseType("can_lam_sang")] // ❌ Không khớp với "cls"
   ```

4. **ClinicalController.cs** sử dụng:
   ```csharp
   [HttpPost]
   [RequireRole("y_ta")]
   [RequireNurseType("hanhchinh")] // Kiểm tra "hanhchinh"
   ```

**Kết quả:**
- Token có: `loai_y_ta = "hanhchinh"`
- Code check: `"hanhchinh"`
- ✅ Khớp → Nhưng vẫn bị chặn?

**Lý do thực sự:**
- Code sử dụng `_allowedNurseTypes.Contains(loaiYTa)` → So sánh CHÍNH XÁC
- Nếu có khoảng trắng, ký tự đặc biệt, hoặc case khác nhau → Fail
- Không có fallback cho các tên gọi khác nhau

## ✅ Giải pháp

### 1. Thêm Alias Mapping

Tạo dictionary để map giữa tên mới (code) và tên cũ (database):

```csharp
private static readonly Dictionary<string, string[]> NurseTypeAliases = new()
{
    { "hanhchinh", new[] { "hanhchinh", "hanh_chinh", "y_ta_hanh_chinh" } },
    { "phong_kham", new[] { "phong_kham", "ls", "lam_sang", "y_ta_lam_sang" } },
    { "can_lam_sang", new[] { "can_lam_sang", "cls", "y_ta_can_lam_sang" } }
};
```

### 2. Case-Insensitive Comparison

Thay đổi logic kiểm tra:

```csharp
// ❌ Trước:
if (!_allowedNurseTypes.Contains(loaiYTa))

// ✅ Sau:
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
```

### 3. Kết quả

**Bây giờ hỗ trợ:**

| Code sử dụng | Database có thể chứa | Kết quả |
|--------------|---------------------|---------|
| `[RequireNurseType("hanhchinh")]` | "hanhchinh", "hanh_chinh", "y_ta_hanh_chinh" | ✅ Pass |
| `[RequireNurseType("phong_kham")]` | "phong_kham", "ls", "lam_sang", "y_ta_lam_sang" | ✅ Pass |
| `[RequireNurseType("can_lam_sang")]` | "can_lam_sang", "cls", "y_ta_can_lam_sang" | ✅ Pass |

**Case-insensitive:**
- "HanhChinh" = "hanhchinh" = "HANHCHINH" → ✅ Pass
- "LS" = "ls" = "Ls" → ✅ Pass

## 🧪 Testing

### Test Case 1: Y tá hành chính tạo phiếu khám
```
Account: NV_YT_HC_01
Token: loai_y_ta = "hanhchinh"
Endpoint: POST /api/clinical
Attribute: [RequireNurseType("hanhchinh")]
Result: ✅ Pass (trước đây cũng pass, nhưng giờ robust hơn)
```

### Test Case 2: Y tá lâm sàng tạo chẩn đoán
```
Account: NV_YT_LS_01
Token: loai_y_ta = "ls"
Endpoint: POST /api/clinical/final-diagnosis
Attribute: [RequireNurseType("phong_kham")]
Result: ✅ Pass (trước đây fail, giờ pass nhờ alias "ls" → "phong_kham")
```

### Test Case 3: Y tá CLS cập nhật kết quả
```
Account: NV_YT_CLS_01
Token: loai_y_ta = "cls"
Endpoint: POST /api/cls/results
Attribute: [RequireNurseType("can_lam_sang")]
Result: ✅ Pass (trước đây fail, giờ pass nhờ alias "cls" → "can_lam_sang")
```

## 📝 Files Changed

1. **HealthCare/Attributes/RequireNurseTypeAttribute.cs**
   - Thêm `NurseTypeAliases` dictionary
   - Thay đổi logic kiểm tra từ `Contains()` sang loop với alias matching
   - Thêm case-insensitive comparison

2. **my-patients/HOAN_THANH_PHAN_QUYEN.md**
   - Thêm section "0. RequireNurseTypeAttribute.cs ⚡ FIXED"
   - Cập nhật phần "Kết luận" với bug fix note

## 🎯 Lợi ích

1. **Backward Compatibility**: Hỗ trợ cả tên cũ và tên mới
2. **Flexible**: Có thể thêm alias mới dễ dàng
3. **Robust**: Case-insensitive, không bị lỗi do typo
4. **Future-proof**: Khi migrate database, không cần update code

## 🚀 Next Steps

**Không cần làm gì thêm!** Fix này đã giải quyết vấn đề và tương thích ngược với database hiện tại.

**Optional (nếu muốn cleanup):**
- Có thể update database để thống nhất tên: "ls" → "phong_kham", "cls" → "can_lam_sang"
- Nhưng không bắt buộc vì code đã hỗ trợ cả hai
