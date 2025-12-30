# KẾ HOẠCH CHUẨN HÓA PHÂN TRANG

## ✅ ĐÃ LÀM

### Backend:
1. ✅ Chuẩn hóa PageSize mặc định = 50 cho tất cả các service
   - PharmacyService: 500 → 50
   - QueueService: 500 → 50  
   - ClsService: 500 → 50
   - DTOs: PageSize mặc định = 50

2. ✅ Giữ nguyên các giá trị khác:
   - HistoryService: 50 (đã đúng)
   - ClinicalService: 20 (đã đúng, cho search)
   - ClsSummary: 20 (đã đúng, cho summary)
   - NotificationService: 50 (đã đúng)

### Frontend:
1. ✅ Tạo component Pagination.jsx
2. ✅ Cập nhật searchStock() để trả về PagedResult đầy đủ
3. ✅ Thêm hook useSearchStock() với keepPreviousData

---

## 🔄 CẦN LÀM TIẾP

### 1. Cập nhật Prescriptions.jsx
- Thay `useStock()` → `useSearchStock()` với filter từ state
- Thêm state cho pagination: `[stockPage, setStockPage] = useState(1)`
- Chuyển logic filter từ FE → BE (qua API searchStock)
- Thêm Pagination component vào StockTable

### 2. Cập nhật RxPickerModal.jsx
- Thay `useStock()` → `useSearchStock()` với keyword từ input
- Thêm pagination nếu kết quả nhiều
- Có thể dùng infinite scroll hoặc pagination đơn giản

### 3. Các nơi khác cần phân trang:
- History.jsx (nếu có list)
- Appointments.jsx (nếu có list)
- Staff.jsx (nếu có list)
- Notifications.jsx (nếu có list)
- Reports.jsx (nếu có list)

---

## 📋 CHI TIẾT IMPLEMENT

### Prescriptions.jsx - Stock Tab:

```javascript
// Thay đổi từ:
const stockQuery = useQuery({
  queryKey: ["rxStock"],
  queryFn: getStock,
  staleTime: 30_000,
});
const stock = stockQuery.data || [];

// Thành:
const [stockPage, setStockPage] = useState(1);
const stockQuery = useSearchStock({
  keyword: qStockDef,
  status: stockStatus === "all" ? null : stockStatus,
  page: stockPage,
  pageSize: 50,
});
const stockResult = stockQuery.data || { Items: [], TotalItems: 0, Page: 1, PageSize: 50 };
const stock = stockResult.Items || [];
const stockTotalItems = stockResult.TotalItems || 0;
const stockTotalPages = Math.ceil(stockTotalItems / 50);

// Xóa filteredStock (filter đã làm ở BE)
// Thêm Pagination:
<Pagination
  currentPage={stockPage}
  totalPages={stockTotalPages}
  totalItems={stockTotalItems}
  pageSize={50}
  onPageChange={setStockPage}
  className="px-4 py-3 border-t"
/>
```

### RxPickerModal.jsx:

```javascript
// Thay đổi từ:
const { data: stock = [], isFetching } = useStock({ enabled: !!open });

// Thành:
const [stockPage, setStockPage] = useState(1);
const stockQuery = useSearchStock({
  keyword: qDef,
  page: stockPage,
  pageSize: 20, // Ít hơn vì là modal
}, { enabled: !!open });
const stockResult = stockQuery.data || { Items: [], TotalItems: 0 };
const stock = stockResult.Items || [];
const isFetching = stockQuery.isFetching;
```

---

## ⚠️ LƯU Ý

1. **Prescriptions.jsx hiện đang filter ở FE** (filteredStock) - cần chuyển logic này sang BE
2. **Stats (stockCount, stockActiveCount, etc.)** cần tính từ BE hoặc từ TotalItems
3. **Unit filter** có thể cần thêm vào API searchStock nếu chưa có
4. **RxPickerModal** có thể không cần pagination nếu dữ liệu ít, nhưng nên có để tương lai

