# Báo Cáo Phân Tích Filter, Sort, Search và Pagination

## Tổng Quan

Báo cáo này phân tích toàn bộ hệ thống HIS để xác định trạng thái hiện tại của các thao tác filter, sort, search và pagination trên tất cả các trang Frontend (FE) và Backend (BE).

**Ngày phân tích:** 30/12/2024

---

## 1. Phân Tích Các Trang Frontend

### 1.1. Trang Lịch Hẹn (Appointments)

**File:** `my-patients/src/routes/Appointments.jsx`

**Trạng thái hiện tại:**
- ❌ **KHÔNG có filter/sort/search UI**
- ✅ Chỉ hiển thị danh sách lịch hẹn theo ngày
- ✅ BE đã hỗ trợ search API với filter đầy đủ

**API được sử dụng:**
- `POST /api/appointments/search` với `AppointmentFilterRequest`

**DTO Filter (BE):**
```csharp
public record class AppointmentFilterRequest
{
    public DateTime? FromDate { get; set;}
    public DateTime? ToDate { get; set;}
    public string? MaBenhNhan { get; set;}
    public string? LoaiHen { get; set;}
    public string? TrangThai { get; set;}
    public int Page { get; set;} = 1;
    public int PageSize { get; set;} = 50;
}
```

**Đánh giá:**
- ⚠️ **DTO thiếu:** `Keyword`, `SortBy`, `SortDirection`
- ✅ **Có:** `Page`, `PageSize` (mặc định 50)
- ⚠️ **FE không sử dụng:** Filter UI, chỉ lọc theo ngày

**Khuyến nghị:**
1. Thêm `Keyword`, `SortBy`, `SortDirection` vào `AppointmentFilterRequest`
2. Thêm UI filter/search vào trang Appointments nếu cần
3. Cập nhật Service để xử lý Keyword (tìm trong TenBenhNhan, SoDienThoai, TenBacSiKham)

---

### 1.2. Trang Khám Bệnh (Examination)

**File:** `my-patients/src/routes/Examination.jsx`

**Trạng thái hiện tại:**
- ✅ **CÓ filter UI:** `QueueFilterPopover` với source, kind, status, search
- ✅ **CÓ pagination:** Page, PageSize = 50
- ✅ **BE đã hỗ trợ đầy đủ**

**API được sử dụng:**
- `POST /api/queue/search` với `QueueSearchFilter`

**DTO Filter (BE):**
```csharp
public record class QueueSearchFilter
{
    public string? MaPhong { get; set; }
    public string? Vaitro { get; set; }
    public string? MaNhanSu { get; set; }
    public string? LoaiHangDoi { get; set; }
    public string? TrangThai { get; set; }
    public string? Nguon { get; set; }
    public string? Keyword { get; set; }
    public DateTime? FromTime { get; set; }
    public DateTime? ToTime { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
```

**Đánh giá:**
- ✅ **DTO đầy đủ:** Có tất cả các field cần thiết
- ✅ **FE sử dụng đúng:** Map filter params sang BE
- ✅ **Pagination:** Đã implement đầy đủ
- ✅ **Reset page khi filter thay đổi**

**Khuyến nghị:**
- ✅ **KHÔNG CẦN CẢI THIỆN** - Đây là mẫu chuẩn để áp dụng cho các trang khác

---

### 1.3. Tab Kê Thuốc (RxPickerModal trong ExamDetail)

**File:** `my-patients/src/components/exam/RxPickerModal.jsx`

**Trạng thái hiện tại:**
- ✅ **CÓ search UI:** Input tìm kiếm thuốc
- ✅ **CÓ pagination:** Page, PageSize = 20 (nhỏ hơn vì là modal)
- ✅ **BE đã hỗ trợ đầy đủ**

**API được sử dụng:**
- `POST /api/pharmacy/stock/search` với `DrugSearchFilter`

**DTO Filter (BE):**
```csharp
public record class DrugSearchFilter
{
    public string? Keyword { get; set; }
    public string? TrangThai { get; set; }
    public string? DonViTinh { get; set; }
    public DateTime? HanSuDungFrom { get; set; }
    public DateTime? HanSuDungTo { get; set; }
    public int? TonToiThieu { get; set; }
    public int? TonToiDa { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
```

**Đánh giá:**
- ✅ **DTO đầy đủ:** Có tất cả các field cần thiết
- ✅ **FE sử dụng đúng:** Truyền keyword, page, pageSize
- ✅ **Pagination:** Hiển thị pagination controls
- ✅ **Reset page khi search thay đổi**

