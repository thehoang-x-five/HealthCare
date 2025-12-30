# KẾ HOẠCH BỔ SUNG PHÂN TRANG CHO TẤT CẢ DANH SÁCH

## ✅ ĐÃ LÀM
1. Kho thuốc (Prescriptions - Stock)
2. Tab kê thuốc (RxPickerModal)

---

## 🔄 CẦN LÀM

### 1. Bệnh nhân (Patients)
**File:** `api/patients.js`, `routes/Patients.jsx`
- ✅ API đã hỗ trợ phân trang (Page, PageSize)
- ❌ Hook `usePatientsList` chưa trả về PagedResult đầy đủ
- ❌ Route chưa có UI phân trang
- **Action:** 
  - Cập nhật `usePatientsList` để trả về { Items, TotalItems, Page, PageSize }
  - Thêm Pagination vào `Patients.jsx`
  - Đổi pageSize mặc định từ 500 → 50

### 2. Thông báo (Notifications)
**File:** `api/notifications.js`, `routes/Notifications.jsx`
- ✅ API đã hỗ trợ phân trang
- ❌ Hook chưa trả về PagedResult đầy đủ
- ❌ Route chưa có UI phân trang
- **Action:**
  - Cập nhật hook để trả về PagedResult
  - Thêm Pagination
  - Đổi pageSize mặc định từ 500 → 50

### 3. Hàng chờ (Queue)
**File:** `api/queue.js`, `routes/Examination.jsx`
- ✅ API đã hỗ trợ phân trang
- ❌ Hook chưa trả về PagedResult đầy đủ
- ❌ Route chưa có UI phân trang
- **Action:**
  - Cập nhật hook để trả về PagedResult
  - Thêm Pagination (nếu cần)
  - Đổi pageSize mặc định từ 500 → 50

### 4. Lịch hẹn (Appointments)
**File:** `api/appointments.js`, `routes/Appointments.jsx`
- ✅ API đã hỗ trợ phân trang
- ❌ Hook chưa trả về PagedResult đầy đủ
- ❌ Route chưa có UI phân trang
- **Action:**
  - Cập nhật hook để trả về PagedResult
  - Thêm Pagination
  - Đổi pageSize mặc định từ 500 → 50

### 5. Nhân sự (Staff)
**File:** `api/staff.js`, `routes/Staff.jsx`
- ✅ API đã hỗ trợ phân trang (PageSize mặc định = 50)
- ❌ Hook chưa trả về PagedResult đầy đủ
- ❌ Route chưa có UI phân trang
- **Action:**
  - Cập nhật hook để trả về PagedResult
  - Thêm Pagination

### 6. Lịch sử (History)
**File:** `api/history.js`, `routes/History.jsx`
- ✅ API đã hỗ trợ phân trang
- ❌ Hook chưa trả về PagedResult đầy đủ
- ❌ Route chưa có UI phân trang
- **Action:**
  - Cập nhật hook để trả về PagedResult
  - Thêm Pagination
  - Đổi pageSize mặc định từ 500 → 50

### 7. Phòng (Departments)
**File:** `api/departments.js`, `routes/Departments.jsx`
- ✅ API đã hỗ trợ phân trang (PageSize mặc định = 50)
- ✅ Hook đã trả về PagedResult (có items, totalItems, page, pageSize)
- ❌ Route chưa có UI phân trang
- **Action:**
  - Thêm Pagination vào `Departments.jsx`

---

## 📋 PATTERN CHUNG

### 1. Cập nhật API Hook:
```javascript
// Trước:
export function useXXXList(params = {}, options) {
  return useQuery({
    queryKey: ["xxx", params],
    queryFn: () => listXXX(params),
    select: (res) => {
      const items = res?.items ?? res?.Items ?? res ?? [];
      return Array.isArray(items) ? items.map(normalize) : [];
    },
    ...options,
  });
}

// Sau:
export function useXXXList(params = {}, options) {
  return useQuery({
    queryKey: ["xxx", params],
    queryFn: () => listXXX(params),
    select: (res) => {
      // Trả về PagedResult đầy đủ
      const items = res?.Items ?? res?.items ?? [];
      return {
        Items: Array.isArray(items) ? items.map(normalize) : [],
        TotalItems: res?.TotalItems ?? res?.totalItems ?? items.length,
        Page: res?.Page ?? res?.page ?? (params.page ?? 1),
        PageSize: res?.PageSize ?? res?.pageSize ?? (params.pageSize ?? 50),
      };
    },
    keepPreviousData: true,
    ...options,
  });
}
```

### 2. Cập nhật Route Component:
```javascript
// Thêm state
const [page, setPage] = useState(1);

// Sử dụng hook
const { data: result = { Items: [], TotalItems: 0, Page: 1, PageSize: 50 } } = useXXXList({
  ...filters,
  page,
  pageSize: 50,
});

const items = result.Items || [];
const totalItems = result.TotalItems || 0;
const totalPages = Math.ceil(totalItems / 50);

// Reset page khi filter thay đổi
useEffect(() => {
  if (page > 1) setPage(1);
}, [filters]); // dependencies là các filter

// Thêm Pagination component
{totalPages > 1 && (
  <Pagination
    currentPage={page}
    totalPages={totalPages}
    totalItems={totalItems}
    pageSize={50}
    onPageChange={setPage}
    className="..."
  />
)}
```

---

## 🎯 THỨ TỰ THỰC HIỆN

1. ✅ Patients (quan trọng nhất)
2. ✅ Notifications
3. ✅ Appointments
4. ✅ Queue (nếu cần)
5. ✅ Staff
6. ✅ History
7. ✅ Departments

