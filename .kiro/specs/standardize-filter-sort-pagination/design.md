# Design Document

## Overview

Thiết kế này mô tả cách chuẩn hóa các thao tác filter, sort, search và pagination trong toàn bộ hệ thống HIS. Mục tiêu là chuyển tất cả logic xử lý từ Frontend (client-side) sang Backend (server-side) để đảm bảo:

1. **Tính chính xác**: Filter/sort áp dụng cho toàn bộ dataset, không chỉ 1 page
2. **Hiệu suất**: Database engine tối ưu hóa queries tốt hơn JavaScript
3. **Khả năng mở rộng**: Hệ thống hoạt động tốt khi data tăng lên
4. **Nhất quán**: Tất cả các trang đều dùng cùng pattern

### Phạm Vi

Dựa trên phân tích, có **2 trang chính** cần chuẩn hóa:

1. **Notifications.jsx** - Cần chuyển từ NotificationFilterRequest sang NotificationSearchFilter và xóa client-side filtering
2. **Departments.jsx** - Cần sử dụng RoomSearchFilter đã có sẵn và thêm pagination

Các trang khác đã được chuẩn hóa hoặc đang trong quá trình chuẩn hóa khác.

## Architecture

### Kiến Trúc Tổng Quan

```
┌─────────────────────────────────────────────────────────────┐
│                         Frontend (React)                     │
├─────────────────────────────────────────────────────────────┤
│  1. User Input (keyword, filters, sort, page)               │
│  2. React Query Hook (useNotificationSearch, useRoomSearch)  │
│  3. API Call với SearchFilter params                         │
│  4. Nhận PagedResult từ BE                                   │
│  5. Hiển thị data + pagination controls                      │
└─────────────────────────────────────────────────────────────┘
                              ↓ HTTP POST
┌─────────────────────────────────────────────────────────────┐
│                      Backend (ASP.NET Core)                  │
├─────────────────────────────────────────────────────────────┤
│  1. Controller nhận SearchFilter DTO                         │
│  2. Service xử lý:                                           │
│     - Keyword search (LIKE query)                            │
│     - Specific filters (WHERE clauses)                       │
│     - Sort (ORDER BY)                                        │
│     - Pagination (SKIP/TAKE)                                 │
│  3. Trả về PagedResult<T>                                    │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                      Database (SQL Server)                   │
├─────────────────────────────────────────────────────────────┤
│  - Thực thi query với WHERE, ORDER BY, OFFSET/FETCH         │
│  - Trả về kết quả đã filter/sort/paginate                    │
└─────────────────────────────────────────────────────────────┘
```

### Pattern Chuẩn

Hệ thống sử dụng **Server-Side Filtering/Sorting/Pagination Pattern**:

1. **Frontend**: Chỉ quản lý UI state và gọi API
2. **Backend**: Xử lý toàn bộ logic filter/sort/pagination
3. **Database**: Thực thi query tối ưu

## Components and Interfaces

### 1. Backend Components

#### 1.1. DTO Filter Chuẩn

Tất cả SearchFilter DTO phải tuân theo cấu trúc chuẩn:

```csharp
public record class [Entity]SearchFilter
{
    // Tìm kiếm chung
    public string? Keyword { get; set; }
    
    // Các filter cụ thể (tùy entity)
    public string? [SpecificField1] { get; set; }
    public string? [SpecificField2] { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    // Sort
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
    
    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
```

#### 1.2. Controller Pattern

```csharp
[HttpPost("search")]
public async Task<ActionResult<PagedResult<[Entity]Dto>>> Search(
    [FromBody] [Entity]SearchFilter filter)
{
    var result = await _service.Search[Entity]Async(filter);
    return Ok(result);
}
```

#### 1.3. Service Pattern

Service method phải xử lý theo thứ tự:
1. **Keyword search** - Tìm kiếm trong các field text quan trọng
2. **Specific filters** - Áp dụng các filter cụ thể
3. **Sort** - Sắp xếp theo field và direction
4. **Pagination** - Lấy data theo page và pageSize

