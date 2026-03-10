# Hướng dẫn hoàn thành Tuần 4 - Tích hợp VietQR, Kiểm thử & Đóng gói Báo Cáo

> **File:** `UPGRADE_IMPLEMENTATION_PLAN.md` (Mục 4.6, 5.1, 5.2)

## Nhiệm vụ 1: Thanh Toán VietQR
1. **Frontend Thanh Toán (`Billing.jsx`)**:
   - Select Phương thức thanh toán (Tiền Mặt, Thẻ, VietQR).
   - Khi ấn nút Render QR (hay tự động nếu chọn QR): Call API `/api/billing/{id}/generate-qr` để lấy hình ảnh mã QR động và render lên popup Modal cho người dùng quét. Xử lý timeout/Đóng popup.

## Nhiệm vụ 2: Kiểm Thử Toàn Cục (UAT)
1. **Test Concurrency (Race Condition)**:
   - Tạo script JS test hoặc dùng Postman bắn 10 request đặt 1 khung giờ cùng lúc xem có bị trùng lịch hẹn / xuất kho không (Chỉ được 1 pass).
2. **Test Lịch sử Mở Rộng + Analytics**:
   - Thêm một Loại Event Mới chưa từng code vào Mongo qua Tool/DB Compass xem app Frontend có parse được linh hoạt không.
   - Thử nghiệm Data lớn Aggregator.

## Nhiệm vụ 3: Chuẩn Bị Tài Liệu & Demo
1. **Đóng gói**:
   - Chuẩn bị Báo cáo Slide/PDF theo các tài liệu đã có.
   - Record video demo quay lại từng chức năng "Biến số" (Admin Dashboard, QR, Lịch sử MongoDB, Race Condition). Hoàn tất công việc. Mọi thứ đã lưu vào codebase.
