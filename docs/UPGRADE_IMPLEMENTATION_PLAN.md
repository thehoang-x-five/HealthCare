# Kế Hoạch Triển Khai Nâng Cấp HealthCare+ (Verified Upgrade Plan)

> **Đã đối chiếu với: Source code + Tất cả sơ đồ UML + DB_DESIGN_DEFENSE.md**
> Chỉ liệt kê những gì THỰC SỰ cần làm. Không lặp lại tính năng đã có.
>
> **Cập nhật lần cuối: 2026-04-02 — Sau khi hoàn tất Week 1-2-3**

---

## Giai Đoạn 1: Hạ Tầng (Infrastructure) — ✅ ĐÃ XONG (Week 1)

### 1.1 Cài MongoDB — ✅
- [x] Cài **MongoDB Community Server**.
- [x] Thêm NuGet `MongoDB.Driver` vào `HealthCare.csproj`.
- [x] Cấu hình `appsettings.json`:
  ```json
  "MongoDb": { "ConnectionString": "mongodb://localhost:27017", "DatabaseName": "healthcare_plus" }
  ```
- [x] Tạo `MongoDbContext.cs` (Singleton, DI Registration).

### 1.2 Migration SQL — Thêm cột/bảng mới (theo ERD_Diff) — ✅
- [x] **`BenhNhan`**: Thêm `MaCha`, `MaMe`, `CCCD`, `NgayTao`, `NgayCapNhat`.
- [x] **`KetQuaDichVu`**: Thêm `LoaiKetQua`, `KetLuanChuyen`, `GhiChu`, `TepDinhKem` (JSON), `ThoiGianChot`.
- [ ] ~~Xóa `NoiDungKetQua`~~ → **🔴 CHƯA XÓA** — xem Giai Đoạn 3.6
- [x] **`PhieuChanDoanCuoi`**: Thêm `MaICD10`, `NgayTaiKham`, `GhiChuTaiKham`, `ThoiGianTao`, `ThoiGianCapNhat`.
- [x] **`HoaDonThanhToan`**: Thêm `SoTienPhaiTra`, `MaGiaoDich`, `ThoiGianHuy`, `MaNhanSuHuy`. Mở rộng `PhuongThucThanhToan`.
- [x] **`DonThuoc`**: Thêm `ThoiGianThanhToan`, `ThoiGianPhat`, `MaNhanSuPhat`.
- [x] **`ChiTietDonThuoc`**: Thêm `LieuDung`, `TanSuatDung`, `SoNgayDung`, `GhiChu`.
- [x] **`HangDoi`**: Thêm `SoLanGoi`, `ThoiGianGoiGanNhat`.
- [x] **`LuotKhamBenh`**: Thêm `ThoiGianThucTe`, `SinhHieuTruocKham` (JSON), `GhiChu`.
- [x] **ENTITY MỚI `LichSuXuatKho`**: Entity + table `lich_su_xuat_kho`.
- [x] **ENTITY MỚI `ThongBaoMau`**: Entity + table `thong_bao_mau`.
- [x] Chạy migration thành công.
- [x] **CHƯA XÓA 8 cột y tế `BenhNhan`** — chờ MongoDB xong mới xóa (đúng plan).

### 1.3 DB Constraints — CHECK + TRIGGER (Defense in Depth) — ✅
- [x] **CHECK** `kho_thuoc`: `CHECK (SoLuong >= 0)`.
- [x] **CHECK** `lich_hen_kham`: `CHECK (TrangThai IN (...))`.
- [x] **CHECK** `luot_kham_benh`, `don_thuoc`, `hoa_don_thanh_toan`.
- [x] **TRIGGER** `tr_LichHen_ValidateTransition`.
- [x] **TRIGGER** `tr_KhoThuoc_PreventNegative`.
- [x] **TRIGGER** `tr_DonThuoc_RollbackKho`.

---

## Giai Đoạn 2: 4 Chức Năng Bắt Buộc ("The Big 4") — ✅ ĐÃ XONG (Week 1-3)

### 2.1 Đặt Lịch SERIALIZABLE (SQL Stored Procedure) — ✅ W1
- [x] SP `sp_BookAppointment` với `SERIALIZABLE`.
- [x] `TaoLichHenAsync()` → gọi SP.
- [x] **GIỮ NGUYÊN** `FindConflicts` ở trước làm pre-check.

### 2.2 Pha Hệ Di Truyền (SQL Recursive CTE) — ✅ W2
- [x] `GenealogyService.cs` + `GenealogyController.cs`.
- [x] SQL CTE recursive.
- [x] API `GET /api/patients/{id}/genealogy`.
- [x] API `POST /api/patients/{id}/link-parent`.
- [x] API `GET /api/patients/{id}/family-diseases`.

