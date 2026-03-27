# Hướng dẫn hoàn thành Tuần 5 — Dev 2: UI Permissions, Thanh Toán Wizard, QL Nhân Viên & Chuẩn Hóa FE

> **File gốc:** `UPGRADE_IMPLEMENTATION_PLAN.md` (Mục 4.1, 4.2, 4.6) + `PROJECT_GAP_ANALYSIS.md` (F.3)
> **Mở rộng & Override:** Tuần 1-4 đã hoàn thành Sidebar filter cơ bản, AdminUsers page, Luồng hủy UI, VietQR popup, Tab timeline/pha hệ. Tuần 5 **OVERRIDE tư duy phân quyền** từ "ẩn menu" → "hiện rộng + khóa chức năng + scope data FE", đồng thời **xây wizard thanh toán inline** và **trang QL Nhân Viên tách riêng khỏi Staff**.

---

## ⚠️ OVERRIDE so với Tuần 1-4

1. **Sidebar filter cũ** (Tuần 1) chỉ ẩn menu theo role. Tuần 5 **GIỮ** phần ẩn menu cho các tab thực sự ẩn (Lịch hẹn cho CLS/KTV, QL Nhân viên cho non-admin), nhưng **THÊM** phân quyền 7 tầng bên trong mỗi trang.
2. **AdminUsers page** (Tuần 3) đang CRUD trực tiếp NhanVienYTe. Tuần 5 **SỬA** → CRUD qua UserAccount + NhanVienYTe (do Dev 1 tách bảng).
3. **VietQR popup** (Tuần 4) là popup rời. Tuần 5 **TÍCH HỢP** vào wizard thanh toán inline ngay trong luồng tạo phiếu.
4. **Login store** (`useAuthStore`) đang lưu trực tiếp data từ NhanVienYTe. Tuần 5 **SỬA** → lưu data từ UserAccount + NhanVienYTe join.
5. **`permissions.js`** đang chỉ có helper level đơn giản. Tuần 5 **MỞ RỘNG** thành ma trận permissions đầy đủ.

---

## Nhiệm vụ 1: Cập Nhật Auth Store & Permission System

### 1.1 Mục tiêu
Cập nhật toàn bộ FE auth model theo UserAccount mới từ Dev 1. Mở rộng `permissions.js` thành hệ thống phân quyền đầy đủ.

### 1.2 Việc cần làm

#### A. Sửa `useAuthStore` (Zustand store)

Sau khi Dev 1 bàn giao login response DTO mới, sửa store:

**File: `src/stores/authStore.js`**
```javascript
// Trước (Login response từ NhanVienYTe):
// { MaNhanVien, HoTen, VaiTro, LoaiYTa, ChucVu, MaKhoa, AccessToken, RefreshToken }

// Sau (Login response từ UserAccount + NhanVienYTe):
// { MaUser, MaNhanVien, HoTen, VaiTro, LoaiYTa, MaKhoa, AccessToken, RefreshToken }

// user object phải lưu thêm MaUser
setUser: (loginData) => set({
  user: {
    maUser: loginData.MaUser || loginData.maUser,
    maNhanVien: loginData.MaNhanVien || loginData.maNhanVien,
    hoTen: loginData.HoTen || loginData.hoTen,
    vaiTro: loginData.VaiTro || loginData.vaiTro,
    loaiYTa: loginData.LoaiYTa || loginData.loaiYTa,
    maKhoa: loginData.MaKhoa || loginData.maKhoa,
  },
  accessToken: loginData.AccessToken || loginData.accessToken,
  refreshToken: loginData.RefreshToken || loginData.refreshToken,
})
```

**Lưu ý**: `ChucVu` ĐÃ BỊ XÓA khỏi NhanVienYTe → mọi chỗ FE đang dùng `user.chucVu` phải đổi sang `user.vaiTro` + `user.loaiYTa`.

#### B. Mở rộng `permissions.js`

**File: `src/utils/permissions.js`**

Thêm ma trận phân quyền chi tiết:

