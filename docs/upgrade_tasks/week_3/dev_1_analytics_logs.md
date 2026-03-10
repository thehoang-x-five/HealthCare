# Hướng dẫn hoàn thành Tuần 3 - MongoDB Analytics & Tính Năng Mở Rộng

> **File:** `UPGRADE_IMPLEMENTATION_PLAN.md` (Mục 2.4, 3.1, 3.2, 3.5.7)

## Nhiệm vụ 1: MongoDB Aggregation (Analytics)
1. **API Thống Kê**:
   - Viết Component `AnalyticsService.cs`.
   - Kết nối MongoDB bằng cú pháp Aggregation Framework của C#.
   - API `GET /api/analytics/abnormal-stats`: Thống kê tần suất xuất hiện chỉ số bất thường.
   - API `GET /api/analytics/disease-trends`: Thống kê nhóm bệnh ICD.
   - API `GET /api/analytics/popular-drugs`: Lấy danh sách TOP thuốc tiêu thụ.

## Nhiệm vụ 2: MongoDB Audit Logs
1. **Ghi Log (Mọi Hành Động Toàn Cục)**:
   - Tạo Repository và Controller middleware (hoặc Interceptor) chặn các request mutation (POST/PUT/DELETE) và lưu thông tin chi tiết vào collection `audit_logs`.
   - Cấu hình TTL index trong Mongo: Xóa bản ghi quá 365 ngày chạy tự động.

## Nhiệm vụ 3: Luồng Nghiệp vụ Nhỏ & Hủy Hóa Đơn
1. **Bảng Giao dịch SQL Mới**:
   - `LichSuXuatKhoService.cs`: Inject vào `PharmacyService`. Ghi log mỗi khi số lượng kho được trừ hoặc cộng (hoàn trả).
   - API lấy bảng ThongBaoMau.
2. **Hủy Đối Tượng (Hóa đơn, Cận Lâm Sàng)**:
   - Thêm BE handle luồng hủy Hóa Đơn (`chua_thu` -> `da_huy`) thay vì luôn `da_thu` lúc tạo. Validate transition.
   - Viết API `HuyPhieuCls`, `HuyDonThuoc` gồm cả rollback hoàn trả tồn kho.
