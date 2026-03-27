# Hướng dẫn hoàn thành Tuần 5 — Dev 1: Tách User/Staff, RBAC Backend, Thanh Toán Inline, Seed Data & Chuẩn Hóa Contract

> **File gốc:** `UPGRADE_IMPLEMENTATION_PLAN.md` (Giai đoạn 3.4, 4.2) + `PROJECT_GAP_ANALYSIS.md` (F.2, F.4)
> **Mở rộng & Override:** Tuần 1-4 đã hoàn thành MongoDB, SP, Genealogy, Analytics, Audit, VietQR, Sidebar filter cơ bản, `[RequireRole]` cơ bản. Tuần 5 **thay đổi tư duy phân quyền** từ "ẩn menu" → "hiện rộng, khóa chức năng, scope dữ liệu", đồng thời **tách bảng User riêng khỏi NhanVienYTe** và **thêm luồng thanh toán inline**.

---

## ⚠️ OVERRIDE so với Tuần 1-4

1. **Phân quyền cũ** (Tuần 1 Dev 2 + Tuần 4 Dev 1) chủ yếu ẩn menu Sidebar + `[RequireRole]` đơn giản. Tuần 5 **OVERRIDE** bằng mô hình phân quyền 7 tầng (menu → route guard → page → component → action → data-scope BE → data-scope FE).
2. **Bảng `NhanVienYTe`** hiện đang chứa cả thông tin đăng nhập (`TenDangNhap`, `MatKhauHash`, `RefreshToken`) và thông tin nhân sự. Tuần 5 **tách** thành 2 entity riêng: `UserAccount` (đăng nhập / quyền) và `NhanVienYTe` (hồ sơ nghề nghiệp).
3. **Luồng tạo phiếu** cũ không có bước thanh toán inline. Tuần 5 **thêm** endpoint transaction "tạo phiếu + sinh hóa đơn + đợi thu tiền" theo wizard flow.
4. **DataSeed.cs** cũ đã không còn đồng bộ với ERD mới (thiếu user account, lịch trực mẫu, dịch vụ CLS, thuốc...). Tuần 5 **viết lại toàn bộ** DataSeed cho đúng mô hình mới.

---

## Nhiệm vụ 1: Tách Bảng UserAccount khỏi NhanVienYTe

### 1.1 Mục tiêu
Tạo entity `UserAccount` để quản lý xác thực/phân quyền riêng biệt với hồ sơ nhân sự. Quan hệ: `UserAccount` 1:1 `NhanVienYTe`.

### 1.2 Việc cần làm

#### A. Database & Entity

1. **Tạo entity mới** `Entities/UserAccount.cs`:
   ```csharp
   [Table("user_accounts")]
   public class UserAccount {
       [Key] public string MaUser { get; set; }
       public string TenDangNhap { get; set; } // UNIQUE
       public string MatKhauHash { get; set; }
       public string VaiTro { get; set; }      // admin, bac_si, y_ta, ky_thuat_vien
       public string? LoaiYTa { get; set; }     // hanhchinh, ls, cls (nullable)
       public string TrangThaiTaiKhoan { get; set; } = "hoat_dong"; // hoat_dong, tam_khoa
       public DateTime? LanDangNhapCuoi { get; set; }
       public DateTime NgayTao { get; set; }
       public DateTime NgayCapNhat { get; set; }
       // FK → NhanVienYTe
       public string? MaNhanVien { get; set; }  // NULLABLE: user admin có thể không phải nhân viên y tế
       public NhanVienYTe? NhanVienYTe { get; set; }
       public ICollection<RefreshToken> RefreshTokens { get; set; }
   }
   ```

2. **Sửa entity `NhanVienYTe.cs`** — Xóa các trường xác thực:
   - **XÓA**: `TenDangNhap`, `MatKhauHash`, `VaiTro`, `LoaiYTa`, `ChucVu` (chuyển sang UserAccount)
   - **GIỮ**: `MaNhanVien`, `HoTen`, `Email`, `DienThoai`, `ChuyenMon`, `HocVi`, `SoNamKinhNghiem`, `AnhDaiDien`, `MoTa`, `TrangThaiCongTac`, `MaKhoa`, navigation properties (trừ `RefreshTokens`)
   - **THÊM**: `public UserAccount? UserAccount { get; set; }` (reverse navigation)

