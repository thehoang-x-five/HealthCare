# Phân Tích & Đánh Giá Nâng Cấp HealthCare+ (Gap Analysis)

> **Đã đối chiếu với: Source code (22 Entity, 14 Controller, 9 Service folder) + Tất cả sơ đồ UML (5 module) + DB_DESIGN_DEFENSE.md**

---

## 1. Tổng Quan: Hiện Tại (Code) vs Thiết Kế Mới (Sơ đồ)

### 1.1 Kiến trúc tổng thể

| Đặc điểm | Code hiện tại | Sơ đồ thiết kế mới (ERD/ARCH) | Gap |
|:---|:---|:---|:---|
| **Database** | MySQL only (EF Core) | **Polyglot**: MySQL + MongoDB | ❌ THIẾU MongoDB |
| **Đặt Lịch** | C# code check conflict (`FindConflictsForConfirmedAsync`) | **Stored Procedure + SERIALIZABLE** (ERD_02, ARCH_04) | ⚠️ CẦN NÂNG CẤP |
| **Pha hệ** | Không có cột `MaCha`/`MaMe` | Self-referencing FK + **Recursive CTE** (ERD_02, DEFENSE 4.1) | ❌ CHƯA CÓ |
| **Lịch sử khám** | SQL cứng (`BenhNhan` 8 cột y tế) | **MongoDB `medical_histories`** — flat document per event (ERD_06) | ❌ CHƯA CÓ |
| **Analytics** | Không có | **MongoDB Aggregation Pipeline** (ERD_06, UC_11) | ❌ CHƯA CÓ |
| **Kết quả CLS** | `KetQuaDichVu.NoiDungKetQua` (string) | ~~NoiDungKetQua~~ **ĐÃ XÓA** per design → MongoDB (ERD_Diff) | ⚠️ CODE CHƯA CẬP NHẬT |
| **Phiếu Tổng Hợp** | `PhieuTongHopKetQua` có thể có SnapshotJson | ~~SnapshotJson~~ **ĐÃ XÓA** per design → MongoDB (ERD_Diff, DEFENSE 4.9) | ⚠️ CODE CHƯA CẬP NHẬT |
| **Hóa đơn** | `PhuongThucThanhToan = "tien_mat"` only | `ENUM(TienMat,The,ChuyenKhoan,VietQR)` + `MaGiaoDich`, `SoTienPhaiTra` (ERD_04) | ⚠️ CẦN MỞ RỘNG |
| **VietQR** | Không có | UC_10: Tạo mã QR động theo chuẩn Napas (UC82.5-82.12) | ❌ CHƯA CÓ |
| **Audit Logs** | Không có | MongoDB `audit_logs` collection + TTL 365 ngày (ERD_06) | ❌ CHƯA CÓ |

### 1.2 Các tính năng ĐÃ CÓ SẴN (Giữ nguyên, không sửa)

| Tính năng | File/Service | Đã xác thực |
|---|---|---|
| Auto-Queue | `QueueService.ThemVaoHangDoiAsync` | ✅ |
| Priority 4-Tier (0/10/20/30) | `QueueService.TinhDoUuTien` | ✅ |
| Auto-Billing (Khám) | `ClinicalService` (line 352-401) | ✅ |
| Inventory Transaction + Rollback | `PharmacyService` (line 310-388) | ✅ |
| SignalR Real-time | `RealtimeService` + `RealtimeHub` | ✅ |
| Notification System | `NotificationService` | ✅ |
| Daily Reset | `DailyResetService` | ✅ |
| Visit History (SQL) | `HistoryService` (653 dòng) + `HistoryController` + `LuotKhamBenh` | ✅ |

---

## 2. Chi Tiết Từng GAP

### A. Bảng `BenhNhan` — Cần sửa đổi SQL

**Hiện tại (Code):**
```csharp
// BenhNhan.cs — 8 cột y tế lưu cứng
public string? DiUng { get; set; }
public string? ChongChiDinh { get; set; }
public string? ThuocDangDung { get; set; }
public string? TieuSuBenh { get; set; }
public string? TienSuPhauThuat { get; set; }
public string? NhomMau { get; set; }
public string? BenhManTinh { get; set; }
public string? SinhHieu { get; set; }
// THIẾU: MaCha, MaMe, CCCD
```

