# Phân Tích Vấn Đề Filtering & Sorting với Pagination

## 🔍 Vấn Đề Hiện Tại

### 1. **Patients.jsx**
- ✅ **Filtering**: Đã làm ở **Backend** (keyword, status, accountStatus, todayOnly)
- ❌ **Sorting**: Đang làm ở **Frontend** (priority, name, date) → **CHỈ ÁP DỤNG CHO 1 PAGE**
- Backend đã hỗ trợ `SortBy` và `SortDirection` nhưng FE chưa sử dụng

### 2. **Prescriptions.jsx - Stock Tab**
- ✅ **Filtering**: Keyword và Status đã làm ở **Backend**
- ❌ **Filtering**: Unit vẫn filter ở **Frontend** → **CHỈ ÁP DỤNG CHO 1 PAGE**

### 3. **Prescriptions.jsx - Orders Tab**
- ❌ **Filtering**: Tất cả đang làm ở **Frontend** (keyword, status, date range) → **CHỈ ÁP DỤNG CHO 1 PAGE**
- ❌ **Sorting**: Chưa có, nhưng nếu có cũng sẽ chỉ áp dụng cho 1 page

### 4. **History.jsx**
- ❌ **Filtering**: Tất cả đang làm ở **Frontend** (date range, keyword, scope, visitType, txnType) → **CHỈ ÁP DỤNG CHO 1 PAGE**
- ❌ **Sorting**: Đang sort ở **Frontend** (date) → **CHỈ ÁP DỤNG CHO 1 PAGE**

### 5. **Examination.jsx - Queue**
- ❌ **Filtering**: Tất cả đang làm ở **Frontend** (source, status, kind, search) → **CHỈ ÁP DỤNG CHO 1 PAGE**
- ⚠️ Queue có logic đặc biệt (ưu tiên, appointment time) → cần cân nhắc

---

## 📊 Đánh Giá Các Phương Án

### **Phương Án 1: Filtering & Sorting ở Backend (RECOMMENDED)**

#### ✅ **Ưu điểm:**
1. **Đúng với pagination**: Filtering/sorting áp dụng cho toàn bộ dataset, không chỉ 1 page
2. **Hiệu năng tốt hơn**: Database engine tối ưu hóa queries tốt hơn JavaScript
3. **Giảm tải frontend**: Không cần load toàn bộ data rồi filter/sort ở FE
4. **Scalability**: Khi data tăng, backend vẫn hoạt động tốt
5. **Nhất quán**: Tất cả các trang đều dùng cùng pattern

#### ❌ **Nhược điểm:**
1. **Phải implement ở backend**: Cần thêm logic cho mỗi API
2. **Phải call API mỗi lần filter/sort**: Tăng số lượng request (nhưng có React Query cache)
3. **Phức tạp hơn**: Một số logic phức tạp (như priority scoring) khó implement ở backend

#### 💡 **Khi nào nên dùng:**
- ✅ Hầu hết các trường hợp
- ✅ Khi có pagination
- ✅ Khi dataset lớn
- ✅ Khi cần filter/sort theo nhiều tiêu chí

---

### **Phương Án 2: Filtering & Sorting ở Frontend**

#### ✅ **Ưu điểm:**
1. **Linh hoạt**: Dễ implement logic phức tạp (priority scoring, custom sorting)
2. **Instant feedback**: Không cần call API, filter/sort ngay lập tức
3. **Ít request**: Chỉ load data 1 lần

#### ❌ **Nhược điểm:**
1. **❌ KHÔNG TƯƠNG THÍCH VỚI PAGINATION**: Chỉ filter/sort được data trong 1 page
2. **Hiệu năng kém**: Phải load toàn bộ data về FE
3. **Không scalable**: Khi data lớn (>1000 items) sẽ chậm
4. **Tốn băng thông**: Phải load data không cần thiết

#### 💡 **Khi nào nên dùng:**
- ⚠️ Chỉ khi KHÔNG có pagination
- ⚠️ Khi dataset nhỏ (<100 items)
- ⚠️ Khi logic filter/sort quá phức tạp không thể làm ở backend

---

### **Phương Án 3: Hybrid (Kết hợp)**

#### ✅ **Ưu điểm:**
1. **Linh hoạt**: Có thể chọn filter/sort ở đâu tùy từng trường hợp
2. **Tối ưu**: Backend làm những gì làm được, FE làm phần còn lại

