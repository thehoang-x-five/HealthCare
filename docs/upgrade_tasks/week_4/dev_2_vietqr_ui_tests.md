# Hướng dẫn hoàn thành Tuần 4 — Dev 2: UI Permissions 7 Tầng, Thanh Toán Wizard, QL Nhân Viên, VietQR UI & Chuẩn Hóa FE

> **File:** `UPGRADE_IMPLEMENTATION_PLAN.md` (Mục 4.1, 4.2, 4.6) + `PROJECT_GAP_ANALYSIS.md` (F.3)
> **Override:** Tuần 4 gốc (VietQR UI + Testing + Demo) mở rộng đáng kể. Testing/Demo chuyển sang Tuần 5.

---

## ⚠️ OVERRIDE TUẦN CŨ

| Nội dung cũ (Tuần 4 gốc) | Xử lý |
|--------------------------|-------|
| VietQR Frontend popup | **HẤP THU** vào Nhiệm vụ 3 (tích hợp trong Payment Wizard) |
| Testing UAT | **CHUYỂN SANG** Tuần 5 |
| Tài liệu & Demo | **CHUYỂN SANG** Tuần 5 |
| Auth store update (Tuần 5 cũ) | **DỒN VÀO** Tuần 4 thành Nhiệm vụ 1 |
| 7-layer RBAC UI (Tuần 5 cũ) | **DỒN VÀO** Tuần 4 thành Nhiệm vụ 2 |
| Payment Wizard (Tuần 5 cũ) | **DỒN VÀO** Tuần 4 thành Nhiệm vụ 3 |
| QL Nhân Viên (Tuần 5 cũ) | **DỒN VÀO** Tuần 4 thành Nhiệm vụ 4 |
| FE adapter chuẩn hóa (Tuần 5 cũ) | **DỒN VÀO** Tuần 4 thành Nhiệm vụ 5 |

---

## Nhiệm vụ 1: Cập Nhật Auth Store & Permission System ⭐ ƯU TIÊN CAO — CHỜ DEV 1 BÀN GIAO

### 1.1 Mục tiêu
Cập nhật FE auth model theo UserAccount mới. Mở rộng `permissions.js` thành ma trận đầy đủ.

> **⚠️ Không chỉ đổi UI, mà phải cập nhật toàn bộ FE model / API mapping / form / hook / guard / render logic đang giả định staff=user.**

### 1.2 Sửa `useAuthStore`

Sau khi Dev 1 bàn giao Login Response DTO mới:

```javascript
// TRƯỚC (từ NhanVienYTe): { MaNhanVien, HoTen, ChucVu, VaiTro, LoaiYTa, MaKhoa }
// SAU (từ UserAccount + NhanVienYTe): { MaUser, MaNhanVien, HoTen, VaiTro, LoaiYTa, MaKhoa }
// LƯU Ý: ChucVu ĐÃ BỊ XÓA → mọi chỗ dùng user.chucVu PHẢI đổi sang user.vaiTro
```

### 1.3 Mở rộng `permissions.js`

Thêm:
```javascript
// TAB VISIBILITY
export const TAB_VISIBILITY = {
  overview: () => true,
  appointments: (u) => canManageReception(u) || isAdmin(u),
  patients: () => true,
  examination: () => true,
  departments: () => true,
  staff: () => true,
  userManagement: (u) => isAdmin(u),
  prescriptions: () => true,
  history: () => true,
  notifications: () => true,
  reports: () => true,
};

// ACTION PERMISSIONS
export const canCreateAppointment = (u) => isReceptionNurse(u);
export const canViewAppointment = (u) => isReceptionNurse(u) || isAdmin(u);
export const canCreatePatient = (u) => isReceptionNurse(u) || isAdmin(u);
export const canEditPatient = (u) => isReceptionNurse(u) || isAdmin(u);
export const canEditDepartment = (u) => isAdmin(u);
export const canManageStaff = (u) => isAdmin(u);
export const canPrescribe = (u) => isDoctor(u);
export const canDispenseMedicine = (u) => isReceptionNurse(u);
export const canViewRevenueReport = (u) => isAdmin(u) || isReceptionNurse(u);
export const canViewVisitReport = (u) => isAdmin(u) || isDoctor(u);
export const canViewStaffReport = (u) => isAdmin(u);

// COMPONENT HELPERS
export const isReadOnly = (u, module) => { ... };
export const hasGlobalScope = (u) => isAdmin(u) || isReceptionNurse(u);
```