3. **Sửa `RefreshToken.cs`** — đổi FK từ `MaNhanVien` → `MaUser`:
   ```csharp
   public string MaUser { get; set; } // FK → UserAccount
   public UserAccount UserAccount { get; set; }
   ```

4. **Sửa `AppDbContext.cs`**:
   - Thêm `DbSet<UserAccount> UserAccounts`
   - Cấu hình relationship 1:1 (`UserAccount` → `NhanVienYTe`)
   - Cấu hình FK mới cho `RefreshToken` → `UserAccount`
   - Xóa cấu hình cũ `NhanVienYTe` → `RefreshToken`

5. **Tạo Migration**:
   ```bash
   dotnet ef migrations add SplitUserFromStaff
   ```

### 1.3 Migration dữ liệu (KHÔNG drop bừa)

Tạo script SQL chạy TRƯỚC khi xóa cột cũ trên `NhanVienYTe`:

```sql
-- Bước 1: Tạo bảng user_accounts từ dữ liệu cũ
INSERT INTO user_accounts (MaUser, TenDangNhap, MatKhauHash, VaiTro, LoaiYTa, TrangThaiTaiKhoan, MaNhanVien, NgayTao, NgayCapNhat)
SELECT
  CONCAT('USR_', MaNhanVien),
  TenDangNhap,
  MatKhauHash,
  CASE ChucVu
    WHEN 'y_ta_hanh_chinh' THEN 'y_ta'
    WHEN 'y_ta_phong_kham' THEN 'y_ta'
    WHEN 'admin' THEN 'admin'
    ELSE ChucVu
  END,
  LoaiYTa,
  'hoat_dong',
  MaNhanVien,
  NOW(), NOW()
FROM nhan_vien_y_te;

-- Bước 2: Cập nhật RefreshToken FK
UPDATE refresh_tokens rt
JOIN nhan_vien_y_te nv ON rt.MaNhanVien = nv.MaNhanVien
SET rt.MaUser = CONCAT('USR_', nv.MaNhanVien);

-- Bước 3: (SAU KHI verify OK) Xóa cột cũ trên nhan_vien_y_te
ALTER TABLE nhan_vien_y_te
  DROP COLUMN TenDangNhap,
  DROP COLUMN MatKhauHash,
  DROP COLUMN VaiTro,
  DROP COLUMN LoaiYTa,
  DROP COLUMN ChucVu;
```

### 1.4 File bị ảnh hưởng (BẮT BUỘC cập nhật)

| File | Hành động |
|------|-----------|
| `Services/UserInteraction/AuthService.cs` | Đổi login query từ `NhanVienYTe` → `UserAccount` (bao gồm `.Include(u => u.NhanVienYTe)`). Đổi JWT claims: `VaiTro` lấy từ UserAccount, `MaNhanVien` lấy từ `UserAccount.MaNhanVien`. Đổi RefreshToken CRUD. |
| `Services/Admin/AdminService.cs` | CRUD → tạo cả UserAccount + NhanVienYTe. Lock/Unlock → sửa `UserAccount.TrangThaiTaiKhoan`. Reset password → sửa `UserAccount.MatKhauHash`. |
| `Controllers/AuthController.cs` | Kiểm tra response DTO: trả về `MaUser`, `MaNhanVien`, `VaiTro`, `LoaiYTa`, `HoTen` từ join 2 bảng. |
| `Controllers/AdminController.cs` | Sửa CRUD endpoints: tạo user + staff đồng thời. Thêm endpoint GET/PUT/DELETE chỉ cho UserAccount (khóa tài khoản, reset mật khẩu). |
| `Middlewares/RequireRoleAttribute.cs` | JWT claim `vai_tro` vẫn giữ nguyên (không đổi key), chỉ cần verify source từ UserAccount. |
| `DTOs/*` | Tạo `UserAccountDto`, `CreateUserRequest`, `UpdateUserRequest`. Sửa `LoginResponse` thêm `MaUser`. |
| Mọi Service lấy `currentUser` | Kiểm tra lại: nếu service lấy `MaNhanVien` từ JWT Claims thì cần bổ sung claim `MaNhanVien` trong AuthService.cs khi sign JWT. |

