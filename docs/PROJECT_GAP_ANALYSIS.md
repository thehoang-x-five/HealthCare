# Phân Tích & Đánh Giá Nâng Cấp HealthCare+ (Gap Analysis)

> **Đã đối chiếu với: Source code (22 Entity, 14 Controller, 9 Service folder) + Tất cả sơ đồ UML (5 module) + DB_DESIGN_DEFENSE.md**
>
> **Cập nhật lần cuối: 2026-04-03 — Sau khi hoàn tất Week 1-2-3 + Week 4 (FE RBAC + PaymentWizard)**

---

## 1. Tổng Quan: Hiện Tại (Code) vs Thiết Kế Mới (Sơ đồ)

### 1.1 Kiến trúc tổng thể

| Đặc điểm | Code hiện tại | Sơ đồ thiết kế mới (ERD/ARCH) | Gap | Trạng thái |
|:---|:---|:---|:---|:---:|
| **Database** | MySQL + MongoDB | **Polyglot**: MySQL + MongoDB | — | ✅ W1 |
| **Đặt Lịch** | C# code check conflict + **SP SERIALIZABLE** | **Stored Procedure + SERIALIZABLE** (ERD_02, ARCH_04) | — | ✅ W1 |
| **Pha hệ** | `MaCha`/`MaMe` + **GenealogyService + Recursive CTE** | Self-referencing FK + **Recursive CTE** (ERD_02, DEFENSE 4.1) | — | ✅ W2 |
| **Lịch sử khám** | **MongoDB `medical_histories`** — dual-write từ 4 service | **MongoDB `medical_histories`** — flat document per event (ERD_06) | ⚠️ Dual-write xong, chưa chuyển FE đọc từ MongoDB | 🟡 W2 |
| **Analytics** | **AnalyticsService** + MongoDB Aggregation + SQL stats | **MongoDB Aggregation Pipeline** (ERD_06, UC_11) | — | ✅ W3 |
| **Kết quả CLS** | `KetQuaDichVu.NoiDungKetQua` (string) + dual-write MongoDB | ~~NoiDungKetQua~~ **ĐÃ XÓA** per design → MongoDB (ERD_Diff) | ⚠️ **Chưa xóa `NoiDungKetQua`** — FE/HistoryService vẫn đọc từ SQL | 🔴 CHƯA |
| **Phiếu Tổng Hợp** | `PhieuTongHopKetQua` — status only, detail → MongoDB | ~~SnapshotJson~~ **ĐÃ XÓA** per design → MongoDB (ERD_Diff, DEFENSE 4.9) | — | ✅ W2 |
| **Hóa đơn** | `PhuongThucThanhToan` mở rộng + `SoTienPhaiTra`, `MaGiaoDich` | `ENUM(TienMat,The,ChuyenKhoan,VietQR)` + `MaGiaoDich`, `SoTienPhaiTra` (ERD_04) | — | ✅ W1 |
| **VietQR** | Không có | UC_10: Tạo mã QR động theo chuẩn Napas (UC82.5-82.12) | ❌ | 🟢 Tùy chọn |
| **Audit Logs** | **AuditLogMiddleware** + **AuditLogRepository** + TTL 365 ngày | MongoDB `audit_logs` collection + TTL 365 ngày (ERD_06) | — | ✅ W3 |

### 1.2 Các tính năng ĐÃ CÓ SẴN (Giữ nguyên, không sửa)

| Tính năng | File/Service | Đã xác thực |
|---|---|---|
| Auto-Queue | `QueueService.ThemVaoHangDoiAsync` | ✅ |
| Priority 4-Tier (0/10/20/30) | `QueueService.TinhDoUuTien` | ✅ |
| Auto-Billing (Khám) | `ClinicalService` (line 352-401) | ✅ |
| Inventory Transaction + Rollback | `PharmacyService` (line 310-388) | ✅ |
| SignalR Real-time | `RealtimeService` + `RealtimeHub` | ✅ |
| Notification System | `NotificationService` — **phân loại chi tiết 6 vai trò** (W3) | ✅ |
| Daily Reset | `DailyResetService` | ✅ |
| Visit History (SQL) | `HistoryService` (653 dòng) + `HistoryController` + `LuotKhamBenh` | ✅ |

---

## 2. Chi Tiết Từng GAP — Trạng thái sau W1-W2-W3

