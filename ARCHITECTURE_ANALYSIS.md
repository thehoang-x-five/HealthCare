# PHÂN TÍCH KIẾN TRÚC: TẠO LƯỢT KHÁM & PHIẾU CLS

## 🎯 VẤN ĐỀ 1: Tạo lượt khám - Chỉ truyền MaHangDoi hay cả MaPhieuKham?

### ✅ KẾT LUẬN: **CHỈ CẦN TRUYỀN MaHangDoi**

### Phân tích:

#### 1. Entity Relationship:
```csharp
// HangDoi.cs
public class HangDoi
{
    public string? MaPhieuKham { get; set; }  // ← Foreign key (nullable)
    public PhieuKhamLamSang? PhieuKhamLamSang { get; set; }  // ← Navigation property
    // ...
}
```

#### 2. Backend Logic:
```csharp
// HistoryService.TaoLuotKhamAsync() - dòng 302-309
var hangDoi = await _db.HangDois
    .Include(h => h.PhieuKhamLamSang)  // ← Tự động load phiếu khám
    .FirstOrDefaultAsync(h => h.MaHangDoi == maHangDoiReq);

var phieuLs = hangDoi.PhieuKhamLamSang;  // ← Đã có sẵn, không cần truyền thêm
```

#### 3. Request DTO:
```csharp
// HistoryDtos.cs - HistoryVisitCreateRequest
public class HistoryVisitCreateRequest
{
    public string MaHangDoi { get; set; } = default!;  // ← CHỈ CẦN MaHangDoi
    // KHÔNG có MaPhieuKham
}
```

### ✅ Giải pháp tối ưu:

**CHỈ TRUYỀN `MaHangDoi`** - Đây là cách đúng:

1. ✅ **Giảm payload request** - Chỉ 1 field thay vì 2
2. ✅ **Backend tự load** - Dùng Include() để load PhieuKhamLamSang khi cần
3. ✅ **Đảm bảo data consistency** - MaPhieuKham lấy từ HangDoi, không lo mismatch
4. ✅ **Đơn giản hóa API** - 1 parameter thay vì 2

### ⚠️ Lưu ý về Performance:

**Response hàng chờ có lớn không?**

- **DTO QueueItemDto** chỉ chứa các field cần thiết cho UI
- **Không load full PhiếuKhamLamSang** vào DTO (chỉ load khi cần)
- Khi tạo lượt khám, backend mới Include() PhieuKhamLamSang → chỉ load 1 lần, không ảnh hưởng response queue

**Kết luận:** Response queue không bị lớn vì chỉ chứa DTO, không phải full entity.

---

## 🎯 VẤN ĐỀ 2: Tạo phiếu CLS - Tạo sớm hay cập nhật sau?

### ❓ Câu hỏi:
Khi bác sĩ chỉ định CLS, có nên:
1. **Tạo luôn phiếu CLS** khi chỉ định → Ở lập phiếu CLS thì cập nhật?
2. Hay **chỉ lưu danh sách chỉ định** → Tạo phiếu CLS khi lập phiếu?

### 📊 Phân tích hiện tại:

#### Backend Logic (ClsService.TaoPhieuClsAsync):
```csharp
// Kiểm tra đã có phiếu CLS chưa
var existedCls = await _db.PhieuKhamCanLamSangs
    .FirstOrDefaultAsync(c => c.MaPhieuKhamLs == request.MaPhieuKhamLs);

if (existedCls is not null)
{
    if (!string.Equals(existedCls.TrangThai, "da_hoan_tat", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Phiếu CLS đang chưa hoàn tất, không thể tạo thêm.");
    // ...
}
```

**→ Backend đã hỗ trợ: Nếu đã có phiếu CLS chưa hoàn tất → không cho tạo mới**

### 🔍 Cần kiểm tra: Frontend chỉ định CLS làm gì?

**Cần xem:** Khi bác sĩ chỉ định CLS trong Examination.jsx, có gọi API tạo phiếu CLS ngay không, hay chỉ lưu vào state/localStorage?

---

## 💡 ĐỀ XUẤT GIẢI PHÁP:

### Option A: Tạo phiếu CLS ngay khi chỉ định (RECOMMENDED)

**Flow:**
```
1. Bác sĩ chỉ định CLS trong Examination
   → POST /api/cls (TaoPhieuClsAsync)
   → Tạo phiếu CLS + ChiTietDichVu
   → Trạng thái: "da_lap"
   
2. Lập phiếu CLS (nếu cần chỉnh sửa)
   → GET /api/cls/{maPhieuCls}
   → PUT /api/cls/{maPhieuCls} (nếu có endpoint cập nhật)
   → Cập nhật thông tin + trạng thái
```

**Ưu điểm:**
- ✅ Data được persist ngay → không mất khi refresh
- ✅ Có thể track được lịch sử chỉ định
- ✅ Backend đã hỗ trợ (check duplicate, không cho tạo mới nếu đã có)
- ✅ Có thể tạo hàng đợi CLS ngay sau khi chỉ định

**Nhược điểm:**
- ⚠️ Có thể tạo nhiều request API (1 request cho mỗi chỉ định)

### Option B: Lưu tạm → Tạo khi lập phiếu (KHÔNG KHUYẾN NGHỊ)

**Flow:**
```
1. Bác sĩ chỉ định CLS
   → Chỉ lưu vào state/localStorage
   
2. Lập phiếu CLS
   → Lấy danh sách chỉ định từ state
   → POST /api/cls (TaoPhieuClsAsync)
   → Tạo phiếu CLS + ChiTietDichVu
```

**Nhược điểm:**
- ❌ Data không được persist → mất khi refresh
- ❌ Không có lịch sử chỉ định
- ❌ Phải giữ state phức tạp

---

## ✅ KẾT LUẬN & KHUYẾN NGHỊ:

### 1. Tạo lượt khám:
✅ **CHỈ TRUYỀN `MaHangDoi`** - Đây là cách đúng và tối ưu

### 2. Tạo phiếu CLS:
✅ **TẠO NGAY KHI CHỈ ĐỊNH** (Option A)

**Lý do:**
- Backend đã hỗ trợ check duplicate
- Data được persist ngay
- Có thể tạo hàng đợi CLS ngay
- Nếu cần chỉnh sửa → có thể dùng API cập nhật (hoặc tạo mới nếu chưa có)

**Nếu muốn "cập nhật" thay vì "tạo mới":**
- Backend đã có logic: Nếu đã có phiếu CLS chưa hoàn tất → không cho tạo mới
- Cần thêm endpoint `PUT /api/cls/{maPhieuCls}` để cập nhật thông tin + thêm ChiTietDichVu mới

---

## 🔧 CẦN KIỂM TRA THÊM:

1. ✅ Frontend: Khi chỉ định CLS, có gọi API tạo phiếu CLS ngay không?
2. ⚠️ Backend: Có endpoint cập nhật phiếu CLS không? (Thêm ChiTietDichVu, cập nhật thông tin)
3. ⚠️ Backend: Logic tạo hàng đợi CLS có tự động chạy khi tạo phiếu CLS không?