### 1.4 File bị ảnh hưởng
- `src/stores/authStore.js` — thêm `maUser`, bỏ `chucVu`
- `src/utils/permissions.js` — mở rộng đầy đủ
- `src/api/auth.js` — login response mapping mới
- **Mọi file dùng `user.chucVu`** → `grep -r "chucVu" src/` → đổi hết sang `user.vaiTro`

### 1.5 Test
- [ ] Login mọi vai trò → store đúng `maUser`, `vaiTro`, `loaiYTa`
- [ ] Không còn tham chiếu `chucVu` trong FE codebase
- [ ] **Verify JWT claim `loai_y_ta` (snake_case) map đúng vào `authStore.loaiYTa`** — nếu sai → nurseType = null → notification realtime không nhận theo subtype, SignalR group join sai

---

## Nhiệm vụ 2: Phân Quyền UI 7 Tầng

### 2.1 Ma trận Tab × Role × Hành vi

| Tab | Admin | Y tá HC | Y tá LS | Y tá CLS | Bác sĩ | KTV |
|-----|-------|---------|---------|----------|--------|-----|
| Tổng quan | ✅ Global | ✅ Global | ✅ Scope khoa | ✅ Scope khoa | ✅ Scope khoa | ✅ Scope khoa |
| Lịch hẹn | ✅ Xem only | ✅ Full CRUD | ❌ Ẩn | ❌ Ẩn | ❌ Ẩn | ❌ Ẩn |
| Bệnh nhân | ✅ Full | ✅ Full CRUD | ✅ Xem only | ✅ Xem scope | ✅ Xem only | ✅ Xem scope |
| Khám bệnh | ✅ Xem global | ✅ Full | ✅ Full LS | ✅ Full CLS | ✅ Full | ✅ CLS only |
| Khoa phòng | ✅ Full CRUD | ✅ Xem | ✅ Xem | ✅ Xem | ✅ Xem | ✅ Xem |
| Nhân sự | ✅ Xem+action | ✅ Xem | ✅ Xem | ✅ Xem | ✅ Xem | ✅ Xem |
| QL Nhân viên | ✅ Full CRUD | ❌ Ẩn | ❌ Ẩn | ❌ Ẩn | ❌ Ẩn | ❌ Ẩn |
| Đơn thuốc | ✅ Xem+QL | ✅ Phát thuốc | ✅ Xem | ✅ Xem | ✅ Kê đơn | ✅ Xem |
| Lịch sử | ✅ Global | ✅ Global | ✅ Scope | ✅ Scope | ✅ Scope | ✅ Scope |
| Thông báo | ✅ Full QL | ✅ Xem | ✅ Xem | ✅ Xem | ✅ Xem | ✅ Xem |
| Báo cáo | ✅ Full | ✅ DT+Kho | ✅ Giới hạn | ✅ Giới hạn | ✅ Lượt khám | ✅ Giới hạn |

### 2.2 Implement 7 tầng

#### Tầng 1: Menu Visibility — `Sidebar.jsx`
Dùng `TAB_VISIBILITY` để filter `links` array.

#### Tầng 2: Route Guard — `ProtectedRoute.jsx` [NEW]
```jsx
export function ProtectedRoute({ permKey, children }) {
  const { user } = useAuthStore();
  if (!TAB_VISIBILITY[permKey]?.(user)) return <Navigate to="/" replace />;
  return children;
}
```
Wrap routes trong `App.jsx`.

#### Tầng 3-5: Page/Component/Action Level
Mỗi trang dùng helpers `canCreate*`, `canEdit*`, `isReadOnly`:
- `Appointments.jsx` — Admin thấy nhưng nút tạo/sửa ẩn
- `Patients.jsx` — Form disabled cho non-YTHC
- `Departments.jsx` — Button CRUD disabled cho non-Admin
- `Staff.jsx` — read-only cho non-Admin
- `Examination.jsx` — Tab LS/CLS visible theo role
- `Reports.jsx` — Filter loại report theo quyền
- `Prescriptions.jsx` — Kê (bác sĩ) vs Phát (y_ta_hc)

