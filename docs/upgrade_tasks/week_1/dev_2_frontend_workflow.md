# Hướng dẫn hoàn thành Tuần 1 - Luồng trạng thái Core & Frontend

> **File:** `UPGRADE_IMPLEMENTATION_PLAN.md` (Mục 3.5, 4.1)

## Nhiệm vụ 1: Cập nhật Frontend Menu (Sidebar) theo Quyền
1. **Frontend Sidebar.jsx**:
   - Sử dụng các hàm trong `permissions.js` (như `canManageReception`, `isAdmin`, v.v.)
   - Lọc mảng `links` theo vai trò của người dùng đang đăng nhập. Không render tĩnh toàn bộ danh sách.
   - Test lại với quyền Admin, Y tá HC, Bác sĩ, và KTV.

## Nhiệm vụ 2: Xử lý Luồng Lịch Hẹn & Lượt Khám
1. **Lịch hẹn (BE + FE)**:
   - BE (`AppointmentService.cs`): Thêm điều kiện (validate) khi update trạng thái. Chỉ cho `dang_cho` -> `da_xac_nhan` hoặc `da_huy`.
   - FE (`Appointments.jsx`): Thêm UI (Nút "Xác nhận", Nút "Hủy") cho các lịch hẹn với trạng thái tương ứng.
2. **Luồng bỏ về giữa chừng (BN)**:
   - BE: Sửa `UpdateDailyStatus` trong `PatientService.cs` cho phép ghi nhận Trạng thái là `da_huy` từ bất kì khâu nào chưa xong.
   - FE (`Patients.jsx`): Thêm nút "Bệnh nhân bỏ về" có confirm dialog -> set `status: "da_huy"`.
3. **Hủy Lượt Khám**:
   - BE (`ClinicalService.cs`): Thêm hàm `HuyLuotKhamAsync(maLuotKham)` (cập nhật Lượt Khám: `da_huy`, Hàng Đợi: `da_phuc_vu`, Bệnh nhân: `da_huy`). Exposed qua controller.
   - FE (`Examination.jsx`): Nút "Hủy lượt khám" hiển thị ở hàng đợi.