### 2.3 Lịch Sử Khám MongoDB (Schema Evolution) — ✅ W2 (dual-write)
- [x] Tạo `IMongoHistoryRepository.cs` + `MongoHistoryRepository.cs`.
- [x] `ClinicalService.TaoChanDoanCuoiAsync()` → push `event_type: "kham_lam_sang"`.
- [x] `ClsService` lưu kết quả CLS → push `event_type: "xet_nghiem"` / `"chan_doan_hinh_anh"`.
- [x] `PharmacyService` phát đơn → push `event_type: "don_thuoc"`.
- [x] `BillingService` thu tiền → push `event_type: "thanh_toan"`.
- [ ] ~~Sửa `ClinicalService.TaoPhieuKhamAsync`: Ghi `medicalProfile` vào MongoDB thay SQL~~ → **🟡 CHƯA** (xem 3.7)
- [x] API `GET /api/patients/{id}/medical-history?type=...&from=...&to=...` → query MongoDB.
- [ ] ~~Viết Migration Script: Chuyển 8 cột y tế SQL → MongoDB~~ → **🟡 CHƯA** (xem 3.7)
- [ ] ~~Sau migration: Xóa 8 cột khỏi `BenhNhan.cs`~~ → **🟡 CHƯA** (xem 3.7)

### 2.4 Analytics MongoDB (Aggregation Pipeline) — ✅ W3
- [x] `AnalyticsService.cs` + `AnalyticsController.cs`.
- [x] API `GET /api/analytics/abnormal-stats`.
- [x] API `GET /api/analytics/disease-trends`.
- [x] API `GET /api/analytics/popular-drugs`.

---

## Giai Đoạn 3: Tính Năng Bổ Sung — ✅ PHẦN LỚN ĐÃ XONG (Week 1-3)

### 3.1 Audit Logs (MongoDB) — ✅ W3
- [x] `IAuditLogRepository.cs` + `MongoAuditLogRepository.cs`.
- [x] TTL Index: tự xóa sau 365 ngày.
- [x] `AuditLogMiddleware`: Tự ghi audit log cho POST/PUT/DELETE.

### 3.2 LichSuXuatKho — ✅ W3
- [x] `LichSuXuatKhoService.cs`.
- [x] Tích hợp vào `PharmacyService.XuatThuocAsync()`.

### 3.3 VietQR (Tùy chọn) — 🟢 Chưa làm
- [ ] Mở rộng `HoaDonThanhToan.PhuongThucThanhToan` enum — Entity đã có sẵn enum mở rộng.
- [ ] Tạo `BankingService.cs` → Generate QR.
- [ ] API `POST /api/billing/{id}/generate-qr`.

### 3.4 Phân Quyền Backend (RequireRole) — ⏳ Week 4
- [ ] `MasterDataController.cs` — Admin mới được CUD.
- [ ] `PatientsController.cs` — Cho BS quyền GET.
- [ ] `HistoryController.cs` — Giới hạn cho Y tá HC + BS.
- [ ] `ReportsController.cs` — Phân quyền theo loại.
- [ ] Tạo `AdminController.cs`.

### 3.5 Luồng Trạng Thái — ✅ ĐÃ XONG (W1-W3)
- [x] 3.5.1 `LichHenKham` — Xác nhận + Hủy — ✅ W1
- [x] 3.5.2 `BenhNhan.TrangThaiHomNay` — BN bỏ về — ✅ W1
- [x] 3.5.3 `LuotKhamBenh` — Hủy lượt khám — ✅ W1
- [x] 3.5.4 `PhieuKhamLamSang` — Hủy phiếu LS — ✅ W1
- [x] 3.5.5 `PhieuKhamCanLamSang` — Hủy CLS — ✅ W3
- [x] 3.5.6 `DonThuoc` — Hủy đơn + Hoàn kho — ✅ W3
- [x] 3.5.7 `HoaDonThanhToan` — Thu tiền / Hủy — ✅ W3

### 3.6 🔴 Chuyển KetQuaDichVu.NoiDungKetQua SQL → MongoDB — CHƯA XONG

> **Thiết kế gốc**: `KetQuaDichVu = "Mục lục"` — chỉ giữ metadata, chi tiết → MongoDB (DEFENSE 4.10)
> **Hiện tại**: Dual-write hoạt động (SQL + MongoDB), nhưng FE/HistoryService vẫn ĐỌC từ SQL.

**Cần làm (3 bước)**:

#### Bước 1: Sửa ClsService — Chuyển nguồn đọc
- [ ] Khi tạo/cập nhật KQ CLS: chỉ ghi `KetLuanChuyen`, `TepDinhKem`, `ThoiGianChot` vào SQL.
- [ ] Chi tiết (`chi_so[]`, `mo_ta_hinh_anh`, `noi_dung`) → **chỉ ghi MongoDB** (giữ code dual-write hiện tại).
- [ ] Khi đọc KQ CLS (GetClsResultAsync, GetClsOrdersAsync): lấy chi tiết từ MongoDB thay vì `kq.NoiDungKetQua`.

