# Phân Tích & Đánh Giá Nâng Cấp Dự Án HealthCare+ (Gap Analysis)

## 1. Tổng Quan về Sự Thay Đổi (Current vs Target)

| Đặc điểm | Dự án Hiện tại (Legacy) | Dự án Mục tiêu (HealthCare+) | Đánh giá |
| :--- | :--- | :--- | :--- |
| **Kiến trúc DB** | Monolithic SQL Server (Dữ liệu dồn hết vào bảng) | **Polyglot Persistence** (SQL + MongoDB kết hợp) | **THIẾU NGHIÊM TRỌNG** |
| **Lịch hẹn** | Check trùng đơn giản (code C#), dễ bị Race Condition | **SQL Transaction SERIALIZABLE** (Stored Procedure) | **CẦN NÂNG CẤP** |
| **Bệnh án** | Cột cứng trong bảng `BenhNhan` (TieuSu, DiUng...) | **Document linh hoạt** (MongoDB Collection `Histories`) | **CẦN REFAGTOR** |
| **Truy vấn** | Query phẳng (SELECT * FROM) | **Recursive CTE** (Gia phả) & **Aggregation** (Thống kê) | **THIẾU** |
| **Real-time** | Cơ bản (hoặc chưa tối ưu) | **SignalR Full Flow** (Queue, Lab, Billing) | **CẦN BỔ SUNG** |

---

## 2. Chi Tiết Các Điểm Cần Chỉnh Sửa & Bổ Sung

### A. Database (SQL Server) - Cần "Gọt Giũa"
**Tình trạng hiện tại:** Bảng `BenhNhan` đang bị "phình to" vì chứa các trường không cấu trúc cố định.
**Cần làm:**
1.  **XÓA (Remove):** Các cột `TieuSuBenh`, `DiUng`, `TienSuPhauThuat`, `GhiChu` khỏi bảng SQL `BenhNhan` và `PhieuKham`.
2.  **THÊM (Add):** Cột `ParentID` (Int, Nullable) vào bảng `BenhNhan` để phục vụ **Truy vấn Đệ quy (Genealogy)**.
3.  **GIỮ LẠI (Keep):** Các bảng cốt lõi `LichHen`, `NhanSu`, `HoaDon`, `KhoThuoc`. Đây là các dữ liệu cần ACID cao.

### B. Database (MongoDB) - Cần "Xây Mới"
**Tình trạng hiện tại:** Chưa có.
**Cần làm:**
1.  **Cài đặt:** Thêm thư viện `MongoDB.Driver` vào .NET Backend.
2.  **Tạo Collection `Histories`:**
    *   Lưu trữ toàn bộ sự kiện y tế: Khám, Xét nghiệm, Tiêm chủng.
    *   **Schema Evolution:** Cấu trúc tự do. Ví dụ: Bản ghi năm 2024 có trường `vaccine_covid`, bản ghi năm 2020 không có cũng không sao.
    *   **Migration:** Viết script chuyển dữ liệu từ các cột SQL đã xóa sang Document này.

### C. Logic Backend (C# Services) - 4 Chức năng "Ăn điểm"
Đây là phần quan trọng nhất để đạt điểm cao (9.0+).

#### 1. Xử lý Đặt lịch (Appointment Service)
*   **Hiện tại:** Check `count > 0` rồi insert -> **Sai** (Bị Race Condition khi 2 người bấm cùng lúc).
*   **Nâng cấp:** Chuyển logic `INSERT` vào **Stored Procedure** SQL.
    *   Bắt buộc dùng: `SET TRANSACTION ISOLATION LEVEL SERIALIZABLE`.
    *   Test: Demo 2 request song song, 1 cái thành công, 1 cái báo lỗi.

#### 2. Cây Gia Phả (Genealogy Service)
*   **Hiện tại:** Không có.
*   **Nâng cấp:** Viết API `GET /api/patients/{id}/ancestors`.
    *   Dùng **SQL Recursive CTE** (`WITH RECURSIVE`).
    *   Trả về danh sách Cha, Ông, Cụ... từ `ParentID`.

#### 3. Bệnh Án Linh Hoạt (Clinical Service)
*   **Hiện tại:** Lưu vào các bảng chi tiết cứng nhắc.
*   **Nâng cấp:** Khi bác sĩ nhập bệnh án:
    *   Thông tin hành chính -> Lưu SQL Transaction.
    *   Thông tin lâm sàng/ghi chú -> Push vào mảng `events` trong MongoDB.

#### 4. Báo Cáo Phân Tích (Analytics Service)
*   **Hiện tại:** Count/Sum SQL đơn giản.
*   **Nâng cấp:** Thống kê Xu hướng bệnh (Ví dụ: "Đếm số ca Sốt xuất huyết theo tháng").
    *   Dùng **MongoDB Aggregation Pipeline**: `$match` (lọc bệnh), `$unwind` (bung mảng event), `$group` (đếm).

### D. Workflow & Automation
*   **Hiện tại:** Dựa nhiều vào thao tác tay của y tá/thu ngân.
*   **Nâng cấp:**
    *   **Auto-Queue:** Tự động xếp hàng ưu tiên (Cấp cứu/Hẹn trước).
    *   **Auto-Bill:** Tự động tạo hóa đơn khi kê đơn.
    *   **Inventory Lock:** Trừ kho ngay khi kê đơn (Transaction).

---

## 3. Khuyến Nghị Lộ Trình (Roadmap)

1.  **Tuần 1: Infrastructure & Refactoring**
    *   Cài MongoDB.
    *   Refactor/Clean bảng SQL `BenhNhan`.
    *   Setup Project Structure (Repository Pattern cho MongoDB).

2.  **Tuần 2: Core Logic (The "Big 4")**
    *   Implement Stored Proc `sp_BookAppointment` (Serializable).
    *   Implement Recursive Query `GetAncestors`.
    *   Design & Implement MongoDB `Histories` Collection.
    *   Build Aggregation Endpoint.

3.  **Tuần 3: Integration & UI Update**
    *   Sửa màn hình Bác sĩ (Load history từ Mongo).
    *   Sửa màn hình Đặt lịch.
    *   Thêm Tab "Gia phả" và "Báo cáo" vào UI.

4.  **Tuần 4: Testing & Defense Prep**
    *   Quay Video demo Race Condition.
    *   Viết báo cáo giải trình (Tại sao chọn Polyglot?).

---

## 4. Kết Luận
Dự án hiện tại là một nền tảng tốt (Base), nhưng để đạt yêu cầu Topic 9 HealthCare+, cần **thay đổi tư duy từ "Lưu trữ bảng" (Relational) sang "Lưu trữ đa hình" (Polyglot)**. Các chức năng hiển thị (UI) có thể giữ nguyên 70%, nhưng Backend và Database cần đập đi xây lại khoảng 40-50% (đặc biệt là phần xử lý dữ liệu bệnh án và đặt lịch).
