# Hướng dẫn hoàn thành Tuần 4 — Dev 1: Tách User/Staff, RBAC Backend, Thanh Toán Inline, VietQR API, Seed Data & Chuẩn Hóa Contract

> **File:** `UPGRADE_IMPLEMENTATION_PLAN.md` (Mục 3.3, 3.4) + `PROJECT_GAP_ANALYSIS.md` (F.2, F.4)
> **Override:** Tuần 4 gốc (chỉ `[RequireRole]` + VietQR) được mở rộng đáng kể. Tuần 1-3 đã hoàn thành MongoDB, SP, Genealogy, Analytics, Audit, luồng hủy. Tuần 4 tiến hành thay đổi lớn nhất về mô hình dữ liệu & phân quyền.

---

## ⚠️ OVERRIDE TUẦN CŨ

| Nội dung cũ (Tuần 4 gốc) | Xử lý |
|--------------------------|-------|
| `[RequireRole]` trên 5 Controller | **HẤP THU** vào Nhiệm vụ 2 (RBAC Backend toàn diện) — mở rộng thêm data scope |
| VietQR API generation | **GIỮ NGUYÊN** thành Nhiệm vụ 4 |
| Tách User khỏi Staff (Tuần 5 cũ) | **DỒN VÀO** Tuần 4 thành Nhiệm vụ 1 |
| Thanh toán inline (Tuần 5 cũ) | **DỒN VÀO** Tuần 4 thành Nhiệm vụ 3 |
| Chuẩn hóa contract (Tuần 5 cũ) | **DỒN VÀO** Tuần 4 thành Nhiệm vụ 5 |
| DataSeed viết lại (Tuần 5 cũ) | **DỒN VÀO** Tuần 4 thành Nhiệm vụ 6 |
| Testing / Demo / Tài liệu (Tuần 4 cũ) | **CHUYỂN SANG** Tuần 5 (tuần chốt) |

---

## Nhiệm vụ 0: Chuyển NoiDungKetQua SQL → MongoDB + DROP Column 🔴 LÀM TRƯỚC TIÊN

### 0.1 Mục tiêu
Hoàn tất nợ kỹ thuật W2: `KetQuaDichVu = "Mục lục"` — chỉ giữ metadata (LoaiKetQua, KetLuanChuyen, ThoiGianChot) trong SQL, chi tiết đọc từ MongoDB (DEFENSE 4.10). **Làm TRƯỚC tách UserAccount** vì sau migration schema khó sửa hơn.

### 0.2 Bước 1: Sửa ClsService — Chuyển nguồn đọc
- Khi tạo/cập nhật KQ CLS: chỉ ghi `KetLuanChuyen`, `TepDinhKem`, `ThoiGianChot` vào SQL.
- Chi tiết (`chi_so[]`, `mo_ta_hinh_anh`, `noi_dung`) → **chỉ ghi MongoDB** (giữ code dual-write hiện tại).
- Khi đọc KQ CLS (`GetClsResultAsync`, `GetClsOrdersAsync`): lấy chi tiết từ MongoDB thay vì `kq.NoiDungKetQua`.

### 0.3 Bước 2: Sửa HistoryService + ClsResultDto
- `HistoryService.cs` line 220: đọc chi tiết KQ từ MongoDB thay vì `kq.NoiDungKetQua`.
- `ClsResultDto`: xóa field `NoiDungKetQua`, thay bằng `ChiTiet` (object từ MongoDB).
- `ClsResultCreateRequest`: giữ `NoiDungKetQua` cho input nhưng ClsService chỉ ghi MongoDB.

### 0.4 Bước 3: Xóa field + Migration
- Xóa `[Obsolete] NoiDungKetQua` khỏi `KetQuaDichVu.cs`.
- Sửa `DataSeed.cs`: bỏ seed `NoiDungKetQua`.
- Tạo migration `DROP COLUMN NoiDungKetQua`.

### 0.5 File bị ảnh hưởng
- `Services/OutpatientCare/ClsService.cs` (line 683, 693, 725-741, 773, 919)
- `Services/OutpatientCare/HistoryService.cs` (line 220)
- `DTOs/ClsDtos.cs`
- `Entities/KetQuaDichVu.cs`
- `Datas/DataSeed.cs`

