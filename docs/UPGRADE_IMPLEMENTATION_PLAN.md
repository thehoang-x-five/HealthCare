# Kế Hoạch Triển Khai Nâng Cấp HealthCare+ (Verified Upgrade Plan)

> **Đã đối chiếu với: Source code + Tất cả sơ đồ UML + DB_DESIGN_DEFENSE.md**
> Chỉ liệt kê những gì THỰC SỰ cần làm. Không lặp lại tính năng đã có.

---

## Giai Đoạn 1: Hạ Tầng (Infrastructure)

### 1.1 Cài MongoDB
- [ ] Cài **MongoDB Community Server** (hoặc Atlas).
- [ ] Thêm NuGet `MongoDB.Driver` vào `HealthCare.csproj`.
- [ ] Cấu hình `appsettings.json`:
  ```json
  "MongoDb": { "ConnectionString": "mongodb://localhost:27017", "DatabaseName": "healthcare_plus" }
  ```
- [ ] Tạo `MongoDbContext.cs` (Singleton, DI Registration).

### 1.2 Migration SQL — Thêm cột/bảng mới (theo ERD_Diff)
- [ ] **`BenhNhan`**: Thêm `MaCha` (VARCHAR 20 FK self-ref NULLABLE), `MaMe` (VARCHAR 20 FK self-ref NULLABLE), `CCCD` (VARCHAR 12 UNIQUE), `NgayTao`, `NgayCapNhat`.
- [ ] **`KetQuaDichVu`**: Thêm `LoaiKetQua` (Enum), `KetLuanChuyen`, `GhiChu`, `TepDinhKem` (JSON), `ThoiGianChot`. Xóa `NoiDungKetQua`.
- [ ] **`PhieuChanDoanCuoi`**: Thêm `MaICD10`, `NgayTaiKham`, `GhiChuTaiKham`, `ThoiGianTao`, `ThoiGianCapNhat`.
- [ ] **`HoaDonThanhToan`**: Thêm `SoTienPhaiTra`, `MaGiaoDich`, `ThoiGianHuy`, `MaNhanSuHuy`. Mở rộng `PhuongThucThanhToan` → `ENUM(tien_mat, the, chuyen_khoan, vietqr)`.
- [ ] **`DonThuoc`**: Thêm `ThoiGianThanhToan`, `ThoiGianPhat`, `MaNhanSuPhat`.
- [ ] **`ChiTietDonThuoc`**: Thêm `LieuDung`, `TanSuatDung` (Enum), `SoNgayDung`, `GhiChu`.
- [ ] **`HangDoi`**: Thêm `SoLanGoi`, `ThoiGianGoiGanNhat`.
- [ ] **`LuotKhamBenh`**: Thêm `ThoiGianThucTe`, `SinhHieuTruocKham` (JSON), `GhiChu`.
- [ ] **ENTITY MỚI `LichSuXuatKho`**: Tạo entity + table `lich_su_xuat_kho` (MaThuoc, MaDonThuoc, MaNhanSuXuat, LoaiGiaoDich, SoLuong, SoLuongConLai).
- [ ] **ENTITY MỚI `ThongBaoMau`**: Tạo entity + table `thong_bao_mau` (MaMau, TenMau, NoiDungMau, BienDong).
- [ ] Chạy `dotnet ef migrations add HealthCarePlusUpgrade`.
- [ ] **CHƯA XÓA 8 cột y tế `BenhNhan`** — chờ MongoDB hoạt động ổn định.

### 1.3 DB Constraints — CHECK + TRIGGER (Defense in Depth)

> **Nguyên tắc:** Giữ code-first ở BE cho hiệu năng/UX. Thêm DB constraints làm **tầng bảo vệ cuối cùng** cho Data Integrity. Xem chi tiết: `DB_DESIGN_DEFENSE.md` mục 4.12.

