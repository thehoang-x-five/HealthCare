# Phân Tích & Đánh Giá Nâng Cấp HealthCare+ (Gap Analysis)

> **Đã đối chiếu với: Source code (22 Entity, 14 Controller, 9 Service folder) + Tất cả sơ đồ UML + DB_DESIGN_DEFENSE.md**
>
> **Cập nhật lần cuối: 2026-04-05 — Full audit Week 1-4 hoàn tất**

---

## 1. Tổng Quan: Hiện Tại (Code) vs Thiết Kế Mới

### 1.1 Kiến trúc tổng thể

| Đặc điểm | Trạng thái | Week | Ghi chú |
|:---|:---:|:---:|:---|
| **Polyglot DB** (MySQL + MongoDB) | ✅ | W1 | MongoDbContext + medical_histories + audit_logs |
| **Đặt Lịch SERIALIZABLE** (SP) | ✅ | W1 | sp_BookAppointment, FindConflicts pre-check |
| **Pha Hệ Di Truyền** (Recursive CTE) | ✅ | W2 | GenealogyService + GenealogyController |
| **Lịch Sử Khám MongoDB** (dual-write) | ✅ | W2 | 5 event types, API GET với filter |
| **Analytics MongoDB** (Aggregation) | ✅ | W3 | 3 APIs: abnormal-stats, disease-trends, popular-drugs |
| **Audit Logs** (MongoDB + TTL) | ✅ | W3 | AuditLogMiddleware, TTL 365 ngày |
| **VietQR** | ✅ | W4 | VietQRService + POST /generate-qr + FE hook |
| **Phân quyền RBAC** (7 tầng FE + BE) | ✅ | W4 | [RequireRole] + permissions.js 19 helpers |
| **Admin Management** | ✅ | W4 | AdminController CRUD + Lock/Unlock/Reset |
| **Thanh toán Inline** | ✅ | W4 | PaymentWizard 4-step + PUT /confirm |
| **Staff Unified Page** | ✅ | W4 | Card↔Table toggle + auth columns + admin actions |

### 1.2 Các tính năng ĐÃ CÓ SẴN (Giữ nguyên)

| Tính năng | File/Service | Verified |
|---|---|:---:|
| Auto-Queue | `QueueService.ThemVaoHangDoiAsync` | ✅ |
| Priority 4-Tier (0/10/20/30) | `QueueService.TinhDoUuTien` | ✅ |
| Auto-Billing (Khám) | `ClinicalService` (line 352-401) | ✅ |
| Inventory Transaction + Rollback | `PharmacyService` (line 310-388) | ✅ |
| SignalR Real-time | `RealtimeService` + `RealtimeHub` | ✅ |
| Notification System (6 vai trò) | `NotificationService` | ✅ |
| Daily Reset | `DailyResetService` | ✅ |
| Visit History (SQL) | `HistoryService` (653 dòng) | ✅ |
| Cancel workflows (4 entities) | HuyLuotKham, HuyPhieuCls, HuyDonThuoc, HuyHoaDon | ✅ |
| LichSuXuatKho | `LichSuXuatKhoService.cs` | ✅ |

---

## 2. Chi Tiết Từng GAP — Trạng thái sau W1-W4

### A. Bảng `BenhNhan` — ✅ W1
- ✅ Thêm `MaCha`, `MaMe`, `CCCD`, `NgayTao`, `NgayCapNhat`
- 🟡 **8 cột y tế**: ĐANG GIỮ (deferred — chờ MongoDB migration)

### B. MongoDB — ✅ W1-W3
- ✅ `medical_histories` — dual-write 5 event types + indexes + API
- ✅ `audit_logs` — TTL 365 ngày + middleware

### C. Entities SQL — ✅ W1
- ✅ 10 entities updated + 2 new entities
- 🔴 `KetQuaDichVu.NoiDungKetQua` — field vẫn còn trong entity (data đọc từ MongoDB rồi)

### D. Logic Backend — 4 Chức năng bắt buộc ✅
| # | Chức năng | Trạng thái |
|---|---|:---:|
| 1 | Đặt Lịch SERIALIZABLE (SP) | ✅ W1 |
| 2 | Pha Hệ Di Truyền (Recursive CTE) | ✅ W2 |
| 3 | Lịch Sử Khám MongoDB (Dual-write) | ✅ W2 |
| 4 | Analytics MongoDB (Aggregation) | ✅ W3 |

### E. VietQR — ✅ W4
- ✅ `VietQRService.cs` + POST endpoint + FE hook + config

### F. Phân Quyền & Tác Nhân — ✅ W4

**Backend:**
- ✅ `[RequireRole]` trên AdminController, ReportsController, MasterDataController, BillingController
- ✅ `AdminController` — CRUD + Lock/Unlock + Reset Password