```javascript
// ===== TAB VISIBILITY =====
export const TAB_VISIBILITY = {
  overview:      () => true,  // Tất cả
  appointments:  (u) => canManageReception(u) || isAdmin(u),  // Y tá HC + Admin
  patients:      () => true,  // Tất cả (nhưng action bị giới hạn)
  examination:   () => true,  // Tất cả (nhưng scope khác nhau)
  departments:   () => true,  // Tất cả (admin=CRUD, khác=xem)
  staff:         () => true,  // Tất cả (xem đồng nghiệp)
  userManagement: (u) => isAdmin(u),  // Chỉ Admin
  prescriptions: () => true,  // Tất cả (nhưng action bị giới hạn)
  history:       () => true,  // Tất cả (nhưng scope theo role)
  notifications: () => true,  // Tất cả
  reports:       () => true,  // Tất cả (nhưng loại report bị giới hạn)
};

// ===== ACTION PERMISSIONS =====
export const canCreateAppointment = (u) => isReceptionNurse(u); // Chỉ Y tá HC
export const canEditAppointment = (u) => isReceptionNurse(u);
export const canViewAppointment = (u) => isReceptionNurse(u) || isAdmin(u); // Admin chỉ xem

export const canCreatePatient = (u) => isReceptionNurse(u) || isAdmin(u);
export const canEditPatient = (u) => isReceptionNurse(u) || isAdmin(u);
export const canViewPatient = (u) => true; // Tất cả

export const canEditDepartment = (u) => isAdmin(u);
export const canManageStaff = (u) => isAdmin(u); // QL Nhân viên (User+Staff)

export const canPrescribe = (u) => isDoctor(u);
export const canDispenseMedicine = (u) => isReceptionNurse(u);
export const canViewPrescription = (u) => true;

export const canViewRevenueReport = (u) => isAdmin(u) || isReceptionNurse(u);
export const canViewVisitReport = (u) => isAdmin(u) || isDoctor(u);
export const canViewInventoryReport = (u) => isAdmin(u) || isReceptionNurse(u);
export const canViewStaffReport = (u) => isAdmin(u);

// ===== DATA SCOPE =====
// BE đã scope data, nhưng FE cần biết để hiển thị UI hint
export const hasGlobalScope = (u) => isAdmin(u) || isReceptionNurse(u);
export const getDepartmentScope = (u) => u?.maKhoa || null;

// ===== COMPONENT HELPERS =====
// Dùng để disable/readonly component
export const isReadOnly = (u, module) => {
  switch(module) {
    case 'departments': return !isAdmin(u);
    case 'staff': return !isAdmin(u);
    case 'appointments': return isAdmin(u); // Admin xem, không thao tác
    case 'patients': return !isReceptionNurse(u) && !isAdmin(u);
    default: return false;
  }
};
```

### 1.3 File bị ảnh hưởng

| File | Thay đổi |
|------|----------|
| `src/stores/authStore.js` | Cập nhật user model (thêm `maUser`, bỏ `chucVu`) |
| `src/utils/permissions.js` | Mở rộng ma trận permissions, thêm `TAB_VISIBILITY`, action helpers, scope helpers |
| `src/api/auth.js` | Cập nhật login response mapping |
| **Mọi file dùng `user.chucVu`** | `grep -r "chucVu"` → đổi sang `user.vaiTro` + `user.loaiYTa` |

### 1.4 Dependency với Dev 1
- **CHỜ**: Dev 1 bàn giao Login Response DTO mới + JWT Claims list trước khi sửa.

### 1.5 Test
- [ ] Login mọi vai trò → store lưu đúng `maUser`, `vaiTro`, `loaiYTa`
- [ ] `permissions.js` → test từng helper function với mỗi vai trò
- [ ] Không còn tham chiếu `chucVu` trong codebase FE

---

## Nhiệm vụ 2: Phân Quyền UI 7 Tầng

### 2.1 Mục tiêu
Implement đầy đủ 7 tầng phân quyền FE theo yêu cầu mới (không chỉ ẩn menu).

### 2.2 Việc cần làm

#### Tầng 1: Menu Visibility (Sidebar.jsx)

**SỬA** logic filter trong Sidebar, sử dụng `TAB_VISIBILITY`:

```jsx
// src/components/ui/Sidebar.jsx
import { TAB_VISIBILITY } from '../../utils/permissions';

const allLinks = [
  ["/", "Tổng quan", "overview"],
  ["/appointments", "Lịch hẹn", "appointments"],
  ["/patients", "Bệnh nhân", "patients"],
  ["/examination", "Khám bệnh", "examination"],
  ["/departments", "Khoa phòng", "departments"],
  ["/staff", "Nhân sự", "staff"],
  ["/user-management", "QL Nhân viên", "userManagement"],
  ["/prescriptions", "Đơn thuốc", "prescriptions"],
  ["/history", "Lịch sử", "history"],
  ["/notifications", "Thông báo", "notifications"],
  ["/reports", "Báo cáo", "reports"],
];

const visibleLinks = allLinks.filter(([, , key]) => TAB_VISIBILITY[key]?.(user));
```

