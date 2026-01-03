# Hướng dẫn sử dụng Enum cho Trạng thái

## Tổng quan

Thay vì sử dụng string literals (`"da_hoan_tat"`, `"cho_kham"`, ...), hệ thống giờ đây sử dụng **static constants** để quản lý tất cả các trạng thái.

**File:** `HealthCare/Enums/StatusEnums.cs`

## Lợi ích

✅ **Type-safe** - Compiler kiểm tra lỗi typo  
✅ **IntelliSense** - IDE gợi ý tự động  
✅ **Refactoring dễ dàng** - Đổi tên một chỗ, update toàn bộ  
✅ **Dễ đọc** - Code rõ ràng, dễ hiểu hơn  
✅ **Tránh lỗi** - Không còn viết sai `"da_hoan_tat"` thành `"da_hoan_tat"`  

---

## Danh sách Enum

### 1. Trạng thái Tài khoản Bệnh nhân
```csharp
TrangThaiTaiKhoan.HoatDong          // "hoat_dong"
TrangThaiTaiKhoan.KhongHoatDong     // "khong_hoat_dong"
TrangThaiTaiKhoan.DaXoa             // "da_xoa"
```

### 2. Trạng thái Hôm nay (Workflow trong ngày)
```csharp
TrangThaiHomNay.ChoTiepNhan         // "cho_tiep_nhan" - Mặc định khi mới vào
TrangThaiHomNay.ChoKham             // "cho_kham"
TrangThaiHomNay.DangKham            // "dang_kham" - Đang khám LS
TrangThaiHomNay.ChoTiepNhanDv       // "cho_tiep_nhan_dv"
TrangThaiHomNay.ChoKhamDv           // "cho_kham_dv"
TrangThaiHomNay.DangKhamDv          // "dang_kham_dv" - Đang khám CLS
TrangThaiHomNay.ChoXuLy             // "cho_xu_ly"
TrangThaiHomNay.ChoXuLyDv           // "cho_xu_ly_dv"
TrangThaiHomNay.DaHoanTat           // "da_hoan_tat"
TrangThaiHomNay.DaHuy               // "da_huy"
```

### 3. Trạng thái Phiếu khám Lâm sàng
```csharp
TrangThaiPhieuKhamLs.DaLap          // "da_lap"
TrangThaiPhieuKhamLs.DangThucHien   // "dang_thuc_hien"
TrangThaiPhieuKhamLs.DaLapChanDoan  // "da_lap_chan_doan"
TrangThaiPhieuKhamLs.DaHoanTat      // "da_hoan_tat"
TrangThaiPhieuKhamLs.DaHuy          // "da_huy"
```

### 4. Trạng thái Phiếu khám CLS
```csharp
TrangThaiPhieuKhamCls.DaLap         // "da_lap"
TrangThaiPhieuKhamCls.DangThucHien  // "dang_thuc_hien"
TrangThaiPhieuKhamCls.DaHoanTat     // "da_hoan_tat"
TrangThaiPhieuKhamCls.DaHuy         // "da_huy"
```

### 5. Trạng thái Chi tiết Dịch vụ CLS
```csharp
TrangThaiChiTietDv.DaLap            // "da_lap"
TrangThaiChiTietDv.DangThucHien     // "dang_thuc_hien"
TrangThaiChiTietDv.DaCoKetQua       // "da_co_ket_qua"
TrangThaiChiTietDv.ChuaCoKetQua     // "chua_co_ket_qua"
TrangThaiChiTietDv.DaHuy            // "da_huy"
```

### 6. Trạng thái Đơn thuốc
```csharp
TrangThaiDonThuoc.DaKe              // "da_ke"
TrangThaiDonThuoc.ChoPhat           // "cho_phat"
TrangThaiDonThuoc.DaPhat            // "da_phat"
TrangThaiDonThuoc.DaHuy             // "da_huy"
```

### 7. Trạng thái Lịch hẹn
```csharp
TrangThaiLichHen.DangCho            // "dang_cho"
TrangThaiLichHen.DaXacNhan          // "da_xac_nhan"
TrangThaiLichHen.DaCheckin          // "da_checkin"
TrangThaiLichHen.DaHuy              // "da_huy"
```

### 8. Trạng thái Hàng đợi
```csharp
TrangThaiHangDoi.ChoGoi             // "cho_goi"
TrangThaiHangDoi.DangGoi            // "dang_goi"
TrangThaiHangDoi.DangThucHien       // "dang_thuc_hien"
TrangThaiHangDoi.DaPhucVu           // "da_phuc_vu"
```

### 9. Trạng thái Lượt khám
```csharp
TrangThaiLuotKham.DangThucHien      // "dang_thuc_hien"
TrangThaiLuotKham.HoanTat           // "hoan_tat"
```