**Khuyến nghị:**
- ✅ **KHÔNG CẦN CẢI THIỆN** - Đã implement đúng chuẩn

---

### 1.4. Trang Thông Báo (Notifications)

**File:** `my-patients/src/routes/Notifications.jsx`

**Trạng thái hiện tại:**
- ❌ **API đang dùng sai DTO:** `NotificationFilterRequest` thay vì `NotificationSearchFilter`
- ❌ **FE đang filter client-side:** keyword, type, priority
- ❌ **Không có pagination**
- ✅ **BE đã có NotificationSearchFilter đầy đủ** nhưng không được sử dụng

**API được sử dụng:**
- `GET /api/notifications` với `NotificationFilterRequest` (sai)

**DTO Filter (BE) - Đang dùng:**
```csharp
// NotificationFilterRequest - DTO hiện tại (thiếu nhiều field)
public record class NotificationFilterRequest
{
    public string? Tab { get; set; } // all | unread | today
}
```

**DTO Filter (BE) - Nên dùng:**
```csharp
// NotificationSearchFilter - DTO đầy đủ (đã có sẵn nhưng không dùng)
public record class NotificationSearchFilter
{
    public string? Keyword { get; set; }
    public string? LoaiThongBao { get; set; } // system | appointment | patient | pharmacy | billing
    public string? MucDoUuTien { get; set; } // high | normal
    public bool? DaDoc { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
```

**Đánh giá:**
- ❌ **API dùng sai DTO:** Cần chuyển từ NotificationFilterRequest sang NotificationSearchFilter
- ❌ **FE filter client-side:** Dòng 136-195 filter keyword, type, priority ở FE
- ❌ **Không có pagination:** Tải toàn bộ dữ liệu
- ⚠️ **Service cần cập nhật:** Xử lý Keyword (tìm trong TieuDe, NoiDung)

**Khuyến nghị:**
1. Cập nhật NotificationController để dùng NotificationSearchFilter
2. Cập nhật NotificationService xử lý Keyword, LoaiThongBao, MucDoUuTien, Sort
3. Xóa logic filter client-side ở FE (dòng 136-195)
4. Thêm pagination UI

---

### 1.5. Trang Phòng Khoa (Departments)

**File:** `my-patients/src/routes/Departments.jsx`

**Trạng thái hiện tại:**
- ❌ **FE đang filter client-side:** keyword, status, roomType, sort
- ❌ **Không có pagination**
- ✅ **BE đã có RoomSearchFilter đầy đủ** nhưng FE không dùng

**API được sử dụng:**
- `GET /api/rooms/cards/search` - Trả về toàn bộ dữ liệu, không có filter params

**DTO Filter (BE) - Đã có sẵn:**
```csharp
public record class RoomSearchFilter
{
    public string? Keyword { get; set; }
    public string? LoaiPhong { get; set; } // kham_lam_sang | can_lam_sang
    public string? TrangThai { get; set; } // active | inactive
    public string? MaKhoa { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
```

**Đánh giá:**
- ✅ **DTO đầy đủ:** BE đã có RoomSearchFilter hoàn chỉnh
- ❌ **FE không dùng:** Dòng 186-227 filter client-side
- ❌ **Không có pagination:** Tải toàn bộ dữ liệu
- ⚠️ **Service cần kiểm tra:** Xem đã xử lý filter/sort chưa

**Khuyến nghị:**
1. Cập nhật FE gọi API với RoomSearchFilter params
2. Xóa logic filter client-side (dòng 186-227)
3. Thêm pagination UI
4. Kiểm tra Service đã xử lý Keyword (tìm trong TenPhong, TenKhoa, TenBacSi, TenDieuDuong)

---

### 1.6. Tab Kho Thuốc trong Prescriptions

**File:** `my-patients/src/routes/Prescriptions.jsx`

**Trạng thái hiện tại:**
- ⚠️ **FE vẫn filter unit ở client-side** (dòng 236-256)
- ✅ **BE đã hỗ trợ DonViTinh** trong DrugSearchFilter
- ✅ **Đã có pagination**

**API được sử dụng:**
- `POST /api/pharmacy/stock/search` với `DrugSearchFilter`