**Thiết kế mới (ERD_02):**
```
BenhNhan:
  + MaCha : VARCHAR(20) <<FK>> <<NULLABLE>>  ← CẦN THÊM
  + MaMe  : VARCHAR(20) <<FK>> <<NULLABLE>>  ← CẦN THÊM
  + CCCD  : VARCHAR(12) <<UNIQUE>>           ← CẦN THÊM
  + NgayTao, NgayCapNhat                     ← CẦN THÊM
  ─ 8 cột y tế: GIỮ (chờ MongoDB xong mới xóa)
```

> [!CAUTION]
> `ClinicalService.TaoPhieuKhamAsync` (line 134-142) đang ghi trực tiếp 8 cột y tế vào SQL. Phải chuyển sang ghi MongoDB trước khi xóa.

### B. MongoDB — Xây hoàn toàn mới

**Hiện tại:** Không có code MongoDB. Sơ đồ `ERD_06_MongoDB.puml` chỉ là thiết kế trên giấy.

**Cần xây (theo ERD_06):**

**Collection 1: `medical_histories`** — MỖI document = MỘT event (flat, KHÔNG embedded array)
```json
{
  "_id": "ObjectId",
  "patient_id": "BN001",
  "event_type": "kham_lam_sang",  // 5 loại: kham_lam_sang, xet_nghiem, chan_doan_hinh_anh, don_thuoc, thanh_toan
  "event_date": "ISODate",
  "metadata": { "created_by": "...", "version": 1 },
  "data": { /* flexible per event_type */ }
}
```

Event types và data schema (từ ERD_06):
| event_type | data chứa | Nguồn SQL tương ứng |
|---|---|---|
| `kham_lam_sang` | sinh_hieu, trieu_chung, chan_doan_so_bo, chan_doan_cuoi, ma_icd10, huong_xu_tri, loi_khuyen | `PhieuKham` + `PhieuChanDoanCuoi` |
| `xet_nghiem` | chi_so[] (ten, gia_tri, don_vi, nguong_min/max, bat_thuong), ket_luan | `KetQuaDichVu` |
| `chan_doan_hinh_anh` | loai (X-quang/SA/CT/MRI), mo_ta_hinh_anh, ket_luan, files[] | `KetQuaDichVu` |
| `don_thuoc` | thuoc[] (ten, hoat_chat, so_luong, lieu_dung, tan_suat, so_ngay), tong_tien | `DonThuoc` + `ChiTietDonThuoc` |
| `thanh_toan` | loai_dot_thu, chi_tiet[], tong_tien, phuong_thuc, ma_giao_dich | `HoaDonThanhToan` |

Indexes: `patient_id`, `event_type`, `event_date` (desc), compound: `(patient_id, event_type, event_date)`

**Collection 2: `audit_logs`** — Ghi các hành động CRUD
- TTL Index: Tự xóa sau 365 ngày
- Schema: `action, entity, entity_id, user_*, timestamp, old_value, new_value, changes`

### C. Entities SQL cần cập nhật (theo ERD_Diff)