#### Bước 2: Sửa HistoryService + ClsResultDto
- [ ] `HistoryService.cs` line 220: đọc chi tiết KQ từ MongoDB API thay vì `kq.NoiDungKetQua`.
- [ ] `ClsResultDto`: xóa field `NoiDungKetQua`, thay bằng `ChiTiet` (object từ MongoDB).
- [ ] `ClsResultCreateRequest`: giữ `NoiDungKetQua` cho input nhưng ClsService chỉ ghi MongoDB.

#### Bước 3: Xóa field + Migration
- [ ] Xóa `[Obsolete] NoiDungKetQua` khỏi `KetQuaDichVu.cs`.
- [ ] Sửa `DataSeed.cs`: bỏ seed `NoiDungKetQua`.
- [ ] Tạo migration `DROP COLUMN NoiDungKetQua`.

### 3.7 🟡 Chuyển 8 cột y tế BenhNhan → MongoDB — CHƯA XONG (Thấp)

> **Ưu tiên thấp** — không ảnh hưởng tính năng Week 4-5. Làm sau cùng.

- [ ] Migration script: chuyển 8 cột SQL → MongoDB `medicalProfile` document.
- [ ] Sửa `ClinicalService.TaoPhieuKhamAsync`: đọc profile từ MongoDB.
- [ ] Xóa 8 cột khỏi `BenhNhan.cs` + DROP columns.

### 3.8 ✅ Notification chi tiết 6 vai trò — ĐÃ XONG (W3)
- [x] `RealtimeService.BroadcastNotificationCreatedAsync`: route theo `y_ta_hanh_chinh`, `y_ta_cls`, `y_ta_phong_kham` thay vì gom `y_ta` chung.
- [x] `BillingService`: hóa đơn → `y_ta_hanh_chinh`.
- [x] `PharmacyService`: đơn thuốc → `y_ta_hanh_chinh`.
- [x] `ClsService`: phiếu CLS → `y_ta_cls`.
- [x] `NotificationService`: fix inbox query (broadcast notifications visible cho đúng vai trò).
- [x] FE `notifications.js`: gửi `bac_si`/`y_ta` thay vì `nhan_vien_y_te`.

---

## Giai Đoạn 4: Frontend Integration — ⏳ Week 4-5

### 4.1 Phân Quyền Sidebar (BẮT BUỘC) — ⏳ W4
- [ ] Sửa `Sidebar.jsx`: lọc theo vai trò.
- [ ] Dùng `permissions.js` helpers.

### 4.2 Trang Quản Trị Admin (UC10) — ⏳ W4
- [ ] Route `/admin/users`.
- [ ] Component `AdminUsers.jsx` CRUD.

### 4.3 Màn Hình Bệnh Nhân — ⏳ W4
- [ ] Tab Pha hệ.
- [ ] Tab Lịch sử khám (MongoDB Timeline).
- [ ] Tab Lịch sử giao dịch.

### 4.4 Màn Hình Bác Sĩ — ⏳ W4
- [ ] Load bệnh án từ MongoDB.
- [ ] Hiển thị timeline lịch sử.

### 4.5 Màn Hình Analytics — ⏳ W4
- [ ] Dashboard xu hướng bệnh tật.
- [ ] Dashboard thuốc hay dùng.

### 4.6 Màn Hình Thanh Toán — 🟢 Tùy chọn
- [ ] VietQR nếu có time.

---

## Giai Đoạn 5: Test & Đóng Gói — ⏳ Week 5

### 5.1 Test Bắt Buộc
- [ ] **Race Condition Test**.
- [ ] **Schema Evolution Test**.
- [ ] **Recursive CTE Test**.
- [ ] **Aggregation Test**.

### 5.2 Tài Liệu Nộp
- [ ] Báo cáo PDF.
- [ ] AI Audit Log.
- [ ] Video demo 4 chức năng.

---

## Những Gì KHÔNG CẦN Làm (Đã Có Sẵn)

> Các tính năng sau đã triển khai đầy đủ. **Không cần refactor:**

- ✅ Auto-Queue (`QueueService.ThemVaoHangDoiAsync`)
- ✅ Auto-Billing (`ClinicalService` line 352-401)
- ✅ Priority 4-Tier (`QueueService.TinhDoUuTien`)
- ✅ Inventory Transaction + Rollback (`PharmacyService` line 310-388)
- ✅ SignalR Real-time (Queue, Exam, Dashboard, Notification) — **phân loại 6 vai trò**
- ✅ Notification System (`NotificationService`) — **targeting chi tiết nurse_type**
- ✅ Daily Reset (`DailyResetService`)
- ✅ Visit History SQL (`HistoryService` 653 dòng + `HistoryController` + `LuotKhamBenh`)
- ✅ Conflict Detection Logic (`AppointmentService.FindConflictsForConfirmedAsync` — wrapped vào SP)
- ✅ Cancel workflows (HuyLuotKham, HuyPhieuCls, HuyDonThuoc, HuyHoaDon) — **rollback + notification**
- ✅ Analytics (AnalyticsService — MongoDB Aggregation + SQL stats)
- ✅ Audit Logs (AuditLogMiddleware + TTL 365 ngày)
- ✅ LichSuXuatKho (inventory transaction logging)