#### Tầng 2: Route Guard (App.jsx / Router)

**THÊM** ProtectedRoute wrapper:

```jsx
// src/components/auth/ProtectedRoute.jsx
import { Navigate } from 'react-router-dom';
import { TAB_VISIBILITY } from '../../utils/permissions';

export function ProtectedRoute({ permKey, children }) {
  const { user } = useAuthStore();
  if (!TAB_VISIBILITY[permKey]?.(user)) return <Navigate to="/" replace />;
  return children;
}
```

**SỬA** `App.jsx` routes:
```jsx
<Route path="/user-management" element={
  <ProtectedRoute permKey="userManagement"><UserManagement /></ProtectedRoute>
} />
<Route path="/appointments" element={
  <ProtectedRoute permKey="appointments"><Appointments /></ProtectedRoute>
} />
```

#### Tầng 3-4: Page & Component Level Permission

Trong mỗi trang, dùng `isReadOnly` và action helpers:

**Ví dụ: `Departments.jsx`**
```jsx
const readOnly = isReadOnly(user, 'departments');
// ...
<button disabled={readOnly} onClick={handleEdit}>Sửa</button>
{!readOnly && <button onClick={handleDelete}>Xóa</button>}
```

**Ví dụ: `Appointments.jsx`** (Admin xem, KHÔNG thao tác)
```jsx
const canCreate = canCreateAppointment(user);
// Admin thấy bảng lịch hẹn, nhưng không có nút Tạo/Sửa/Hủy
{canCreate && <button onClick={...}>Tạo lịch hẹn</button>}
```

**Ví dụ: `Patients.jsx`** (read-only cho non y_ta_hc)
```jsx
const canEdit = canEditPatient(user);
// Form fields: disabled={!canEdit}
<input value={hoTen} disabled={!canEdit} onChange={...} />
// Nút tạo tiếp nhận: chỉ Y tá HC + Admin
{canCreatePatient(user) && <button>Tiếp nhận</button>}
```

**Ví dụ: `Examination.jsx`** (scope theo role)
```jsx
// Y tá CLS chỉ thấy tab CLS, không thấy tab LS
// Bác sĩ thấy cả 2 nhưng không thao tác bên CLS
const showLsTab = canManageClinical(user) || isDoctor(user) || isAdmin(user);
const showClsTab = canManageCls(user) || isDoctor(user) || isAdmin(user);
```

**Ví dụ: `Reports.jsx`** (scope loại báo cáo)
```jsx
const reports = [
  { key: 'revenue', label: 'Doanh thu', visible: canViewRevenueReport(user) },
  { key: 'visits', label: 'Lượt khám', visible: canViewVisitReport(user) },
  { key: 'inventory', label: 'Kho thuốc', visible: canViewInventoryReport(user) },
  { key: 'staff', label: 'Nhân viên', visible: canViewStaffReport(user) },
].filter(r => r.visible);
```

#### Tầng 5: Action Level

Đã cover ở Tầng 3-4 qua các helper `can*()`.

#### Tầng 6-7: Data Scope (BE đã xử lý, FE hiển thị hint)

```jsx
// Hiện badge scope trên Dashboard
{!hasGlobalScope(user) && (
  <div className="text-xs text-amber-600 bg-amber-50 px-2 py-1 rounded">
    📋 Dữ liệu hiển thị theo khoa: {user.maKhoa}
  </div>
)}
```

### 2.3 Ma trận tổng hợp Tab × Role × Hành vi