- [ ] **CHECK** `kho_thuoc`: `CHECK (SoLuong >= 0)` — không cho tồn kho âm.
- [ ] **CHECK** `lich_hen_kham`: `CHECK (TrangThai IN ('dang_cho','da_xac_nhan','da_checkin','da_huy'))` — chỉ cho phép 4 giá trị.
- [ ] **CHECK** `luot_kham_benh`: `CHECK (TrangThai IN ('dang_thuc_hien','hoan_tat','da_huy'))` — 3 giá trị.
- [ ] **CHECK** `don_thuoc`: `CHECK (TrangThai IN ('da_ke','cho_phat','da_phat','da_huy'))` — 4 giá trị.
- [ ] **CHECK** `hoa_don_thanh_toan`: `CHECK (TrangThai IN ('chua_thu','da_thu','da_huy'))` — 3 giá trị *(dùng `chua_thu` đã có trong StatusEnums.cs)*.
- [ ] **TRIGGER** `tr_LichHen_ValidateTransition`: Kiểm tra `OLD.TrangThai` → `NEW.TrangThai` hợp lệ (VD: da_huy → * = REJECT).
- [ ] **TRIGGER** `tr_KhoThuoc_PreventNegative`: Kiểm tra `NEW.SoLuong >= 0` trước UPDATE (bổ sung cho CHECK).
- [ ] **TRIGGER** `tr_DonThuoc_RollbackKho`: Khi `NEW.TrangThai = 'da_huy'` → tự động hoàn `SoLuong` vào `kho_thuoc`.

---

## Giai Đoạn 2: 4 Chức Năng Bắt Buộc ("The Big 4")

### 2.1 Đặt Lịch SERIALIZABLE (SQL Stored Procedure)

> **Defense in Depth:** GIỮ NGUYÊN `AppointmentService.FindConflictsForConfirmedAsync()` ở BE (Tầng 1).
> Thêm SP `sp_BookAppointment` ở DB (Tầng 2). Cả 2 tầng cùng kiểm tra trùng lịch.
> - BE kiểm tra trước → lọc 99% lỗi, trả 400 nhanh, UX tốt.
> - DB kiểm tra cuối → đảm bảo tuyệt đối, chống Race Condition.

> **File KHÔNG XÓA:** `AppointmentService.cs` line 575-610 (`FindConflictsForConfirmedAsync`)
> **File cần thêm:** Stored Procedure + sửa `TaoLichHenAsync` wrap SP call

- [ ] Viết Stored Procedure `sp_BookAppointment`:
  ```sql
  CREATE PROCEDURE sp_BookAppointment(
    IN p_MaBenhNhan VARCHAR(20), IN p_NgayHen DATE,
    IN p_GioHen TIME, IN p_MaLichTruc VARCHAR(20), ...
  )
  BEGIN
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    START TRANSACTION;
    -- Check overlap: NgayHen + GioHen + MaLichTruc + ThoiLuongPhut
    IF EXISTS(SELECT 1 FROM lich_hen_kham WHERE ...) THEN
      ROLLBACK; SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Trùng lịch';
    ELSE
      INSERT INTO lich_hen_kham(...); COMMIT;
    END IF;
  END
  ```
- [ ] Cập nhật `TaoLichHenAsync()` → gọi SP qua `_context.Database.ExecuteSqlRawAsync("CALL sp_BookAppointment(...)")`. **GIỮ NGUYÊN** `FindConflicts` ở trước làm pre-check.
- [ ] **Test Race Condition:** 2+ request đồng thời → chỉ 1 thành công.

### 2.2 Pha Hệ Di Truyền (SQL Recursive CTE)

> **File cần tạo:** `Services/PatientManagement/GenealogyService.cs`, `Controllers/GenealogyController.cs`

- [ ] Viết SQL CTE (từ DEFENSE 4.1 + ERD_02):
  ```sql
  WITH RECURSIVE PhaHe AS (
    SELECT * FROM benh_nhan WHERE MaBenhNhan = @id
    UNION ALL
    SELECT bn.* FROM benh_nhan bn
    JOIN PhaHe ph ON bn.MaBenhNhan = ph.MaCha
       OR bn.MaBenhNhan = ph.MaMe
  )
  SELECT * FROM PhaHe;
  ```
