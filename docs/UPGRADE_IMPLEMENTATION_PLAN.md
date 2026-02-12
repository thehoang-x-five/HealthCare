# Kế Hoạch Triển Khai Nâng Cấp HealthCare+ (Detailed Upgrade Plan)

## Tầm Nhìn
Chuyển đổi hệ thống từ Monolithic SQL sang kiến trúc **Polyglot Persistence** (SQL + MongoDB), tập trung vào tính toàn vẹn dữ liệu (Transaction) và tính linh hoạt (Schema Evolution).

---

## Giai Đoạn 1: Chuẩn Bị & Tái Cấu Trúc (Infrastructure & Refactoring)

### 1.1 Môi Trường (Setup Environment)
- [ ] Cài đặt **MongoDB Community Server** (Local) hoặc MongoDB Atlas (Cloud).
- [ ] Cài đặt **MongoDB Compass** (GUI quản lý).
- [ ] Thêm thư viện `MongoDB.Driver` vào `HealthCare.csproj`.
- [ ] Cấu hình Connection String trong `appsettings.json`.

### 1.2 "Gọt Giũa" SQL (Refactor SQL Database)
- [ ] Xóa các cột "rác" trong bảng `BenhNhan`: `TieuSuBenh`, `DiUng`, `TienSuPhauThuat`, `GhiChu`.
- [ ] Thêm cột `ParentID` (Int, Allow Null) vào bảng `BenhNhan`.
- [ ] Chạy lệnh `UPDATE-DATABASE` (Entity Framework) để đồng bộ Schema.

### 1.3 Thiết Lập Dự Án (Project Setup)
- [ ] Tạo thư mục `Services/NoSql` và `Services/Sql`.
- [ ] Tạo class `MongoDbContext` để quản lý kết nối NoSQL.
- [ ] Đăng ký Dependency Injection cho `MongoDbContext`.

---

## Giai Đoạn 2: Xây Dựng Logic Cốt Lõi (Core Business Logic) - "The Big 4"

### 2.1 Chức Năng 1: Đặt Lịch Chặt Chẽ (Strict Scheduling - SQL)
*Mục tiêu: Ngăn chặn Double-Booking bằng Transaction.*
- [ ] Viết Stored Procedure `sp_BookAppointment` trong SQL Server.
    - [ ] Logic: `SET TRANSACTION ISOLATION LEVEL SERIALIZABLE`.
    - [ ] Logic: Check `COUNT(*)` trùng giờ & bác sĩ -> Nếu > 0 thì `ROLLBACK`, ngược lại `INSERT` & `COMMIT`.
- [ ] Cập nhật `AppointmentService.cs` để gọi Stored Procedure này thay vì dùng EF Core `Add()`.

### 2.2 Chức Năng 2: Cây Gia Phả (Genealogy - SQL)
*Mục tiêu: Truy vấn đệ quy cha mẹ/ông bà.*
- [ ] Viết câu lệnh SQL CTE: `WITH RECURSIVE Ancestors AS ...`.
- [ ] Tạo API `GET /api/patients/{id}/ancestors`.
- [ ] Trả về danh sách cây gia phả (dạng phẳng hoặc lồng nhau).

### 2.3 Chức Năng 3: Bệnh Án Linh Hoạt (Flexible Records - NoSQL)
*Mục tiêu: Lưu trữ đa dạng dữ liệu không cấu trúc.*
- [ ] Thiết kế Model `PatientHistory` (MongoDB Document).
- [ ] Tạo Collection `Histories`.
- [ ] Viết API `POST /api/clinical/{id}/events`:
    - [ ] Nhận JSON tùy ý (Dynamic Object).
    - [ ] Lưu vào mảng `events` trong document của bệnh nhân.
- [ ] Viết API `GET /api/clinical/{id}/history`: Lấy toàn bộ lịch sử.

### 2.4 Chức Năng 4: Phân Tích Dữ Liệu (Analytics - NoSQL)
*Mục tiêu: Thống kê xu hướng bệnh.*
- [ ] Viết Aggregation Pipeline:
    - [ ] `$match`: Lọc theo khoảng thời gian.
    - [ ] `$unwind`: Bung mảng `events`.
    - [ ] `$group`: Đếm số lượng theo `diagnosis`.
    - [ ] `$sort`: Sắp xếp giảm dần.
- [ ] Tạo API `GET /api/analytics/trends`.

---

## Giai Đoạn 3: Tự Động Hóa Workflow (Automation)

### 3.1 Quy Trình Tiếp Đón
- [ ] Cập nhật `QueueService`: Tự động tính độ ưu tiên (0/10/20/30) khi thêm vào hàng đợi.
- [ ] Cập nhật `ReceptionController`: Khi tạo phiếu khám -> Tự động tạo hóa đơn tạm (Pending).

### 3.2 Quy Trình Dược & Thanh Toán
- [ ] Cập nhật `InventoryService`: Dùng Transaction khóa kho khi bác sĩ kê đơn.
- [ ] Tích hợp `VietQR API`: Tạo mã QR động dựa trên số tiền hóa đơn (`BankingService`).

---

## Giai Đoạn 4: Cập Nhật Giao Diện (Frontend Integration)

### 4.1 Màn Hình Bác Sĩ (Doctor UI)
- [ ] Hiển thị Timeline bệnh án từ MongoDB (thay vì bảng SQL cũ).
- [ ] Thêm nút "Xem Gia Phả" -> Popup hiển thị cây gia phả.

### 4.2 Màn Hình Quản Trị (Admin UI)
- [ ] Thêm Dashboard "Xu Hướng Bệnh Tật" (Biểu đồ từ API Analytics).

### 4.3 Màn Hình Thanh Toán (Billing UI)
- [ ] Thêm chức năng "Quét QR" và hiển thị ảnh QR code.

---

## Giai Đoạn 5: Kiểm Thử & Đóng Gói (Verification & Delivery)

### 5.1 Kiểm Thử Kỹ Thuật (Technical Testing)
- [ ] **Test Race Condition**: Dùng công cụ (JMeter hoặc Script) gửi 10 request đặt lịch cùng lúc -> Chỉ 1 thành công.
- [ ] **Test Schema Evolution**: Thêm trường mới vào bản ghi mới -> Đọc lại bản ghi cũ không lỗi.

### 5.2 Đóng Gói Tài Liệu
- [ ] Cập nhật Swagger.
- [ ] Quay video demo 4 chức năng chính.
- [ ] Tổng hợp file báo cáo PDF.