| Entity | Cần thêm | Cần xóa |
|---|---|---|
| `BenhNhan` | `MaCha`, `MaMe`, `CCCD`, `NgayTao`, `NgayCapNhat` | 8 cột y tế (SAU KHI MongoDB xong) |
| `KetQuaDichVu` | `LoaiKetQua`, `KetLuanChuyen`, `GhiChu`, `TepDinhKem` (JSON), `ThoiGianChot` | ~~`NoiDungKetQua`~~ |
| `PhieuChanDoanCuoi` | `MaICD10`, `NgayTaiKham`, `GhiChuTaiKham`, `ThoiGianTao`, `ThoiGianCapNhat` | — |
| `HoaDonThanhToan` | `SoTienPhaiTra`, `MaGiaoDich`, `ThoiGianHuy`, `MaNhanSuHuy` + Mở rộng `PhuongThucThanhToan` enum | — |
| `DonThuoc` | `ThoiGianThanhToan`, `ThoiGianPhat`, `MaNhanSuPhat` | — |
| `ChiTietDonThuoc` | `LieuDung`, `TanSuatDung`, `SoNgayDung`, `GhiChu` | — |
| `HangDoi` | `SoLanGoi`, `ThoiGianGoiGanNhat` | — |
| `LuotKhamBenh` | `ThoiGianThucTe`, `SinhHieuTruocKham` (JSON), `GhiChu` | — |
| **`LichSuXuatKho`** | **ENTITY MỚI** (MaThuoc, MaDonThuoc, MaNhanSuXuat, LoaiGiaoDich, SoLuong, SoLuongConLai) | — |
| **`ThongBaoMau`** | **ENTITY MỚI** (MaMau, TenMau, NoiDungMau, BienDong) | — |

### D. Logic Backend — 4 Chức năng bắt buộc (The Big 4)

#### 1. Đặt Lịch SERIALIZABLE (SQL Stored Procedure)
- **Code hiện tại:** `AppointmentService.FindConflictsForConfirmedAsync` — check trùng bằng C#, không có Transaction Isolation
- **Thiết kế mới:** ERD_02 ghi rõ "SERIALIZABLE Transaction de tranh trung lich"
- **Gap:** Cần viết SP `sp_BookAppointment` với `SET TRANSACTION ISOLATION LEVEL SERIALIZABLE`

#### 2. Pha Hệ Di Truyền (Recursive CTE)
- **Code hiện tại:** Không có `MaCha`/`MaMe`
- **Thiết kế mới:** ERD_02 + DEFENSE 4.1 định nghĩa rõ:
  ```sql
  WITH RECURSIVE PhaHe AS (
    SELECT * FROM BenhNhan WHERE MaBenhNhan = ?
    UNION ALL
    SELECT bn.* FROM BenhNhan bn
    JOIN PhaHe ph ON bn.MaBenhNhan = ph.MaCha
       OR bn.MaBenhNhan = ph.MaMe
  )
  SELECT * FROM PhaHe;
  ```
- **Gap:** Cần thêm `MaCha`/`MaMe` + API `GET /api/patients/{id}/genealogy`
- **UC_04 định nghĩa:** 6 Use Case (UC23.1-23.6): Xem/Liên kết pha hệ, Cây pha hệ, Tiền sử bệnh gia đình

#### 3. Lịch Sử Khám MongoDB (Schema Evolution)
- **Code hiện tại:** `HistoryService` (653 dòng) quản lý `LuotKhamBenh` (SQL)
- **Thiết kế mới:** Ghi **thêm** vào `medical_histories` (MongoDB) khi hoàn tất lượt khám (One-way sync: MySQL → MongoDB, DEFENSE 4.5)
- **Gap:** Cần tích hợp MongoDB ghi vào `HistoryService`/`ClinicalService`/`ClsService`
- **UC_04 định nghĩa:** 6 Use Case (UC24.1-24.6): Timeline, Lọc theo loại/thời gian, Chi tiết XN/Don thuoc

#### 4. Phân Tích MongoDB (Aggregation Pipeline)
- **Code hiện tại:** Không có analytics
- **Thiết kế mới:** ERD_06 ghi rõ aggregation example:
  ```javascript
  db.medical_histories.aggregate([
    { $match: { event_type: "xet_nghiem" } },
    { $unwind: "$data.chi_so" },
    { $match: { "data.chi_so.bat_thuong": true } },
    { $group: { _id: "$data.chi_so.ten", count: { $sum: 1 } } }
  ])
  ```
- **UC_11 định nghĩa:** "Phan tich theo Nhom benh (ICD)" (UC91.6) + "Thong ke Thuoc hay dung" (UC94.5)

### E. VietQR (Tùy chọn nhưng đã thiết kế sẵn)