**DTO Filter (BE):**
```csharp
public record class DrugSearchFilter
{
    public string? Keyword { get; set; }
    public string? TrangThai { get; set; }
    public string? DonViTinh { get; set; } // ✅ Đã có
    public DateTime? HanSuDungFrom { get; set; }
    public DateTime? HanSuDungTo { get; set; }
    public int? TonToiThieu { get; set; }
    public int? TonToiDa { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
```

**Code FE cần xóa (dòng 236-256):**
```javascript
const filteredStock = useMemo(() => {
  if (!stock || !stock.length) return [];

  return stock.filter((r) => {
    const unitOk = unit
      ? (r.unit || r.donViTinh || "")
          .toLowerCase()
          .includes(unit.toLowerCase())
      : true;
    
    // ... logic khác
    return unitOk;
  });
}, [stock, unit]);
```

**Đánh giá:**
- ✅ **BE đã hỗ trợ:** DonViTinh trong DrugSearchFilter
- ⚠️ **FE filter thừa:** Dòng 236-256 filter unit ở client-side
- ✅ **Pagination:** Đã có

**Khuyến nghị:**
- ✅ **KHÔNG CẦN CẢI THIỆN** - Đã implement đúng chuẩn

---

### 1.4. Trang Thông Báo (Notifications)

**File:** `my-patients/src/routes/Notifications.jsx`

**Trạng thái hiện tại:**
- ❌ **API đang dùng sai DTO:** `NotificationFilterRequest` thay vì `NotificationSearchFilter`
- ❌ **FE đang filter client-side:** keyword, type, priority (dòng 136-195)
- ❌ **Không có pagination**
- ✅ **BE đã có NotificationSearchFilter đầy đủ** nhưng không được sử dụng

**API được sử dụng:**
- `GET /api/notifications` với `NotificationFilterRequest` (sai)

**DTO Filter (BE) - Đang dùng:**
```csharp
// NotificationFilterRequest - DTO hiện tại (thiếu nhiều field)
public record class NotificationFilterRequest
{
    public string? Tab { get; set; } // all | unread | today
}
```

**DTO Filter (BE) - Nên dùng:**
```csharp
// NotificationSearchFilter - DTO đầy đủ (đã có sẵn nhưng không dùng)
public record class NotificationSearchFilter
{
    public string? Keyword { get; set; }
    public string? LoaiThongBao { get; set; } // system | appointment | patient | pharmacy | billing
    public string? MucDoUuTien { get; set; } // high | normal
    public bool? DaDoc { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
```

**Code FE cần xóa (dòng 136-195):**
```javascript
const filtered = useMemo(() => {
  let list = Array.isArray(items) ? [...items] : [];
  
  // Tab: unread / today
  if (tab === "unread") {
    list = list.filter((n) => !n.read);
  } else if (tab === "today") {
    list = list.filter((n) => {
      // ... filter logic
    });
  }
  
  // Keyword
  const kw = (deferredKeyword || "").trim().toLowerCase();
  if (kw) {
    list = list.filter((n) => {
      // ... search logic
    });
  }
  
  // Filter type
  if (filters.type && filters.type !== "all") {
    // ... filter logic
  }
  
  // Filter priority
  if (filters.priority && filters.priority !== "all") {
    // ... filter logic
  }
  
  // Sort
  list.sort((a, b) => {
    // ... sort logic
  });
  
  return list;
}, [items, tab, today, deferredKeyword, filters.type, filters.priority]);
```

**Đánh giá:**
- ❌ **API dùng sai DTO:** Cần chuyển từ NotificationFilterRequest sang NotificationSearchFilter
- ❌ **FE filter client-side:** Tất cả logic filter/sort đang ở FE
- ❌ **Không có pagination:** Tải toàn bộ dữ liệu
- ⚠️ **Service cần cập nhật:** Xử lý Keyword (tìm trong TieuDe, NoiDung)

**Khuyến nghị:**
1. Cập nhật NotificationController để dùng NotificationSearchFilter
2. Cập nhật NotificationService xử lý Keyword, LoaiThongBao, MucDoUuTien, Sort
3. Xóa logic filter client-side ở FE (dòng 136-195)
4. Thêm pagination UI
5. Cập nhật FE gọi API với params: keyword, type, priority, page, pageSize

---

### 1.5. Trang Phòng Khoa (Departments)

**File:** `my-patients/src/routes/Departments.jsx`

