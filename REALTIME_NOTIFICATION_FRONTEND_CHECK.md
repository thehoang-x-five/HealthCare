# REALTIME & NOTIFICATION FRONTEND CHECK - 2025-01-03

## 🎯 MỤC TIÊU
Kiểm tra xem Frontend đã xử lý đúng realtime và notification theo cấu trúc 3 loại y tá chưa.

---

## ✅ BACKEND ĐÃ ĐÚNG

### 1. RealtimeService.cs
**Filtering strategy:**
- ✅ Clinical Exams → Bác sĩ + Y tá trong phòng (room group)
- ✅ CLS Orders → Bác sĩ + Y tá trong phòng CLS (room group)
- ✅ Invoices → TẤT CẢ y tá (y tá hành chính xử lý)
- ✅ Prescriptions → Bác sĩ kê đơn + TẤT CẢ y tá (y tá hành chính phát thuốc)
- ✅ Appointments → Bác sĩ + TẤT CẢ y tá (y tá hành chính quản lý)

**Group structure:**
```csharp
// Tất cả y tá join vào cùng một group
private static readonly string NurseRoleGroupName = "role:y_ta";

// Filtering chi tiết hơn dựa trên room
var roomGroup = RealtimeHub.GetRoomGroupName(maPhong);
```

### 2. NotificationService.cs
**Filtering:**
- ✅ Backend đã filter đúng theo `LoaiNguoiNhan` và `MaNguoiNhan`
- ✅ API `/notification/inbox` trả về chỉ thông báo của user hiện tại
- ✅ Tương thích ngược với "thu_ngan", "phat_thuoc" (map về y_ta)

---

## ✅ FRONTEND ĐÃ ĐÚNG

### 1. realtime.js - SignalR Connection

**Join groups:**
```javascript
export async function initStaffRealtime({
    staffId,
    rooms = [],
    staffRole, // "bac_si" | "y_ta"
  } = {}) {
  // Join role group
  if (staffRole === "bac_si") {
    await conn.invoke("JoinRoleAsync", "bac_si");
  } else if (staffRole === "y_ta") {
    await conn.invoke("JoinRoleAsync", "y_ta");
  }
  
  // Join user group
  if (staffId) {
    await conn.invoke("JoinUserAsync", "nhan_vien_y_te", staffId);
    await conn.invoke("JoinUserAsync", "bac_si", staffId);
  }
  
  // Join room groups
  for (const maPhong of rooms) {
    await conn.invoke("JoinRoomAsync", maPhong);
  }
}
```

**✅ ĐÚNG:**
- Tất cả y tá (hành chính, LS, CLS) đều join vào `role:y_ta`
- Join thêm room groups để nhận realtime theo phòng
- Join user groups để nhận thông báo cá nhân

### 2. notifications.js - API & Subscription

**API call:**
```javascript
export function useNotifications({ params } = {}) {
  return useQuery({
    queryKey: ["notifications", params],
    queryFn: () => listNotifications(params),
    // ...
  });
}
```

**Realtime subscription:**
```javascript
export function subscribeNotifications(queryClient) {
  const handler = (dto) => {
    const mapped = normalizeNotification(dto);
    queryClient.invalidateQueries({ queryKey: ["notifications"] });
    window.dispatchEvent(
      new CustomEvent("app:new-notification", { detail: mapped })
    );
  };

  const offCreated = on("NotificationCreated", handler);
  const offUpdated = on("NotificationUpdated", handler);
  
  return () => {
    offCreated?.();
    offUpdated?.();
  };
}
```

**✅ ĐÚNG:**
- API đã được filter ở backend (chỉ trả về thông báo của user)
- Realtime subscription đúng event names
- Invalidate queries để refresh danh sách

### 3. NotifBell.jsx - Notification Bell

**Data fetching:**
```javascript
const { data } = useNotifications({ params: { take: 5 } });
```

**Realtime handling:**
```javascript
useEffect(() => {
  const off = subscribeNotifications(qc);
  return () => {
    if (typeof off === "function") off();
  };
}, [qc]);
```

**✅ ĐÚNG:**
- Lấy data từ API (đã được filter ở backend)
- Subscribe realtime để nhận thông báo mới
- Hiển thị toast khi có thông báo mới

---

## ⚠️ CẦN LƯU Ý

### 1. Room Joining Strategy

**Hiện tại:**
```javascript
// FE truyền rooms khi init
await initStaffRealtime({
  staffId: "YT001",
  staffRole: "y_ta",
  rooms: ["PK01", "PK02"]  // ⚠️ Cần truyền đúng phòng
});
```

**Vấn đề:**
- Y tá hành chính có thể làm việc ở nhiều phòng (không cố định)
- Y tá LS/CLS thường cố định ở một phòng

**Giải pháp hiện tại:**
- Backend broadcast rộng cho TẤT CẢ y tá (Invoice, Prescription, Appointment)
- Y tá LS/CLS join room cụ thể để nhận Clinical/CLS realtime
- Frontend không cần filter thêm (backend đã filter đúng)

### 2. Notification Filtering

**Backend đã filter:**
```csharp
// NotificationService.cs
query = query.Where(x => x.tn.MaNhanSu == filter.MaNguoiNhan);
```

**Frontend chỉ cần hiển thị:**
```javascript
// NotifBell.jsx - không cần filter thêm
const items = data.items; // Đã được filter ở backend
```

