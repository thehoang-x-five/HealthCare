# 🔍 CÁC VẤN ĐỀ PHÁT HIỆN TRONG FLOW

## ❌ VẤN ĐỀ NGHIÊM TRỌNG 1: PatientModal.jsx tạo lượt khám SAI

### Vị trí:
- File: `my-patients/src/components/patients/PatientModal.jsx`
- Dòng: 1567-1582

### Vấn đề:
```javascript
// ❌ SAI: Gọi với MaPhieuKhamLs thay vì MaHangDoi
await createHistoryVisitMut.mutateAsync({
  MaBenhNhan: pid,
  MaPhieuKhamLs: maPhieuKham,  // ❌ Backend yêu cầu MaHangDoi!
  MaKhoa: maKhoa,
  MaPhong: maPhong,
  // ...
});
```

### Backend yêu cầu:
```csharp
// HistoryService.TaoLuotKhamAsync() - dòng 269-271
var maHangDoiReq = request.MaHangDoi?.Trim();
if (string.IsNullOrWhiteSpace(maHangDoiReq))
    throw new ArgumentException("MaHangDoi là bắt buộc", nameof(request.MaHangDoi));
```

### Hậu quả:
1. ❌ **Lỗi 400 Bad Request** - Backend sẽ reject vì thiếu MaHangDoi
2. ⚠️ Hoặc nếu API chấp nhận MaPhieuKhamLs, sẽ tạo lượt khám SAI (không link với hàng đợi)
3. ❌ **Duplicate lượt khám** - Examination.jsx cũng sẽ tạo lượt khám đúng

### Giải pháp:
**XÓA** đoạn code tạo lượt khám trong `PatientModal.jsx` (dòng 1567-1582)

Lý do:
- Examination.jsx đã tạo lượt khám đúng với MaHangDoi
- Lượt khám chỉ nên tạo SAU KHI gọi vào khám (trong Examination.jsx)
- PatientModal chỉ nên tạo phiếu khám và hàng đợi, không tạo lượt khám

---

## ✅ CÁC PHẦN ĐÚNG (KHÔNG CẦN SỬA):

### 1. Tạo phiếu khám (ClinicalService.TaoPhieuKhamAsync)
- ✅ Logic tái sử dụng phiếu đang hoạt động
- ✅ Tự động tạo hàng đợi
- ✅ Logic thu phí đúng (service_return miễn phí, tái khám đúng giờ miễn phí)

### 2. Thu phí (BillingService)
- ✅ Tự động tạo hóa đơn khi cần
- ✅ Logic skip billing đúng

### 3. Đẩy vào hàng đợi (QueueService.ThemVaoHangDoiAsync)
- ✅ Tự động tạo khi tạo phiếu khám
- ✅ Kiểm tra duplicate
- ✅ Tính độ ưu tiên đúng

### 4. Tạo lượt khám (Examination.jsx)
- ✅ Dùng MaHangDoi đúng
- ✅ Gọi API TaoLuotKhamAsync() đúng cách

---

## 📋 CHECKLIST SỬA LỖI:

- [ ] Xóa đoạn tạo lượt khám trong PatientModal.jsx (dòng 1567-1582)
- [ ] Kiểm tra xem có chỗ nào khác gọi createHistoryVisitMut với MaPhieuKhamLs không
- [ ] Test lại flow: Tạo phiếu khám → Gọi vào khám → Tạo lượt khám