| Tab | Admin | Y tá HC | Y tá LS | Y tá CLS | Bác sĩ | KTV |
|-----|-------|---------|---------|----------|--------|-----|
| **Tổng quan** | ✅ Global | ✅ Global | ✅ Scope khoa | ✅ Scope khoa | ✅ Scope khoa | ✅ Scope khoa |
| **Lịch hẹn** | ✅ Xem only | ✅ Full CRUD | ❌ Ẩn | ❌ Ẩn | ❌ Ẩn | ❌ Ẩn |
| **Bệnh nhân** | ✅ Full CRUD | ✅ Full CRUD | ✅ Xem only | ✅ Xem scope CLS | ✅ Xem only | ❌ Xem scope CLS |
| **Khám bệnh** | ✅ Xem global | ✅ Full (gọi/hủy) | ✅ Full LS | ✅ Full CLS | ✅ Full (chuẩn đoán) | ✅ CLS only |
| **Khoa phòng** | ✅ Full CRUD | ✅ Xem | ✅ Xem | ✅ Xem | ✅ Xem | ✅ Xem |
| **Nhân sự** | ✅ Xem + action | ✅ Xem | ✅ Xem | ✅ Xem | ✅ Xem | ✅ Xem |
| **QL Nhân viên** | ✅ Full CRUD | ❌ Ẩn | ❌ Ẩn | ❌ Ẩn | ❌ Ẩn | ❌ Ẩn |
| **Đơn thuốc** | ✅ Xem + quản lý | ✅ Phát thuốc | ✅ Xem | ✅ Xem | ✅ Kê đơn | ✅ Xem |
| **Lịch sử** | ✅ Global | ✅ Global | ✅ Scope khoa | ✅ Scope khoa | ✅ Scope khoa/mình | ✅ Scope CLS |
| **Thông báo** | ✅ Full quản lý | ✅ Xem | ✅ Xem | ✅ Xem | ✅ Xem | ✅ Xem |
| **Báo cáo** | ✅ Full | ✅ DT+Kho | ✅ Xem giới hạn | ✅ Xem giới hạn | ✅ Lượt khám | ✅ Xem giới hạn |

### 2.4 File bị ảnh hưởng

| File | Thay đổi |
|------|----------|
| `src/components/ui/Sidebar.jsx` | Sửa filter logic dùng `TAB_VISIBILITY` |
| `src/App.jsx` | Thêm `ProtectedRoute` wrapper |
| `src/components/auth/ProtectedRoute.jsx` | [NEW] |
| `src/routes/Appointments.jsx` | Thêm disable/readonly cho Admin |
| `src/routes/Patients.jsx` | Thêm read-only mode cho non-YTHC |
| `src/routes/Departments.jsx` | Thêm read-only cho non-Admin |
| `src/routes/Staff.jsx` | Thêm read-only cho non-Admin |
| `src/routes/Examination.jsx` | Tab visibility theo role |
| `src/routes/Prescriptions.jsx` | Action permissions (kê vs phát) |
| `src/routes/History.jsx` | Scope hint UI |
| `src/routes/Reports.jsx` | Filter report types theo role |
| `src/routes/Notifications.jsx` | Admin quản lý vs others xem |

### 2.5 Dependency với Dev 1
- BE đã scope data → FE chỉ cần disable UI + hiện hint scope.
- **NHẬN**: Permission Matrix JSON từ Dev 1 để verify UI match BE.

### 2.6 Test
- [ ] Đăng nhập KTV → chỉ thấy: Tổng quan, Khám bệnh, Khoa phòng, Nhân sự, Đơn thuốc (xem), Lịch sử, Thông báo, Báo cáo (giới hạn)
- [ ] Đăng nhập Admin → thấy tất cả **kể cả QL Nhân viên**, nhưng Lịch hẹn chỉ xem
- [ ] Đăng nhập Bác sĩ → Bệnh nhân hiện nhưng nút Tạo/Sửa bị ẩn
- [ ] Truy cập URL `/user-management` bằng Bác sĩ → redirect về `/`
- [ ] Đăng nhập Y tá LS → Khoa phòng hiện nhưng mọi button sửa/xóa disabled
- [ ] Dashboard: Admin thấy "Toàn phòng khám", Bác sĩ thấy "Khoa Nội"

---

## Nhiệm vụ 3: Wizard Thanh Toán Inline (Tạo Phiếu → Thu Tiền)

### 3.1 Mục tiêu
Xây UI multi-step/wizard ngay trong luồng tạo phiếu khám. Bước 1: tạo phiếu. Bước 2: thu tiền/chọn phương thức.

### 3.2 Việc cần làm

#### A. Tạo Component Stepper

**File: `src/components/exam/PaymentWizard.jsx` [NEW]**