### 0.6 Test
- [ ] Tạo KQ CLS → MongoDB có document, SQL KHÔNG có NoiDungKetQua
- [ ] Đọc KQ CLS via API → chi tiết từ MongoDB, không 500
- [ ] HistoryService lịch sử hiện chi tiết KQ từ MongoDB
- [ ] Migration DROP column chạy OK
- [ ] `grep -r "NoiDungKetQua" --include="*.cs"` → chỉ còn trong Request DTO input

---

## Nhiệm vụ 1: Tách Bảng UserAccount khỏi NhanVienYTe ⭐ ƯU TIÊN CAO — LÀM SAU NV0

### 1.1 Mục tiêu
Tạo entity `UserAccount` quản lý xác thực/phân quyền riêng biệt. Quan hệ: `UserAccount` 1:1 `NhanVienYTe`.

> **⚠️ Không chỉ thêm bảng User, mà phải refactor toàn bộ code BE/DB/auth/relationship đang phụ thuộc vào mô hình cũ (NhanVienYTe = User).**

### 1.2 Database & Entity

1. **Tạo entity** `Entities/UserAccount.cs`:
   ```csharp
   [Table("user_accounts")]
   public class UserAccount {
       [Key] public string MaUser { get; set; }
       public string TenDangNhap { get; set; } // UNIQUE
       public string MatKhauHash { get; set; }
       public string VaiTro { get; set; }       // admin, bac_si, y_ta, ky_thuat_vien
       public string? LoaiYTa { get; set; }      // hanhchinh, ls, cls
       public string TrangThaiTaiKhoan { get; set; } = "hoat_dong";
       public DateTime? LanDangNhapCuoi { get; set; }
       public DateTime NgayTao { get; set; }
       public DateTime NgayCapNhat { get; set; }
       // FK
       public string? MaNhanVien { get; set; }
       public NhanVienYTe? NhanVienYTe { get; set; }
       public ICollection<RefreshToken> RefreshTokens { get; set; }
   }
   ```

2. **Sửa `NhanVienYTe.cs`** — **XÓA**: `TenDangNhap`, `MatKhauHash`, `VaiTro`, `LoaiYTa`, `ChucVu`. **GIỮ** mọi trường nhân sự. **THÊM**: `public UserAccount? UserAccount { get; set; }`

3. **Sửa `RefreshToken.cs`** — FK từ `MaNhanVien` → `MaUser`

4. **Sửa `AppDbContext.cs`** — Thêm `DbSet<UserAccount>`, cấu hình 1:1 relationship, FK mới RefreshToken

5. **Migration**:
   ```bash
   dotnet ef migrations add SplitUserFromStaff
   ```

### 1.3 Migration dữ liệu (script SQL chạy THỦ CÔNG trước EF migration)

```sql
-- Bước 1: Tạo bảng + copy dữ liệu
INSERT INTO user_accounts (MaUser, TenDangNhap, MatKhauHash, VaiTro, LoaiYTa, TrangThaiTaiKhoan, MaNhanVien, NgayTao, NgayCapNhat)
SELECT CONCAT('USR_', MaNhanVien), TenDangNhap, MatKhauHash,
  CASE ChucVu WHEN 'y_ta_hanh_chinh' THEN 'y_ta' WHEN 'y_ta_phong_kham' THEN 'y_ta' ELSE ChucVu END,
  LoaiYTa, 'hoat_dong', MaNhanVien, NOW(), NOW()
FROM nhan_vien_y_te;

-- Bước 2: RefreshToken FK
UPDATE refresh_tokens rt
JOIN nhan_vien_y_te nv ON rt.MaNhanVien = nv.MaNhanVien
SET rt.MaUser = CONCAT('USR_', nv.MaNhanVien);

-- Bước 3: (SAU verify) Xóa cột cũ
ALTER TABLE nhan_vien_y_te DROP COLUMN TenDangNhap, DROP COLUMN MatKhauHash, DROP COLUMN VaiTro, DROP COLUMN LoaiYTa, DROP COLUMN ChucVu;
```

### 1.4 File BẮT BUỘC cập nhật sau khi tách