- **Code hiện tại:** Không có
- **Thiết kế mới (UC_10, ERD_04):**
  - UC82.5-82.12: Tạo mã VietQR động, nhúng Số tiền + Mã Hóa đơn, Hiển thị cho BN
  - ERD_04: HoaDon có `PhuongThucThanhToan: ENUM(TienMat,The,ChuyenKhoan,VietQR)` + `MaGiaoDich`
  - DEFENSE 4.4: Giải thích tại sao VietQR thay vì Stripe/PayPal (phong khám VN)

### F. Phân Quyền & Tác Nhân (Role-based Access Control)

#### F.1 Tác nhân theo UC_00 vs Code thực tế

**Sơ đồ UC_00 định nghĩa 6 tác nhân:**

| Tác nhân (UC_00) | Backend: Entity `NhanVienYTe` | Backend: `[RequireRole]` | Trạng thái |
|---|---|---|---|
| **Admin** | `ChucVu = "admin"` | Chỉ có trên `BillingController` | ⚠️ THIẾU trên MasterData, Reports |
| **Y tá HC** | `VaiTro = "y_ta"` + `LoaiYTa = "hanhchinh"` | `"y_ta"` trên Appointments, Patients, Clinical, Pharmacy | ✅ OK |
| **Y tá LS** | `VaiTro = "y_ta"` + `LoaiYTa = "phong_kham"` | `"y_ta"` trên Clinical | ✅ OK |
| **Y tá CLS** | `VaiTro = "y_ta"` + `LoaiYTa = "can_lam_sang"` | `"y_ta"` trên CLS | ✅ OK |
| **Bác sĩ** | `ChucVu = "bac_si"` | `"bac_si"` trên Clinical, CLS, Pharmacy (kê đơn) | ✅ OK |
| **KTV** | `ChucVu = "ky_thuat_vien"` | `"ky_thuat_vien"` trên CLS (4 endpoint) | ✅ OK |

#### F.2 Ma trận phân quyền chuẩn (theo UC_00 + UC_03)

| Trang Frontend | Admin | Y tá HC | Y tá LS | Y tá CLS | Bác sĩ | KTV |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Tổng quan (Dashboard) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Lịch hẹn | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Bệnh nhân | ❌ | ✅ CRUD | ✅ xem | ❌ | ✅ xem | ❌ |
| Khám bệnh (Hàng đợi) | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Khoa phòng | ✅ CRUD | ✅ xem | ✅ xem | ✅ xem | ✅ xem | ✅ xem |
| Nhân sự | ✅ CRUD | ❌ | ❌ | ❌ | ❌ | ❌ |
| Đơn thuốc | ❌ | ✅ phát | ❌ | ❌ | ✅ kê | ❌ |
| Lịch sử | ❌ | ✅ | ❌ | ❌ | ✅ | ❌ |
| Thông báo | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Báo cáo | ✅ full | ✅ doanh thu, kho | ❌ | ❌ | ✅ lượt khám | ❌ |
| Cài đặt | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

#### F.3 Gap: Sidebar không lọc theo vai trò

**`Sidebar.jsx` (line 7-18)** — Hardcode 10 link cố định, hiện cho MỌI user:
```js
const links = [
  ["/", "Tổng quan"],        // Tất cả
  ["/appointments", "Lịch hẹn"],  // Chỉ Y tá HC → HIỆN cho tất cả ❌
  ["/patients", "Bệnh nhân"],     // Y tá HC + BS → HIỆN cho tất cả ❌
  ["/examination", "Khám bệnh"],  // Tất cả (trừ Admin)
  ["/departments", "Khoa phòng"], // Admin=CRUD, Others=Read
  ["/staff", "Nhân sự"],          // Admin ONLY → HIỆN cho tất cả ❌
  ["/prescriptions", "Đơn thuốc"],// Y tá HC + BS → HIỆN cho tất cả ❌
  ["/history", "Lịch sử"],        // Y tá HC + BS
  ["/notifications", "Thông báo"],// Tất cả
  ["/reports", "Báo cáo"],        // Admin + Y tá HC + BS
];
```

**Hậu quả:** KTV đăng nhập vẫn thấy "Lịch hẹn", "Nhân sự", "Đơn thuốc"... → bấm vào API trả 403 nhưng UI vẫn hiện menu.

