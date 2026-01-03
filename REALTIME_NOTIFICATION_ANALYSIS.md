# Phân tích Flow Realtime & Thông báo

## Ngày phân tích: 2026-01-03

---

## 1. TỔNG QUAN KIẾN TRÚC

### Backend (SignalR)
- **Hub:** `RealtimeHub` - Quản lý connections
- **Service:** `RealtimeService` - Facade để broadcast events
- **Groups:**
  - `role:bac_si` - Tất cả bác sĩ
  - `role:y_ta` - Tất cả y tá (bao gồm HC, thu ngân, phát thuốc)
  - `room:{maPhong}` - Theo phòng
  - `user:{loai}:{ma}` - Theo user cụ thể

### Notification Service
- Tạo thông báo trong DB
- Broadcast qua realtime
- Quản lý người nhận (ThongBaoNguoiNhan)

---

## 2. PHÂN TÍCH THEO CHỨC NĂNG

### 2.1. Dashboard & KPI
**Broadcast đến:** Tất cả nhân sự (bác sĩ + y tá)

| Event | Người nhận | Đúng? |
|-------|-----------|-------|
| `DashboardTodayUpdated` | bac_si + y_ta | ✅ |
| `TodayPatientsKpiUpdated` | bac_si + y_ta | ✅ |
| `TodayAppointmentsKpiUpdated` | bac_si + y_ta | ✅ |
| `TodayRevenueKpiUpdated` | bac_si + y_ta | ✅ |
| `TodayExamOverviewUpdated` | bac_si + y_ta | ✅ |
| `UpcomingAppointmentsUpdated` | bac_si + y_ta | ✅ |
| `RecentActivitiesUpdated` | bac_si + y_ta | ✅ |

**Kết luận:** ✅ Đúng - Dashboard cho toàn bộ nhân sự

---

### 2.2. Bệnh nhân
**Broadcast đến:** Tất cả nhân sự

| Event | Người nhận | Đúng? | Ghi chú |
|-------|-----------|-------|---------|
| `PatientCreated` | bac_si + y_ta | ✅ | Tất cả nhân sự cần biết BN mới |
| `PatientUpdated` | bac_si + y_ta | ✅ | Cập nhật thông tin hành chính |
| `PatientStatusUpdated` | bac_si + y_ta | ✅ | Trạng thái hôm nay thay đổi |

**Kết luận:** ✅ Đúng

---

### 2.3. Phiếu khám Lâm sàng
**Broadcast đến:** Tất cả nhân sự + phòng cụ thể

| Event | Người nhận | Đúng? | Vấn đề |
|-------|-----------|-------|--------|
| `ClinicalExamCreated` | bac_si + y_ta + room | ⚠️ | Nên chỉ gửi cho bác sĩ phụ trách + y tá phòng đó |
| `ClinicalExamUpdated` | bac_si + y_ta + room | ⚠️ | Quá rộng, không cần tất cả nhân sự |
| `FinalDiagnosisChanged` | bac_si + y_ta | ⚠️ | Chỉ cần bác sĩ + y tá liên quan |

**Vấn đề:**
- Broadcast quá rộng, tất cả nhân sự nhận được mọi phiếu khám
- Nên filter theo phòng hoặc bác sĩ phụ trách

---

### 2.4. CLS (Cận lâm sàng)
**Broadcast đến:** Tất cả nhân sự + phòng CLS

| Event | Người nhận | Đúng? | Vấn đề |
|-------|-----------|-------|--------|
| `ClsOrderCreated` | bac_si + y_ta + room | ⚠️ | Nên chỉ gửi y tá CLS + bác sĩ yêu cầu |
| `ClsOrderUpdated` | bac_si + y_ta + room | ⚠️ | Quá rộng |
| `ClsOrderStatusUpdated` | bac_si + y_ta + room | ⚠️ | Quá rộng |
| `ClsResultCreated` | bac_si + y_ta | ⚠️ | Chỉ cần bác sĩ yêu cầu + y tá CLS |
| `ClsSummaryCreated` | bac_si + y_ta | ⚠️ | Chỉ cần y tá LS + bác sĩ |
| `ClsSummaryUpdated` | bac_si + y_ta | ⚠️ | Chỉ cần y tá LS + bác sĩ |
| `ClsItemUpdated` | bac_si + y_ta | ⚠️ | Chỉ cần y tá CLS phòng đó |

**Vấn đề:**
- Tất cả bác sĩ và y tá nhận thông báo CLS của mọi bệnh nhân
- Nên filter theo:
  - Bác sĩ yêu cầu CLS
  - Y tá phòng CLS thực hiện
  - Y tá LS nhận kết quả