- [ ] API `GET /api/patients/{id}/genealogy` → trả cây pha hệ.
- [ ] API `POST /api/patients/{id}/link-parent` → liên kết MaCha/MaMe.
- [ ] API `GET /api/patients/{id}/family-diseases` → tiền sử bệnh gia đình (UC23.5).

### 2.3 Lịch Sử Khám MongoDB (Schema Evolution)

> **File cần sửa:** `Services/OutpatientCare/ClinicalService.cs`, `ClsService.cs`, `HistoryService.cs`
>
> **LƯU Ý:** Hệ thống đã có `HistoryService` (653 dòng) + `LuotKhamBenh` (SQL). MongoDB GHI THÊM (One-way sync), KHÔNG thay thế SQL.
> **LƯU Ý:** MongoDB dùng **flat document** (1 document = 1 event), KHÔNG dùng embedded array.

- [ ] Tạo `IMongoHistoryRepository.cs` + `MongoHistoryRepository.cs`.
- [ ] Khi `ClinicalService.TaoChanDoanCuoiAsync()` hoàn tất → push 1 document `event_type: "kham_lam_sang"` vào MongoDB (gồm sinh_hieu, chan_doan, huong_xu_tri, loi_khuyen).
- [ ] Khi `ClsService` lưu kết quả CLS → push 1 document `event_type: "xet_nghiem"` hoặc `"chan_doan_hinh_anh"` (gồm chi_so[], ket_luan, files[]).
- [ ] Khi `PharmacyService` phát đơn → push 1 document `event_type: "don_thuoc"`.
- [ ] Khi `BillingService` thu tiền → push 1 document `event_type: "thanh_toan"`.
- [ ] Sửa `ClinicalService.TaoPhieuKhamAsync` (line 134-142): Ghi `medicalProfile` vào MongoDB thay SQL.
- [ ] API `GET /api/patients/{id}/medical-history?type=...&from=...&to=...` → query MongoDB.
- [ ] **Demo Schema Evolution:** Thêm event `tiem_vac_xin` → bản ghi cũ không lỗi, không cần migration.
- [ ] Viết Migration Script: Chuyển 8 cột y tế SQL → MongoDB `medicalProfile` cho BN hiện có.
- [ ] Sau migration: Xóa 8 cột khỏi `BenhNhan.cs` Entity.

### 2.4 Analytics MongoDB (Aggregation Pipeline)

> **File cần tạo:** `Services/Report/AnalyticsService.cs`, `Controllers/AnalyticsController.cs`

- [ ] Viết Aggregation Pipeline (theo ERD_06):
  ```javascript
  // Thống kê chỉ số bất thường
  db.medical_histories.aggregate([
    { $match: { event_type: "xet_nghiem" } },
    { $unwind: "$data.chi_so" },
    { $match: { "data.chi_so.bat_thuong": true } },
    { $group: { _id: "$data.chi_so.ten", count: { $sum: 1 } } }
  ])
  ```
- [ ] API `GET /api/analytics/abnormal-stats` → chỉ số XN bất thường.
- [ ] API `GET /api/analytics/disease-trends?from=...&to=...` → thống kê nhóm bệnh (ICD) (UC91.6).
- [ ] API `GET /api/analytics/popular-drugs` → thuốc hay dùng (UC94.5).

---

## Giai Đoạn 3: Tính Năng Bổ Sung

### 3.1 Audit Logs (MongoDB)
- [ ] Tạo `IAuditLogRepository.cs` + `MongoAuditLogRepository.cs`.
- [ ] Tạo TTL Index: tự xóa sau 365 ngày.
- [ ] Middleware/Interceptor: Tự ghi audit log cho CREATE/UPDATE/DELETE.