**`permissions.js` đã có sẵn các helper** (`canManageReception`, `canManageClinical`, `canManageCls`, `isAdmin`) **nhưng CHƯA** được dùng trong Sidebar.

#### F.4 Gap: Backend thiếu `[RequireRole]` trên nhiều Controller

| Controller | Hiện tại | Cần sửa |
|---|---|---|
| `MasterDataController` | Chỉ `[Authorize]` chung | Thêm `[RequireRole("admin")]` trên endpoint CUD (Thêm/Sửa/Xóa). GET giữ `[Authorize]` cho tất cả. |
| `HistoryController` | Chỉ `[Authorize]` chung | Thêm `[RequireRole("y_ta", "bac_si")]` — KTV không cần xem lịch sử. |
| `DashboardController` | Chỉ `[Authorize]` chung | Giữ `[Authorize]` (tất cả xem Dashboard). |
| `NotificationsController` | Chỉ `[Authorize]` chung | Giữ `[Authorize]` (tất cả xem thông báo). |
| `ReportsController` | Chỉ `[Authorize]` chung | Phân quyền: Admin=full, Y tá HC=doanh thu+kho, BS=lượt khám. |
| `PatientsController` | `[RequireRole("y_ta")]` trên CUD | Thêm cho BS quyền GET (xem thông tin BN). |

### G. Trạng Thái & Luồng Nghiệp Vụ (Status Enums & State Machines)

> **Vấn đề cốt lõi:** Hiện tại hệ thống chỉ cài đặt **happy path** (luôn hoàn thành, không cho gãy giữa chừng). Các giá trị enum như `da_huy`, `tam_nghi` tồn tại trên Entity nhưng **không có code xử lý thực sự** trên cả BE lẫn FE.

#### G.1 Tổng hợp 10 Entity có TrangThai

| # | Entity | Trường | Enum values hiện tại | Default |
|---|---|---|---|---|
| 1 | `LichHenKham` | `TrangThai` | `dang_cho`, `da_xac_nhan`, `da_checkin`, `da_huy` | `dang_cho` |
| 2 | `BenhNhan` | `TrangThaiHomNay` | `cho_tiep_nhan`, `cho_kham`, `dang_kham`, `cho_xu_ly`, `hoan_tat`, `da_huy` + 4 biến thể `_dv` | nullable |
| 3 | `BenhNhan` | `TrangThaiTaiKhoan` | `hoat_dong`, `khong_hoat_dong`, `da_xoa` | `hoat_dong` |
| 4 | `HangDoi` | `TrangThai` | `cho_goi`, `dang_goi`, `da_phuc_vu` | `cho_goi` |
| 5 | `LuotKhamBenh` | `TrangThai` | `dang_thuc_hien`, `hoan_tat` | `dang_thuc_hien` |
| 6 | `PhieuKhamLamSang` | `TrangThai` | `da_lap`, `dang_thuc_hien`, `da_lap_chan_doan`, `da_hoan_tat`, `da_huy` | `da_lap` |
| 7 | `PhieuKhamCanLamSang` | `TrangThai` | `da_lap`, `dang_thuc_hien`, `da_hoan_tat`, **`da_huy`** *(StatusEnums.cs có, Entity comment chưa cập nhật)* | `da_lap` |
| 8 | `ChiTietDichVu` | `TrangThai` | `da_lap`, `dang_thuc_hien`, `da_co_ket_qua`, `chua_co_ket_qua`, **`da_huy`** *(StatusEnums.cs có thêm 3 giá trị)* | `da_lap` |
| 9 | `PhieuTongHopKetQua` | `TrangThai` | `cho_xu_ly`, `dang_xu_ly`, `da_hoan_tat` | `cho_xu_ly` |
| 10 | `DonThuoc` | `TrangThai` | `da_ke`, `cho_phat`, `da_phat`, **`da_huy`** *(StatusEnums.cs có, Entity comment chưa cập nhật)* | `da_ke` |
| 11 | `HoaDonThanhToan` | `TrangThai` | **`chua_thu`**, `da_thu`, `da_huy` *(StatusEnums.cs đã có `chua_thu`)* | `da_thu` |
| 12 | `ThongBaoHeThong` | `TrangThai` | `cho_gui`, `da_gui`, `da_doc` | `cho_gui` |
| 13 | `KhoThuoc` | `TrangThai` | `hoat_dong`, `tam_dung`, `sap_het_han`, `sap_het_ton` | `hoat_dong` |
| 14 | `KhoaChuyenMon` | `TrangThai` | `hoat_dong`, `tam_dung` | `hoat_dong` |
| 15 | `Phong` | `TrangThai` | `hoat_dong`, `tam_dung` | `hoat_dong` |
| 16 | `NhanVienYTe` | `TrangThaiCongTac` | `dang_cong_tac`, `tam_nghi`, `nghi_viec` | `dang_cong_tac` |