```jsx
// Step 1: Tạo phiếu (form đã có)
// Step 2: Thanh toán (mới)
// Step 3: Hoàn tất (confirmation)

const PaymentWizard = ({ type, onComplete, onCancel }) => {
  const [step, setStep] = useState(1); // 1=tạo phiếu, 2=thanh toán, 3=xong
  const [invoiceData, setInvoiceData] = useState(null);
  
  // Step 1: Khi tạo phiếu OK → nhận { MaPhieuKham, MaHoaDon, SoTien }
  const handleCreateSuccess = (result) => {
    setInvoiceData(result);
    setStep(2);
  };
  
  // Step 2: Xác nhận thanh toán
  const handleConfirmPayment = async (method) => {
    await confirmPayment(invoiceData.MaHoaDon, {
      PhuongThucThanhToan: method, // "tien_mat" | "vietqr"
      MaGiaoDich: method === 'vietqr' ? qrTransactionId : null,
    });
    setStep(3);
  };
  
  // Step 2: Skip thanh toán (thu sau)
  const handleSkipPayment = () => {
    toast.info("Hóa đơn ở trạng thái 'Chưa thu', cần thu tiền sau.");
    onComplete(invoiceData);
  };
  
  return (
    <div>
      {/* Stepper indicator */}
      <div className="flex items-center gap-2 mb-4">
        <Step num={1} active={step===1} done={step>1} label="Tạo phiếu" />
        <Step num={2} active={step===2} done={step>2} label="Thanh toán" />
        <Step num={3} active={step===3} label="Hoàn tất" />
      </div>
      
      {step === 1 && <CreateExamForm type={type} onSuccess={handleCreateSuccess} />}
      {step === 2 && <PaymentStep invoice={invoiceData} onConfirm={handleConfirmPayment} onSkip={handleSkipPayment} />}
      {step === 3 && <CompletionStep invoice={invoiceData} onDone={onComplete} />}
    </div>
  );
};
```

#### B. PaymentStep Component

**File: `src/components/billing/PaymentStep.jsx` [NEW]**

```jsx
const PaymentStep = ({ invoice, onConfirm, onSkip }) => {
  const [method, setMethod] = useState('tien_mat');
  
  return (
    <div className="space-y-4">
      <div className="bg-blue-50 p-4 rounded-lg">
        <p className="text-sm text-blue-700">Hóa đơn: {invoice.MaHoaDon}</p>
        <p className="text-2xl font-bold text-blue-900">
          {new Intl.NumberFormat('vi-VN').format(invoice.SoTien)} đ
        </p>
      </div>
      
      {/* Chọn phương thức */}
      <div className="flex gap-3">
        <PaymentMethodCard method="tien_mat" label="Tiền mặt" icon="💵" selected={method==='tien_mat'} onClick={() => setMethod('tien_mat')} />
        <PaymentMethodCard method="vietqr" label="VietQR" icon="📱" selected={method==='vietqr'} onClick={() => setMethod('vietqr')} />
      </div>
      
      {/* QR Code nếu chọn VietQR */}
      {method === 'vietqr' && <VietQRDisplay invoiceId={invoice.MaHoaDon} amount={invoice.SoTien} />}
      
      <div className="flex gap-2">
        <button onClick={() => onConfirm(method)} className="btn-primary">Xác nhận thu tiền</button>
        <button onClick={onSkip} className="btn-secondary">Thu sau</button>
      </div>
    </div>
  );
};
```

#### C. Tích hợp vào các luồng tạo phiếu

1. **`Examination.jsx`** — luồng tạo phiếu khám LS:
   - Khi bấm "Bắt đầu khám", mở wizard thay vì tạo phiếu trực tiếp.
   - Sau khi hoàn tất wizard → chuyển vào hàng đợi bình thường.

2. **`Examination.jsx`** — luồng tạo phiếu CLS:
   - Khi bác sĩ chỉ định CLS → mở wizard (bước 1: tạo order, bước 2: thu tiền CLS).

3. **`Prescriptions.jsx`** — luồng kê đơn thuốc:
   - Khi bác sĩ kê đơn xong → mở wizard thanh toán thuốc.

#### D. API Client cập nhật

**File: `src/api/billing.js` — THÊM:**
```javascript
export async function confirmPayment(maHoaDon, payload) {
  const res = await http.put(`/billing/${maHoaDon}/confirm`, payload);
  return unwrap(res);
}

export async function cancelPayment(maHoaDon) {
  const res = await http.put(`/billing/${maHoaDon}/cancel`);
  return unwrap(res);
}

export function useConfirmPayment(options = {}) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ maHoaDon, ...payload }) => confirmPayment(maHoaDon, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["billing"] });
      qc.invalidateQueries({ queryKey: ["queue"] });
    },
    ...options,
  });
}
```

### 3.3 File bị ảnh hưởng