### 3.2 LichSuXuatKho
- [ ] Tạo `LichSuXuatKhoService.cs`.
- [ ] Tích hợp vào `PharmacyService.XuatThuocAsync()` — ghi `LichSuXuatKho` khi phát thuốc.

### 3.3 VietQR (Tùy chọn)
- [ ] Mở rộng `HoaDonThanhToan.PhuongThucThanhToan` enum (code hiện tại chỉ `"tien_mat"`).
- [ ] Tạo `BankingService.cs` → Generate QR theo chuẩn VietQR/Napas.
- [ ] API `POST /api/billing/{id}/generate-qr`.

### 3.4 Phân Quyền Backend (RequireRole)

> **Vấn đề:** Nhiều Controller hiện chỉ có `[Authorize]` chung (ai đăng nhập cũng được), chưa phân biệt vai trò theo UC_00.

#### 3.4.1 `MasterDataController.cs` — Admin mới được CUD
- [ ] Giữ `[Authorize]` cho các endpoint **GET** (tất cả vai trò xem được Khoa/Phòng/DịchVụ/Thuốc/Lịch trực).
- [ ] Thêm `[RequireRole("admin")]` trên các endpoint **POST/PUT/DELETE** (Thêm/Sửa/Xóa Khoa, Phòng, Dịch vụ, Thuốc, Lịch trực).
- [ ] Theo UC_03: Admin=CRUD, Others=Read only.

#### 3.4.2 `PatientsController.cs` — Cho Bác sĩ quyền xem
- [ ] Hiện tại: `[RequireRole("y_ta")]` trên endpoint tạo/sửa BN → giữ nguyên.
- [ ] Thêm: Cho phép `"bac_si"` gọi endpoint **GET** xem thông tin BN (hiện chỉ y_ta mới xem được).
- [ ] Theo UC_04: Y tá HC = CRUD, Bác sĩ = Xem.

#### 3.4.3 `HistoryController.cs` — Giới hạn cho Y tá HC + Bác sĩ
- [ ] Thêm `[RequireRole("y_ta", "bac_si")]` thay vì `[Authorize]` chung.
- [ ] KTV không cần xem lịch sử khám.

#### 3.4.4 `ReportsController.cs` — Phân quyền theo loại báo cáo
- [ ] Endpoint báo cáo doanh thu: `[RequireRole("admin", "y_ta_hanh_chinh")]`.
- [ ] Endpoint báo cáo lượt khám: `[RequireRole("admin", "bac_si")]`.
- [ ] Endpoint báo cáo kho thuốc: `[RequireRole("admin", "y_ta_hanh_chinh")]`.
- [ ] Endpoint báo cáo nhân viên: `[RequireRole("admin")]` only.
- [ ] Theo UC_11: Admin=full, Y tá HC=doanh thu+kho, BS=lượt khám.

#### 3.4.5 Tạo Controller mới cho Admin
- [ ] Tạo `AdminController.cs` với `[RequireRole("admin")]`.
- [ ] API `GET /api/admin/users` — Lấy danh sách nhân viên (UC10.1).
- [ ] API `POST /api/admin/users` — Thêm nhân viên mới (UC10.5-10.9).
- [ ] API `PUT /api/admin/users/{id}` — Sửa thông tin nhân viên (UC10.10).
- [ ] API `PUT /api/admin/users/{id}/status` — Khóa/Mở khóa tài khoản (UC10.11).
- [ ] API `POST /api/admin/users/{id}/reset-password` — Reset mật khẩu (UC10.12).

### 3.5 Luồng Trạng Thái — DB + BE + FE (Unified)

> **Vấn đề cốt lõi:** Hệ thống chỉ cài **happy path** (luôn hoàn thành). Các status `da_huy` tồn tại nhưng **không có code xử lý** ở cả 3 layer.
>
> **Nguyên tắc:** Mỗi mục dưới đây bao gồm cả 3 layer (DB Entity → BE Service → FE Component) để triển khai theo **feature slice** — không làm từng layer riêng.