**Trạng thái hiện tại:**
- ❌ **FE đang filter client-side:** keyword, status, roomType, sort (dòng 186-227)
- ❌ **Không có pagination**
- ✅ **BE đã có RoomSearchFilter đầy đủ** nhưng FE không dùng

**API được sử dụng:**
- `GET /api/rooms/cards/search` - Trả về toàn bộ dữ liệu, không có filter params

**DTO Filter (BE) - Đã có sẵn:**
```csharp
public record class RoomSearchFilter
{
    public string? Keyword { get; set; }
    public string? LoaiPhong { get; set; } // kham_lam_sang | can_lam_sang
    public string? TrangThai { get; set; } // active | inactive
    public string? MaKhoa { get; set; }
    public string? SortBy { get; set; } // TenPhong | SucChua | TenKhoa
    public string? SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
```

**Code FE cần xóa (dòng 186-227):**
```javascript
const filtered = useMemo(() => {
  let arr = [...all];

  // trạng thái
  if (filters.status === "online") {
    arr = arr.filter((d) => d.status === "active");
  } else if (filters.status === "offline") {
    arr = arr.filter((d) => d.status === "inactive");
  }

  // loại phòng
  if (filters.roomType === "cls") {
    arr = arr.filter((d) => isClsRoom(d));
  } else if (filters.roomType === "ls") {
    arr = arr.filter((d) => !isClsRoom(d));
  }

  // keyword
  const kw = filters.keyword.trim().toLowerCase();
  if (kw) {
    arr = arr.filter((d) => searchMatch(d, kw));
  }

  // sort
  if (filters.sort === "capacity_asc" || filters.sort === "capacity_desc") {
    // ... sort logic
  }

  return arr;
}, [all, filters]);
```

**Đánh giá:**
- ✅ **DTO đầy đủ:** BE đã có RoomSearchFilter hoàn chỉnh
- ❌ **FE không dùng:** Tất cả logic filter/sort đang ở FE
- ❌ **Không có pagination:** Tải toàn bộ dữ liệu
- ⚠️ **Service cần kiểm tra:** Xem đã xử lý filter/sort chưa

**Khuyến nghị:**
1. Cập nhật FE gọi API với RoomSearchFilter params
2. Xóa logic filter client-side (dòng 186-227)
3. Thêm pagination UI
4. Kiểm tra Service đã xử lý Keyword (tìm trong TenPhong, TenKhoa, TenBacSi, TenDieuDuong)
5. Kiểm tra Service đã xử lý Sort theo SucChua (capacity)

---

### 1.6. Tab Kho Thuốc trong Prescriptions

**File:** `my-patients/src/routes/Prescriptions.jsx`

**Trạng thái hiện tại:**
- ⚠️ **FE vẫn filter unit ở client-side** (dòng 236-256)
- ✅ **BE đã hỗ trợ DonViTinh** trong DrugSearchFilter
- ✅ **Đã có pagination**
- ✅ **FE đã truyền unit vào API** (dòng 177)

**API được sử dụng:**
- `POST /api/pharmacy/stock/search` với `DrugSearchFilter`

**DTO Filter (BE):**
```csharp
public record class DrugSearchFilter
{
    public string? Keyword { get; set; }
    public string? TrangThai { get; set; }
    public string? DonViTinh { get; set; } // ✅ Đã có
    public DateTime? HanSuDungFrom { get; set; }
    public DateTime? HanSuDungTo { get; set; }
    public int? TonToiThieu { get; set; }
    public int? TonToiDa { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
```

**Code FE cần xóa (dòng 236-256):**
```javascript
const filteredStock = useMemo(() => {
  if (!stock || !stock.length) return [];

  return stock.filter((r) => {
    const unitOk = unit
      ? (r.unit || r.donViTinh || "")
          .toLowerCase()
          .includes(unit.toLowerCase())
      : true;
    
    const statusCode = getDrugStatusCode(r);
    
    // 🔒 Không hiển thị thuốc tạm dừng
    if (statusCode === "tam_dung") return false;
    
    return unitOk;
  });
}, [stock, unit]);
```

**Đánh giá:**
- ✅ **BE đã hỗ trợ:** DonViTinh trong DrugSearchFilter
- ✅ **FE đã truyền unit:** Dòng 177 truyền `unit` vào API
- ⚠️ **FE filter thừa:** Dòng 236-256 vẫn filter unit lại ở client-side
- ✅ **Pagination:** Đã có