### 10. Trạng thái Hóa đơn
```csharp
TrangThaiHoaDon.ChuaThu             // "chua_thu"
TrangThaiHoaDon.DaThu               // "da_thu"
TrangThaiHoaDon.DaHuy               // "da_huy"
```

### 11. Các Enum khác

**Loại hàng đợi:**
```csharp
LoaiHangDoi.KhamLamSang             // "kham_lam_sang"
LoaiHangDoi.CanLamSang              // "can_lam_sang"
```

**Nguồn hàng đợi:**
```csharp
NguonHangDoi.Appointment            // "appointment"
NguonHangDoi.Walkin                 // "walkin"
NguonHangDoi.ServiceReturn          // "service_return"
```

**Phân loại đến:**
```csharp
PhanLoaiDen.DungGio                 // "dung_gio"
PhanLoaiDen.DenSom                  // "den_som"
PhanLoaiDen.DenMuon                 // "den_muon"
```

**Trạng thái phiếu tổng hợp:**
```csharp
TrangThaiPhieuTongHop.DangThucHien  // "dang_thuc_hien"
TrangThaiPhieuTongHop.ChoXuLy       // "cho_xu_ly"
TrangThaiPhieuTongHop.DangXuLy      // "dang_xu_ly"
TrangThaiPhieuTongHop.DaHoanTat     // "da_hoan_tat"
```

**Trạng thái thông báo:**
```csharp
TrangThaiThongBao.ChuaGui           // "chua_gui"
TrangThaiThongBao.DaGui             // "da_gui"
TrangThaiThongBao.DaDoc             // "da_doc"
```

**Loại Y tá:**
```csharp
LoaiYTa.LamSang                     // "lamsang"
LoaiYTa.CanLamSang                  // "canlamsang"
LoaiYTa.HanhChinh                   // "hanhchinh"
```

---

## Cách sử dụng

### Import namespace

```csharp
using HealthCare.Enums;
```

### Ví dụ 1: Tạo phiếu khám

**Trước:**
```csharp
var phieu = new PhieuKhamLamSang
{
    TrangThai = "da_lap",  // ❌ Dễ typo
    HinhThucTiepNhan = "walkin"
};
```

**Sau:**
```csharp
var phieu = new PhieuKhamLamSang
{
    TrangThai = TrangThaiPhieuKhamLs.DaLap,  // ✅ Type-safe
    HinhThucTiepNhan = HinhThucTiepNhan.Walkin
};

// Cập nhật trạng thái bệnh nhân
benhNhan.TrangThaiHomNay = TrangThaiHomNay.DangKham;  // Đang khám LS
```

### Ví dụ 2: Cập nhật trạng thái

**Trước:**
```csharp
phieu.TrangThai = "dang_thuc_hien";  // ❌ Không có IntelliSense
```

**Sau:**
```csharp
phieu.TrangThai = TrangThaiPhieuKhamLs.DangThucHien;  // ✅ IDE gợi ý
```

### Ví dụ 3: Query với LINQ

**Trước:**
```csharp
var phieus = await _db.PhieuKhamLamSangs
    .Where(p => p.TrangThai == "da_hoan_tat")  // ❌ Magic string
    .ToListAsync();
```

**Sau:**
```csharp
var phieus = await _db.PhieuKhamLamSangs
    .Where(p => p.TrangThai == TrangThaiPhieuKhamLs.DaHoanTat)  // ✅ Clear
    .ToListAsync();
```

### Ví dụ 4: Switch statement

**Trước:**
```csharp
switch (phieu.TrangThai)
{
    case "da_lap":
        // ...
        break;
    case "dang_thuc_hien":
        // ...
        break;
    case "da_hoan_tat":
        // ...
        break;
}
```

**Sau:**
```csharp
switch (phieu.TrangThai)
{
    case TrangThaiPhieuKhamLs.DaLap:
        // ...
        break;
    case TrangThaiPhieuKhamLs.DangThucHien:
        // ...
        break;
    case TrangThaiPhieuKhamLs.DaHoanTat:
        // ...
        break;
}
```

### Ví dụ 5: Bulk update

**Trước:**
```csharp
await _db.PhieuKhamLamSangs
    .Where(p => p.TrangThai != "da_hoan_tat")
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(p => p.TrangThai, "da_huy"));
```

**Sau:**
```csharp
await _db.PhieuKhamLamSangs
    .Where(p => p.TrangThai != TrangThaiPhieuKhamLs.DaHoanTat)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(p => p.TrangThai, TrangThaiPhieuKhamLs.DaHuy));
```

---

## Migration Guide

### Bước 1: Tìm tất cả string literals

```bash
# Tìm tất cả "da_hoan_tat" trong code
grep -r '"da_hoan_tat"' HealthCare/Services/
grep -r '"cho_kham"' HealthCare/Services/
grep -r '"dang_thuc_hien"' HealthCare/Services/
```

### Bước 2: Replace từng file

**Ví dụ với ClinicalService.cs:**

