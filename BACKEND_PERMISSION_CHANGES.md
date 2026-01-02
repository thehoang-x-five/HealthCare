# Thay đổi phân quyền Backend - Summary

## ✅ Đã hoàn thành:

### 1. Attributes
- ✅ `RequireRoleAttribute.cs` - Đã thêm Admin bypass
- ✅ `RequireNurseTypeAttribute.cs` - Đã thêm Admin bypass

### 2. ClinicalController.cs
- ✅ Tạo phiếu khám: Y tá HC/LS + Admin
- ✅ Cập nhật trạng thái: Bác sĩ + Y tá LS + Admin
- ✅ Chẩn đoán: Bác sĩ + Admin
- ✅ Hoàn tất: Bác sĩ + Admin

---

## ⏳ Cần cập nhật tiếp:

### 3. AppointmentsController.cs

Thêm vào đầu file:
```csharp
using HealthCare.Attributes;
```

Cập nhật các endpoint:

```csharp
// Tạo lịch hẹn - CHỈ Y tá HC + Admin
[HttpPost]
[Authorize]
[RequireRole("y_ta")]
[RequireNurseType("hanhchinh")]
public async Task<ActionResult<AppointmentReadRequestDto>> Create(...)

// Cập nhật lịch hẹn - CHỈ Y tá HC + Admin
[HttpPut("{maLichHen}")]
[Authorize]
[RequireRole("y_ta")]
[RequireNurseType("hanhchinh")]
public async Task<ActionResult<AppointmentReadRequestDto>> Update(...)

// Check-in - CHỈ Y tá HC + Admin
[HttpPut("{maLichHen}/status")]
[Authorize]
[RequireRole("y_ta")]
[RequireNurseType("hanhchinh")]
public async Task<ActionResult<AppointmentReadRequestDto>> UpdateStatus(...)
```

---

### 4. PatientsController.cs

Thêm vào đầu file:
```csharp
using HealthCare.Attributes;
```

Cập nhật các endpoint:

```csharp
// Tạo/cập nhật bệnh nhân - CHỈ Y tá HC + Admin
[HttpPost]
[Authorize]
[RequireRole("y_ta")]
[RequireNurseType("hanhchinh")]
public async Task<ActionResult<PatientDto>> UpsertPatient(...)

// Cập nhật trạng thái - CHỈ Y tá HC + Admin
[HttpPut("{maBenhNhan}/status")]
[Authorize]
[RequireRole("y_ta")]
[RequireNurseType("hanhchinh")]
public async Task<ActionResult<PatientDto>> UpdateDailyStatus(...)
```

---

### 5. ClsController.cs

Cập nhật các endpoint:

```csharp
// Tạo phiếu CLS (chỉ định) - Bác sĩ + Y tá LS + Admin
[HttpPost("orders")]
[RequireRole("bac_si", "y_ta")]
[RequireNurseType("phong_kham")]
public async Task<ActionResult<ClsOrderDto>> TaoPhieuCls(...)

// Cập nhật trạng thái - KTV + Y tá CLS + Admin
[HttpPut("orders/{maPhieuKhamCls}/status")]
[RequireRole("ky_thuat_vien", "y_ta")]
[RequireNurseType("can_lam_sang")]
public async Task<ActionResult<ClsOrderDto>> CapNhatTrangThaiPhieu(...)

// Tạo kết quả - KTV + Y tá CLS + Admin
[HttpPost("results")]
[RequireRole("ky_thuat_vien", "y_ta")]
[RequireNurseType("can_lam_sang")]
public async Task<ActionResult<ClsResultDto>> TaoKetQua(...)

// Tạo tổng hợp - KTV + Y tá CLS + Admin
[HttpPost("summary/{maPhieuKhamCls}")]
[RequireRole("ky_thuat_vien", "y_ta")]
[RequireNurseType("can_lam_sang")]
public async Task<ActionResult<ClsSummaryDto>> TaoTongHop(...)

// Cập nhật tổng hợp - KTV + Y tá CLS + Admin
[HttpPut("summary/{maPhieuTongHop}")]
[RequireRole("ky_thuat_vien", "y_ta")]
[RequireNurseType("can_lam_sang")]
public async Task<ActionResult<ClsSummaryDto>> CapNhatSummary(...)
```

---

### 6. ClinicalController.cs (Cập nhật thêm)

Sửa lại để Bác sĩ + Y tá LS có quyền bằng nhau:

```csharp
// Cập nhật trạng thái - Bác sĩ + Y tá LS + Admin
[HttpPut("{maPhieuKham}/status")]
[RequireRole("bac_si", "y_ta")]
[RequireNurseType("phong_kham")]
public async Task<ActionResult<ClinicalExamDto>> CapNhatTrangThai(...)

// Tạo chẩn đoán - Bác sĩ + Y tá LS + Admin
[HttpPost("final-diagnosis")]
[RequireRole("bac_si", "y_ta")]
[RequireNurseType("phong_kham")]
public async Task<ActionResult<FinalDiagnosisDto>> TaoHoacCapNhatChanDoan(...)

// Hoàn tất khám - Bác sĩ + Y tá LS + Admin
[HttpPost("{maPhieuKham}/complete")]
[RequireRole("bac_si", "y_ta")]
[RequireNurseType("phong_kham")]
public async Task<ActionResult<ClinicalExamDto>> CompleteExam(...)
```

---

## Lưu ý:
- Admin luôn bypass tất cả kiểm tra (đã implement trong Attributes)
- `RequireNurseType` chỉ áp dụng khi user là Y tá, các vai trò khác tự động pass
- Các endpoint GET (xem) không cần thêm phân quyền - giữ nguyên `[Authorize]`