```csharp
public async Task<PagedResult<[Entity]Dto>> Search[Entity]Async(
    [Entity]SearchFilter filter)
{
    var query = _context.[Entity].AsQueryable();
    
    // 1. Keyword search
    if (!string.IsNullOrWhiteSpace(filter.Keyword))
    {
        var kw = filter.Keyword.ToLower();
        query = query.Where(x => 
            x.Field1.ToLower().Contains(kw) ||
            x.Field2.ToLower().Contains(kw)
        );
    }
    
    // 2. Specific filters
    if (!string.IsNullOrWhiteSpace(filter.SpecificField))
        query = query.Where(x => x.SpecificField == filter.SpecificField);
    
    // 3. Sort
    query = ApplySort(query, filter.SortBy, filter.SortDirection);
    
    // 4. Pagination
    var total = await query.CountAsync();
    var items = await query
        .Skip((filter.Page - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ToListAsync();
    
    return new PagedResult<[Entity]Dto>
    {
        Items = items.Select(MapToDto).ToList(),
        TotalItems = total,
        Page = filter.Page,
        PageSize = filter.PageSize
    };
}
```

### 2. Frontend Components

#### 2.1. API Function Pattern

```javascript
export async function search[Entity](filter = {}) {
  const body = {
    Keyword: filter.keyword || null,
    [SpecificField]: filter.specificField || null,
    SortBy: filter.sortBy || null,
    SortDirection: filter.sortDirection || "asc",
    Page: filter.page || 1,
    PageSize: filter.pageSize || 50,
  };
  
  const res = await http.post("/api/[entity]/search", body);
  return {
    Items: res.data?.Items || [],
    TotalItems: res.data?.TotalItems || 0,
    Page: res.data?.Page || 1,
    PageSize: res.data?.PageSize || 50,
  };
}
```

#### 2.2. React Query Hook Pattern

```javascript
export function use[Entity]Search(params = {}, options = {}) {
  const normalizedParams = useMemo(() => ({
    keyword: params.keyword || "",
    sortBy: params.sortBy || null,
    sortDirection: params.sortDirection || "asc",
    page: params.page || 1,
    pageSize: params.pageSize || 50,
  }), [
    params.keyword, 
    params.sortBy, 
    params.sortDirection, 
    params.page, 
    params.pageSize
  ]);
  
  return useQuery({
    queryKey: ["[entity]", "search", normalizedParams],
    queryFn: () => search[Entity](normalizedParams),
    keepPreviousData: true,
    staleTime: 60000,
    ...options
  });
}
```

#### 2.3. Component Pattern

```javascript
function [Entity]Page() {
  const [filters, setFilters] = useState({
    keyword: "",
    specificField: null,
    sortBy: null,
    sortDirection: "asc",
    page: 1,
    pageSize: 50,
  });
  
  const { data, isLoading } = use[Entity]Search(filters);
  
  // Reset page khi filter thay đổi
  useEffect(() => {
    setFilters(prev => ({ ...prev, page: 1 }));
  }, [filters.keyword, filters.specificField]);
  
  return (
    <div>
      {/* Filter UI */}
      <FilterBar 
        onFilterChange={(newFilters) => 
          setFilters(prev => ({ ...prev, ...newFilters, page: 1 }))
        }
      />
      
      {/* Data display */}
      <DataTable data={data?.Items || []} />
      
      {/* Pagination */}
      <Pagination
        currentPage={data?.Page || 1}
        totalItems={data?.TotalItems || 0}
        pageSize={data?.PageSize || 50}
        onPageChange={(page) => 
          setFilters(prev => ({ ...prev, page }))
        }
      />
    </div>
  );
}
```

## Data Models

### PagedResult<T>

Cấu trúc chuẩn cho kết quả phân trang:

```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
```

### NotificationSearchFilter

DTO cho trang Notifications (đã có sẵn, cần sử dụng):

```csharp
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

### RoomSearchFilter

DTO cho trang Departments (đã có sẵn, cần sử dụng):

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