**Khuyến nghị:**
1. Xóa logic filter unit ở FE (dòng 236-256)
2. Sử dụng trực tiếp `stock` từ BE thay vì `filteredStock`
3. Đảm bảo BE đã filter DonViTinh đúng (kiểm tra PharmacyService)
4. Giữ logic filter statusCode "tam_dung" ở FE (vì đây là business logic phức tạp)

---

### 1.7. Trang Danh Sách Bệnh Nhân (Patients)

**File:** `my-patients/src/routes/Patients.jsx` (không có trong context nhưng có API)

**API được sử dụng:**
- `GET /api/patient` với `PatientSearchFilter`

**DTO Filter (BE):**
```csharp
public record class PatientSearchFilter
{
    public string? Keyword { get; set; }
    public bool OnlyToday { get; set; } = false;
    public string? MaBenhNhan { get; set; }
    public string? DienThoai { get; set; }
    public string? GioiTinh { get; set; }
    public string? TrangThaiTaiKhoan { get; set; }
    public string? TrangThaiHomNay { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 500; // ⚠️ Quá lớn!
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
}
```

**Đánh giá:**
- ✅ **DTO đầy đủ:** Có tất cả các field cần thiết
- ⚠️ **PageSize quá lớn:** 500 items - nên giảm xuống 50
- ❓ **FE chưa rõ:** Cần kiểm tra file Patients.jsx để xác nhận

**Khuyến nghị:**
1. Giảm PageSize mặc định từ 500 xuống 50
2. Kiểm tra FE xem có sử dụng pagination không
3. Nếu FE không có pagination, cần thêm UI pagination

---

## 2. Phân Tích Backend Services

### 2.1. QueueService

**Trạng thái:**
- ✅ **Đã implement đầy đủ** filter, sort, search, pagination
- ✅ **Xử lý Keyword:** Tìm trong TenBenhNhan, MaBenhNhan, TenBacSiKham, TenKhoa, SoDienThoai
- ✅ **Xử lý Sort:** Hỗ trợ DoUuTien, ThoiGianCheckin, ThoiGianLichHen
- ✅ **Pagination:** Trả về PagedResult chuẩn

**Mẫu code tham khảo:** `HealthCare/Services/OutpatientCare/QueueService.cs`

---

### 2.2. PharmacyService

**Trạng thái:**
- ✅ **Đã implement đầy đủ** filter, sort, search, pagination
- ✅ **Xử lý Keyword:** Tìm trong TenThuoc, MaThuoc, CongDung
- ✅ **Xử lý Sort:** Hỗ trợ TenThuoc, SoLuongTon, HanSuDung
- ✅ **Pagination:** Trả về PagedResult chuẩn

---

### 2.3. AppointmentService

**Trạng thái:**
- ⚠️ **Chưa đầy đủ:** DTO thiếu Keyword, SortBy, SortDirection
- ❓ **Cần kiểm tra:** Service có xử lý sort không

**Khuyến nghị:**
1. Thêm Keyword, SortBy, SortDirection vào AppointmentFilterRequest
2. Cập nhật Service để xử lý Keyword (tìm trong TenBenhNhan, SoDienThoai, TenBacSiKham)
3. Cập nhật Service để xử lý Sort (theo NgayHen, GioHen, TenBenhNhan)

---

### 2.4. PatientService

**Trạng thái:**
- ✅ **DTO đầy đủ:** Có Keyword, SortBy, SortDirection
- ⚠️ **PageSize quá lớn:** 500 - nên giảm xuống 50
- ❓ **Cần kiểm tra:** Service có xử lý sort đúng không

**Khuyến nghị:**
1. Giảm PageSize mặc định từ 500 xuống 50
2. Kiểm tra Service xử lý Keyword và Sort

---

## 3. Tổng Kết Trạng Thái

### 3.1. Các Trang Đã Chuẩn Hóa ✅

1. **Trang Khám Bệnh (Examination)** - Mẫu chuẩn
   - ✅ Filter UI đầy đủ
   - ✅ Pagination hoàn chỉnh
   - ✅ BE xử lý đúng

2. **Tab Kê Thuốc (RxPickerModal)** - Mẫu chuẩn
   - ✅ Search UI
   - ✅ Pagination
   - ✅ BE xử lý đúng

### 3.2. Các Trang Cần Cải Thiện ⚠️

1. **Trang Lịch Hẹn (Appointments)**
   - ⚠️ DTO thiếu Keyword, SortBy, SortDirection
   - ⚠️ FE không có filter/search UI
   - ⚠️ Service cần cập nhật xử lý Keyword và Sort