#### Tầng 6-7: Data Scope (BE đã xử lý, FE hiện hint)
```jsx
{!hasGlobalScope(user) && <Badge>Dữ liệu khoa: {user.maKhoa}</Badge>}
```

### 2.3 File bị ảnh hưởng
- `Sidebar.jsx`, `App.jsx`, `ProtectedRoute.jsx` [NEW]
- Mọi route file: `Appointments.jsx`, `Patients.jsx`, `Departments.jsx`, `Staff.jsx`, `Examination.jsx`, `Prescriptions.jsx`, `History.jsx`, `Reports.jsx`, `Notifications.jsx`

### 2.4 Test
- [ ] KTV đăng nhập → không thấy Lịch hẹn, QL Nhân viên
- [ ] Admin → thấy cả QL Nhân viên, Lịch hẹn chỉ xem
- [ ] BS → Bệnh nhân hiện nhưng nút Tạo/Sửa ẩn
- [ ] URL trực tiếp `/user-management` bằng BS → redirect `/`
- [ ] Dashboard: Admin = "Toàn phòng khám", BS = "Khoa Nội"

---

## Nhiệm vụ 3: Wizard Thanh Toán Inline (Tạo Phiếu → Thu Tiền)

### 3.1 Mục tiêu
Wizard multi-step ngay trong luồng tạo phiếu. Bước 1: tạo phiếu → Bước 2: thu tiền → Bước 3: hoàn tất.

### 3.2 Components mới

1. **`src/components/exam/PaymentWizard.jsx` [NEW]** — Stepper wrapper:
   - Step 1: Form tạo phiếu (dùng form hiện có)
   - Step 2: `PaymentStep` — chọn phương thức + QR nếu VietQR
   - Step 3: Confirmation
   - Nút "Thu sau" → giữ hóa đơn `chua_thu`, tiếp tục workflow

2. **`src/components/billing/PaymentStep.jsx` [NEW]** — Thanh toán:
   - Hiện số tiền, chọn Tiền mặt / VietQR
   - VietQR → embed VietQRDisplay component (từ Tuần 4 cũ, chuyển từ popup → inline)
   - Nút "Xác nhận thu tiền" + "Thu sau"

3. **API client**: `src/api/billing.js` thêm:
   ```javascript
   export async function confirmPayment(maHoaDon, payload) { ... }
   export async function cancelPayment(maHoaDon) { ... }
   export function useConfirmPayment() { ... }
   ```

### 3.3 Tích hợp vào luồng hiện tại
- `Examination.jsx` — bắt đầu khám → wizard thay vì tạo phiếu trực tiếp
- `Examination.jsx` — chỉ định CLS → wizard
- `Prescriptions.jsx` — kê đơn xong → wizard thanh toán thuốc
- Phát thuốc: check hóa đơn `da_thu` → nếu `chua_thu` hiện warning "Cần thu tiền trước"

### 3.4 Test
- [ ] Tạo phiếu LS → wizard → Tiền mặt → Xác nhận → OK
- [ ] Tạo phiếu CLS → wizard → VietQR → QR hiện → Xác nhận
- [ ] "Thu sau" → hóa đơn `chua_thu`, luồng tiếp tục
- [ ] Phát thuốc khi `chua_thu` → warning

---

## Nhiệm vụ 4: Trang QL Nhân Viên (Tách riêng Staff)

### 4.1 Tách 2 trang

**`/staff` (Nhân sự)** — TẤT CẢ vai trò xem:
- Danh sách đồng nghiệp (tên, chức vụ, khoa, trạng thái)
- Chi tiết: read-only cho non-admin
- Admin: thêm action buttons

**`/user-management` (QL Nhân viên)** — CHỈ ADMIN:
- Bảng UserAccount (username, vai trò, trạng thái, đăng nhập cuối)
- Tạo user mới (2 bước: tài khoản + nhân sự)
- Sửa: 2 tab (Tài khoản / Nhân sự)
- Actions: Khóa/Mở khóa, Reset mật khẩu