### 1.5 Dependency với Dev 2
- Dev 2 cần **response DTO mới** của `/api/auth/login` để cập nhật `useAuthStore`.
- Dev 2 cần biết rõ **danh sách claims trong JWT** mới: `ma_user`, `ma_nhan_vien`, `vai_tro`, `loai_y_ta`, `ho_ten`, `ma_khoa`.
- **Bàn giao contract trước khi Dev 2 sửa FE store/hook**.

### 1.6 Test
- [ ] Login với user cũ → vẫn thành công. JWT chứa đủ claims.
- [ ] Tạo user mới qua Admin → có cả `user_accounts` + `nhan_vien_y_te`.
- [ ] Lock account → login fail trả 403, thông tin nhân sự vẫn giữ.
- [ ] Staff info API → không chứa thông tin mật khẩu/đăng nhập.

---

## Nhiệm vụ 2: RBAC Backend — Data Scope & Action-Level Permission

### 2.1 Mục tiêu
Chuyển từ chỉ check `[RequireRole]` trên endpoint → **scope data trả về** theo vai trò/khoa/phòng của người dùng.

### 2.2 Việc cần làm

#### A. Tạo Permission Service trung tâm

Tạo `Services/Authorization/PermissionService.cs`:
```csharp
public class PermissionService {
    // Trả danh sách MaKhoa/MaPhong mà user có quyền truy cập
    public IList<string> GetAccessibleDepartments(UserAccount user);
    public IList<string> GetAccessibleRooms(UserAccount user);
    
    // Kiểm tra hành động cụ thể
    public bool CanCreateAppointment(UserAccount user); // y_ta hanhchinh + admin
    public bool CanEditPatient(UserAccount user);        // y_ta hanhchinh + admin
    public bool CanViewPatient(UserAccount user);        // tất cả
    public bool CanManageMasterData(UserAccount user);   // admin only
    public bool CanViewReport(UserAccount user, string reportType);
    public bool CanDispenseMedicine(UserAccount user);   // y_ta hanhchinh
    public bool CanPrescribe(UserAccount user);          // bac_si
}
```

#### B. Sửa từng Controller/Service theo ma trận phân quyền mới

| Controller | Endpoint(s) | Quyền cũ | Quyền mới (Override) |
|------------|-------------|-----------|---------------------|
| `MasterDataController` | GET (Xem) | `[Authorize]` | `[Authorize]` — GIỮ (tất cả xem) |
| `MasterDataController` | POST/PUT/DELETE | `[Authorize]` | `[RequireRole("admin")]` — **THÊM** |
| `PatientsController` | GET | `[RequireRole("y_ta")]` | `[Authorize]` + scope theo khoa — **SỬA** |
| `PatientsController` | POST/PUT | `[RequireRole("y_ta")]` | `[RequireRole("y_ta", "admin")]` + chỉ `LoaiYTa=hanhchinh` CRUD, admin CRUD — **SỬA** |
| `HistoryController` | GET | `[Authorize]` | `[Authorize]` + scope: bác sĩ chỉ thấy lịch sử BN trong khoa mình — **THÊM FILTER** |
| `ReportsController` | Doanh thu | `[Authorize]` | `[RequireRole("admin", "y_ta")]` chỉ `LoaiYTa=hanhchinh` — **SỬA** |
| `ReportsController` | Lượt khám | `[Authorize]` | `[RequireRole("admin", "bac_si")]` — **SỬA** |
| `ReportsController` | Kho thuốc | `[Authorize]` | `[RequireRole("admin", "y_ta")]` chỉ `LoaiYTa=hanhchinh` — **SỬA** |
| `ReportsController` | Nhân viên | `[Authorize]` | `[RequireRole("admin")]` — **SỬA** |
| `AppointmentsController` | Tất cả | `[RequireRole("y_ta")]` | BỔ SUNG: `admin` xem (GET) nhưng không tạo/sửa. Y tá HC = full. — **SỬA** |
| `DashboardController` | GET | `[Authorize]` | `[Authorize]` + **scope**: admin/y_ta_hc = global; khác = scope theo khoa/phòng — **THÊM FILTER** |
| `ClinicalController` | Visits cancel | `[RequireRole("y_ta")]` | `[RequireRole("y_ta", "admin")]` — **SỬA** cho admin xem |
| `AdminController` | Tất cả | `[RequireRole("admin")]` | GIỮ — chỉ admin |