2. **Trang Danh Sách Bệnh Nhân (Patients)**
   - ⚠️ PageSize quá lớn (500)
   - ❓ Cần kiểm tra FE có pagination không

3. **Trang Thông Báo (Notifications)**
   - ⚠️ API đang dùng NotificationFilterRequest thay vì NotificationSearchFilter
   - ⚠️ NotificationSearchFilter đã có đầy đủ field nhưng không được sử dụng
   - ⚠️ FE đang filter client-side (keyword, type, priority)
   - ⚠️ Cần chuyển sang gọi API search với params đầy đủ

4. **Trang Phòng Khoa (Departments)**
   - ⚠️ BE đã có RoomSearchFilter đầy đủ nhưng FE không dùng
   - ⚠️ FE đang filter client-side (keyword, status, roomType, sort)
   - ⚠️ Không có pagination
   - ⚠️ Cần chuyển sang gọi API search với pagination

5. **Tab Kho Thuốc trong Prescriptions**
   - ⚠️ BE đã hỗ trợ filter DonViTinh
   - ⚠️ FE vẫn filter unit ở client-side (dòng 236-256)
   - ⚠️ Cần xóa logic filter FE và dùng BE

### 3.3. Các Trang Chưa Kiểm Tra ❓

Các trang khác trong hệ thống cần được kiểm tra:
- Dashboard
- Reports
- Billing
- History
- Notifications
- Master Data

---

## 4. Khuyến Nghị Ưu Tiên

### Ưu Tiên Cao 🔴

1. **Chuẩn hóa Notifications - Chuyển từ NotificationFilterRequest sang NotificationSearchFilter**
   - API đang dùng sai DTO
   - FE đang filter client-side
   - Ước tính: 2-3 giờ

2. **Chuẩn hóa Departments - Chuyển từ filter FE sang BE**
   - BE đã có RoomSearchFilter đầy đủ
   - FE cần chuyển sang gọi API search với pagination
   - Ước tính: 3-4 giờ

3. **Chuẩn hóa Prescriptions Stock Tab - Xóa filter unit ở FE**
   - BE đã hỗ trợ DonViTinh
   - FE cần xóa logic filter client-side (dòng 236-256)
   - Ước tính: 1 giờ

4. **Chuẩn hóa AppointmentFilterRequest**
   - Thêm Keyword, SortBy, SortDirection
   - Cập nhật AppointmentService xử lý Keyword và Sort
   - Ước tính: 2-3 giờ

5. **Giảm PageSize của PatientSearchFilter**
   - Từ 500 xuống 50
   - Kiểm tra FE có pagination không, nếu không thì thêm
   - Ước tính: 1-2 giờ

### Ưu Tiên Trung Bình 🟡

6. **Kiểm tra và chuẩn hóa các trang còn lại**
   - Dashboard, Reports, Billing, etc.
   - Ước tính: 4-6 giờ

7. **Tạo tài liệu hướng dẫn chuẩn hóa**
   - Mô tả cấu trúc DTO filter chuẩn
   - Hướng dẫn xử lý trong Service
   - Hướng dẫn gọi API từ FE
   - Ước tính: 2-3 giờ

### Ưu Tiên Thấp 🟢

8. **Thêm filter/search UI cho Appointments nếu cần**
   - Chỉ làm nếu người dùng yêu cầu
   - Ước tính: 3-4 giờ

---

## 5. Cấu Trúc Chuẩn Đề Xuất

### 5.1. DTO Filter Chuẩn

```csharp
public record class [Entity]SearchFilter
{
    // Tìm kiếm chung
    public string? Keyword { get; set; }
    
    // Các filter cụ thể
    public string? [SpecificField1] { get; set; }
    public string? [SpecificField2] { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    // Sort
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
    
    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50; // ✅ Chuẩn hóa: 50 items
}
```

### 5.2. Service Method Chuẩn