### 4.2 API Client `src/api/admin.js` [NEW]
```javascript
export async function getUsers(params) { /* GET /admin/users */ }
export async function createUser(payload) { /* POST /admin/users */ }
export async function lockUser(id) { /* PUT /admin/users/{id}/lock */ }
export async function unlockUser(id) { /* PUT /admin/users/{id}/unlock */ }
export async function resetPassword(id) { /* POST /admin/users/{id}/reset-password */ }
```

### 4.3 File bị ảnh hưởng
- `src/routes/UserManagement.jsx` [NEW] hoặc refactor `AdminUsers.jsx`
- `src/routes/Staff.jsx` — đổi thành xem chung
- `src/api/admin.js` [NEW]
- `src/App.jsx` — route `/user-management`
- `Sidebar.jsx` — link "QL Nhân viên" (admin only)

### 4.4 Test
- [ ] Admin → `/user-management` → bảng user accounts
- [ ] Tạo user → login thành công
- [ ] Khóa → login fail, Mở khóa → login OK
- [ ] Non-admin → `/user-management` → redirect `/`
- [ ] Mọi role → `/staff` → xem đồng nghiệp (read-only)

---

## Nhiệm vụ 5: Chuẩn Hóa FE API Client & Enum Constants

### 5.1 Tạo `src/constants/enums.js` [NEW]
```javascript
export const TRANG_THAI_HOA_DON = { CHUA_THU: 'chua_thu', DA_THU: 'da_thu', DA_HUY: 'da_huy' };
export const PHUONG_THUC_THANH_TOAN = { TIEN_MAT: 'tien_mat', VIETQR: 'vietqr', ... };
export const VAI_TRO = { ADMIN: 'admin', BAC_SI: 'bac_si', Y_TA: 'y_ta', KY_THUAT_VIEN: 'ky_thuat_vien' };
export const LOAI_Y_TA = { HANH_CHINH: 'hanhchinh', LAM_SANG: 'ls', CAN_LAM_SANG: 'cls' };
// ... tất cả enum khác
```

### 5.2 Rà soát adapter
- Mọi `src/api/*.js` — verify field names match BE contract mới
- Pagination shape nhất quán: `{ Items, TotalItems, Page, PageSize }`
- Không hardcode enum string → dùng constants
- `src/api/auth.js` — login mapping theo DTO mới

### 5.3 Test
- [ ] Không còn hardcoded enum string
- [ ] Login → API call → FE render đúng field names
- [ ] Pagination đúng shape

---

## Workflow Phối Hợp Tuần 4

| Ngày | Dev 1 làm | Dev 2 làm |
|------|-----------|-----------|
| 1-2 | Tách bảng User + migration + sửa auth | Đọc contract cũ, chuẩn bị permissions.js + enums.js |
| 2-3 | Bàn giao login DTO → RBAC backend | Sửa auth store + login + **bắt đầu RBAC UI** |
| 3-4 | Payment SDK + VietQR API + admin endpoints | Wizard thanh toán + VietQR inline + UserManagement |
| 4-5 | Contract chuẩn hóa + DataSeed | Chuẩn hóa adapters + integration sơ bộ |

---

## Rủi Ro

| # | Rủi ro | Phòng tránh |
|---|--------|-------------|
| 1 | `chucVu` bỏ sót | `grep -r "chucVu" src/ --include="*.jsx" --include="*.js"` |
| 2 | AdminUsers.jsx cũ CRUD NhanVienYTe trực tiếp | Refactor → gọi `/api/admin/users` mới |
| 3 | VietQR popup cũ vỡ khi embed wizard | Extract QR logic thành component riêng |
| 4 | Route guard bypass khi refresh | ProtectedRoute check auth state → redirect `/login` |
| 5 | Wizard cancel ở bước 2 → phiếu bước 1 đã tạo | UX: "Hóa đơn đã tạo, thu tiền sau" + retry |

---

## 📌 GHI CHÚ: 8 cột y tế BenhNhan → MongoDB

> Mục 3.7 trong `UPGRADE_IMPLEMENTATION_PLAN.md` — chuyển 8 cột y tế SQL → MongoDB `medicalProfile`.
>
> **Quyết định**: Thực hiện **SAU Week 5** nếu còn thời gian. Không ảnh hưởng tính năng W4-5. Dual-write hoạt động, SQL vẫn là nguồn đọc chính.