| File | Thay đổi |
|------|----------|
| `Services/UserInteraction/AuthService.cs` | Login query → `UserAccount` (`.Include(u => u.NhanVienYTe)`). JWT claims source từ UserAccount. RefreshToken CRUD đổi FK. |
| `Services/Admin/AdminService.cs` | CRUD → tạo cả UserAccount + NhanVienYTe. Lock/Unlock → `UserAccount.TrangThaiTaiKhoan`. |
| `Controllers/AuthController.cs` | Response DTO mới: `MaUser`, `MaNhanVien`, `VaiTro`, `LoaiYTa`, `HoTen`, `MaKhoa`. |
| `Controllers/AdminController.cs` | Mở rộng: CRUD user+staff, lock/unlock, reset password. |
| `Middlewares/RequireRoleAttribute.cs` | JWT claim key giữ nguyên (`vai_tro`), chỉ verify source mới. |
| `DTOs/*` | Tạo `UserAccountDto`, `CreateUserRequest`, `UpdateUserRequest`. Sửa `LoginResponse`. |
| Mọi Service lấy `currentUser` | Verify claim `MaNhanVien` vẫn có trong JWT khi sign token. |

### 1.5 Bàn giao cho Dev 2
- Login Response DTO mới (JSON)
- JWT Claims list: `{ ma_user, ma_nhan_vien, vai_tro, loai_y_ta, ho_ten, ma_khoa }`
- Seed accounts (username/password)

### 1.6 Test
- [ ] Login user cũ → OK, JWT đủ claims
- [ ] Tạo user mới qua Admin → có cả `user_accounts` + `nhan_vien_y_te`
- [ ] Lock account → login fail 403
- [ ] `grep -r "TenDangNhap" --include="*.cs"` → chỉ còn trong `UserAccount.cs`

---

## Nhiệm vụ 2: RBAC Backend — Data Scope & Action-Level Permission

### 2.1 Mục tiêu
Vượt qua `[RequireRole]` đơn giản → thêm **data scope** theo vai trò/khoa/phòng.

> **Hấp thu** nội dung Tuần 4 cũ ("RequireRole trên 5 Controller") + mở rộng thêm scope filter.

### 2.2 Tạo PermissionService

`Services/Authorization/PermissionService.cs`:
- `GetAccessibleDepartments(user)` → trả danh sách MaKhoa
- `CanCreateAppointment(user)` → y_ta hanhchinh only
- `CanEditPatient(user)` → y_ta hanhchinh + admin
- `CanManageMasterData(user)` → admin only
- `CanViewReport(user, reportType)` → theo ma trận

### 2.3 Sửa Controller — Ma trận đầy đủ

| Controller | Endpoint | Quyền mới |
|------------|----------|-----------|
| `MasterDataController` | GET | `[Authorize]` tất cả |
| `MasterDataController` | POST/PUT/DELETE | `[RequireRole("admin")]` |
| `PatientsController` | GET | `[Authorize]` + scope khoa |
| `PatientsController` | POST/PUT | `[RequireRole("y_ta", "admin")]` chỉ `hanhchinh` CRUD |
| `HistoryController` | GET | `[Authorize]` + scope: BS chỉ trong khoa |
| `ReportsController` | Doanh thu | `[RequireRole("admin", "y_ta")]` chỉ `hanhchinh` |
| `ReportsController` | Lượt khám | `[RequireRole("admin", "bac_si")]` |
| `ReportsController` | Nhân viên | `[RequireRole("admin")]` |
| `AppointmentsController` | GET | BỔ SUNG: admin xem (không tạo/sửa) |
| `DashboardController` | GET | `[Authorize]` + scope: admin/y_ta_hc = global, khác = scope khoa |
| `MasterDataController` | `PUT staff/{id}/status` | `[RequireRole("admin")]` — FE `staff.js` đã gọi nhưng BE chưa có |

### 2.4 Implement Data Scope ở Service Layer

Các Service cần thêm scope filter:
- `DashboardService.cs` — KPI scope theo khoa/phòng
- `QueueService.cs` — hàng đợi chỉ hiện BN trong phòng user trực
- `PatientService.cs` — danh sách BN scope theo khoa
- `HistoryService.cs` — lịch sử scope theo bác sĩ/khoa
- `ReportService.cs` — dữ liệu báo cáo scope theo quyền