---

### 2.5. Lượt khám (Visit/History)
**Broadcast đến:** Tất cả nhân sự

| Event | Người nhận | Đúng? | Vấn đề |
|-------|-----------|-------|--------|
| `VisitCreated` | bac_si + y_ta | ⚠️ | Chỉ cần nhân sự phòng đó |
| `VisitStatusUpdated` | bac_si + y_ta | ⚠️ | Chỉ cần nhân sự phòng đó |

**Vấn đề:**
- Tất cả nhân sự nhận thông báo lượt khám của mọi phòng
- Nên gửi theo phòng

---

### 2.6. Hàng đợi (Queue)
**Broadcast đến:** Theo phòng

| Event | Người nhận | Đúng? |
|-------|-----------|-------|
| `QueueByRoomUpdated` | room:{maPhong} | ✅ |
| `QueueItemChanged` | room:{maPhong} | ✅ |

**Kết luận:** ✅ Đúng - Hàng đợi theo phòng là hợp lý

---

### 2.7. Lịch hẹn
**Broadcast đến:** Tất cả nhân sự + bác sĩ cụ thể

| Event | Người nhận | Đúng? | Vấn đề |
|-------|-----------|-------|--------|
| `AppointmentChanged` | bac_si + y_ta + doctor | ⚠️ | Chỉ cần y tá HC + bác sĩ được hẹn |

**Vấn đề:**
- Tất cả bác sĩ và y tá nhận thông báo mọi lịch hẹn
- Nên chỉ gửi:
  - Y tá hành chính (quản lý lịch hẹn)
  - Bác sĩ được hẹn

---

### 2.8. Hóa đơn
**Broadcast đến:** Tất cả nhân sự

| Event | Người nhận | Đúng? | Vấn đề |
|-------|-----------|-------|--------|
| `InvoiceChanged` | bac_si + y_ta | ⚠️ | Chỉ cần thu ngân + bác sĩ liên quan |

**Vấn đề:**
- Tất cả nhân sự nhận thông báo mọi hóa đơn
- Nên chỉ gửi thu ngân

---

### 2.9. Thuốc & Đơn thuốc
**Broadcast đến:** Y tá (thuốc) / Tất cả (đơn thuốc)

| Event | Người nhận | Đúng? | Vấn đề |
|-------|-----------|-------|--------|
| `DrugChanged` | y_ta | ✅ | Đúng - kho thuốc cho y tá |
| `PrescriptionCreated` | bac_si + y_ta | ⚠️ | Chỉ cần bác sĩ kê + y tá phát thuốc |
| `PrescriptionStatusUpdated` | bac_si + y_ta | ⚠️ | Chỉ cần bác sĩ kê + y tá phát thuốc |

**Vấn đề:**
- Tất cả bác sĩ nhận thông báo đơn thuốc của bác sĩ khác
- Nên chỉ gửi:
  - Bác sĩ kê đơn
  - Y tá phát thuốc (phòng thuốc)

---

### 2.10. Thông báo (Notification)
**Broadcast đến:** Theo loại người nhận

| Loại người nhận | Logic hiện tại | Đúng? |
|-----------------|----------------|-------|
| Không chỉ định | bac_si + y_ta | ✅ |
| `bac_si` | bac_si | ✅ |
| `y_ta` | y_ta | ✅ |
| `thu_ngan` | y_ta | ✅ |
| `phat_thuoc` | y_ta | ✅ |
| Có mã cụ thể | user:{loai}:{ma} | ✅ |

**Kết luận:** ✅ Đúng - Thông báo đã được filter đúng

---

## 3. TỔNG KẾT VẤN ĐỀ

### 3.1. Vấn đề chính: BROADCAST QUÁ RỘNG

**Các event broadcast cho TẤT CẢ nhân sự:**
1. ❌ Phiếu khám LS (mọi bác sĩ nhận phiếu của bác sĩ khác)
2. ❌ CLS (mọi y tá nhận CLS của mọi phòng)
3. ❌ Lượt khám (mọi nhân sự nhận lượt khám mọi phòng)
4. ❌ Lịch hẹn (mọi bác sĩ nhận lịch hẹn của bác sĩ khác)
5. ❌ Hóa đơn (mọi nhân sự nhận mọi hóa đơn)
6. ❌ Đơn thuốc (mọi bác sĩ nhận đơn của bác sĩ khác)

