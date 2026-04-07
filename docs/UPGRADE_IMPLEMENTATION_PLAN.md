# Kế Hoạch Triển Khai Nâng Cấp HealthCare+ (Verified Upgrade Plan)

> **Đã đối chiếu với: Source code + Tất cả sơ đồ UML + DB_DESIGN_DEFENSE.md**
> Chỉ liệt kê những gì THỰC SỰ cần làm. Không lặp lại tính năng đã có.
>
> **Cập nhật lần cuối: 2026-04-05 — Sau khi hoàn tất Week 1-2-3-4 (Full audit verified)**

---

> [!IMPORTANT]
> **Quyết định kiến trúc 2026-04-05:**
> - ❌ KHÔNG tách bảng `UserAccount` khỏi `NhanVienYTe` — Auth fields giữ nguyên trong NhanVienYTe
> - ❌ KHÔNG tạo trang `/user-management` riêng — Admin quản lý qua Staff page (toggle Card↔Table)
> - ✅ RBAC thực hiện qua `[RequireRole]` attribute + data-scoped permissions
> - ✅ Staff page dùng chung: Admin thấy auth fields + admin actions, Non-admin thấy HR cards read-only

---

## Giai Đoạn 1: Hạ Tầng (Infrastructure) — ✅ HOÀN TẤT (Week 1)

### 1.1 MongoDB — ✅
- [x] `MongoDB.Driver` NuGet + `MongoDbContext.cs` (Singleton DI)
- [x] `appsettings.json`: `MongoDb` section (Atlas connection)
- [x] 2 collections: `medical_histories`, `audit_logs`

### 1.2 SQL Migration — ✅
- [x] **`BenhNhan`**: +MaCha, +MaMe, +CCCD, +NgayTao, +NgayCapNhat
- [x] **`KetQuaDichVu`**: +LoaiKetQua, +KetLuanChuyen, +GhiChu, +TepDinhKem, +ThoiGianChot
- [x] **`PhieuChanDoanCuoi`**: +MaICD10, +NgayTaiKham, +GhiChuTaiKham, +timestamps
- [x] **`HoaDonThanhToan`**: +SoTienPhaiTra, +MaGiaoDich, +ThoiGianHuy, +MaNhanSuHuy
- [x] **`DonThuoc`**: +ThoiGianThanhToan, +ThoiGianPhat, +MaNhanSuPhat
- [x] **`ChiTietDonThuoc`**: +LieuDung, +TanSuatDung, +SoNgayDung, +GhiChu
- [x] **`HangDoi`**: +SoLanGoi, +ThoiGianGoiGanNhat
- [x] **`LuotKhamBenh`**: +ThoiGianThucTe, +SinhHieuTruocKham, +GhiChu
- [x] **ENTITY MỚI**: `LichSuXuatKho`, `ThongBaoMau`
- [x] Migration chạy thành công

### 1.3 DB Constraints — ✅
- [x] CHECK constraints: `kho_thuoc`, `lich_hen_kham`, `luot_kham_benh`, `don_thuoc`, `hoa_don_thanh_toan`
- [x] TRIGGER: `tr_LichHen_ValidateTransition`, `tr_KhoThuoc_PreventNegative`, `tr_DonThuoc_RollbackKho`

---

## Giai Đoạn 2: 4 Chức Năng Bắt Buộc — ✅ HOÀN TẤT (Week 1-3)

### 2.1 Đặt Lịch SERIALIZABLE — ✅ W1
- [x] SP `sp_BookAppointment` với `SERIALIZABLE` isolation
- [x] `TaoLichHenAsync()` → gọi SP, giữ `FindConflicts` pre-check

### 2.2 Pha Hệ Di Truyền — ✅ W2
- [x] `GenealogyService.cs` + `GenealogyController.cs` + SQL Recursive CTE
- [x] APIs: genealogy, link-parent, family-diseases

### 2.3 Lịch Sử Khám MongoDB — ✅ W2
- [x] `MongoHistoryRepository` — dual-write 5 event types
- [x] API `GET /api/patients/{id}/medical-history`

### 2.4 Analytics MongoDB — ✅ W3
- [x] `AnalyticsService.cs` + `AnalyticsController.cs`
- [x] APIs: abnormal-stats, disease-trends, popular-drugs

---

## Giai Đoạn 3: Tính Năng Bổ Sung — ✅ PHẦN LỚN HOÀN TẤT (Week 1-4)

### 3.1 Audit Logs (MongoDB) — ✅ W3
- [x] `AuditLogMiddleware` + `AuditLogRepository` + TTL 365 ngày