```csharp
public async Task<PagedResult<[Entity]Dto>> Search[Entity]Async([Entity]SearchFilter filter)
{
    var query = _context.[Entity].AsQueryable();
    
    // 1. Xử lý Keyword
    if (!string.IsNullOrWhiteSpace(filter.Keyword))
    {
        var keyword = filter.Keyword.ToLower();
        query = query.Where(x => 
            x.Field1.ToLower().Contains(keyword) ||
            x.Field2.ToLower().Contains(keyword) ||
            x.Field3.ToLower().Contains(keyword)
        );
    }
    
    // 2. Xử lý các filter cụ thể
    if (!string.IsNullOrWhiteSpace(filter.SpecificField1))
        query = query.Where(x => x.SpecificField1 == filter.SpecificField1);
    
    // 3. Xử lý Sort
    query = filter.SortBy?.ToLower() switch
    {
        "field1" => filter.SortDirection == "desc" 
            ? query.OrderByDescending(x => x.Field1)
            : query.OrderBy(x => x.Field1),
        "field2" => filter.SortDirection == "desc"
            ? query.OrderByDescending(x => x.Field2)
            : query.OrderBy(x => x.Field2),
        _ => query.OrderBy(x => x.DefaultSortField)
    };
    
    // 4. Pagination
    var totalItems = await query.CountAsync();
    var items = await query
        .Skip((filter.Page - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ToListAsync();
    
    return new PagedResult<[Entity]Dto>
    {
        Items = items.Select(MapToDto).ToList(),
        TotalItems = totalItems,
        Page = filter.Page,
        PageSize = filter.PageSize
    };
}
```

### 5.3. FE API Call Chuẩn

```javascript
// API function
export async function search[Entity](filter = {}) {
  const body = {
    Keyword: filter.keyword || filter.Keyword || null,
    [SpecificField]: filter.specificField || null,
    SortBy: filter.sortBy || filter.SortBy || null,
    SortDirection: filter.sortDirection || filter.SortDirection || "asc",
    Page: filter.page || filter.Page || 1,
    PageSize: filter.pageSize || filter.PageSize || 50,
  };
  
  const res = await http.post("/api/[entity]/search", body);
  return {
    Items: res.data?.Items || res.data?.items || [],
    TotalItems: res.data?.TotalItems || res.data?.totalItems || 0,
    Page: res.data?.Page || res.data?.page || 1,
    PageSize: res.data?.PageSize || res.data?.pageSize || 50,
  };
}

// React Query hook
export function use[Entity]Search(params = {}, options = {}) {
  const normalizedParams = useMemo(() => ({
    keyword: params.keyword || "",
    sortBy: params.sortBy || null,
    sortDirection: params.sortDirection || "asc",
    page: params.page || 1,
    pageSize: params.pageSize || 50,
  }), [params.keyword, params.sortBy, params.sortDirection, params.page, params.pageSize]);
  
  return useQuery({
    queryKey: ["[entity]", "search", normalizedParams],
    queryFn: () => search[Entity](normalizedParams),
    keepPreviousData: true,
    staleTime: 60000,
    ...options
  });
}
```

---

## 6. Kết Luận

### Trạng Thái Tổng Quan

- ✅ **2/7 trang đã chuẩn hóa hoàn toàn** (Examination, RxPickerModal)
- ⚠️ **5/7 trang cần cải thiện** (Appointments, Patients, Notifications, Departments, Prescriptions Stock Tab)
- ❓ **Nhiều trang chưa kiểm tra** (Dashboard, Reports, Billing, etc.)

### Điểm Mạnh

1. Hệ thống đã có cấu trúc PagedResult chuẩn
2. Một số trang đã implement đúng chuẩn (Examination, RxPickerModal)
3. BE đã có sẵn infrastructure để xử lý filter/sort/search

### Điểm Cần Cải Thiện

1. **Nhiều trang đang filter client-side** (Notifications, Departments, Prescriptions Stock)
2. Một số DTO filter thiếu field Keyword, SortBy, SortDirection (Appointments)
3. **API dùng sai DTO** (Notifications dùng NotificationFilterRequest thay vì NotificationSearchFilter)
4. PageSize không đồng nhất (50 vs 500)
5. Một số Service chưa xử lý Keyword và Sort
6. **Nhiều trang không có pagination** (Notifications, Departments)
7. Thiếu tài liệu hướng dẫn chuẩn hóa

### Lợi Ích Khi Chuẩn Hóa

1. **Hiệu suất:** Giảm tải dữ liệu không cần thiết
2. **Trải nghiệm người dùng:** Tìm kiếm và lọc nhanh hơn
3. **Bảo trì:** Code đồng nhất, dễ maintain
4. **Mở rộng:** Dễ dàng thêm trang mới theo chuẩn

---

**Người phân tích:** Kiro AI Assistant  
**Ngày:** 30/12/2024