| File | Thay đổi |
|------|----------|
| `src/components/exam/PaymentWizard.jsx` | [NEW] Wizard component |
| `src/components/billing/PaymentStep.jsx` | [NEW] Payment step |
| `src/components/billing/VietQRDisplay.jsx` | SỬA: tích hợp từ popup → inline trong wizard |
| `src/api/billing.js` | THÊM: `confirmPayment`, `cancelPayment` hooks |
| `src/routes/Examination.jsx` | SỬA: tích hợp wizard vào luồng tạo phiếu |
| `src/routes/Prescriptions.jsx` | SỬA: wizard cho luồng kê đơn |

### 3.4 Dependency với Dev 1
- **NHẬN**: endpoint `PUT /api/billing/{id}/confirm` với request shape `{ PhuongThucThanhToan, MaGiaoDich }`.
- **NHẬN**: response endpoint tạo phiếu mới trả thêm `MaHoaDon`.
- **QUAN TRỌNG**: workflow rule: phát thuốc chỉ khi hóa đơn thuốc `da_thu` → FE cần check trước khi cho phát.

### 3.5 Test
- [ ] Tạo phiếu khám LS → wizard hiện bước 2 "Thanh toán" → Chọn "Tiền mặt" → Xác nhận → Hoàn tất
- [ ] Tạo phiếu CLS → wizard → chọn VietQR → hiện QR code → xác nhận → hoàn tất
- [ ] Bước 2: bấm "Thu sau" → hóa đơn vẫn `chua_thu`, luồng tiếp tục bình thường
- [ ] Phát thuốc khi hóa đơn thuốc `chua_thu` → hiện cảnh báo "Cần thu tiền trước"
- [ ] Step indicator hiện đúng bước active/done

---

## Nhiệm vụ 4: Trang QL Nhân Viên (Tách riêng Staff)

### 4.1 Mục tiêu
Tạo trang `/user-management` dành riêng cho Admin quản lý UserAccount + lien kết NhanVienYTe. **KHÁC** với trang `/staff` (mọi người xem thông tin đồng nghiệp).

### 4.2 Việc cần làm

#### A. Trang Staff (Sửa lại)

**File: `src/routes/Staff.jsx`** — Trang xem thông tin đồng nghiệp:
- Hiện cho TẤT CẢ vai trò.
- Danh sách nhân viên (tên, chức vụ, khoa, trạng thái).
- Click vào → xem chi tiết (read-only cho non-admin).
- Admin: thêm nút "Sửa" trên chi tiết (redirect sang QL Nhân viên hoặc inline edit).

#### B. Trang QL Nhân Viên (Mới hoặc nâng cấp AdminUsers)

**File: `src/routes/UserManagement.jsx` [NEW hoặc rename từ `AdminUsers.jsx`]**

Chức năng Admin:
1. **Danh sách User Accounts** — bảng có:
   - Tên đăng nhập, Họ tên (từ NhanVienYTe), Vai trò, Trạng thái tài khoản, Đăng nhập cuối.
   - Bộ lọc: theo vai trò, trạng thái, khoa.
   - Search: theo tên/username.

2. **Tạo User mới** — Modal/form:
   - Bước 1: Thông tin tài khoản (username, password, vai trò, loại y tá)
   - Bước 2: Thông tin nhân sự (họ tên, khoa, chuyên môn, liên hệ)
   - Submit → gọi `POST /api/admin/users` (Dev 1 tạo cả 2 bảng)

3. **Sửa User** — 2 tab:
   - Tab "Tài khoản": sửa vai trò, loại y tá, trạng thái
   - Tab "Nhân sự": sửa họ tên, khoa, chuyên môn, liên hệ (giống Staff edit)

4. **Actions**:
   - Khóa/mở khóa tài khoản (gọi `PUT /api/admin/users/{id}/lock|unlock`)
   - Reset mật khẩu (gọi `POST /api/admin/users/{id}/reset-password`)
   - Confirmation dialog cho mọi action nhạy cảm

#### C. API Client

**File: `src/api/admin.js` [NEW]**
```javascript
export async function getUsers(params) { return http.get('/admin/users', { params }); }
export async function getUser(id) { return http.get(`/admin/users/${id}`); }
export async function createUser(payload) { return http.post('/admin/users', payload); }
export async function updateUser(id, payload) { return http.put(`/admin/users/${id}`, payload); }
export async function lockUser(id) { return http.put(`/admin/users/${id}/lock`); }
export async function unlockUser(id) { return http.put(`/admin/users/${id}/unlock`); }
export async function resetPassword(id) { return http.post(`/admin/users/${id}/reset-password`); }

export function useUsers(params) {
  return useQuery({ queryKey: ['admin-users', params], queryFn: () => getUsers(params) });
}
// ... useMutation hooks
```

