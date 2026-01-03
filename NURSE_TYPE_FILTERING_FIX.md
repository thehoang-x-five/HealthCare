# NURSE TYPE FILTERING FIX - 2025-01-03

**Priority:** 🔴 CRITICAL  
**Status:** ✅ COMPLETED

---

## 🎯 PROBLEM

Y tá CLS và Y tá LS đang nhận thông báo về công việc hành chính (invoices, prescriptions, appointments) - **KHÔNG ĐÚNG VAI TRÒ**!

### Vấn đề cũ:
- ❌ Tất cả y tá join vào `role:y_ta` (chung)
- ❌ Backend broadcast cho `role:y_ta` → TẤT CẢ y tá nhận
- ❌ Y tá CLS nhận thông báo về phát thuốc, thu ngân
- ❌ Y tá LS nhận thông báo về phát thuốc, thu ngân
- ❌ Không phân biệt rõ ràng vai trò từng loại y tá

---

## ✅ SOLUTION

### 1. Backend: Tạo groups riêng cho từng loại y tá

**RealtimeHub.cs - Thêm method join nurse type:**

```csharp
/// <summary>
/// Join group theo loại y tá cụ thể (hanhchinh, phong_kham, can_lam_sang)
/// </summary>
public Task JoinNurseTypeAsync(string nurseType)
{
    if (string.IsNullOrWhiteSpace(nurseType))
        throw new ArgumentException("nurseType is required", nameof(nurseType));

    var groupName = GetNurseTypeGroupName(nurseType);
    return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
}

public static string GetNurseTypeGroupName(string nurseType)
    => $"nurse_type:{nurseType}";
```

**Groups mới:**
- `nurse_type:hanhchinh` - Y tá hành chính
- `nurse_type:phong_kham` - Y tá lâm sàng
- `nurse_type:can_lam_sang` - Y tá cận lâm sàng

### 2. Backend: Broadcast đúng cho từng loại y tá

**RealtimeService.cs - Cập nhật broadcast strategy:**

```csharp
// Y tá hành chính (quản lý lịch, thu ngân, phát thuốc)
private static readonly string AdminNurseGroupName =
    RealtimeHub.GetNurseTypeGroupName("hanhchinh");

// Y tá lâm sàng (hỗ trợ bác sĩ trong phòng khám)
private static readonly string ClinicalNurseGroupName =
    RealtimeHub.GetNurseTypeGroupName("phong_kham");

// Y tá cận lâm sàng (xét nghiệm, siêu âm, X-quang...)
private static readonly string ClsNurseGroupName =
    RealtimeHub.GetNurseTypeGroupName("can_lam_sang");
```

**Broadcast rules mới:**

| Event | Recipients |
|-------|-----------|
| **Appointments** | Bác sĩ + **CHỈ y tá hành chính** |
| **Invoices** | **CHỈ y tá hành chính** |
| **Prescriptions** | Bác sĩ + **CHỈ y tá hành chính** |
| **Drug Stock** | **CHỈ y tá hành chính** |
| **Clinical Exams** | Bác sĩ + Y tá LS trong phòng (via room group) |
| **CLS Orders** | Bác sĩ + Y tá CLS trong phòng (via room group) |
| **Queue** | Nhân sự trong phòng (via room group) |
| **Patients/Dashboard** | TẤT CẢ nhân sự |

### 3. Frontend: Join đúng nurse type group

**realtime.js - Thêm parameter nurseType:**

```javascript
export async function initStaffRealtime({
    staffId,
    rooms = [],
    staffRole, // "bac_si" | "y_ta"
    nurseType, // "hanhchinh" | "phong_kham" | "can_lam_sang"
  } = {}) {
  const conn = await ensureStarted();
  
  // Join role group
  if (staffRole === "y_ta") {
    await conn.invoke("JoinRoleAsync", "y_ta");
    
    // ✅ Join nurse type group (CHỈ Y TÁ)
    if (nurseType) {
      await conn.invoke("JoinNurseTypeAsync", nurseType);
    }
  }
  
  // Join user groups, room groups...
}
```

**App.jsx - Truyền nurseType:**

```javascript
const user = useAuthStore.getState().user;
const staffRole = user?.VaiTro || user?.vaiTro || user?.role || VaiTro || null;
const nurseType = user?.LoaiYTa || user?.loaiYTa || user?.nurseType || null;

initStaffRealtime({ 
  staffId: MaNguoiNhan,
  staffRole: staffRole,
  nurseType: nurseType, // ✅ Truyền loại y tá
  rooms: rooms
});
```

---

## 📊 FILTERING MATRIX

### Y tá Hành chính (hanhchinh)

**Nhận realtime:**
- ✅ Appointments (quản lý lịch hẹn)
- ✅ Invoices (thu ngân)
- ✅ Prescriptions (phát thuốc)
- ✅ Drug Stock (quản lý kho)
- ✅ Patients (quản lý bệnh nhân)
- ✅ Dashboard/KPI

**KHÔNG nhận:**
- ❌ Clinical Exams (trừ khi join room cụ thể)
- ❌ CLS Orders (trừ khi join room cụ thể)
- ❌ Queue (trừ khi join room cụ thể)

### Y tá Lâm sàng (phong_kham)

**Nhận realtime:**
- ✅ Clinical Exams trong phòng (via room group)
- ✅ Queue trong phòng (via room group)
- ✅ Patients (quản lý bệnh nhân)
- ✅ Dashboard/KPI

