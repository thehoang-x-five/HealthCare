# Hướng dẫn hoàn thành Tuần 2 - Đệ quy SQL & Giao diện MongoDB Timeline

> **File:** `UPGRADE_IMPLEMENTATION_PLAN.md` (Mục 2.2, 4.3, 4.4)

## Nhiệm vụ 1: Backend Pha Hệ
1. **SQL Đệ Quy (Recursive CTE)**:
   - Viết Raw SQL Query hoặc dùng Entity Framework raw properties trong service `GenealogyService.cs`.
   - Câu lệnh đệ quy lấy con cháu / bố mẹ thông qua `MaCha`, `MaMe`.
2. **API Pha Hệ**:
   - Tạo `GenealogyController.cs`.
   - API GET lấy cây gia đình.
   - API POST để liên kết `MaCha`, `MaMe`.
   - API GET lấy tiền sử bệnh của gia phả.

## Nhiệm vụ 2: Frontend Bệnh Nhân
1. **Tab Pha Hệ (`Patients.jsx`)**:
   - Tạo UI để hiển thị cây gia đình dưới dạng Node/Tree interactive hoặc dạng bảng tuyến tính. Thêm chức năng chọn Bệnh Nhân khác để link làm bố/mẹ.
2. **Tab Lịch Sử Khám**:
   - Tạo component Timeline. Call API MongoDB từ BE của Dev 1.
   - Mở rộng chi tiết cho TỪNG LOẠI SỰ KIỆN: click vào Đơn Thuốc hiển thị popup thuốc, click vào XN hiển thị bảng chỉ số.
3. **Tab Giao Dịch**:
   - Bổ sung lịch sử tài chính cho từng bệnh nhân dựa trên dữ liệu hóa đơn tĩnh.