#### 3.5.1 `LichHenKham` — Xác nhận + Hủy lịch hẹn  *(~1h)*
- [ ] **BE** `AppointmentService.UpdateAppointmentStatus`: Thêm **transition validation**:
  - Chỉ cho phép: `dang_cho` → `da_xac_nhan` | `da_huy`
  - Chỉ cho phép: `da_xac_nhan` → `da_checkin` | `da_huy`
  - Không cho: `da_checkin` → bất kỳ (đã vào khám)
  - Không cho: `da_huy` → bất kỳ (đã hủy = terminal)
- [ ] **FE** `Appointments.jsx`: Thêm nút **"Xác nhận"** (dang_cho → da_xac_nhan) + nút **"Hủy"** (→ da_huy) trên mỗi card lịch hẹn, có confirm dialog.
- [ ] **FE** `appointments.js`: Thêm constant `APPT_STATUS_LABEL` cho trạng thái mới nếu cần.

#### 3.5.2 `BenhNhan.TrangThaiHomNay` — BN bỏ về giữa chừng  *(~1h)*
- [ ] **BE** `PatientService.UpdateDailyStatus`: Thêm **transition matrix** — cho phép `da_huy` từ mọi trạng thái chưa hoàn tất (cho_tiep_nhan, cho_kham, dang_kham, cho_xu_ly).
- [ ] **FE** `Patients.jsx` hoặc `PatientModal.jsx`: Thêm nút **"BN bỏ về"** với confirm dialog → gọi `updatePatientStatus({id, status: "da_huy"})`.
- [ ] **FE** `patients.js`: `STATUSES.CANCELLED` đã có `da_huy` — OK, kiểm tra status badge hiện đúng (đỏ).

#### 3.5.3 `LuotKhamBenh` — Hủy lượt khám  *(~1.5h)*
- [ ] **DB** Entity `LuotKhamBenh.cs`: Thêm `da_huy` vào comment enum (`dang_thuc_hien, hoan_tat, da_huy`).
- [ ] **BE** `ClinicalService`: Tạo method `HuyLuotKhamAsync(maLuotKham)`:
  - Set `luot.TrangThai = "da_huy"`.
  - Set `hangDoi.TrangThai = "da_phuc_vu"` (giải phóng vị trí hàng đợi).
  - Set `benhNhan.TrangThaiHomNay = "da_huy"`.
- [ ] **BE** `ClinicalController`: Thêm endpoint `PUT /api/clinical/visits/{id}/cancel`.
- [ ] **FE** `Examination.jsx`: Thêm nút **"Hủy lượt khám"** trên bảng hàng đợi (chỉ hiện cho lượt đang `cho_goi` hoặc `dang_goi`).

#### 3.5.4 `PhieuKhamLamSang` — Hủy phiếu LS  *(~0.5h)*
- [ ] **BE** `ClinicalService.UpdateExamination`: Thêm validate — chỉ cho `da_huy` từ `da_lap` hoặc `dang_thuc_hien` (không cho hủy sau khi đã lập chẩn đoán).
- [ ] **FE** `Examination.jsx`: Thêm nút **"Hủy phiếu"** khi phiếu ở `da_lap` (chưa bắt đầu khám).

#### 3.5.5 `PhieuKhamCanLamSang` — Hủy phiếu CLS  *(~0.5h)*
- [ ] **DB** Entity `PhieuKhamCanLamSang.cs`: Cập nhật comment cho khớp `StatusEnums.cs` *(`da_huy` ĐÃ CÓ trong `TrangThaiPhieuKhamCls`)*.
- [ ] **BE** `ClsService`: Tạo method `HuyPhieuClsAsync(maPhieu)` — set `da_huy` + rollback `ChiTietDichVu.TrangThai`.
- [ ] **FE** `Examination.jsx` (tab CLS): Thêm nút "Hủy phiếu CLS" trên danh sách phiếu đã tạo.