### A. Bảng `BenhNhan` — ✅ ĐÃ XONG (W1)

- ✅ Thêm `MaCha`, `MaMe`, `CCCD`, `NgayTao`, `NgayCapNhat`
- ⚠️ **8 cột y tế: ĐANG GIỮ** (chờ MongoDB chuyển xong mới xóa)
  - `DiUng`, `ChongChiDinh`, `ThuocDangDung`, `TieuSuBenh`, `TienSuPhauThuat`, `NhomMau`, `BenhManTinh`, `SinhHieu`
  - **Plan**: FE/BE chuyển đọc từ MongoDB `medical_histories` (event_type: medicalProfile) → xóa 8 cột

### B. MongoDB — ✅ ĐÃ XONG (W1-W2)

**Collection 1: `medical_histories`** — ✅ Dual-write hoạt động
- ✅ `kham_lam_sang` — ClinicalService.TaoChanDoanCuoiAsync → LogEventAsync
- ✅ `xet_nghiem` — ClsService.LuuKetQuaClsAsync → LogEventAsync
- ✅ `chan_doan_hinh_anh` — ClsService.LuuKetQuaClsAsync → LogEventAsync
- ✅ `don_thuoc` — PharmacyService.XuatThuocAsync → LogEventAsync
- ✅ `thanh_toan` — BillingService.TaoHoaDonAsync → LogEventAsync
- ✅ Indexes: `patient_id`, `event_type`, `event_date`, compound index
- ✅ API `GET /api/patients/{id}/medical-history`

**Collection 2: `audit_logs`** — ✅ ĐÃ XONG (W3)
- ✅ AuditLogMiddleware chặn POST/PUT/DELETE
- ✅ TTL Index 365 ngày
- ✅ Schema: action, entity, entity_id, user_*, timestamp, old_value, new_value

### C. Entities SQL — ✅ ĐÃ XONG (W1)

| Entity | Trạng thái | Week |
|---|---|---|
| `BenhNhan` + MaCha/MaMe/CCCD/NgayTao/NgayCapNhat | ✅ | W1 |
| `KetQuaDichVu` + LoaiKetQua/KetLuanChuyen/GhiChu/TepDinhKem/ThoiGianChot | ✅ | W1 |
| `KetQuaDichVu` — **Xóa `NoiDungKetQua`** | 🔴 **CHƯA** | — |
| `PhieuChanDoanCuoi` + MaICD10/NgayTaiKham/GhiChuTaiKham/timestamps | ✅ | W1 |
| `HoaDonThanhToan` + SoTienPhaiTra/MaGiaoDich/ThoiGianHuy/MaNhanSuHuy | ✅ | W1 |
| `DonThuoc` + ThoiGianThanhToan/ThoiGianPhat/MaNhanSuPhat | ✅ | W1 |
| `ChiTietDonThuoc` + LieuDung/TanSuatDung/SoNgayDung/GhiChu | ✅ | W1 |
| `HangDoi` + SoLanGoi/ThoiGianGoiGanNhat | ✅ | W1 |
| `LuotKhamBenh` + ThoiGianThucTe/SinhHieuTruocKham/GhiChu | ✅ | W1 |
| **`LichSuXuatKho`** (ENTITY MỚI) | ✅ | W1 |
| **`ThongBaoMau`** (ENTITY MỚI) | ✅ | W1 |

### D. Logic Backend — 4 Chức năng bắt buộc (The Big 4)

| # | Chức năng | Trạng thái | Week |
|---|---|---|---|
| 1 | **Đặt Lịch SERIALIZABLE** (SP `sp_BookAppointment`) | ✅ | W1 |
| 2 | **Pha Hệ Di Truyền** (Recursive CTE + GenealogyService + GenealogyController) | ✅ | W2 |
| 3 | **Lịch Sử Khám MongoDB** (Dual-write 5 event types + API GET) | ✅ (dual-write) | W2 |
| 4 | **Analytics MongoDB** (Aggregation Pipeline + AnalyticsService + AnalyticsController) | ✅ | W3 |

### E. VietQR — 🟢 Tùy chọn

- Chưa triển khai. Đây là tính năng tùy chọn, không bắt buộc.

### F. Phân Quyền & Tác Nhân — 🟡 Week 4 (FE xong, BE chờ)

