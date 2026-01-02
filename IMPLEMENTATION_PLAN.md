# Kế hoạch triển khai phân quyền

## Phase 1: Backend - Attributes (✅ Hoàn thành một phần)

### 1.1. Tạo/Cập nhật Attributes
- [x] `RequireRoleAttribute.cs` - Đã có
- [x] `RequireNurseTypeAttribute.cs` - Đã tạo
- [ ] Cập nhật để Admin bypass

### 1.2. Cập nhật Controllers
- [ ] `AppointmentsController.cs` - Thêm phân quyền Y tá HC
- [ ] `PatientsController.cs` - Thêm phân quyền Y tá HC
- [x] `ClinicalController.cs` - Đã cập nhật (cần sửa thêm)
- [ ] `ClsController.cs` - Thêm phân quyền KTV/Y tá CLS

---

## Phase 2: Frontend - UI Permissions

### 2.1. Tạo Permission Helper
File: `my-patients/src/utils/permissions.js`
- Hàm check quyền theo vai trò
- Hàm check quyền theo loại Y tá
- Constants cho vai trò

### 2.2. Cập nhật Components

#### Trang Lịch hẹn (`Appointments.jsx`):
- Ẩn nút "+ Tạo lịch hẹn" với BS/Y tá LS/KTV/Y tá CLS
- Ẩn nút "Check-in" với BS/Y tá LS/KTV/Y tá CLS
- Ẩn nút "Sửa/Xóa" trong modal với BS/Y tá LS/KTV/Y tá CLS

#### Trang Bệnh nhân (`Patients.jsx`):
- Ẩn nút "+ Thêm" với BS/Y tá LS/KTV/Y tá CLS
- Ẩn nút "✎ Sửa" với BS/Y tá LS/KTV/Y tá CLS
- Ẩn nút "Tạo lịch hẹn" trong modal với BS/Y tá LS/KTV/Y tá CLS
- Disable form edit thông tin BN với BS/Y tá LS/KTV/Y tá CLS

#### Trang Khám bệnh (`Examination.jsx`):
- Ẩn nút "Gọi vào" với Y tá HC (chỉ xem danh sách)
- Phân data LS/CLS đã có sẵn

---

## Phase 3: Testing

### 3.1. Backend Testing
- [ ] Test API với Y tá HC
- [ ] Test API với Bác sĩ
- [ ] Test API với Y tá LS
- [ ] Test API với KTV
- [ ] Test API với Y tá CLS
- [ ] Test API với Admin

### 3.2. Frontend Testing
- [ ] Test UI với từng vai trò
- [ ] Test ẩn/hiện nút đúng
- [ ] Test API call bị chặn khi không có quyền

---

## Thứ tự thực hiện:

1. ✅ Cập nhật `RequireRoleAttribute` và `RequireNurseTypeAttribute` để Admin bypass
2. ✅ Cập nhật `AppointmentsController.cs`
3. ✅ Cập nhật `PatientsController.cs`
4. ✅ Cập nhật `ClinicalController.cs` (hoàn thiện)
5. ✅ Cập nhật `ClsController.cs`
6. ✅ Tạo `permissions.js` helper
7. ✅ Cập nhật UI components
8. ⏳ Testing

Bắt đầu từ bước 1!