### 4.3 File bị ảnh hưởng

| File | Thay đổi |
|------|----------|
| `src/routes/UserManagement.jsx` | [NEW] hoặc rename/refactor từ AdminUsers.jsx |
| `src/routes/Staff.jsx` | SỬA: đổi thành trang xem chung, read-only cho non-admin |
| `src/api/admin.js` | [NEW] API client cho user management |
| `src/components/ui/Sidebar.jsx` | Thêm link "QL Nhân viên" (chỉ admin thấy), giữ link "Nhân sự" cho tất cả |
| `src/App.jsx` | Thêm route `/user-management` |

### 4.4 Test
- [ ] Admin truy cập `/user-management` → thấy bảng user accounts
- [ ] Tạo user mới → login bằng user mới thành công
- [ ] Khóa tài khoản → login fail 403
- [ ] Mở khóa → login thành công lại
- [ ] Reset mật khẩu → login bằng pw cũ fail, pw mới (mặc định) pass
- [ ] Non-admin truy cập `/user-management` → redirect `/`
- [ ] Mọi role truy cập `/staff` → thấy danh sách đồng nghiệp (read-only)

---

## Nhiệm vụ 5: Chuẩn Hóa FE API Client & Field Mapping

### 5.1 Mục tiêu
Rà soát toàn bộ `src/api/*.js`, chuẩn hóa mapping FE-BE, đảm bảo mọi adapter gọi đúng contract mới từ Dev 1.

### 5.2 Việc cần làm

#### A. Rà soát naming

| Vấn đề | File | Fix |
|--------|------|-----|
| `user.chucVu` đã bị xóa | Mọi component dùng `chucVu` | Đổi sang `user.vaiTro` + `user.loaiYTa` |
| Response hàng đợi thiếu `MaLuotKham` | `examination.js` adapter | Dùng `fld()` helper fallback (ĐÃ FIX ở Tuần trước, review lại cho chắc) |
| Enum status không nhất quán | Toàn bộ `src/api/*.js` | Tạo file `src/constants/enums.js` chứa mọi enum chuẩn |
| Response pagination shape | Nhiều file | Chuẩn hóa output: `{ Items, TotalItems, Page, PageSize }` |

#### B. Tạo file enum constants

**File: `src/constants/enums.js` [NEW]**
```javascript
export const TRANG_THAI_HOA_DON = {
  CHUA_THU: 'chua_thu',
  DA_THU: 'da_thu',
  DA_HUY: 'da_huy',
};

export const TRANG_THAI_LUOT_KHAM = {
  DANG_THUC_HIEN: 'dang_thuc_hien',
  HOAN_TAT: 'hoan_tat',
  DA_HUY: 'da_huy',
};

export const PHUONG_THUC_THANH_TOAN = {
  TIEN_MAT: 'tien_mat',
  THE: 'the',
  CHUYEN_KHOAN: 'chuyen_khoan',
  VIETQR: 'vietqr',
};

export const VAI_TRO = {
  ADMIN: 'admin',
  BAC_SI: 'bac_si',
  Y_TA: 'y_ta',
  KY_THUAT_VIEN: 'ky_thuat_vien',
};

export const LOAI_Y_TA = {
  HANH_CHINH: 'hanhchinh',
  LAM_SANG: 'ls',
  CAN_LAM_SANG: 'cls',
};
// ... thêm các enum khác
```

#### C. Review & Fix adapter functions

| API file | Review items |
|----------|-------------|
| `src/api/examination.js` | Verify field names match BE response → đặc biệt `MaLuotKham` |
| `src/api/billing.js` | Thêm `confirmPayment`, `cancelPayment` |
| `src/api/patients.js` | Verify CRUD endpoints match new permission rules |
| `src/api/appointments.js` | Verify create/update paths |
| `src/api/auth.js` | Sửa theo login response DTO mới |

### 5.3 File bị ảnh hưởng

| File | Thay đổi |
|------|----------|
| `src/constants/enums.js` | [NEW] |
| `src/api/auth.js` | SỬA login response mapping |
| `src/api/billing.js` | THÊM confirm/cancel |
| `src/api/examination.js` | REVIEW field names |
| Mọi file `src/api/*.js` | REVIEW pagination shape |