#### G.2 State Machine: Happy Path hiện tại vs Cần bổ sung

##### ① `LichHenKham` — Lịch hẹn

```
HIỆN TẠI (Happy Path):
  dang_cho → da_xac_nhan → da_checkin → [kết thúc]

THIẾU:
  dang_cho → da_huy     ← FE: có nút Hủy trên Appointments.jsx (filter out) nhưng KHÔNG có API call
  da_xac_nhan → da_huy  ← BE: AppointmentService nhận status bất kỳ nhưng KHÔNG validate transition
  da_checkin → [???]     ← Không có trạng thái "đã khám xong" sau checkin
```

| Layer | Hiện tại | Cần bổ sung |
|---|---|---|
| **DB** | Enum 4 giá trị — OK | — |
| **BE** | `UpdateAppointmentStatus` nhận `targetStatus` bất kỳ → gán thẳng, không validate | Thêm **transition validation** (chỉ cho phép dang_cho→da_xac_nhan, da_xac_nhan→da_checkin/da_huy, etc.) |
| **FE** | `Appointments.jsx` có `useUpdateAppointmentStatus` nhưng chỉ gọi cho CheckIn | Thêm nút **Xác nhận** (dang_cho→da_xac_nhan) + nút **Hủy** (→da_huy) với confirm dialog |

##### ② `BenhNhan.TrangThaiHomNay` — Trạng thái khám trong ngày

```
HIỆN TẠI (Happy Path):
  cho_tiep_nhan → cho_kham → dang_kham → cho_xu_ly → hoan_tat

THIẾU:
  Bất kỳ bước nào → da_huy   ← BN bỏ về giữa chừng → không có cách đánh dấu
  cho_xu_ly → cho_kham         ← BN cần quay lại khám sau khi xét nghiệm → không có reverse
```

| Layer | Hiện tại | Cần bổ sung |
|---|---|---|
| **DB** | 10 giá trị (kể cả `_dv`) — OK | — |
| **BE** | `PatientService.UpdateDailyStatus` gán status thẳng, không validate | Thêm **transition matrix** + cho phép `da_huy` từ mọi trạng thái |
| **FE** | `PatientModal` gọi `onMutatePatient(pid, {status})` nhiều nơi | Thêm nút **"BN bỏ về"** → set `da_huy` + confirm dialog |

##### ③ `LuotKhamBenh` — Lượt khám

```
HIỆN TẠI:
  dang_thuc_hien → hoan_tat   ← CHỈ CÓ 2 trạng thái

THIẾU:
  dang_thuc_hien → da_huy     ← Hủy lượt khám (BN không đến / bỏ về)
  Entity THIẾU enum da_huy
```

| Layer | Hiện tại | Cần bổ sung |
|---|---|---|
| **DB** | Chỉ 2 enum — THIẾU | Thêm `da_huy` vào comment + code |
| **BE** | `ClinicalService` chỉ set `hoan_tat` khi lưu chẩn đoán cuối | Thêm method `HuyLuotKham` cho phép hủy + rollback hàng đợi |
| **FE** | Không có nút hủy lượt khám ở đâu cả | Thêm nút **"Hủy lượt khám"** trên `Examination.jsx` |

##### ④ `PhieuKhamLamSang` — Phiếu khám lâm sàng