### 3.2 LichSuXuatKho — ✅ W3
- [x] `LichSuXuatKhoService.cs` tích hợp `PharmacyService`

### 3.3 VietQR — ✅ W4
- [x] `VietQRService.cs` (Services/Banking/) — VietQR.io API integration
- [x] `POST /api/billing/invoices/{id}/generate-qr` endpoint
- [x] `VietQRRequest` + `VietQRResponse` DTOs
- [x] `appsettings.json` → `VietQR` section (BankId, AccountNo, AccountName, BankName)
- [x] DI: `builder.Services.AddSingleton<VietQRService>()`
- [x] FE: `generateVietQR()` + `useGenerateVietQR()` hook (billing.js)

### 3.4 Phân Quyền — ✅ HOÀN TẤT (W4)

**Backend:**
- [x] `RequireRoleAttribute.cs` — declarative authorization
- [x] `[RequireRole]` on: `AdminController`, `ReportsController`, `MasterDataController`, `BillingController`
- [x] `AdminController.cs` — CRUD + Lock/Unlock + Reset Password (5 endpoints)
- [x] `AdminService.cs` — business logic for admin operations

**Frontend:**
- [x] `permissions.js` — 19 helpers: TAB_VISIBILITY + 15 action perms + canViewStaffAuth/canToggleStaffView/canLockUnlockStaff/canResetStaffPassword
- [x] `ProtectedRoute.jsx` — route-level guard
- [x] `ScopeBadge.jsx` — data scope badge UI (Tầng 6-7)
- [x] `enums.js` — 177 dòng constants centralized
- [x] RBAC Tầng 3-5 trên 6 page: Patients, Appointments, Examination, Reports, Prescriptions, Overview

### 3.5 Luồng Trạng Thái — ✅ HOÀN TẤT (W1-W3)
- [x] 3.5.1 `LichHenKham` — Xác nhận + Hủy — W1
- [x] 3.5.2 `BenhNhan.TrangThaiHomNay` — BN bỏ về — W1
- [x] 3.5.3 `LuotKhamBenh` — Hủy lượt khám — W1
- [x] 3.5.4 `PhieuKhamLamSang` — Hủy phiếu LS — W1
- [x] 3.5.5 `PhieuKhamCanLamSang` — Hủy CLS — W3
- [x] 3.5.6 `DonThuoc` — Hủy đơn + Hoàn kho — W3
- [x] 3.5.7 `HoaDonThanhToan` — Default `chua_thu`, Thu tiền, Hủy — W3-W4

### 3.6 Thanh toán Inline — ✅ W4
- [x] `PaymentConfirmRequest` DTO
- [x] `BillingService.XacNhanThanhToanAsync` — chua_thu → da_thu
- [x] `PUT /api/billing/invoices/{id}/confirm` endpoint
- [x] FE: `PaymentWizard.jsx` (4-step) + `PaymentStep.jsx` + `confirmInvoice` hook
- [x] Hủy hóa đơn: `PUT /cancel` + `useCancelInvoice` hook

### 3.7 Staff Management (Unified) — ✅ W4
- [x] `ViewToggle.jsx` — Card↔Table toggle (admin only)
- [x] `StaffTable.jsx` — table view + auth columns + ActionMenu (Lock/Unlock/Reset)
- [x] `Staff.jsx` rewritten — toggle, admin API integration, mutation hooks
- [x] `Sidebar.jsx` — QL Nhân viên link removed
- [x] `main.jsx` — `/admin/users` redirects to Staff page
- [x] Admin API hooks: `useAdminUsers`, `useUpdateUserStatus`, `useResetPassword`

### 3.8 Notification chi tiết 6 vai trò — ✅ W3
- [x] `RealtimeService` route theo y_ta_hanh_chinh, y_ta_cls, y_ta_phong_kham
- [x] FE `notifications.js` gửi đúng vai trò

---

## Giai Đoạn 4: Frontend Integration — ✅ PHẦN LỚN HOÀN TẤT

### 4.1 RBAC + Route Guard + ScopeBadge — ✅ W4
### 4.2 PaymentWizard — ✅ W4
### 4.3 Staff Management (Unified) — ✅ W4
### 4.4 VietQR FE — ✅ W4

### 4.5 Mở rộng (Tùy chọn / W5):
- [ ] Tab Pha hệ — interactive tree view
- [ ] Tab Lịch sử khám — MongoDB detail timeline
- [ ] Dashboard Analytics — Recharts widgets

---

## 🔴 Nợ Kỹ Thuật (Deferred to W5)