- ✅ **Frontend RBAC 7 tầng hoàn tất** — W4:
  - ✅ Tầng 1: Menu Visibility — `Sidebar.jsx` dùng `TAB_VISIBILITY` mapping
  - ✅ Tầng 2: Route Guard — `ProtectedRoute.jsx` wrap `/appointments`, `/admin/users`
  - ✅ Tầng 3-5: Page/Component/Action — `canManageReception`, `canCallPatient`, `canViewRevenueReport` trên 6 page
  - ✅ Tầng 6-7: Data Scope badge — `ScopeBadge.jsx` trên Overview + Reports
- ✅ **Enum Constants** — `src/constants/enums.js` (177 dòng), thay hardcoded strings
- ✅ **PaymentWizard** — 4-step inline wizard tích hợp ExamDetail + billing hooks
- ✅ **NotifBell fix** — `setPeekItem` → `setPeek` selector bug
- ⏳ Backend `[RequireRole]` trên nhiều Controller — **Week 4-5 Dev1**
- ✅ **Notification targeting chi tiết 6 vai trò** — W3

### G. Trạng Thái & Luồng Nghiệp Vụ — ✅ ĐÃ XONG (W1-W3)

| # | Entity | Trạng thái | Week |
|---|---|---|---|
| ① | `LichHenKham` — Xác nhận + Hủy | ✅ Transition validation + FE nút | W1 |
| ② | `BenhNhan.TrangThaiHomNay` — BN bỏ về | ✅ Transition matrix | W1 |
| ③ | `LuotKhamBenh` — Hủy lượt khám | ✅ HuyLuotKhamAsync + rollback | W1 |
| ④ | `PhieuKhamLamSang` — Hủy phiếu LS | ✅ Validate transition | W1 |
| ⑤ | `PhieuKhamCanLamSang` — Hủy CLS | ✅ HuyPhieuClsAsync + rollback | W3 |
| ⑥ | `DonThuoc` — Hủy đơn + Hoàn kho | ✅ HuyDonThuocAsync + rollback kho | W3 |
| ⑦ | `HoaDonThanhToan` — Thu tiền / Hủy | ✅ Default `chua_thu`, HuyHoaDonAsync | W3 |
| ⑧ | `HangDoi` — Không cần hủy | ✅ 3 giá trị đủ | — |

---

## 3. Nguyên tắc Kiến trúc (từ DB_DESIGN_DEFENSE.md)

| Nguyên tắc | Chi tiết | Trạng thái |
|---|---|---|
| **CQRS** | SQL = Write/Validate (Operational), MongoDB = Read/History (Historical) — DEFENSE 4.11 | ⚠️ **Chưa hoàn tất**: FE/HistoryService vẫn đọc từ SQL thay vì MongoDB |
| **One-way Sync** | MySQL → MongoDB (không đồng bộ ngược) — DEFENSE 4.5 | ✅ Dual-write hoạt động |
| **Single Responsibility** | HangDoi (Logistics) ≠ LuotKham (Clinical) — DEFENSE 3.3 | ✅ |
| **Schema Evolution** | Thêm event_type mới → không cần ALTER TABLE — DEFENSE 2.2 | ✅ |
| **PhieuTongHop = "Đèn báo"** | Chỉ giữ TrangThai, không lưu chi tiết (chi tiết → MongoDB) — DEFENSE 4.9 | ✅ |
| **KetQuaDichVu = "Mục lục"** | Chỉ giữ metadata, chi tiết → MongoDB — DEFENSE 4.10 | ⚠️ `NoiDungKetQua` vẫn còn |

---

## 4. CÒN THIẾU — Cần hoàn tất trước Week 4

> [!WARNING]
> Các mục sau đây là **nợ kỹ thuật từ plan gốc** mà chưa hoàn tất trong W1-W3.
> Phải xử lý trước khi bắt đầu Week 4 (Tách User + Phân quyền).

### 4.1 🔴 Chuyển READ từ SQL → MongoDB cho KetQuaDichVu

**Thiết kế gốc (DEFENSE 4.10)**: `KetQuaDichVu = "Mục lục"` — chỉ giữ metadata (LoaiKetQua, KetLuanChuyen, ThoiGianChot), **chi tiết đọc từ MongoDB**.