### 2.5 Test
- [ ] Login Admin → Dashboard = toàn phòng khám
- [ ] Login Bác sĩ → Dashboard = chỉ khoa BS đang thuộc
- [ ] POST /api/master-data bằng BS → 403
- [ ] Admin GET /api/appointments → 200 (xem), POST → 403

---

## Nhiệm vụ 3: Luồng Thanh Toán Inline

### 3.1 Mục tiêu
Tạo phiếu → sinh hóa đơn `chua_thu` → FE chuyển bước thu tiền TRƯỚC KHI tiếp tục workflow.

### 3.2 Việc cần làm

1. **`ClinicalService.TaoPhieuKhamAsync`** — Đổi default `da_thu` → `chua_thu`. Response trả thêm `MaHoaDon`.
2. **`BillingService`** — Thêm:
   - `PUT /api/billing/{id}/confirm` → `chua_thu` → `da_thu` (body: PhuongThuc, MaGiaoDich)
   - `PUT /api/billing/{id}/cancel` → `chua_thu` → `da_huy`
   - Validate: `da_thu` → bất kỳ = REJECT (terminal)
3. **`ClsService`** — Tương tự: hóa đơn CLS `chua_thu`.
4. **`PharmacyService`** — Hóa đơn thuốc `chua_thu`. Phát thuốc chỉ khi đã `da_thu`.

### 3.3 File bị ảnh hưởng
- `BillingService.cs`, `ClinicalService.cs`, `ClsService.cs`, `PharmacyService.cs`
- `BillingController.cs` — endpoints mới
- `DTOs/PaymentConfirmRequest.cs` [NEW]

### 3.4 Test
- [ ] Tạo phiếu → hóa đơn `chua_thu`
- [ ] Confirm → `da_thu`, không cho đổi lại
- [ ] Phát thuốc khi hóa đơn `chua_thu` → Reject

---

## Nhiệm vụ 4: VietQR API (Giữ từ Tuần 4 cũ)

### 4.1 Việc cần làm (giữ nguyên)

1. **Sửa Enum** `HoaDonThanhToan.PhuongThucThanhToan` → hỗ trợ `tien_mat`, `the`, `chuyen_khoan`, `vietqr`.
2. **Tạo `BankingService.cs`** → Generate QR chuẩn VietQR/Napas. API: `POST /api/billing/{id}/generate-qr` → trả Base64 QR image.

### 4.2 Test
- [ ] Gọi API generate-qr → trả về QR Base64 hợp lệ
- [ ] QR chứa đúng: BankID, AccountNo, Amount, Memo (MaHoaDon)

---

## Nhiệm vụ 5: Chuẩn Hóa API Contract FE-BE