```csharp
// Thêm using
using HealthCare.Enums;

// Replace
- p.TrangThai != "da_hoan_tat"
+ p.TrangThai != TrangThaiPhieuKhamLs.DaHoanTat

- phieu.TrangThai = "dang_thuc_hien"
+ phieu.TrangThai = TrangThaiPhieuKhamLs.DangThucHien

- benhNhan.TrangThaiHomNay = "cho_kham"
+ benhNhan.TrangThaiHomNay = TrangThaiHomNay.ChoKham
```

### Bước 3: Test

```bash
# Build để kiểm tra lỗi compile
dotnet build

# Chạy tests
dotnet test
```

---

## Best Practices

### ✅ DO

```csharp
// Sử dụng enum constants
phieu.TrangThai = TrangThaiPhieuKhamLs.DaHoanTat;

// So sánh với enum
if (phieu.TrangThai == TrangThaiPhieuKhamLs.DaHoanTat)
{
    // ...
}

// Query với enum
var phieus = await _db.PhieuKhamLamSangs
    .Where(p => p.TrangThai == TrangThaiPhieuKhamLs.DaHoanTat)
    .ToListAsync();
```

### ❌ DON'T

```csharp
// Không dùng string literals
phieu.TrangThai = "da_hoan_tat";  // ❌

// Không hardcode
if (phieu.TrangThai == "da_hoan_tat")  // ❌
{
    // ...
}

// Không dùng magic strings
var phieus = await _db.PhieuKhamLamSangs
    .Where(p => p.TrangThai == "da_hoan_tat")  // ❌
    .ToListAsync();
```

---

## Troubleshooting

### Lỗi: "Cannot implicitly convert type 'string' to 'string'"

**Nguyên nhân:** Bạn đang so sánh enum với string literal

**Giải pháp:**
```csharp
// ❌ Sai
if (phieu.TrangThai == "da_hoan_tat")

// ✅ Đúng
if (phieu.TrangThai == TrangThaiPhieuKhamLs.DaHoanTat)
```

### Lỗi: "The name 'TrangThaiPhieuKhamLs' does not exist"

**Nguyên nhân:** Chưa import namespace

**Giải pháp:**
```csharp
using HealthCare.Enums;
```

### Enum không hiển thị trong IntelliSense

**Nguyên nhân:** File chưa được build

**Giải pháp:**
```bash
dotnet build
```

---

## Checklist Migration

- [ ] Thêm `using HealthCare.Enums;` vào tất cả service files
- [ ] Replace string literals trong Services/
- [ ] Replace string literals trong Controllers/
- [ ] Replace string literals trong DTOs/
- [ ] Replace string literals trong Entities/ (nếu có default values)
- [ ] Build và fix compile errors
- [ ] Run tests
- [ ] Test thủ công các chức năng chính
- [ ] Update documentation

---

## Files cần migration

### Priority 1 (Critical)
- ✅ `Services/Background/DailyResetService.cs` - Đã update
- [ ] `Services/OutpatientCare/ClinicalService.cs`
- [ ] `Services/OutpatientCare/ClsService.cs`
- [ ] `Services/OutpatientCare/QueueService.cs`
- [ ] `Services/OutpatientCare/HistoryService.cs`

### Priority 2 (Important)
- [ ] `Services/PatientManagement/PatientService.cs`
- [ ] `Services/PatientManagement/AppointmentService.cs`
- [ ] `Services/MedicationBilling/PharmacyService.cs`
- [ ] `Services/MedicationBilling/BillingService.cs`

### Priority 3 (Nice to have)
- [ ] `Services/Report/DashboardService.cs`
- [ ] `Services/Report/ReportService.cs`
- [ ] `Services/UserInteraction/NotificationService.cs`

---

## Tương lai

### Cải tiến có thể thêm:

1. **Validation attributes**
```csharp
[AllowedValues(
    TrangThaiPhieuKhamLs.DaLap,
    TrangThaiPhieuKhamLs.DangThucHien,
    TrangThaiPhieuKhamLs.DaHoanTat)]
public string TrangThai { get; set; }
```

2. **Extension methods**
```csharp
public static class TrangThaiExtensions
{
    public static bool IsCompleted(this string trangThai)
    {
        return trangThai == TrangThaiPhieuKhamLs.DaHoanTat;
    }
}
```

3. **Enum descriptions**
```csharp
public static string GetDisplayName(this string trangThai)
{
    return trangThai switch
    {
        TrangThaiPhieuKhamLs.DaLap => "Đã lập",
        TrangThaiPhieuKhamLs.DangThucHien => "Đang thực hiện",
        TrangThaiPhieuKhamLs.DaHoanTat => "Đã hoàn tất",
        _ => trangThai
    };
}
```

---

**Ngày tạo:** 2026-01-03  
**Phiên bản:** 1.0  
**Status:** Ready for migration ✅