**Hiện tại**: 10+ chỗ vẫn đọc/ghi `NoiDungKetQua` từ MySQL:
- `ClsService.cs` line 683, 693 — ghi khi tạo/cập nhật kết quả
- `ClsService.cs` line 725-741 — parse + dual-write sang MongoDB
- `ClsService.cs` line 773, 919 — map ra DTO
- `HistoryService.cs` line 220 — đọc cho lịch sử
- `ClsDtos.cs` — DTO field
- `DataSeed.cs` — seed data

**Cần làm**:
1. Sửa **ClsService**: khi tạo KQ CLS, chỉ ghi `KetLuanChuyen`, `TepDinhKem`, `ThoiGianChot` vào SQL. Chi tiết (`chi_so[]`, `mo_ta_hinh_anh`) chỉ ghi MongoDB.
2. Sửa **HistoryService**: đọc chi tiết KQ từ MongoDB thay vì `kq.NoiDungKetQua`.
3. Sửa **DTO**: loại `NoiDungKetQua` khỏi `ClsResultDto`, thay bằng data từ MongoDB.
4. Xóa `NoiDungKetQua` khỏi Entity + Migration DROP column.
5. Cập nhật DataSeed.

### 4.2 🟡 Chuyển READ 8 cột y tế BenhNhan → MongoDB

**Thiết kế gốc (GAP line 64-68)**: 8 cột y tế GIỮ tạm, chờ MongoDB xong mới xóa.

**Hiện tại**: `ClinicalService.TaoPhieuKhamAsync` (line 134-142) đọc 8 cột y tế từ SQL để hiển thị trong phiếu khám.

**Cần làm**:
1. Migration script: chuyển 8 cột SQL → MongoDB `medicalProfile` document cho BN hiện có
2. Sửa `ClinicalService`: đọc profile từ MongoDB thay vì SQL
3. Sau migration: Xóa 8 cột khỏi `BenhNhan.cs` + DROP columns

> [!NOTE]
> Mục 4.2 có thể để sau Week 4 vì không ảnh hưởng tính năng mới (tách User/phân quyền). Nhưng 4.1 (`NoiDungKetQua`) nên làm ngay vì thiết kế yêu cầu XÓA.

---

## 5. Tóm Tắt Khối Lượng Công Việc

| Hạng mục | Khối lượng | Trạng thái |
|---|---|---|
| MongoDB Setup (Driver, Context, 2 Collections) | ~3h | ✅ W1 |
| SQL Migration (MaCha, MaMe, CCCD, Entity mới) | ~2h | ✅ W1 |
| SP `sp_BookAppointment` (SERIALIZABLE) | ~3h | ✅ W1 |
| Recursive CTE + GenealogyService | ~2h | ✅ W2 |
| MongoDB ghi lịch sử (tích hợp ClinicalService/ClsService) | ~4h | ✅ W2 |
| Aggregation Pipeline Analytics | ~3h | ✅ W3 |
| Entity SQL cập nhật (ERD_Diff: ~10 entity) | ~3h | ✅ W1 |
| LichSuXuatKho + ThongBaoMau entity mới | ~2h | ✅ W1+W3 |
| Audit Logs (MongoDB) | ~2h | ✅ W3 |
| Luồng trạng thái (DB enum + BE validate + FE nút Hủy) | ~6h | ✅ W1-W3 |
| Notification phân loại chi tiết 6 vai trò | ~2h | ✅ W3 |
| **Chuyển READ KetQuaDichVu SQL → MongoDB + Xóa NoiDungKetQua** | ~3h | 🔴 CHƯA |
| **Chuyển READ 8 cột y tế BenhNhan → MongoDB** | ~3h | 🟡 CHƯA |
| **Phân quyền Backend** (`[RequireRole]` trên 5 Controller) | ~2h | ⏳ W4-5 |
| **Phân quyền Frontend** (RBAC 7 tầng + enums + ScopeBadge) | ~8h | ✅ W4 |
| **PaymentWizard** (inline 4-step wizard + billing hooks) | ~3h | ✅ W4 |
| **Trang quản trị Admin** (CRUD người dùng — UC10) | ~4h | ⏳ W4-5 |
| Frontend (Timeline, Pha hệ, Analytics, VietQR) | ~8h | ⏳ W4-W5 |
| VietQR Integration | ~4h | 🟢 Tùy chọn |