**KHÔNG nhận:**
- ❌ Appointments
- ❌ Invoices
- ❌ Prescriptions
- ❌ Drug Stock
- ❌ CLS Orders (không phải phòng của mình)

### Y tá Cận lâm sàng (can_lam_sang)

**Nhận realtime:**
- ✅ CLS Orders trong phòng (via room group)
- ✅ Queue trong phòng (via room group)
- ✅ Patients (quản lý bệnh nhân)
- ✅ Dashboard/KPI

**KHÔNG nhận:**
- ❌ Appointments
- ❌ Invoices
- ❌ Prescriptions
- ❌ Drug Stock
- ❌ Clinical Exams (không phải phòng của mình)

---

## 🧪 TESTING

### Test Case 1: Y tá hành chính
```
1. Login as Y tá hành chính (LoaiYTa = "hanhchinh")
2. Check console: "joined group: nurse_type:hanhchinh"
3. Bác sĩ kê đơn thuốc → Y tá HC nhận realtime ✅
4. Bác sĩ tạo lịch hẹn → Y tá HC nhận realtime ✅
5. Bệnh nhân thanh toán → Y tá HC nhận realtime ✅
```

### Test Case 2: Y tá lâm sàng
```
1. Login as Y tá LS (LoaiYTa = "phong_kham", MaPhong = "PK01")
2. Check console: "joined group: nurse_type:phong_kham"
3. Check console: "joined group: room:PK01"
4. Bác sĩ tạo phiếu khám PK01 → Y tá LS nhận realtime ✅
5. Bác sĩ kê đơn thuốc → Y tá LS KHÔNG nhận ✅
6. Bác sĩ tạo lịch hẹn → Y tá LS KHÔNG nhận ✅
```

### Test Case 3: Y tá CLS
```
1. Login as Y tá CLS (LoaiYTa = "can_lam_sang", MaPhong = "XN01")
2. Check console: "joined group: nurse_type:can_lam_sang"
3. Check console: "joined group: room:XN01"
4. Bác sĩ chỉ định CLS XN01 → Y tá CLS nhận realtime ✅
5. Bác sĩ kê đơn thuốc → Y tá CLS KHÔNG nhận ✅
6. Bác sĩ tạo lịch hẹn → Y tá CLS KHÔNG nhận ✅
```

---

## 📝 FILES CHANGED

### Backend:
1. **HealthCare/Hubs/RealtimeHub.cs**
   - Added `JoinNurseTypeAsync()` method
   - Added `LeaveNurseTypeAsync()` method
   - Added `GetNurseTypeGroupName()` helper

2. **HealthCare/Realtime/RealtimeService.cs**
   - Added nurse type group constants
   - Updated `BroadcastAppointmentChangedAsync()` - only admin nurses
   - Updated `BroadcastInvoiceChangedAsync()` - only admin nurses
   - Updated `BroadcastPrescriptionCreatedAsync()` - only admin nurses
   - Updated `BroadcastPrescriptionStatusUpdatedAsync()` - only admin nurses
   - Updated `BroadcastDrugChangedAsync()` - only admin nurses

### Frontend:
3. **my-patients/src/api/realtime.js**
   - Added `nurseType` parameter to `initStaffRealtime()`
   - Added `JoinNurseTypeAsync()` call for nurses

4. **my-patients/src/App.jsx**
   - Extract `nurseType` from user object
   - Pass `nurseType` to `initStaffRealtime()`

---

## 🎯 BENEFITS

### Before Fix:
- ❌ Y tá CLS nhận thông báo về phát thuốc (không liên quan)
- ❌ Y tá LS nhận thông báo về thu ngân (không liên quan)
- ❌ Nhiều thông báo không cần thiết
- ❌ Gây nhiễu, giảm hiệu quả làm việc

### After Fix:
- ✅ Mỗi loại y tá chỉ nhận thông báo liên quan đến công việc
- ✅ Y tá HC: Lịch hẹn, Thu ngân, Phát thuốc
- ✅ Y tá LS: Phiếu khám trong phòng
- ✅ Y tá CLS: Chỉ định CLS trong phòng
- ✅ Giảm nhiễu, tăng hiệu quả làm việc

---

## 🔮 FUTURE IMPROVEMENTS

### Short Term:
1. Add UI indicator showing which groups user joined
2. Add notification preferences per nurse type
3. Add statistics on realtime message volume per nurse type

### Long Term:
1. Dynamic room switching for nurses
2. Multi-room support for nurses working in multiple rooms
3. Shift-based group joining (morning/afternoon/night shifts)

---

## ✅ CONCLUSION

**Problem:** Y tá CLS/LS nhận thông báo hành chính không liên quan

**Solution:** 
- Backend: Tạo groups riêng cho từng loại y tá
- Backend: Broadcast đúng cho từng loại
- Frontend: Join đúng nurse type group

**Result:**
- ✅ Y tá hành chính: Chỉ nhận invoices, prescriptions, appointments
- ✅ Y tá LS: Chỉ nhận clinical exams trong phòng
- ✅ Y tá CLS: Chỉ nhận CLS orders trong phòng
- ✅ Giảm nhiễu, tăng hiệu quả

---

**Fixed by:** Kiro AI Assistant  
**Date:** 2025-01-03  
**Time:** ~20 minutes  
**Status:** ✅ COMPLETED & TESTED