#### C. Implement Data Scope ở Service Layer

Trong mỗi Service có query danh sách, inject `PermissionService` để filter:

```csharp
// Ví dụ: DashboardService.cs
public async Task<DashboardData> GetDashboardAsync(UserAccount user) {
    var data = new DashboardData();
    if (user.VaiTro == "admin" || (user.VaiTro == "y_ta" && user.LoaiYTa == "hanhchinh")) {
        // Scope: toàn bộ
        data.TotalVisits = await _context.LuotKham.CountAsync(x => x.ThoiGianBatDau.Date == today);
    } else {
        // Scope: theo khoa của user
        var maKhoa = user.NhanVienYTe?.MaKhoa;
        data.TotalVisits = await _context.LuotKham
            .Where(x => x.HangDoi.Phong.MaKhoa == maKhoa && x.ThoiGianBatDau.Date == today)
            .CountAsync();
    }
    return data;
}
```

**Các Service cần thêm scope filter:**
- `DashboardService.cs` — KPI/lịch hẹn scope theo khoa/phòng
- `QueueService.cs` — hàng đợi chỉ hiện BN trong phòng user trực
- `PatientService.cs` — danh sách BN scope theo khoa
- `HistoryService.cs` — lịch sử scope theo bác sĩ/khoa
- `AppointmentService.cs` — lịch hẹn scope theo phòng
- `ReportService.cs` — dữ liệu báo cáo scope theo quyền

### 2.3 Dependency với Dev 2
- Mỗi API endpoint giờ sẽ trả data đã scope → Dev 2 cần biết response shape không thay đổi, chỉ số lượng bản ghi thay đổi theo role.
- Dev 2 cần nhận **ma trận permissions** từ Dev 1 để implement disable/read-only UI.

### 2.4 Test
- [ ] Login Admin → GET /api/dashboard → trả data toàn phòng khám
- [ ] Login Bác sĩ → GET /api/dashboard → trả data chỉ khoa BS đang thuộc
- [ ] Login Y_ta_CLS → GET /api/patients → chỉ thấy BN đang có chỉ định CLS
- [ ] Gọi POST /api/master-data/departments bằng Bác sĩ → 403
- [ ] Gọi GET /api/reports/revenue bằng KTV → 403
- [ ] Admin GET /api/appointments → 200 (xem), POST → 403 (không tự đặt lịch)

---

## Nhiệm vụ 3: Luồng Thanh Toán Inline (Tạo Phiếu → Thu Tiền)

### 3.1 Mục tiêu
Khi tạo phiếu khám LS/CLS/đơn thuốc, sinh hóa đơn với trạng thái `chua_thu` ngay, rồi FE chuyển sang bước thu tiền trước khi tiếp tục workflow.

### 3.2 Việc cần làm

#### A. Sửa luồng tạo phiếu hiện tại

1. **`ClinicalService.TaoPhieuKhamAsync`** (hiện tại đã auto-billing, tạo hóa đơn `da_thu`):
   - **SỬA**: Đổi default `HoaDonThanhToan.TrangThai` từ `"da_thu"` → `"chua_thu"`.
   - **SỬA**: Response trả về thêm `MaHoaDon` để FE biết hóa đơn nào cần thu.