#### 3.5.6 `DonThuoc` — Hủy đơn + Hoàn kho  *(~1h)*
- [ ] **DB** Entity `DonThuoc.cs`: Cập nhật comment cho khớp `StatusEnums.cs` *(`da_huy` ĐÃ CÓ trong `TrangThaiDonThuoc`)*.
- [ ] **BE** `PharmacyService`: Tạo method `HuyDonThuocAsync(maDonThuoc)`:
  - Chỉ cho hủy khi `da_ke` hoặc `cho_phat` (chưa phát thuốc).
  - Nếu đã trừ kho → **rollback** `KhoThuoc.SoLuong` += số lượng đã trừ.
  - Set `donThuoc.TrangThai = "da_huy"`.
- [ ] **BE** `PharmacyController`: Thêm endpoint `PUT /api/pharmacy/prescriptions/{id}/cancel`.
- [ ] **FE** `Prescriptions.jsx`: Thêm nút **"Hủy đơn"** trên tab "Đã kê" + "Chờ phát". Thêm tab "Đã hủy" để xem đơn đã hủy.

#### 3.5.7 `HoaDonThanhToan` — Luồng thanh toán đúng  *(~0.5h)*
- [ ] **DB** Entity `HoaDonThanhToan.cs`: Cập nhật comment cho khớp `StatusEnums.cs` *(`chua_thu` ĐÃ CÓ trong `TrangThaiHoaDon`)*. Đổi default: `"chua_thu"` thay vì `"da_thu"`.
- [ ] **BE** `BillingService`: Validate transition: `chua_thu` → `da_thu` | `da_huy`. Không cho `da_thu` → `da_huy` (đã thu = final).
- [ ] **FE** Billing UI: Thêm nút **"Thu tiền"** (chua_thu → da_thu) + nút "Hủy" (→ da_huy).

---

## Giai Đoạn 4: Frontend Integration

### 4.1 Phân Quyền Sidebar (BẮT BUỘC)

> **Vấn đề hiện tại:** `Sidebar.jsx` (line 7-18) hiện 10 menu cho TẤT CẢ user, không phân biệt vai trò.
> **Giải pháp:** Dùng `permissions.js` đã có sẵn (`canManageReception`, `canManageClinical`, `canManageCls`, `isAdmin`).

- [ ] Sửa `Sidebar.jsx`: Thay `const links = [...]` cố định → lọc theo vai trò user đang đăng nhập.
- [ ] Lấy `user` từ AuthStore/Context, dùng các helper trong `permissions.js` để lọc.
- [ ] Quy tắc lọc cụ thể (theo bảng UC_00):
  - **Tổng quan** + **Thông báo** + **Cài đặt**: Hiện cho **tất cả** vai trò.
  - **Lịch hẹn**: Chỉ hiện khi `canManageReception(user)` = true (Y tá HC + Admin).
  - **Bệnh nhân**: Chỉ hiện khi `canManageReception(user) || isDoctor(user)` = true.
  - **Khám bệnh**: Hiện cho tất cả **TRỪ** Admin (Admin không cần vào hàng đợi).
  - **Khoa phòng**: Hiện cho **tất cả** (Admin=CRUD, còn lại=xem).
  - **Nhân sự**: Chỉ hiện khi `isAdmin(user)` = true.
  - **Đơn thuốc**: Chỉ hiện khi `canManageReception(user) || isDoctor(user)` = true.
  - **Lịch sử**: Chỉ hiện khi `canManageReception(user) || isDoctor(user)` = true.
  - **Báo cáo**: Chỉ hiện khi `isAdmin(user) || canManageReception(user) || isDoctor(user)` = true.

### 4.2 Trang Quản Trị Admin (UC10 — Quản lý Người dùng)

> **Hiện tại:** Không có trang Admin riêng. Admin đăng nhập thấy cùng giao diện với Y tá.
> **Cần tạo:** Trang `/admin/users` cho phép Admin CRUD nhân viên.