**✅ ĐÚNG:** Frontend không cần filter thêm vì backend đã filter chính xác.

---

## 📊 FLOW HOÀN CHỈNH

### Clinical Exam Created (Phiếu khám LS)

**Backend:**
```csharp
// RealtimeService.cs
public Task BroadcastClinicalExamCreatedAsync(ClinicalExamDto phieuKham)
{
    // Gửi cho bác sĩ
    if (!string.IsNullOrWhiteSpace(phieuKham.MaBacSiKham))
    {
        var doctorGroup = GetUserGroupName("bac_si", phieuKham.MaBacSiKham);
        _hub.Clients.Group(doctorGroup).ClinicalExamCreated(phieuKham);
    }
    
    // Gửi cho y tá trong phòng
    if (!string.IsNullOrWhiteSpace(phieuKham.MaPhong))
    {
        var roomGroup = GetRoomGroupName(phieuKham.MaPhong);
        _hub.Clients.Group(roomGroup).ClinicalExamCreated(phieuKham);
    }
}
```

**Frontend:**
```javascript
// Y tá LS trong phòng PK01
await initStaffRealtime({
  staffId: "YT001",
  staffRole: "y_ta",
  rooms: ["PK01"]  // ✅ Join phòng PK01
});

// Subscribe event
on("ClinicalExamCreated", (phieuKham) => {
  // ✅ Chỉ nhận phiếu khám của phòng PK01
  console.log("Phiếu khám mới:", phieuKham);
});
```

**✅ KẾT QUẢ:**
- Bác sĩ BS001 nhận phiếu khám của mình
- Y tá LS trong phòng PK01 nhận phiếu khám của phòng mình
- Y tá hành chính KHÔNG nhận (không join room PK01)
- Y tá CLS KHÔNG nhận (không join room PK01)

### Prescription Created (Đơn thuốc)

**Backend:**
```csharp
// RealtimeService.cs
public Task BroadcastPrescriptionCreatedAsync(PrescriptionDto donThuoc)
{
    // Gửi cho bác sĩ kê đơn
    if (!string.IsNullOrWhiteSpace(donThuoc.MaBacSiKeDon))
    {
        var doctorGroup = GetUserGroupName("bac_si", donThuoc.MaBacSiKeDon);
        _hub.Clients.Group(doctorGroup).PrescriptionCreated(donThuoc);
    }
    
    // Gửi cho TẤT CẢ y tá
    _hub.Clients.Group(NurseRoleGroupName).PrescriptionCreated(donThuoc);
}
```

**Frontend:**
```javascript
// Y tá hành chính
await initStaffRealtime({
  staffId: "YT002",
  staffRole: "y_ta",
  rooms: []  // ✅ Không cần join room cụ thể
});

// Subscribe event
on("PrescriptionCreated", (donThuoc) => {
  // ✅ Nhận tất cả đơn thuốc (để phát thuốc)
  console.log("Đơn thuốc mới:", donThuoc);
});
```

**✅ KẾT QUẢ:**
- Bác sĩ BS001 nhận đơn thuốc của mình
- TẤT CẢ y tá nhận đơn thuốc (y tá hành chính sẽ phát thuốc)
- Frontend có thể filter hiển thị dựa trên `LoaiYTa` nếu cần

---

## 🎯 KẾT LUẬN

### ✅ FRONTEND ĐÃ CHUẨN:

1. **SignalR Connection:**
   - ✅ Join đúng role groups (`bac_si`, `y_ta`)
   - ✅ Join đúng user groups (nhận thông báo cá nhân)
   - ✅ Join đúng room groups (nhận realtime theo phòng)

2. **Notification API:**
   - ✅ Backend đã filter đúng theo user
   - ✅ Frontend chỉ cần hiển thị (không cần filter thêm)

3. **Notification Bell:**
   - ✅ Subscribe realtime đúng events
   - ✅ Hiển thị toast khi có thông báo mới
   - ✅ Invalidate queries để refresh danh sách

### ❌ KHÔNG CẦN SỬA GÌ Ở FRONTEND

Frontend đã xử lý đúng và không cần thay đổi vì:
- Backend đã filter chính xác
- Frontend chỉ cần join đúng groups
- Notification bell chỉ hiển thị data từ API (đã được filter)

### 📝 KHUYẾN NGHỊ

**Nếu muốn tối ưu hơn trong tương lai:**

1. **Dynamic Room Joining:**
   ```javascript
   // Khi y tá chuyển phòng, join/leave room động
   await leaveRoom("PK01");
   await joinRoom("PK02");
   ```

2. **Frontend Filtering (Optional):**
   ```javascript
   // Nếu muốn filter hiển thị dựa trên LoaiYTa
   const filteredNotifications = notifications.filter(n => {
     if (user.loaiYTa === "hanhchinh") {
       return n.type === "invoice" || n.type === "prescription";
     }
     return true;
   });
   ```

3. **Notification Preferences:**
   ```javascript
   // Cho phép user tắt/bật loại thông báo
   const preferences = {
     invoice: true,
     prescription: true,
     appointment: false
   };
   ```

---

**Ngày kiểm tra:** 2025-01-03  
**Kết luận:** ✅ Frontend đã chuẩn, không cần sửa  
**Người thực hiện:** Kiro AI Assistant