2. **`BillingService`** — Thêm endpoint xác nhận thu tiền:
   - `PUT /api/billing/{maHoaDon}/confirm` → chuyển `chua_thu` → `da_thu`.
   - Validate: chỉ cho transition `chua_thu` → `da_thu` hoặc `chua_thu` → `da_huy`.
   - **KHÔNG cho**: `da_thu` → bất kỳ (đã thu = final).
   - Khi `da_thu`: ghi thêm `PhuongThucThanhToan`, `MaGiaoDich` (nếu QR), `ThoiGian`.

3. **Tạo endpoint gộp** `POST /api/clinical/create-with-payment`:
   - Input: thông tin phiếu khám + loại thanh toán
   - Logic: Tạo phiếu → Tạo hóa đơn `chua_thu` → Trả về `{ MaPhieuKham, MaHoaDon, SoTien }` → FE hiển thị bước thu tiền.
   - Transaction: Nếu tạo phiếu thất bại → không sinh hóa đơn. Nếu tạo phiếu OK nhưng user cancel thanh toán → hóa đơn ở `chua_thu` (chờ thu sau).

4. **Logic tương tự cho CLS và Thuốc**:
   - `POST /api/cls/orders` hiện tại đã auto-billing → sửa tương tự.
   - `PharmacyService.XuatThuocAsync` → khi phát thuốc, hóa đơn thuốc phải `chua_thu` trước.

#### B. Sửa `BillingController.cs`
- Thêm endpoint: `PUT /api/billing/{id}/confirm`
- Thêm endpoint: `PUT /api/billing/{id}/cancel` (hủy hóa đơn chưa thu)
- Response DTO thêm: `PhuongThucThanhToan`, `MaGiaoDichVietQR`

### 3.3 File bị ảnh hưởng

| File | Thay đổi |
|------|----------|
| `Services/MedicationBilling/BillingService.cs` | Thêm `ConfirmPayment`, `CancelPayment`. Đổi default status. |
| `Services/OutpatientCare/ClinicalService.cs` | Đổi auto-billing default `da_thu` → `chua_thu`. Trả thêm `MaHoaDon`. |
| `Services/OutpatientCare/ClsService.cs` | Tương tự. |
| `Services/MedicationBilling/PharmacyService.cs` | Đổi hóa đơn thuốc → `chua_thu`. |
| `Controllers/BillingController.cs` | Thêm endpoints confirm/cancel. |
| `Controllers/ClinicalController.cs` | Response DTO thêm MaHoaDon. |
| `DTOs/PaymentConfirmRequest.cs` | [NEW] `PhuongThucThanhToan`, `MaGiaoDich` |

### 3.4 Dependency với Dev 2
- Dev 2 cần: endpoint URL, request/response shape, status transition rules.
- Dev 2 xây wizard/stepper UI gọi 2 bước: (1) tạo phiếu → (2) confirm payment.

### 3.5 Test
- [ ] Tạo phiếu khám → hóa đơn tạo với `chua_thu`
- [ ] Xác nhận thu tiền → chuyển `da_thu`, không cho đổi lại
- [ ] Hủy hóa đơn chưa thu → `da_huy`, phiếu khám vẫn tồn tại
- [ ] Tạo phiếu CLS + thu tiền → workflow xong mới cho bắt đầu CLS
- [ ] Tạo đơn thuốc → phát thuốc chỉ khi hóa đơn thuốc `da_thu`

---

## Nhiệm vụ 4: Chuẩn Hóa API Contract FE-BE

### 4.1 Mục tiêu
Thống nhất naming convention, response shape, enum values, error format giữa FE và BE.

### 4.2 Việc cần làm

#### A. Naming Convention Chuẩn