**Frontend RBAC 7 tầng:**
- ✅ Tầng 1: Menu Visibility — `Sidebar.jsx` dùng `TAB_VISIBILITY`
- ✅ Tầng 2: Route Guard — `ProtectedRoute.jsx`
- ✅ Tầng 3-5: Page/Component/Action — 15+ permission helpers
- ✅ Tầng 6-7: Data Scope — `ScopeBadge.jsx` + scope labels

**Staff Management (No-Split Strategy):**
- ✅ `ViewToggle.jsx` — Card↔Table toggle (admin only)
- ✅ `StaffTable.jsx` — auth columns + ActionMenu
- ✅ `Staff.jsx` — unified page with admin API integration
- ✅ QL Nhân Viên separate page — ❌ CANCELLED, merged into Staff

### G. Trạng Thái & Luồng Nghiệp Vụ — ✅ W1-W4

| # | Entity | Status | Week |
|---|---|:---:|:---:|
| ① | LichHenKham — Xác nhận + Hủy | ✅ | W1 |
| ② | BenhNhan.TrangThaiHomNay — BN bỏ về | ✅ | W1 |
| ③ | LuotKhamBenh — Hủy lượt khám | ✅ | W1 |
| ④ | PhieuKhamLamSang — Hủy phiếu LS | ✅ | W1 |
| ⑤ | PhieuKhamCanLamSang — Hủy CLS | ✅ | W3 |
| ⑥ | DonThuoc — Hủy đơn + Hoàn kho | ✅ | W3 |
| ⑦ | HoaDonThanhToan — Default chua_thu, Thu/Hủy | ✅ | W3-W4 |

### H. Thanh toán Inline — ✅ W4
- ✅ BE: `PaymentConfirmRequest` + `XacNhanThanhToanAsync` + PUT /confirm
- ✅ FE: `PaymentWizard.jsx` 4-step + `PaymentStep.jsx` + billing hooks
- ✅ Hủy: `PUT /cancel` + `useCancelInvoice`

---

## 3. Nguyên tắc Kiến trúc

| Nguyên tắc | Chi tiết | Trạng thái |
|---|---|:---:|
| **CQRS** | SQL = Write/Validate, MongoDB = Read/History | ✅ (dual-write hoạt động) |
| **One-way Sync** | MySQL → MongoDB (không đồng bộ ngược) | ✅ |
| **Single Responsibility** | HangDoi ≠ LuotKham | ✅ |
| **Schema Evolution** | event_type mới → không ALTER TABLE | ✅ |
| **No-Split Auth** | Auth fields giữ trong NhanVienYTe, RBAC qua [RequireRole] | ✅ W4 |

---

## 4. Nợ Kỹ Thuật (Deferred)

> [!NOTE]
> Các mục sau ưu tiên thấp — không ảnh hưởng tính năng đã hoàn tất.

### 4.1 🔴 `NoiDungKetQua` (KetQuaDichVu)
- Field vẫn còn trong entity C# class (đã xóa khỏi DTO + seed)
- **Action**: DROP column migration + remove from entity

### 4.2 🟡 8 cột y tế `BenhNhan`
- 8 cột y tế SQL chưa chuyển sang MongoDB
- **Action**: Migration script → MongoDB `medicalProfile` + DROP columns

---

## 5. Tóm Tắt Khối Lượng Công Việc

| Hạng mục | Trạng thái | Week |
|---|:---:|:---:|
| MongoDB Setup (Driver, Context, Collections) | ✅ | W1 |
| SQL Migration (10 entities + 2 new) | ✅ | W1 |
| SP sp_BookAppointment (SERIALIZABLE) | ✅ | W1 |
| DB Constraints (CHECK + TRIGGER) | ✅ | W1 |
| Recursive CTE + Genealogy | ✅ | W2 |
| MongoDB dual-write (5 event types) | ✅ | W2 |
| Aggregation Pipeline Analytics | ✅ | W3 |
| Audit Logs (MongoDB + TTL) | ✅ | W3 |
| LichSuXuatKho + ThongBaoMau | ✅ | W1+W3 |
| Cancel workflows (4 entities) | ✅ | W1-W3 |
| Notification 6 vai trò | ✅ | W3 |
| RBAC 7 tầng (FE+BE) | ✅ | W4 |
| Admin Management (CRUD + Lock/Reset) | ✅ | W4 |
| VietQR (Service + API + FE) | ✅ | W4 |
| PaymentWizard (4-step inline) | ✅ | W4 |
| Staff Unified Page (Toggle + Auth) | ✅ | W4 |
| Contract standardization (enums + DTOs) | ✅ | W4 |
| **NoiDungKetQua DROP** | 🔴 | Deferred |
| **8 cột y tế migration** | 🟡 | Deferred |
| **Test & Documentation** | ⏳ | W5 |
