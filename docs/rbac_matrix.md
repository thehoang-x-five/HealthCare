# 🔐 HealthCare+ RBAC Permission Matrix

> **Nguồn dữ liệu**: Trích xuất từ code thực tế — `permissions.js`, tất cả `src/routes/*.jsx`, `src/components/**/*.jsx`, `src/api/*.js`

---

## Vai trò hệ thống

| # | Vai trò | Code | Loại Y tá | Data Scope |
|---|---------|------|-----------|------------|
| 1 | **Admin** | `admin` | — | 🌐 Global (toàn PK) |
| 2 | **Y tá Hành chính** (Tiếp nhận) | `y_ta` | `hanhchinh` | 🌐 Global (toàn PK) |
| 3 | **Bác sĩ** | `bac_si` | — | 🏥 Theo Khoa (`MaKhoa`) |
| 4 | **Y tá Lâm sàng** | `y_ta` | `ls` | 🏥 Theo Khoa |
| 5 | **Y tá CLS** | `y_ta` | `cls` | 🏥 Theo Khoa |
| 6 | **Kỹ thuật viên** | `ky_thuat_vien` | — | 🏥 Theo Khoa |

---

## Tầng 1: Menu / Tab Visibility

> File: [Sidebar.jsx](file:///c:/Users/THINKPAD/Documents/GitHub/my-patients/src/components/ui/Sidebar.jsx) + [permissions.js](file:///c:/Users/THINKPAD/Documents/GitHub/my-patients/src/utils/permissions.js) `TAB_VISIBILITY`

| Trang | Admin | Y tá HC | Bác sĩ | Y tá LS | Y tá CLS | KTV |
|-------|:-----:|:-------:|:------:|:-------:|:--------:|:---:|
| Tổng quan | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Lịch hẹn | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Bệnh nhân | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Khám bệnh | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Khoa phòng | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Nhân sự | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Đơn thuốc | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Lịch sử | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Thông báo | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Báo cáo | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

> [!WARNING]
> **Vấn đề phát hiện**: Chỉ trang "Lịch hẹn" bị ẩn đúng cho vai trò không phù hợp. Tất cả trang còn lại đều `() => true` — bất kỳ ai đăng nhập đều thấy.

---

## Tầng 5: Action-Level Permissions (Chi tiết từng trang)

### 1. 📊 Tổng quan (`/` — Overview)

**Component**: `Overview.jsx` → `KpiRail.jsx`  
**API**: `dashboard.js` — `useDashboard()`

| Hành động | Admin | Y tá HC | Bác sĩ | Y tá LS | Y tá CLS | KTV | Guard hiện tại |
|-----------|:-----:|:-------:|:------:|:-------:|:--------:|:---:|----------------|
| Xem KPI tổng quan | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Xem KPI doanh thu | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| KPI CLS (thay lịch hẹn) | — | — | — | — | ✅ | — | `role === "cls"` trong KpiRail |

> [!NOTE]
> KPI doanh thu trên Dashboard **không** có guard (khác với trang Reports dùng `canViewRevenueReport`). Mọi vai trò đều thấy doanh thu ngày.

---

### 2. 📅 Lịch hẹn (`/appointments`)

**Pages**: `Appointments.jsx` → `ApptCalendar`, `ApptList`, `DayPanel`, `CreateDrawer`, `ApptDetailModal`  
**API**: `appointments.js` — `useCreateAppointment`, `useUpdateAppointment`, `useCheckInAppointment`, `useUpdateAppointmentStatus`

| Hành động | Admin | Y tá HC | Bác sĩ | Y tá LS | Y tá CLS | KTV | Guard hiện tại |
|-----------|:-----:|:-------:|:------:|:-------:|:--------:|:---:|----------------|
| Xem trang (menu) | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | `TAB_VISIBILITY.appointments` |
| Xem danh sách lịch hẹn | ✅ | ✅ | — | — | — | — | `canViewAppointment` |
| Tạo lịch hẹn mới | ❌ | ✅ | — | — | — | — | `canCreateAppointment` (chỉ YTHC) |
| Sửa lịch hẹn (đổi ngày/giờ) | ❌ | ✅ | — | — | — | — | `canEditAppointment` |
| Xác nhận / Hủy lịch | ❌ | ✅ | — | — | — | — | `ApptDetailModal` + BE guard |
| Check-in bệnh nhân | ❌ | ✅ | — | — | — | — | `ApptDetailModal` + BE guard |

> [!IMPORTANT]
> **Hiện trạng chuẩn**: Admin chỉ được xem module lịch hẹn. FE chỉ cho Y tá hành chính tạo/sửa/check-in, và BE cũng chặn admin ở `AppointmentsController` bằng `[RequireRole("y_ta")] + [RequireNurseType("hanhchinh")]`.

---

### 3. 👥 Bệnh nhân (`/patients`)

**Pages**: `Patients.jsx` → `PatientsTable`, `PatientModal` (5 mode: view, add, edit, exam, process)  
**API**: `patients.js`, `examination.js`, `queue.js`, `billing.js`

| Hành động | Admin | Y tá HC | Bác sĩ | Y tá LS | Y tá CLS | KTV | Guard hiện tại |
|-----------|:-----:|:-------:|:------:|:-------:|:--------:|:---:|----------------|
| Xem danh sách BN | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Tìm kiếm / lọc BN | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Tạo BN mới (mode=add) | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | `canCreatePatient` |
| Sửa thông tin BN (mode=edit) | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | `canEditPatient` |
| Xem hồ sơ BN (mode=view) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Lập phiếu khám (mode=exam) | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | `canCreateExam` (YTHC only) |
| Xử lý BN (mode=process) | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | `hasReceptionPermission` → `canManageReception` |
| Tạo lịch hẹn từ hồ sơ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | `canCreateAppointment` |
| Cập nhật trạng thái hôm nay | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | `hasReceptionPermission` trong PatientsTable |
| Hủy lượt khám (Cancel Visit) | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | `hasReceptionPermission` trong PatientsTable |
| In phiếu khám | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | Nằm trong flow exam → chỉ YTHC/Admin mở |
| Enqueue (đưa vào hàng đợi) | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | Nằm trong flow exam |

> [!NOTE]
> `canManageReception = isAdmin || isReceptionNurse` — đây là guard chính cho mọi thao tác tiếp nhận.

---

### 4. 🩺 Khám bệnh (`/examination`)

**Pages**: `Examination.jsx` → `PatientTable` (hàng đợi), `ExamDetail` (khám chi tiết)  
**API**: `queue.js`, `examination.js`, `billing.js`

| Hành động | Admin | Y tá HC | Bác sĩ | Y tá LS | Y tá CLS | KTV | Guard hiện tại |
|-----------|:-----:|:-------:|:------:|:-------:|:--------:|:---:|----------------|
| Xem hàng đợi LS | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | `defaultQueueKind` theo role |
| Xem hàng đợi CLS | ✅ | ✅ | ❌ | ❌ | ✅ | ✅ | `defaultQueueKind` theo role |
| Gọi BN tiếp theo (Dequeue) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | `canCallPatient` |
| Bắt đầu khám (Start Exam) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | **Không guard FE** — ai click cũng được |
| Nhập chẩn đoán (Diagnosis) | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | **Không guard FE** — logic nghiệp vụ cho phép BS |
| Kê đơn thuốc (Prescribe) | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | `canPrescribe` (chỉ BS) |
| Xuất phiếu CLS (Order CLS) | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | **Không guard FE** |
| Nhập kết quả CLS | ✅ | ❌ | ❌ | ❌ | ✅ | ✅ | `canManageCls` |
| Hoàn tất phiếu khám (Complete) | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | **Không guard FE** |
| Thanh toán (PaymentWizard) | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | `canManageReception` |
| In phiếu khám | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | **Không guard FE** |
| Hủy lượt khám | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | **Không guard FE** |
| Hủy phiếu CLS | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | **Không guard FE** |

> [!CAUTION]
> **Vấn đề nghiêm trọng**: Rất nhiều action trong ExamDetail **KHÔNG có guard FE** (nhập chẩn đoán, hoàn tất, hủy). Hiện tại phụ thuộc vào luồng nghiệp vụ (chỉ người gọi BN mới mở được ExamDetail), nhưng nếu ai đó truy cập URL trực tiếp thì có thể thao tác vượt quyền.

---

### 5. 🏥 Khoa phòng (`/departments`)

**Pages**: `Departments.jsx` → `DeptGrid`, `DeptCard`, `DeptModal`, `ScheduleModal`  
**API**: `departments.js` — `useDepartmentRooms`, `useDutyByRoom`

| Hành động | Admin | Y tá HC | Bác sĩ | Y tá LS | Y tá CLS | KTV | Guard hiện tại |
|-----------|:-----:|:-------:|:------:|:-------:|:--------:|:---:|----------------|
| Xem danh sách phòng | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Xem chi tiết phòng | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Xem lịch trực phòng | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Sửa thông tin phòng/khoa | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | `canEditDepartment` (chỉ Admin) |

> [!NOTE]
> `isReadOnly('departments')` trả `true` cho mọi non-Admin → DeptModal hiện tại chỉ hiện read-only. **Đúng chuẩn**.

---

### 6. 👨‍⚕️ Nhân sự (`/staff`)

**Pages**: `Staff.jsx` → `StaffGrid`/`StaffCard` (HR view) + `StaffTable` (Admin table view) + `StaffDetail`/`StaffSchedule`  
**API**: `staff.js`, `admin.js` — `useAdminUsers`, `useUpdateUserStatus`, `useResetPassword`

| Hành động | Admin | Y tá HC | Bác sĩ | Y tá LS | Y tá CLS | KTV | Guard hiện tại |
|-----------|:-----:|:-------:|:------:|:-------:|:--------:|:---:|----------------|
| Xem danh sách NV (Cards HR) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Toggle Card ↔ Table view | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | `canToggleStaffView` |
| Xem auth fields (username, role, status TK) | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | `canViewStaffAuth` |
| Khóa/Mở khóa tài khoản | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | `canLockUnlockStaff` |
| Reset mật khẩu NV | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | `canResetStaffPassword` |
| Xem chi tiết NV | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Xem lịch trực NV | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |

> [!TIP]
> Staff page là trang RBAC **tốt nhất** — đã guard đầy đủ Admin-only actions qua 4 hàm permission riêng.

---

### 7. 💊 Đơn thuốc (`/prescriptions`)

**Pages**: `Prescriptions.jsx` → `OrdersTable` (đơn) + `StockTable` (kho) + `StockModal` (sửa kho) + `OrderViewModal`  
**API**: `pharmacy.js` — `useSearchRxOrders`, `useSearchStock`, `upsertStockItem`, `useCancelPrescription`

| Hành động | Admin | Y tá HC | Bác sĩ | Y tá LS | Y tá CLS | KTV | Guard hiện tại |
|-----------|:-----:|:-------:|:------:|:-------:|:--------:|:---:|----------------|
| Xem danh sách đơn thuốc | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Xem chi tiết đơn (👁️) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Hủy đơn thuốc (✕) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | **⚠️ Không guard FE** — nút hiện cho tất cả |
| Xem kho thuốc | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Sửa kho thuốc (upsert) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | **⚠️ Không guard FE** — nút "Sửa" hiện cho tất cả |
| Kê đơn thuốc (từ ExamDetail) | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | `canPrescribe` (chỉ trong ExamDetail) |
| Phát thuốc (dispatch) | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | `canDispenseMedicine` — **chưa được dùng trong FE** |

> [!CAUTION]
> **2 vấn đề nghiêm trọng**:
> 1. **Hủy đơn thuốc**: Nút `✕` trong `OrdersTable` hiện cho tất cả vai trò khi status = `da_ke` hoặc `cho_phat`. Không có guard — Y tá LS/CLS/KTV có thể hủy đơn.
> 2. **Sửa kho thuốc**: Nút "Sửa" trong `StockTable` → mở `StockModal` và gọi `upsertStockItem` — không có guard FE. Bất kỳ ai cũng sửa được kho.
> 3. **`canDispenseMedicine`** đã declare nhưng **chưa bao giờ được dùng** trong component nào.

---

### 8. 📜 Lịch sử (`/history`)

**Pages**: `History.jsx` → `HistoryTable`, `HistoryDetailModal`  
**API**: `history.js` — `useHistoryVisits`, `useHistoryTransactions`

| Hành động | Admin | Y tá HC | Bác sĩ | Y tá LS | Y tá CLS | KTV | Guard hiện tại |
|-----------|:-----:|:-------:|:------:|:-------:|:--------:|:---:|----------------|
| Xem lượt khám (visits) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Xem giao dịch (transactions) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Xem chi tiết (modal) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Lọc/tìm kiếm | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |

> [!NOTE]
> Trang này chỉ có read actions — không có vấn đề bảo mật. Data scope phụ thuộc BE (dữ liệu trả về theo `MaKhoa` của user hoặc global).

---

### 9. 🔔 Thông báo (`/notifications`)

**Pages**: `Notifications.jsx` → `NotificationList`, `NotificationDetailModal`  
**API**: `notifications.js` — `useNotifications`

| Hành động | Admin | Y tá HC | Bác sĩ | Y tá LS | Y tá CLS | KTV | Guard hiện tại |
|-----------|:-----:|:-------:|:------:|:-------:|:--------:|:---:|----------------|
| Xem thông báo | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Lọc theo loại/ưu tiên | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Xem chi tiết thông báo | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Quản lý thông báo | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | `canManageNotifications` — **khai báo nhưng chưa dùng** |

> [!WARNING]
> `canManageNotifications` đã khai báo trong `permissions.js` nhưng **chưa được import/dùng** trong bất kỳ component nào.

---

### 10. 📈 Báo cáo (`/reports`)

**Pages**: `Reports.jsx` → `KpiCard`, `OverviewChart`, `ReportsTable`, `ClinicalAnalytics`  
**API**: `reports.js` — `useReportsOverview`

| Hành động | Admin | Y tá HC | Bác sĩ | Y tá LS | Y tá CLS | KTV | Guard hiện tại |
|-----------|:-----:|:-------:|:------:|:-------:|:--------:|:---:|----------------|
| Xem KPI doanh thu | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | `canViewRevenueReport` ✅ |
| Xem KPI lượt khám | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | `canViewVisitReport` ✅ |
| Xem KPI bệnh nhân mới | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Xem KPI tái khám | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Xem KPI tỷ lệ hủy | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Xem biểu đồ/bảng tổng hợp | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Xuất CSV | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Xem phân tích lâm sàng | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Không guard |
| Scope badge (hiện khoa) | — | — | ✅ | ✅ | ✅ | ✅ | `!hasGlobalScope` → hiện badge |

> [!TIP]
> Reports là trang **guard tốt nhất**: dùng cả 3 permission (`canViewRevenueReport`, `canViewVisitReport`, `canViewStaffReport`). `scopeLabel` hiện badge cho user non-global.

---

### 11. ⚙️ Cài đặt (`/settings`)

**Pages**: `Settings.jsx` → `SettingsScreen`, `OtpVerificationModal`  
**API**: `auth.js` — `changePasswordApi`

| Hành động | Admin | Y tá HC | Bác sĩ | Y tá LS | Y tá CLS | KTV | Guard hiện tại |
|-----------|:-----:|:-------:|:------:|:-------:|:--------:|:---:|----------------|
| Xem profile cá nhân | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Requires login |
| Đổi mật khẩu (OTP) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Requires email |
| Đăng xuất | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | — |

---

## 🚨 Tổng hợp lỗi RBAC phát hiện

### Lỗi nghiêm trọng (Cần fix)

| # | Trang | Vấn đề | Chi tiết | Fix đề xuất |
|---|-------|--------|----------|-------------|
| 1 | **Đơn thuốc** | Hủy đơn không guard | `OrdersTable` line 254: nút `✕` không check quyền | Thêm `canDispenseMedicine` hoặc `canManageReception` |
| 2 | **Đơn thuốc** | Sửa kho không guard | `StockTable` line 334: nút "Sửa" không check quyền | Thêm `canManageReception` hoặc `isAdmin` |
| 3 | **Khám bệnh** | Nhập chẩn đoán không guard | `ExamDetail`: form diagnosis hiện cho mọi vai trò khi mở được | Thêm `isDoctor || isAdmin` check |
| 4 | **Khám bệnh** | Hoàn tất phiếu không guard | `ExamDetail`: nút Complete hiện cho mọi vai trò | Thêm `isDoctor || isAdmin` check |
| 5 | **Khám bệnh** | Hủy lượt không guard | `ExamDetail`: cancel visit/CLS không check quyền | Thêm guard tương ứng |

### Lỗi trung bình (Nên fix)

| # | Trang | Vấn đề | Chi tiết |
|---|-------|--------|----------|
| 6 | **Lịch hẹn** | Admin thao tác vượt mức | `ApptDetailModal`: Admin dùng được nút Xác nhận/Hủy/Check-in dù `isReadOnly` = true |
| 7 | **Tổng quan** | Doanh thu hiện cho mọi vai trò | `KpiRail`: KPI "Doanh thu hôm nay" không guard (khác Reports) |
| 8 | **Báo cáo** | Xuất CSV không guard | Bất kỳ vai trò nào cũng export được CSV báo cáo |

### Permission chưa sử dụng (Dead code)

| # | Permission | Khai báo | Sử dụng |
|---|-----------|----------|---------|
| 1 | `canDispenseMedicine` | `permissions.js:110` | ❌ Chưa import trong bất kỳ component |
| 2 | `canManageNotifications` | `permissions.js:121` | ❌ Chưa import trong bất kỳ component |

---

## ✅ Đánh giá tổng quan

| Tiêu chí | Đánh giá |
|----------|----------|
| Menu Visibility (Tầng 1) | ⚠️ Quá mở — 9/10 trang `() => true` |
| Route Guard (Tầng 2) | ✅ `ProtectedRoute` + `TAB_VISIBILITY` |
| Page-level guard (Tầng 3) | ⚠️ Chỉ Reports có guard rõ |
| Component-level guard (Tầng 4) | ✅ Staff page tốt, Patients OK |
| Action-level guard (Tầng 5) | ❌ Thiếu nhiều — Đơn thuốc + Khám bệnh ít guard |
| Data scope (Tầng 6-7) | ⚠️ Phụ thuộc BE — FE chỉ hiện `scopeLabel` badge |