```
HIỆN TẠI (Happy Path):
  da_lap → dang_thuc_hien → da_lap_chan_doan → da_hoan_tat

THIẾU:
  da_lap → da_huy            ← Có enum nhưng KHÔNG có code gọi
  dang_thuc_hien → da_huy    ← BS hủy phiếu giữa chừng
```

| Layer | Hiện tại | Cần bổ sung |
|---|---|---|
| **DB** | 5 enum kể cả `da_huy` — OK | — |
| **BE** | `ClinicalService.UpdateExamination` nhận `request.TrangThai` gán thẳng | Thêm validate: chỉ cho `da_huy` từ `da_lap`/`dang_thuc_hien` |
| **FE** | `Examination.jsx` không có nút hủy phiếu | Thêm nút **"Hủy phiếu"** khi phiếu ở trạng thái `da_lap` |

##### ⑤ `PhieuKhamCanLamSang` — Phiếu CLS (xét nghiệm)

```
HIỆN TẠI (Happy Path):
  da_lap → dang_thuc_hien → da_hoan_tat

THIẾU:
  StatusEnums.cs ĐÃ CÓ da_huy (Entity comment chưa cập nhật)
  Nhưng KHÔNG có code gọi da_huy ở BE/FE
```

| Layer | Hiện tại | Cần bổ sung |
|---|---|---|
| **DB** | `StatusEnums.cs` ĐÃ CÓ `DaHuy` — Entity comment cần cập nhật | Cập nhật comment Entity cho khớp |
| **BE** | `ClsService` không có method hủy | Thêm method `HuyPhieuCls` |
| **FE** | Không có nút hủy | Thêm nút **"Hủy phiếu CLS"** |

##### ⑥ `DonThuoc` — Đơn thuốc

```
HIỆN TẠI (Happy Path):
  da_ke → cho_phat → da_phat

THIẾU:
  StatusEnums.cs ĐÃ CÓ da_huy (Entity comment chưa cập nhật)
  da_ke → da_huy             ← BS hủy đơn trước khi phát
  cho_phat → da_huy           ← Y tá hủy trước khi phát
  da_phat → hoàn thuốc?       ← Không có reverse (trả thuốc về kho)
```

| Layer | Hiện tại | Cần bổ sung |
|---|---|---|
| **DB** | `StatusEnums.cs` ĐÃ CÓ `DaHuy` — Entity comment cần cập nhật | Cập nhật comment Entity cho khớp |
| **BE** | `PharmacyService` không có method hủy + không rollback kho | Thêm `HuyDonThuoc` + rollback `KhoThuoc.SoLuong` |
| **FE** | `Prescriptions.jsx` chỉ filter theo 3 tab (Đã kê / Chờ phát / Đã phát) | Thêm nút **"Hủy đơn"** + tab "Đã hủy" |

##### ⑦ `HoaDonThanhToan` — Hóa đơn

```
HIỆN TẠI:
  da_thu (mặc định khi tạo)

THIẾU:
  StatusEnums.cs ĐÃ CÓ chua_thu (Entity comment chưa liệt kê)
  - BE: BillingService.UpdateInvoiceStatus gán thẳng → không validate, không rollback
  - FE: billing.js có useUpdateInvoiceStatus nhưng không có nút Hủy trên UI
  Cần dùng chua_thu (ĐÃ CÓ SẴN) thay vì tạo enum mới
```

| Layer | Hiện tại | Cần bổ sung |
|---|---|---|
| **DB** | `StatusEnums.cs` ĐÃ CÓ `ChuaThu` — Entity comment cần cập nhật | Cập nhật comment Entity + đổi default `"da_thu"` → `"chua_thu"` |
| **BE** | Tạo HĐ = `da_thu` luôn → bỏ qua bước chờ thanh toán | Đổi default → `chua_thu`, thêm validate transition |
| **FE** | Không có nút Hủy HĐ | Thêm nút **"Thu tiền"** (chua_thu→da_thu) + **"Hủy hóa đơn"** (→da_huy) |

##### ⑧ `HangDoi` — Hàng đợi