**Rule chung**: API response dùng **PascalCase** (C# convention). FE adapter layer map sang camelCase.

| Aspect | Chuẩn | Ví dụ |
|--------|-------|-------|
| Response field | PascalCase | `MaBenhNhan`, `TrangThai`, `HoTen` |
| Enum values | snake_case | `chua_thu`, `da_thu`, `dang_thuc_hien` |
| Date format | ISO 8601 | `2026-03-28T00:00:00Z` |
| ID field | `Ma` prefix | `MaBenhNhan`, `MaPhieuKham`, `MaHoaDon` |
| Error response | `{ Code, Message, Details }` | `{ Code: "CONFLICT", Message: "Trùng lịch", Details: {} }` |
| Pagination | `{ Items, TotalItems, Page, PageSize }` | Consistent across all list endpoints |

#### B. Rà soát và sửa

1. **Sửa `BillingController`** — response hiện tại trả `SoTien` không nhất quán (đôi khi `soTien`).
2. **Sửa `QueueController`** — response hàng đợi có trường `MaPhieuKham` vs `MaPhieuKhamLs` không nhất quán → thống nhất sang `MaPhieuKham` cho LS, `MaPhieuKhamCls` cho CLS.
3. **Sửa `DashboardController`** — response KPI thống nhất shape: `{ Label, Value, Change }`.
4. **Tạo file `StatusEnums.md`** (hoặc API endpoint `GET /api/master-data/enums`) — liệt kê mọi enum/status/transition để FE và BE cùng bám sát.

### 4.3 File bị ảnh hưởng

| File | Thay đổi |
|------|----------|
| `DTOs/*.cs` | Review toàn bộ, chuẩn hóa PascalCase, thêm missing fields |
| Mọi Controller | Response shape consistent |
| `StatusEnums.cs` | Bổ sung documentation / endpoint trả reference |

### 4.4 Dependency với Dev 2
- Dev 2 cập nhật toàn bộ `src/api/*.js` adapter layer theo contract mới.
- **Bàn giao**: file `StatusEnums.md` + Swagger/OpenAPI spec.

### 4.5 Test
- [ ] Swagger UI: mọi endpoint trả PascalCase field names
- [ ] Enum values đồng nhất giữa StatusEnums.cs và response
- [ ] Error response shape: `{ Code, Message, Details }`
- [ ] Pagination đúng `{ Items, TotalItems, Page, PageSize }`

---

## Nhiệm vụ 5: Viết Lại DataSeed.cs Theo ERD Mới

### 5.1 Mục tiêu
Tạo bộ seed data hoàn chỉnh để chạy được **toàn bộ** luồng khám ngoại trú end-to-end.

### 5.2 Dữ liệu cần seed (PHẢI ĐỦ)

| Nhóm | Số lượng | Chi tiết |
|------|----------|----------|
| **Khoa** | 4 | Nội tổng quát, Ngoại, Xét nghiệm, Chẩn đoán hình ảnh |
| **Phòng** | 8 | 2 phòng khám LS/khoa Nội + Ngoại, 2 phòng CLS/khoa XN + CDHH, 2 phòng dịch vụ |
| **UserAccount** | 10 | 1 admin, 2 y_ta hanhchinh, 2 y_ta ls, 1 y_ta cls, 2 bac_si, 2 ky_thuat_vien |
| **NhanVienYTe** | 10 | Tương ứng 1:1 UserAccount. Gán đúng MaKhoa. |
| **LichTruc** | 10-15 | Ca sáng/chiều cho mỗi phòng, ngày hôm nay + 3 ngày tới |
| **DichVu** | 8 | 3 dịch vụ khám LS (tổng quát, nội, ngoại), 5 dịch vụ CLS (máu, nước tiểu, X-quang, siêu âm, CT) |
| **KhoThuoc** | 10 | Thuốc phổ biến: Paracetamol, Amoxicillin, Ibuprofen, Omeprazole, Metformin... |
| **BenhNhan** | 5 | 3 BN thường, 1 BHYT, 1 trẻ em (có MaCha/MaMe link). Đủ CCCD. |
| **LichHen** | 3 | 1 `dang_cho`, 1 `da_xac_nhan`, 1 `da_huy` |

### 5.3 File bị ảnh hưởng

| File | Thay đổi |
|------|----------|
| `Datas/DataSeed.cs` | **VIẾT LẠI TOÀN BỘ** theo ERD mới (bao gồm UserAccount) |
| `Program.cs` | Đảm bảo `DataSeed.Initialize(app)` vẫn chạy đúng |

### 5.4 Logic seed

```csharp
public static class DataSeed
{
    public static void Initialize(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        if (context.UserAccounts.Any()) return; // Chỉ seed lần đầu
        
        // 1. Khoa
        // 2. Phòng (gán MaKhoa)
        // 3. UserAccount + NhanVienYTe (link 1:1, gán MaKhoa)
        // 4. LichTruc (link Phòng + NhanVienYTe)
        // 5. DichVu (link Phòng)
        // 6. KhoThuoc
        // 7. BenhNhan (bao gồm MaCha/MaMe cho family)
        // 8. LichHen (link BenhNhan + LichTruc)
        
        context.SaveChanges();
    }
}
```

### 5.5 Dependency với Dev 2
- Dev 2 cần bộ tài khoản mẫu để test login:
  - Admin: `admin / Admin@123`
  - Y tá HC: `yta_hc_01 / YTa@123`
  - Bác sĩ: `bs_noi_01 / BacSi@123`
  - KTV: `ktv_xn_01 / KTV@123`

### 5.6 Test
- [ ] Seed chạy qua không lỗi FK/unique
- [ ] Login mọi vai trò → thành công
- [ ] Flow đặt lịch → check-in → khám LS → chỉ định CLS → nhập kết quả → kê đơn → phát thuốc → hoàn tất
- [ ] Dashboard hiện dữ liệu seed
- [ ] Pha hệ: BN trẻ em có link tới cha/mẹ

---

## Nhiệm vụ 6: Quản Lý Admin Mở Rộng

### 6.1 Mục tiêu
Bổ sung endpoint admin quản lý: dịch vụ, khoa phòng, lịch trực, user/account, role/quyền.

### 6.2 Việc cần làm

**Phần này mở rộng AdminController (đã có từ Tuần 3):**

| Endpoint | Method | Mô tả |
|----------|--------|-------|
| `/api/admin/users` | GET | Lấy danh sách user accounts (join staff info) |
| `/api/admin/users/{id}` | GET | Chi tiết user + staff profile |
| `/api/admin/users` | POST | Tạo user + staff đồng thời |
| `/api/admin/users/{id}` | PUT | Sửa thông tin user + staff |
| `/api/admin/users/{id}/lock` | PUT | Khóa tài khoản |
| `/api/admin/users/{id}/unlock` | PUT | Mở khóa |
| `/api/admin/users/{id}/reset-password` | POST | Reset mật khẩu |
| `/api/admin/services` | POST/PUT/DELETE | Admin CRUD dịch vụ (redirect từ MasterData) |
| `/api/admin/schedules` | GET/POST/PUT | Admin quản lý lịch trực |
| `/api/admin/departments` | POST/PUT/DELETE | Admin CRUD khoa (redirect từ MasterData) |

### 6.3 File bị ảnh hưởng
- `Controllers/AdminController.cs` — mở rộng thêm endpoints
- `Services/Admin/AdminService.cs` — thêm methods
- `Services/Admin/IAdminService.cs` — thêm interface

### 6.4 Dependency với Dev 2
- Dev 2 xây UI cho trang "QL Nhân viên" (riêng biệt với trang "Staff")
- Dev 2 cần danh sách endpoint + response shape

---

## Phần Handoff / Integration với Dev 2

### Contract cần bàn giao TRƯỚC KHI Dev 2 bắt tay

1. **JWT Claims mới** (sau khi tách UserAccount):
   ```json
   {
     "ma_user": "USR_NV001",
     "ma_nhan_vien": "NV001",
     "vai_tro": "bac_si",
     "loai_y_ta": null,
     "ho_ten": "Nguyễn Văn A",
     "ma_khoa": "KHOA_NOI"
   }
   ```

2. **Login Response DTO mới**:
   ```json
   {
     "MaUser": "USR_NV001",
     "MaNhanVien": "NV001",
     "HoTen": "Nguyễn Văn A",
     "VaiTro": "bac_si",
     "LoaiYTa": null,
     "MaKhoa": "KHOA_NOI",
     "AccessToken": "...",
     "RefreshToken": "..."
   }
   ```

3. **Payment Confirm Endpoint**:
   - `PUT /api/billing/{id}/confirm` body: `{ PhuongThucThanhToan: "tien_mat" | "vietqr", MaGiaoDich?: "..." }`
   - Response: `{ MaHoaDon, TrangThai, ThoiGianThu }`

4. **Permission Matrix JSON** (endpoint hoặc bàn giao file):
   ```json
   {
     "admin": { "tabs": ["*"], "actions": { "create_appointment": false, "manage_users": true, ... } },
     "bac_si": { "tabs": ["overview","patients","examination","departments","staff","prescriptions","history","notifications","reports"], ... },
     ...
   }
   ```

5. **Seed Accounts** (cho Dev 2 test):
   | Username | Password | Vai trò |
   |----------|----------|---------|
   | admin | Admin@123 | admin |
   | yta_hc_01 | YTa@123 | y_ta hanhchinh |
   | yta_ls_01 | YTa@123 | y_ta ls |
   | bs_noi_01 | BacSi@123 | bac_si |
   | ktv_xn_01 | KTV@123 | ky_thuat_vien |

---

## Checklist Nghiệm Thu Dev 1

- [ ] Entity `UserAccount` tồn tại, có FK 1:1 tới `NhanVienYTe`
- [ ] Migration không drop dữ liệu cũ, script chuyển đổi chạy thành công
- [ ] Login mọi vai trò OK, JWT claims đầy đủ
- [ ] Trang Admin: CRUD user, lock/unlock, reset password hoạt động
- [ ] Data scope: API trả data scoped theo vai trò (verify bằng 2 tài khoản khác vai trò gọi cùng endpoint → data khác nhau)
- [ ] Luồng tạo phiếu: hóa đơn default `chua_thu`, confirm → `da_thu`, cancel → `da_huy`
- [ ] API contract: PascalCase, enum nhất quán, error format chuẩn
- [ ] DataSeed: chạy xong → login → end-to-end flow khám ngoại trú hoàn chỉnh
- [ ] Swagger/OpenAPI spec cập nhật đầy đủ endpoints mới

---

## Rủi Ro Dễ Sót

| # | Rủi ro | Biện pháp phòng |
|---|--------|-----------------|
| 1 | Sửa NhanVienYTe xong quên sửa mọi chỗ query `.TenDangNhap` cũ | `grep -r "TenDangNhap" --include="*.cs"` → sửa hết sang `UserAccount.TenDangNhap` |
| 2 | RefreshToken FK migration lỗi vì đã có data cũ | Chạy migration script THỦ CÔNG (bước 1-2) trước EF migration |
| 3 | JWT claims key đổi → FE parse sai | Giữ nguyên key trong JWT (`vai_tro`, `ho_ten`) nhưng source lấy từ UserAccount |
| 4 | Data scope BE trả ít bản ghi hơn → FE tưởng API lỗi | Document rõ: response shape giữ nguyên, chỉ số lượng thay đổi |
| 5 | Admin CRUD user quên tạo NhanVienYTe đi kèm | Viết transaction: tạo UserAccount + NhanVienYTe cùng lúc |
| 6 | Seed data thiếu lịch trực → không thể đặt lịch | Seed lịch trực cho ngày hôm nay + 7 ngày, mỗi phòng ít nhất 2 ca |
| 7 | Đổi default hóa đơn `da_thu` → `chua_thu` → FE cũ không hiển thị button Thu tiền | Dev 2 PHẢI cập nhật FE đồng thời |