- [ ] Tạo route `/admin/users` (chỉ Admin truy cập được).
- [ ] Tạo component `AdminUsers.jsx` với các chức năng:
  - Xem danh sách nhân viên (UC10.1) — bảng có tìm kiếm + lọc theo vai trò/khoa (UC10.2-10.3).
  - Xem chi tiết nhân viên (UC10.4).
  - Thêm nhân viên mới (UC10.5-10.9): Nhập thông tin, tạo tài khoản, phân vai trò, gán khoa/phòng.
  - Sửa thông tin nhân viên (UC10.10).
  - Khóa/Mở khóa tài khoản (UC10.11).
  - Reset mật khẩu (UC10.12).
- [ ] Thêm link "Quản trị" vào Sidebar (chỉ hiện khi `isAdmin(user)`).

### 4.3 Màn Hình Bệnh Nhân (`Patients.jsx`)
- [ ] Tab **"Pha hệ"** (UC22.5) → Cây pha hệ interactive + Liên kết Cha/Mẹ (UC23.1-23.6).
- [ ] Tab **"Lịch sử khám"** (UC22.3) → Timeline events từ MongoDB API (UC24.1-24.6).
- [ ] Tab **"Lịch sử giao dịch"** (UC22.4) → Danh sách hóa đơn (UC25.1-25.5).

### 4.4 Màn Hình Bác Sĩ (`Examination.jsx`)
- [ ] Load bệnh án từ MongoDB API thay vì SQL.
- [ ] Hiển thị lịch sử khám cũ (timeline + chi tiết XN).

### 4.5 Màn Hình Quản Trị — Analytics
- [ ] Dashboard **"Xu hướng bệnh tật"** → Biểu đồ từ API Analytics.
- [ ] Dashboard **"Thuốc hay dùng"** → Biểu đồ từ API popular-drugs.

### 4.6 Màn Hình Thanh Toán
- [ ] Hỗ trợ chọn phương thức thanh toán (Tiền mặt / VietQR).
- [ ] Hiển thị mã QR động cho BN quét (UC82.5-82.9).

---

## Giai Đoạn 5: Test & Đóng Gói

### 5.1 Test Bắt Buộc (Demo cho Giảng viên)
- [ ] **Race Condition Test:** 10 request đặt lịch cùng lúc → chỉ 1 thành công.
- [ ] **Schema Evolution Test:** Thêm `tiem_vac_xin` → đọc bản ghi cũ không lỗi.
- [ ] **Recursive CTE Test:** Truy vấn pha hệ 3-4 đời.
- [ ] **Aggregation Test:** Top 5 chỉ số XN bất thường, Top 5 bệnh phổ biến.

### 5.2 Tài Liệu Nộp
- [ ] Báo cáo PDF (ERD mới, JSON Schema, Kiến trúc Polyglot).
- [ ] AI Audit Log (phụ lục: các prompt đã dùng + lỗi AI đã sửa).
- [ ] Video demo 4 chức năng.

---

## Những Gì KHÔNG CẦN Làm (Đã Có Sẵn)

> Các tính năng sau đã triển khai đầy đủ. **Không cần refactor:**

- ✅ Auto-Queue (`QueueService.ThemVaoHangDoiAsync`)
- ✅ Auto-Billing (`ClinicalService` line 352-401)
- ✅ Priority 4-Tier (`QueueService.TinhDoUuTien`)
- ✅ Inventory Transaction (`PharmacyService` line 310-388)
- ✅ SignalR Real-time (Queue, Exam, Dashboard, Notification)
- ✅ Notification System (`NotificationService`)
- ✅ Daily Reset (`DailyResetService`)
- ✅ Visit History SQL (`HistoryService` 653 dòng + `HistoryController` + `LuotKhamBenh`)
- ✅ Conflict Detection Logic (`AppointmentService.FindConflictsForConfirmedAsync` — logic đúng, chỉ cần wrap vào SP)