### 5.4 Test
- [ ] Không còn bất kỳ hardcoded string enum nào → mọi chỗ dùng constants từ `enums.js`
- [ ] Mọi danh sách paginated → response shape nhất quán
- [ ] Login → mọi API call trả dữ liệu đúng field name
- [ ] FE render đúng tên/trạng thái sau khi rename

---

## Phần Handoff / Integration với Dev 1

### Checklist nhận từ Dev 1 TRƯỚC KHI bắt tay

| # | Item | Dev 1 bàn giao | Dev 2 dùng ở |
|---|------|----------------|-------------|
| 1 | JWT Claims list mới | `{ ma_user, ma_nhan_vien, vai_tro, loai_y_ta, ho_ten, ma_khoa }` | `authStore.js`, `permissions.js` |
| 2 | Login Response DTO | `{ MaUser, MaNhanVien, ..., AccessToken }` | `auth.js` adapter |
| 3 | Permission Matrix JSON | File hoặc endpoint | `permissions.js` verify |
| 4 | Payment Confirm Endpoint | `PUT /api/billing/{id}/confirm` request/response | `billing.js`, `PaymentWizard.jsx` |
| 5 | Admin User CRUD Endpoints | `/api/admin/users` CRUD | `admin.js`, `UserManagement.jsx` |
| 6 | Seed Accounts | username/password table | Test login |
| 7 | StatusEnums.md | Enum reference file | `enums.js` |

### Workflow phối hợp

1. **Ngày 1-2**: Dev 1 tách bảng + migration + sửa auth. Dev 2 đọc contract, chuẩn bị permissions.js.
2. **Ngày 2-3**: Dev 1 bàn giao login DTO → Dev 2 sửa auth store + login.
3. **Ngày 3-4**: Dev 1 hoàn thành payment SDK + admin endpoints → Dev 2 xây wizard + UserManagement.
4. **Ngày 4-5**: Dev 1 chuẩn hóa API + seed → Dev 2 chuẩn hóa adapters + test end-to-end.
5. **Ngày 5**: Integration testing cả 2 dev cùng chạy, fix lỗi mapping.

---

## Checklist Nghiệm Thu Dev 2

- [ ] Sidebar hiện đúng menu theo ma trận Tab × Role
- [ ] Route guard chặn truy cập URL trực tiếp cho tab ẩn
- [ ] Mỗi trang: button/form disabled/read-only đúng theo vai trò
- [ ] Dashboard hiện scope hint khi user không phải admin/y_ta_hc
- [ ] Reports chỉ hiện loại báo cáo phù hợp vai trò
- [ ] Wizard thanh toán hoạt động cho cả tiền mặt và VietQR
- [ ] "Thu sau" giữ hóa đơn `chua_thu`, luồng không bị block
- [ ] Trang QL Nhân viên: CRUD user hoạt động, lock/unlock/reset OK
- [ ] Trang Staff: mọi role xem được, chỉ admin có action
- [ ] Login store lưu đúng `maUser`, không còn `chucVu`
- [ ] Mọi enum dùng constants, không hardcode
- [ ] Không có file FE nào còn gọi `user.chucVu`
- [ ] Test với 5 vai trò seed: admin, y_ta_hc, y_ta_ls, bac_si, ktv

---

## Rủi Ro Dễ Sót

| # | Rủi ro | Biện pháp phòng |
|---|--------|-----------------|
| 1 | Sửa `chucVu` → `vaiTro` bỏ sót file | `grep -r "chucVu" src/ --include="*.jsx" --include="*.js"` → sửa hết |
| 2 | AdminUsers.jsx (Tuần 3) đang CRUD NhanVienYTe trực tiếp | Refactor toàn bộ → gọi `/api/admin/users` mới |
| 3 | VietQR popup (Tuần 4) không tương thích wizard mới | Extract logic QR thành component riêng, embed vào PaymentStep |
| 4 | `isReadOnly` logic chưa cover hết edge case | Test bằng ma trận 6 vai trò × 11 tabs × N actions |
| 5 | BE đổi response shape → FE adapter cũ parse sai | Dev 2 phải review mọi `src/api/*.js` sau khi Dev 1 chuẩn hóa |
| 6 | Route guard bypass khi user refresh trang → store rỗng | ProtectedRoute phải check auth state, redirect `/login` nếu chưa đăng nhập |
| 7 | Wizard bước 2 (thanh toán) fail → bước 1 (phiếu) đã tạo rồi | UX: hiện "Hóa đơn đã tạo, bạn có thể thu tiền sau" + nút retry |
