# Hướng dẫn hoàn thành Tuần 3 - Dashboard Admin & Hủy Luồng UI

> **File:** `UPGRADE_IMPLEMENTATION_PLAN.md` (Mục 3.5.4 - 3.5.7, 4.2, 4.5)

## Nhiệm vụ 1: Màn hình Analytics Admin
1. **Dashboard Biểu Đồ (`Dashboard.jsx`)**:
   - Render Chart.js / Recharts hiển thị dữ liệu "Xu hướng bệnh tật", "Thuốc hay dùng". Lấy Data từ API Analytics Dev 1 vừa làm.
   - Thiết kế widget đếm số lượng thống kê bất thường.

## Nhiệm vụ 2: Màn Hình Admin Users (CRUD)
1. **Quản trị Nhân sự (`AdminUsers.jsx`)**:
   - Giao diện Admin quản lý người dùng (mới). Không gộp với UI nhân viên.
   - Form thêm nhân viên, chọn vai trò bác sĩ / y tá / ktv. Khóa tài khoản. Mở / Đóng account.

## Nhiệm vụ 3: UI Luồng Trạng Thái Phần 2 (Hủy & Quay xe)
1. **Phiếu Khám LS & CLS**:
   - Nút "Hủy Phiếu" tại Hàng Đợi khám (Chỉ dành cho Phiếu mới Lập).
2. **Đơn Thuốc**:
   - Tab "Đã Hủy" ở màn Đơn Thuốc. Thêm nút "Hủy đơn" và xác nhận cho đơn thuốc "Chờ phát".
3. **Hóa Đơn**:
   - Đảm bảo Default State "Chưa Thu". Nút "Thu Tiền" (chua_thu -> da_thu) + nút "Hủy" -> da_huy trên Danh sách dòng thu ngân.