```
HIỆN TẠI (Happy Path):
  cho_goi → dang_goi → da_phuc_vu

KHÔNG CẦN HỦY — khi hủy lượt khám, hàng đợi tự động đánh dấu da_phuc_vu.
→ ĐÃ ĐỦ (trạng thái chỉ cần 3 giá trị)
```

#### G.3 Tóm tắt: Thiếu gì ở mỗi layer?

| Hạng mục | DB (Entity) | BE (Service) | FE (Component) |
|---|---|---|---|
| **Transition validation** | — | ⚠️ Tất cả Service gán `TrangThai` thẳng, không validate | — |
| **Enum `da_huy` thiếu** | `LuotKham` *(StatusEnums.cs cũng thiếu)* | — | — |
| **Entity comment lệch StatusEnums** | `PhieuCLS`, `DonThuoc`, `HoaDon`, `ChiTietDV` *(comment Entity thiếu, StatusEnums.cs đã đủ)* | — | — |
| **Method hủy thiếu** | — | `ClinicalService`, `ClsService`, `PharmacyService`, `BillingService` | — |
| **Nút Hủy trên UI thiếu** | — | — | `Appointments`, `Examination`, `Prescriptions`, Billing |
| **Reverse/Rollback** | — | `PharmacyService` (hoàn thuốc kho) | — |

---

## 3. Nguyên tắc Kiến trúc (từ DB_DESIGN_DEFENSE.md)

| Nguyên tắc | Chi tiết |
|---|---|
| **CQRS** | SQL = Write/Validate (Operational), MongoDB = Read/History (Historical) — DEFENSE 4.11 |
| **One-way Sync** | MySQL → MongoDB (không đồng bộ ngược) — DEFENSE 4.5 |
| **Single Responsibility** | HangDoi (Logistics) ≠ LuotKham (Clinical) — DEFENSE 3.3 |
| **Schema Evolution** | Thêm event_type mới → không cần ALTER TABLE — DEFENSE 2.2 |
| **PhieuTongHop = "Đèn báo"** | Chỉ giữ TrangThai, không lưu chi tiết (chi tiết → MongoDB) — DEFENSE 4.9 |
| **KetQuaDichVu = "Mục lục"** | Chỉ giữ metadata, chi tiết → MongoDB — DEFENSE 4.10 |

---

## 4. Tóm Tắt Khối Lượng Công Việc

| Hạng mục | Khối lượng | Ưu tiên |
|---|---|---|
| MongoDB Setup (Driver, Context, 2 Collections) | ~3h | 🔴 Cao |
| SQL Migration (MaCha, MaMe, CCCD, Entity mới) | ~2h | 🔴 Cao |
| SP `sp_BookAppointment` (SERIALIZABLE) | ~3h | 🔴 Cao |
| Recursive CTE + GenealogyService | ~2h | 🔴 Cao |
| MongoDB ghi lịch sử (tích hợp ClinicalService/ClsService) | ~4h | 🔴 Cao |
| Aggregation Pipeline Analytics | ~3h | 🔴 Cao |
| Entity SQL cập nhật (ERD_Diff: ~10 entity) | ~3h | 🟡 Trung bình |
| LichSuXuatKho + ThongBaoMau entity mới | ~2h | 🟡 Trung bình |
| Audit Logs (MongoDB) | ~2h | 🟡 Trung bình |
| **Phân quyền Backend** (`[RequireRole]` trên 5 Controller) | ~2h | 🟡 Trung bình |
| **Phân quyền Frontend** (Sidebar lọc menu + route guard) | ~3h | 🟡 Trung bình |
| **Trang quản trị Admin** (CRUD người dùng — UC10) | ~4h | 🟡 Trung bình |
| **Luồng trạng thái** (DB enum + BE validate + FE nút Hủy) | ~6h | 🟡 Trung bình |
| Frontend (Timeline, Pha hệ, Analytics, VietQR) | ~8h | 🟡 Trung bình |
| VietQR Integration | ~4h | 🟢 Thấp |
| **Tổng ước tính** | **~51h** | |