### 5.1 `NoiDungKetQua` column — 🔴
> Entity field still exists. Removed from DTO + seed but column not DROPped.
> **Action**: Remove from `KetQuaDichVu.cs` + migration DROP column.

### 5.2 8 cột y tế `BenhNhan` — 🟡
> 8 medical columns still in `BenhNhan.cs`. Deferred — doesn't affect features.
> **Action**: Migration script SQL→MongoDB + DROP columns.

---

## Giai Đoạn 5: Test & Đóng Gói — ⏳ Week 5

### 5.1 Test Bắt Buộc
- [ ] Race Condition Test (SP SERIALIZABLE)
- [ ] Schema Evolution Test (MongoDB)
- [ ] Recursive CTE Test
- [ ] Aggregation Test
- [ ] Permission Matrix Test (12 tabs × 5 roles)
- [ ] API Contract Test (FE↔BE field mapping)
- [ ] E2E Flow Test (đặt lịch → khám → CLS → thuốc → thanh toán)

### 5.2 Tài Liệu Nộp
- [ ] Báo cáo PDF
- [ ] Video demo 4 chức năng bắt buộc
- [ ] AI Audit Log

---

## Những Gì KHÔNG CẦN Làm (Đã Có Sẵn)

> Các tính năng sau đã triển khai đầy đủ. **Không cần refactor:**

- ✅ Auto-Queue (`QueueService.ThemVaoHangDoiAsync`)
- ✅ Auto-Billing (`ClinicalService` line 352-401)
- ✅ Priority 4-Tier (`QueueService.TinhDoUuTien`)
- ✅ Inventory Transaction + Rollback (`PharmacyService` line 310-388)
- ✅ SignalR Real-time (Queue, Exam, Dashboard, Notification) — phân loại 6 vai trò
- ✅ Notification System (`NotificationService`) — targeting chi tiết nurse_type
- ✅ Daily Reset (`DailyResetService`)
- ✅ Visit History SQL (`HistoryService` 653 dòng)
- ✅ Cancel workflows (HuyLuotKham, HuyPhieuCls, HuyDonThuoc, HuyHoaDon) — rollback + notification
- ✅ Analytics (AnalyticsService — MongoDB Aggregation + SQL stats)
- ✅ Audit Logs (AuditLogMiddleware + TTL 365 ngày)
- ✅ LichSuXuatKho (inventory transaction logging)
- ✅ VietQR (VietQRService + API + FE hook)
- ✅ Admin Management (AdminController CRUD + Lock/Unlock/Reset)
- ✅ RBAC 7 tầng (FE+BE)
- ✅ PaymentWizard (4-step inline)
- ✅ Staff Unified Page (Card↔Table toggle + admin actions)

---

## Phụ Lục: Yêu Cầu Nâng Cấp Gốc (từ prompt.txt — archived)

> Các yêu cầu ban đầu từ prompt.txt đã được xử lý hoặc hủy bỏ trong W4.
> Ghi lại đây để tham khảo.

| # | Yêu cầu | Trạng thái | Ghi chú |
|---|---------|:---:|---------| 
| I | Tách bảng User riêng khỏi Staff | ❌ HỦY | Auth giữ trong NhanVienYTe. Staff page chung + toggle card/table cho admin |
| II | Phân quyền 7 tầng | ✅ FE+BE | permissions.js 19 helpers + [RequireRole] attribute |
| III | Chức năng admin | ✅ DONE | AdminController CRUD + Lock/Unlock/Reset + [RequireRole] |
| IV | Thanh toán gắn trong luồng tạo phiếu | ✅ FE+BE | PaymentWizard 4-step + XacNhanThanhToanAsync + PUT /confirm |
| V | Chuẩn hóa FE/BE contract | ✅ DONE | enums.js, billing.js constants, VietQR DTO |
| VI | Seed data nền | ✅ Verified | DataSeed.cs 1500+ dòng, full outpatient flow |
| VII | Test kỹ 100% | ⏳ W5 | Chưa bắt đầu |

**File FE tạo mới trong W4:**
- `src/constants/enums.js`, `src/utils/permissions.js`
- `src/components/common/ProtectedRoute.jsx`, `ScopeBadge.jsx`
- `src/components/billing/PaymentWizard.jsx`, `PaymentStep.jsx`
- `src/components/staff/ViewToggle.jsx`, `StaffTable.jsx`

**File FE sửa trong W4:**
- `Sidebar.jsx`, `Overview.jsx`, `Reports.jsx`, `Staff.jsx`, `main.jsx`
- `billing.js`, `admin.js`, `permissions.js`, `NotifBell.jsx`