#### ❌ **Nhược điểm:**
1. **Phức tạp**: Khó maintain, dễ nhầm lẫn
2. **Không nhất quán**: Mỗi trang làm khác nhau
3. **Vẫn có vấn đề**: Phần làm ở FE vẫn chỉ áp dụng cho 1 page

#### 💡 **Khi nào nên dùng:**
- ⚠️ Chỉ khi có lý do đặc biệt (ví dụ: Queue có logic đặc biệt)

---

## 🎯 Giải Pháp Đề Xuất

### **RECOMMENDED: Phương Án 1 - Backend Filtering & Sorting**

#### **Lý do:**
1. ✅ **Đúng với pagination**: Đây là vấn đề cốt lõi - filter/sort phải áp dụng cho toàn bộ dataset
2. ✅ **Backend đã hỗ trợ một phần**: `PatientSearchFilter` đã có `SortBy` và `SortDirection`
3. ✅ **Best practice**: Server-side filtering/sorting với pagination là standard
4. ✅ **Scalable**: Khi data tăng, hệ thống vẫn hoạt động tốt

---

## 📋 Kế Hoạch Triển Khai

### **Bước 1: Patients.jsx** ✅ (Backend đã hỗ trợ)

**Hiện tại:**
- Filtering: ✅ Backend
- Sorting: ❌ Frontend (priority, name, date)

**Cần làm:**
1. Map frontend sort options sang backend `SortBy`:
   - `sort === "name"` → `SortBy: "hoten"`
   - `sort === "date"` → `SortBy: "ngaytrangthai"`  
   - `sort === "priority"` → **Cần implement ở backend** (logic phức tạp)

2. **Vấn đề với "priority" sorting:**
   - Logic priority hiện tại dựa trên status code (cho_tiep_nhan=50, cho_kham=40, ...)
   - Có thể implement ở backend bằng CASE WHEN trong SQL
   - Hoặc có thể thêm field `DoUuTien` vào `BenhNhan` entity

### **Bước 2: Prescriptions.jsx - Stock Tab**

**Hiện tại:**
- Filtering: Keyword + Status ✅ Backend, Unit ❌ Frontend
- Sorting: Chưa có

**Cần làm:**
1. Backend cần hỗ trợ filter theo `DonViTinh` (Unit)
2. Nếu cần sorting, thêm `SortBy` và `SortDirection` vào `DrugSearchFilter`

### **Bước 3: Prescriptions.jsx - Orders Tab**

**Hiện tại:**
- Filtering: ❌ Tất cả ở Frontend
- Sorting: Chưa có

**Cần làm:**
1. Backend cần hỗ trợ:
   - Filter theo keyword (search trong id, ptId, ptName, doctor, diag)
   - Filter theo status (`TrangThai`)
   - Filter theo date range (`fromDate`, `toDate`)
2. Nếu cần sorting, thêm `SortBy` và `SortDirection` vào `PrescriptionSearchFilter`

### **Bước 4: History.jsx**

**Hiện tại:**
- Filtering: ❌ Tất cả ở Frontend
- Sorting: ❌ Frontend (date desc)

**Cần làm:**
1. Backend cần hỗ trợ:
   - Filter theo date range (`fromTime`, `toTime`)
   - Filter theo keyword
   - Filter theo scope (patient, doctor, dept)
   - Filter theo visitType (visits) hoặc txnType (transactions)
   - Sort theo date (`SortBy: "thoigian"`)

2. **Lưu ý**: API `/history/visits/search` và `/billing/invoices/search` có thể đã hỗ trợ một phần, cần kiểm tra

### **Bước 5: Examination.jsx - Queue**

**Hiện tại:**
- Filtering: ❌ Tất cả ở Frontend
- Sorting: Queue có logic đặc biệt (ưu tiên, appointment time)

**Cần làm:**
1. Queue có logic sorting đặc biệt (đã được implement ở backend trong `QueueService.TimKiemHangDoiAsync`)
2. Backend cần hỗ trợ filtering:
   - Filter theo `Nguon` (source)
   - Filter theo `TrangThai` (status)
   - Filter theo `LoaiHangDoi` (kind - cls vs clinical)
   - Filter theo keyword (search trong name, pid, doctor, dept, phone)

3. **Lưu ý**: Queue có logic ưu tiên phức tạp, cần giữ nguyên logic sorting ở backend

---

## 🔧 Implementation Details

### **Pattern chung cho Frontend:**

