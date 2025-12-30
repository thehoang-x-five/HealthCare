# HƯỚNG DẪN THÊM PHÂN TRANG CHO CÁC ROUTE COMPONENTS

## ✅ ĐÃ CẬP NHẬT API HOOKS

Tất cả các API hooks đã được cập nhật để trả về PagedResult đầy đủ:
- ✅ `usePatientsList` - trả về { Items, TotalItems, Page, PageSize }
- ✅ `useNotifications` - trả về { Items, TotalItems, Page, PageSize }
- ✅ `useHistoryVisits` - trả về { Items, TotalItems, Page, PageSize }
- ✅ `useHistoryTransactions` - trả về { Items, TotalItems, Page, PageSize }
- ✅ `useStaff` - trả về { Items, TotalItems, Page, PageSize }
- ✅ `useQueueSearch` - trả về { Items, TotalItems, Page, PageSize }
- ✅ `useDepartmentRooms` - đã có PagedResult (chỉ cần thêm UI)

---

## 📋 CẦN CẬP NHẬT ROUTE COMPONENTS

### Pattern chung để thêm phân trang:

```javascript
// 1. Import Pagination component
import Pagination from "../components/ui/Pagination.jsx";

// 2. Thêm state cho page
const [page, setPage] = useState(1);

// 3. Cập nhật hook để nhận page và pageSize
const { data: result = { Items: [], TotalItems: 0, Page: 1, PageSize: 50 } } = useXXXList({
  ...filters,
  page,
  pageSize: 50,
});

// 4. Lấy data từ result
const items = result.Items || [];
const totalItems = result.TotalItems || 0;
const totalPages = Math.ceil(totalItems / 50);

// 5. Reset page khi filter thay đổi
useEffect(() => {
  if (page > 1) setPage(1);
}, [filter1, filter2, ...]); // dependencies là các filter

// 6. Thêm Pagination component vào JSX (sau table/list)
{totalPages > 1 && (
  <div className="flex-shrink-0 border-t border-slate-200 bg-white">
    <Pagination
      currentPage={page}
      totalPages={totalPages}
      totalItems={totalItems}
      pageSize={50}
      onPageChange={setPage}
      className="px-4 py-3"
    />
  </div>
)}
```

---

## 📝 CÁC ROUTE CẦN CẬP NHẬT

### 1. Notifications.jsx
**Hook:** `useNotifications`
**Pattern:** Tương tự Patients.jsx

### 2. Appointments.jsx
**Hook:** `useAppointmentsByDate` hoặc `useAppointmentsRange`
**Lưu ý:** Appointments có thể dùng calendar view, cần xem xét cách hiển thị phân trang phù hợp

### 3. History.jsx
**Hooks:** 
- `useHistoryVisits` (tab lượt khám)
- `useHistoryTransactions` (tab giao dịch)
**Lưu ý:** Có 2 tabs, cần phân trang riêng cho mỗi tab

### 4. Staff.jsx
**Hook:** `useStaff`
**Pattern:** Tương tự Patients.jsx

### 5. Departments.jsx
**Hook:** `useDepartmentRooms`
**Lưu ý:** Hook đã trả về PagedResult, chỉ cần thêm UI

### 6. Examination.jsx (Queue)
**Hook:** `useQueueSearch`
**Lưu ý:** Queue có thể cần phân trang tùy theo use case

---

## ✅ ĐÃ HOÀN THÀNH

1. ✅ **Patients.jsx** - Đã có phân trang đầy đủ

---

## 🎯 KẾT LUẬN

**Backend và API hooks đã hoàn tất!** Chỉ cần cập nhật UI cho các route components còn lại theo pattern trên.