### 5.1 Rule chung
- Response: **PascalCase** (C# convention)
- Enum: **snake_case** (`chua_thu`, `da_thu`)
- Date: **ISO 8601**
- ID: prefix `Ma` (`MaBenhNhan`, `MaPhieuKham`)
- Error: `{ Code, Message, Details }`
- Pagination: `{ Items, TotalItems, Page, PageSize }`

### 5.2 Rà soát cụ thể
- `BillingController` — chuẩn hóa `SoTien` vs `soTien`
- `QueueController` — thống nhất `MaPhieuKham` (LS) vs `MaPhieuKhamCls` (CLS)
- `DashboardController` — KPI shape: `{ Label, Value, Change }`
- Tạo `StatusEnums.md` hoặc endpoint `GET /api/master-data/enums`

### 5.3 Bàn giao
- File `StatusEnums.md` + Swagger spec → Dev 2

---

## Nhiệm vụ 6: Viết Lại DataSeed.cs

### 6.1 Dữ liệu seed BẮT BUỘC

| Nhóm | Số lượng | Ghi chú |
|------|----------|---------|
| Khoa | 4 | Nội, Ngoại, XN, CDHH |
| Phòng | 8 | 2/khoa |
| UserAccount | 10 | 1 admin, 2 y_ta_hc, 2 y_ta_ls, 1 y_ta_cls, 2 bac_si, 2 ktv |
| NhanVienYTe | 10 | 1:1 UserAccount |
| LichTruc | 15 | Hôm nay + 7 ngày, mỗi phòng ≥2 ca |
| DichVu | 8 | 3 LS + 5 CLS |
| KhoThuoc | 10 | Paracetamol, Amoxicillin... |
| BenhNhan | 5 | 3 thường, 1 BHYT, 1 trẻ em (có MaCha/MaMe) |
| LichHen | 3 | `dang_cho`, `da_xac_nhan`, `da_huy` |

### 6.2 Seed accounts cho Dev 2

| Username | Password | Vai trò |
|----------|----------|---------|
| admin | Admin@123 | admin |
| yta_hc_01 | YTa@123 | y_ta hanhchinh |
| yta_ls_01 | YTa@123 | y_ta ls |
| bs_noi_01 | BacSi@123 | bac_si |
| ktv_xn_01 | KTV@123 | ky_thuat_vien |

### 6.3 Test
- [ ] Seed chạy không lỗi FK/unique
- [ ] Login mọi vai trò OK
- [ ] Flow end-to-end: đặt lịch → check-in → khám LS → CLS → kê đơn → phát thuốc → hoàn tất

---

## Nhiệm vụ 7: Mở Rộng Admin Management

Bổ sung endpoints cho Admin quản lý rộng hơn:

| Endpoint | Method | Mô tả |
|----------|--------|-------|
| `/api/admin/users` | GET/POST | List + tạo user+staff |
| `/api/admin/users/{id}` | GET/PUT | Chi tiết + sửa |
| `/api/admin/users/{id}/lock` | PUT | Khóa tài khoản |
| `/api/admin/users/{id}/unlock` | PUT | Mở khóa |
| `/api/admin/users/{id}/reset-password` | POST | Reset mật khẩu |
| `/api/admin/schedules` | GET/POST/PUT | Lịch trực |

---

## Handoff cho Dev 2

| # | Item bàn giao | Khi nào |
|---|---------------|---------|
| 1 | JWT Claims + Login DTO mới | Sau Nhiệm vụ 1 (ngày 1-2) |
| 2 | Permission Matrix JSON | Sau Nhiệm vụ 2 (ngày 2-3) |
| 3 | Payment Confirm endpoint spec | Sau Nhiệm vụ 3 (ngày 3) |
| 4 | StatusEnums.md | Sau Nhiệm vụ 5 (ngày 4) |
| 5 | Seed accounts table | Sau Nhiệm vụ 6 (ngày 4-5) |

---

## Rủi Ro

| # | Rủi ro | Phòng tránh |
|---|--------|-------------|
| 1 | Sửa NhanVienYTe quên chỗ query `.TenDangNhap` cũ | `grep -r "TenDangNhap" --include="*.cs"` |
| 2 | RefreshToken FK migration lỗi | Script SQL thủ công TRƯỚC EF migration |
| 3 | Đổi default hóa đơn `chua_thu` → FE cũ vỡ | Bàn giao Dev 2 song song |
| 4 | Seed thiếu lịch trực → đặt lịch fail | Seed ≥7 ngày × mỗi phòng ≥2 ca |
| 5 | Data scope trả ít bản ghi → FE tưởng lỗi | Document rõ: shape giữ nguyên, số lượng thay đổi |
| 6 | NV0 DROP NoiDungKetQua → HistoryService/ClsService crash | Test đọc KQ trước khi merge NV1 |

---

## 📌 GHI CHÚ: 8 cột y tế BenhNhan → MongoDB

> Mục 3.7 trong `UPGRADE_IMPLEMENTATION_PLAN.md` (chuyển 8 cột `DiUng`, `ChongChiDinh`, `ThuocDangDung`, `TieuSuBenh`, `TienSuPhauThuat`, `NhomMau`, `BenhManTinh`, `SinhHieu` → MongoDB `medicalProfile`).
>
> **Quyết định**: Thực hiện **SAU Week 5** nếu còn thời gian. Lý do: không ảnh hưởng tính năng W4-5 (tách User/RBAC/thanh toán). Dual-write đang hoạt động, SQL vẫn là nguồn đọc chính cho 8 cột này.