```javascript
// Thay vì:
const filtered = useMemo(() => {
  return items.filter(...).sort(...);
}, [items, filter, sort]);

// Sẽ là:
const { data } = useQuery({
  queryKey: ['resource', filter, sort, page],
  queryFn: () => api.search({ 
    ...filter, 
    sortBy: mapSortToBackend(sort),
    sortDirection: sortDirection,
    page,
    pageSize: 50
  })
});

// Reset page khi filter/sort thay đổi
useEffect(() => {
  setPage(1);
}, [filter, sort]);
```

### **Pattern chung cho Backend:**

```csharp
// 1. Filtering
if (!string.IsNullOrWhiteSpace(filter.Keyword)) {
    query = query.Where(x => x.Name.Contains(filter.Keyword));
}

// 2. Sorting
query = filter.SortBy?.ToLowerInvariant() switch
{
    "name" when filter.SortDirection == "desc" => query.OrderByDescending(x => x.Name),
    "name" => query.OrderBy(x => x.Name),
    "date" when filter.SortDirection == "desc" => query.OrderByDescending(x => x.Date),
    "date" => query.OrderBy(x => x.Date),
    _ => query.OrderBy(x => x.Name) // default
};

// 3. Pagination
var total = await query.CountAsync();
var items = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

---

## ⚠️ Edge Cases & Special Considerations

### **1. Priority Sorting (Patients.jsx)**
- Logic priority phức tạp, dựa trên status code
- **Giải pháp**: Implement ở backend bằng CASE WHEN hoặc thêm field `DoUuTien`

### **2. Queue Sorting (Examination.jsx)**
- Queue có logic ưu tiên đặc biệt (appointment time, checkin time)
- **Giải pháp**: Giữ nguyên logic ở backend, chỉ thêm filtering

### **3. Unit Filtering (Stock Tab)**
- Hiện tại backend chưa hỗ trợ filter theo `DonViTinh`
- **Giải pháp**: Thêm vào `DrugSearchFilter`

### **4. Complex Date Range Filtering**
- Một số filter cần date range (History, Orders)
- **Giải pháp**: Backend đã hỗ trợ `fromTime`/`toTime` hoặc `fromDate`/`toDate`

---

## 📊 So Sánh Performance

### **Frontend Filtering (1 page = 50 items):**
- Load time: ~100-200ms
- Filter time: ~5-10ms
- **Tổng**: ~105-210ms
- **Vấn đề**: Chỉ filter được 50 items, không phải toàn bộ dataset

### **Backend Filtering (toàn bộ dataset):**
- API call: ~150-300ms (có cache)
- **Tổng**: ~150-300ms
- **Ưu điểm**: Filter được toàn bộ dataset, kết quả chính xác

**Kết luận**: Backend filtering chậm hơn một chút nhưng **đúng** và **scalable**

---

## ✅ Checklist Triển Khai

### **Phase 1: Patients.jsx** (Ưu tiên cao)
- [ ] Implement priority sorting ở backend (hoặc dùng field mới)
- [ ] Map frontend sort → backend SortBy/SortDirection
- [ ] Remove frontend sorting logic
- [ ] Test với pagination

### **Phase 2: History.jsx** (Ưu tiên cao)
- [ ] Kiểm tra backend đã hỗ trợ filtering chưa
- [ ] Implement filtering nếu chưa có (date range, keyword, scope, type)
- [ ] Implement sorting (date desc)
- [ ] Remove frontend filtering/sorting logic
- [ ] Test với pagination

### **Phase 3: Prescriptions - Orders Tab** (Ưu tiên trung bình)
- [ ] Implement filtering ở backend (keyword, status, date range)
- [ ] Implement sorting nếu cần
- [ ] Remove frontend filtering logic
- [ ] Test với pagination

### **Phase 4: Prescriptions - Stock Tab** (Ưu tiên trung bình)
- [ ] Thêm filter `DonViTinh` vào backend
- [ ] Remove frontend unit filtering
- [ ] Test với pagination

### **Phase 5: Examination - Queue** (Ưu tiên thấp)
- [ ] Implement filtering ở backend (source, status, kind, keyword)
- [ ] Giữ nguyên logic sorting ở backend
- [ ] Remove frontend filtering logic
- [ ] Test với pagination

---

## 🎯 Kết Luận

**Giải pháp tốt nhất: Backend Filtering & Sorting**

- ✅ Đúng với pagination
- ✅ Scalable
- ✅ Best practice
- ✅ Backend đã hỗ trợ một phần

**Không nên giữ frontend filtering/sorting** vì:
- ❌ Không tương thích với pagination
- ❌ Kết quả không chính xác (chỉ filter/sort 1 page)
- ❌ Không scalable

