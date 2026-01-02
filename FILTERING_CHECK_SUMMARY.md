# Kiểm Tra Filtering & Sorting - Tổng Kết

## ✅ Đã Xử Lý Ở Backend

### 1. **Patients.jsx**
- ✅ Filtering: keyword, status, accountStatus, todayOnly → Backend
- ✅ Sorting: name, date, priority → Backend
- ✅ Pagination: ✅

### 2. **History.jsx** 
- ✅ Filtering: date range, keyword, scope, visitType → Backend
- ✅ Sorting: date desc (mặc định) → Backend
- ✅ Pagination: ✅ (cả 2 tabs: visits & transactions)

### 3. **Prescriptions.jsx - Orders Tab**
- ✅ Filtering: keyword, status, date range → Backend
- ✅ Sorting: date desc (mặc định) → Backend
- ✅ Pagination: ✅

### 4. **Prescriptions.jsx - Stock Tab**
- ✅ Filtering: keyword, status, unit → Backend
- ⚠️ **VẪN CÓ**: Frontend filtering cho unit (cần xóa)
- ✅ Pagination: ✅

### 5. **Examination.jsx - Queue**
- ✅ Filtering: source, status, kind, keyword → Backend
- ✅ Sorting: priority logic đặc biệt → Backend (đúng)
- ✅ Pagination: ✅

### 6. **RxPickerModal** (Modal kê thuốc)
- ✅ Filtering: keyword → Backend
- ✅ Pagination: ✅ (pageSize = 20)

### 7. **Staff.jsx**
- ✅ Filtering: keyword, status, dept, nurseKind → Backend
- ✅ Pagination: ✅ (nếu backend hỗ trợ)

### 8. **Appointments.jsx**
- ✅ Filtering: date range, status → Backend
- ⚠️ **UI Filter**: Chỉ filter ẩn lịch đã hủy ở FE (`.filter((a) => a.status !== APPT_STATUS.DA_HUY)`) - Đây là logic UI, không ảnh hưởng pagination, có thể giữ

---

## ❌ Chưa Xử Lý Ở Backend

### 1. **Notifications.jsx** ⚠️ **QUAN TRỌNG**

**Frontend đang filter:**
- ✅ Tab (unread/today) → Backend đã hỗ trợ (OnlyUnread, FromTime/ToTime)
- ❌ Keyword → Backend chưa hỗ trợ trong `NotificationFilterRequest`
- ❌ Type (loại thông báo) → Backend chưa hỗ trợ trong `NotificationFilterRequest`
- ❌ Priority (ưu tiên) → Backend chưa hỗ trợ trong `NotificationFilterRequest`
- ❌ Sorting → Backend chưa hỗ trợ

**Backend hiện tại:**
- `NotificationFilterRequest` có: LoaiNguoiNhan, MaNguoiNhan, OnlyUnread, TrangThai, FromTime, ToTime, Page, PageSize
- `NotificationSearchFilter` có: LoaiThongBao, MucDoUuTien, TrangThai, Keyword, FromTime, ToTime, Page, PageSize (nhưng có vẻ API không dùng)

**Cần làm:**
1. Thêm Keyword, LoaiThongBao, MucDoUuTien vào `NotificationFilterRequest` (hoặc chuyển sang dùng `NotificationSearchFilter`)
2. Thêm SortBy, SortDirection
3. Update frontend để pass các filter này vào API
4. Remove frontend filtering logic

---

### 2. **Departments.jsx** ⚠️ **QUAN TRỌNG**

**Frontend đang filter:**
- ❌ Status (online/offline) - Dòng 368-408 filter client-side
- ❌ RoomType (ls/cls) - Dòng 368-408 filter client-side
- ❌ Keyword - Dòng 368-408 filter client-side
- ❌ Sorting (capacity) - Dòng 368-408 sort client-side

**Backend hiện tại:**
- BE đã có: `POST /api/master-data/rooms/cards/search` với `RoomSearchFilter`
  - Có: Keyword, LoaiPhong, TrangThai, MaKhoa, SortBy, SortDirection, Page, PageSize
- FE đã gọi API: `listDepartments()` trong `departments.js` (dòng 577)
  - Nhưng KHÔNG TRUYỀN filter params từ UI vào API
  - FE nhận toàn bộ data rồi filter client-side (dòng 368-408 trong Departments.jsx)

**Cần làm:**
1. ✅ Backend đã sẵn sàng - Không cần sửa
2. Update `useDepartmentRooms()` để nhận filter params từ UI
3. Update `Departments.jsx` để pass filters vào `useDepartmentRooms()`
4. Remove frontend filtering logic (dòng 368-408)
5. Thêm pagination UI (hiện tại load toàn bộ)

---

## 📋 Checklist Chi Tiết

### Notifications.jsx
- [ ] **Option 1 (Khuyến nghị):** Thêm Keyword, LoaiThongBao, MucDoUuTien, SortBy, SortDirection vào `NotificationFilterRequest`
- [ ] **Option 2:** Chuyển sang dùng `/api/notification/search` với `NotificationSearchFilter` (cần thêm SortBy, SortDirection)
- [ ] Update NotificationService để xử lý các field mới
- [ ] Update `listNotifications()` trong `notifications.js` để pass filters
- [ ] Update `Notifications.jsx` để pass filters vào API (keyword, type, priority, sortBy, sortDirection)
- [ ] Remove frontend filtering logic (dòng 136-195 trong Notifications.jsx)
- [ ] Thêm pagination UI

### Departments.jsx
- [ ] Update `useDepartmentRooms()` trong `departments.js` để nhận filter params
- [ ] Update `Departments.jsx` để pass filters vào `useDepartmentRooms()` (keyword, status, roomType, sortBy, sortDirection, page, pageSize)
- [ ] Remove frontend filtering logic (dòng 368-408 trong Departments.jsx)
- [ ] Thêm pagination UI
- [ ] Reset page khi filter thay đổi

### Prescriptions.jsx - Stock Tab
- [ ] Xóa frontend unit filtering (dòng 236-256) - BE đã xử lý DonViTinh

---

## ⚠️ Các Filter Không Ảnh Hưởng Pagination

Các filter này chỉ dùng để tính stats hoặc UI logic, không ảnh hưởng đến pagination:

1. **Appointments.jsx**: Filter ẩn lịch đã hủy (`.filter((a) => a.status !== APPT_STATUS.DA_HUY)`) - chỉ để UI
2. **Examination.jsx**: Filter để tính stats (waitingCount, inProgressCount, doneCount) - chỉ để hiển thị số liệu
3. **Patients.jsx**: Filter để tính counts - chỉ để hiển thị số liệu
4. **Prescriptions.jsx**: Filter để tính stats (ordersCreatedCount, ordersPendingCount, etc.) - chỉ để hiển thị số liệu

**→ Các filter này có thể giữ lại ở FE vì không ảnh hưởng pagination**