**Hậu quả:**
- Frontend nhận quá nhiều event không liên quan
- Chuông thông báo reo liên tục
- Danh sách thông báo lộn xộn
- Performance kém

---

### 3.2. Nguyên nhân

Code hiện tại:
```csharp
// ❌ SAI: Gửi cho TẤT CẢ
var tasks = new List<Task>
{
    _hub.Clients.Group(DoctorRoleGroupName).ClinicalExamCreated(phieuKham),
    _hub.Clients.Group(NurseRoleGroupName).ClinicalExamCreated(phieuKham)
};
```

Nên là:
```csharp
// ✅ ĐÚNG: Chỉ gửi cho người liên quan
var doctorGroup = RealtimeHub.GetUserGroupName("bac_si", phieuKham.MaBacSiKham);
var roomGroup = RealtimeHub.GetRoomGroupName(phieuKham.MaPhong);

var tasks = new List<Task>
{
    _hub.Clients.Group(doctorGroup).ClinicalExamCreated(phieuKham),
    _hub.Clients.Group(roomGroup).ClinicalExamCreated(phieuKham)
};
```

---

## 4. GIẢI PHÁP ĐỀ XUẤT

### 4.1. Nguyên tắc mới

**Chỉ broadcast đến người THỰC SỰ cần biết:**

1. **Dashboard/KPI** → Tất cả nhân sự ✅
2. **Bệnh nhân** → Tất cả nhân sự ✅ (cần biết BN mới/cập nhật)
3. **Phiếu khám LS** → Bác sĩ phụ trách + Y tá phòng đó
4. **CLS** → Bác sĩ yêu cầu + Y tá phòng CLS + Y tá LS nhận KQ
5. **Lượt khám** → Nhân sự phòng đó
6. **Hàng đợi** → Nhân sự phòng đó ✅
7. **Lịch hẹn** → Y tá HC + Bác sĩ được hẹn
8. **Hóa đơn** → Thu ngân + Bác sĩ/Y tá liên quan
9. **Đơn thuốc** → Bác sĩ kê + Y tá phát thuốc
10. **Thông báo** → Theo người nhận ✅

---

### 4.2. Cần thêm Groups mới

```csharp
// Theo chức năng cụ thể
"role:y_ta:hanhchinh"  // Y tá hành chính (lịch hẹn)
"role:y_ta:phatthuoc"  // Y tá phát thuốc
"role:thu_ngan"        // Thu ngân (hóa đơn)

// Theo phòng + vai trò
"room:{maPhong}:bac_si"  // Bác sĩ phòng X
"room:{maPhong}:y_ta"    // Y tá phòng X
```

---

## 5. HÀNH ĐỘNG CẦN LÀM

### Priority 1: CẬP NHẬT REALTIME SERVICE

- [ ] Sửa `BroadcastClinicalExamCreatedAsync` - Chỉ gửi bác sĩ + phòng
- [ ] Sửa `BroadcastClinicalExamUpdatedAsync` - Chỉ gửi bác sĩ + phòng
- [ ] Sửa `BroadcastFinalDiagnosisChangedAsync` - Chỉ gửi bác sĩ + y tá LS
- [ ] Sửa tất cả CLS broadcasts - Filter theo phòng + bác sĩ
- [ ] Sửa `BroadcastVisitCreatedAsync` - Chỉ gửi phòng
- [ ] Sửa `BroadcastAppointmentChangedAsync` - Chỉ y tá HC + bác sĩ
- [ ] Sửa `BroadcastInvoiceChangedAsync` - Chỉ thu ngân
- [ ] Sửa Prescription broadcasts - Chỉ bác sĩ kê + y tá phát thuốc

### Priority 2: CẬP NHẬT FRONTEND

- [ ] Kiểm tra SignalR connection - Join đúng groups
- [ ] Kiểm tra notification bell - Chỉ hiện thông báo liên quan
- [ ] Kiểm tra notification list - Filter đúng
- [ ] Test realtime updates - Đảm bảo nhận đúng events

### Priority 3: TESTING

- [ ] Test bác sĩ A không nhận phiếu khám của bác sĩ B
- [ ] Test y tá phòng X không nhận CLS phòng Y
- [ ] Test thu ngân nhận hóa đơn, bác sĩ không nhận
- [ ] Test notification bell chỉ reo khi có thông báo liên quan

---

**Status:** 🔴 CẦN SỬA GẤP  
**Impact:** HIGH - Ảnh hưởng trải nghiệm người dùng  
**Effort:** MEDIUM - Cần refactor RealtimeService
