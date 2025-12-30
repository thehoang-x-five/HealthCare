# PHÂN TÍCH FLOW: LẬP PHIẾU KHÁM → HOÀN TẤT

## ✅ BƯỚC 1: LẬP PHIẾU KHÁM (Frontend: PatientModal.jsx)

### Frontend Flow:
```javascript
// PatientModal.jsx - handleStartExam()
const clinicalExamResult = await createClinicalExamMut.mutateAsync({
  MaBenhNhan: pid,
  MaKhoa: maKhoa,
  MaPhong: maPhong,
  MaBacSiKham: maBacSi,
  MaNguoiLap: maNguoiLap,
  MaDichVuKham: maDichVuKham,
  HinhThucTiepNhan: hinhThucTiepNhan, // walkin | appointment | service_return
  TrieuChung: exam.symptoms || "",
  ...extraFields,
});
```

### Backend: ClinicalService.TaoPhieuKhamAsync()

**✅ ĐÚNG:**
1. Kiểm tra lịch hẹn (nếu có) → tính phân loại đến (den_muon/den_som/dung_gio)
2. Load dịch vụ khám → lấy phòng thực hiện
3. **Rule: 1 bệnh nhân chỉ 1 phiếu LS đang hoạt động** → tái sử dụng hoặc tạo mới
4. Xác định HinhThucTiepNhan (service_return | appointment | walkin)
5. **Đẩy vào hàng đợi** (tự động gọi `_queue.ThemVaoHangDoiAsync()`)
6. **Thu phí** (tự động tạo hóa đơn nếu cần)

**⚠️ VẤN ĐỀ PHÁT HIỆN:**

#### VẤN ĐỀ 1: Frontend tạo History Visit TRƯỚC KHI gọi vào khám
```javascript
// PatientModal.jsx dòng 1568-1582
await createHistoryVisitMut.mutateAsync({
  MaBenhNhan: pid,
  MaPhieuKhamLs: maPhieuKham,
  MaKhoa: maKhoa,
  MaPhong: maPhong,
  MaBacSi: maBacSi,
  // ...
});
```
**❌ SAI:** Tạo lượt khám NGAY sau khi tạo phiếu, chưa có MaHangDoi!
- `TaoLuotKhamAsync()` yêu cầu `MaHangDoi` (bắt buộc)
- Nhưng frontend đang gọi với `MaPhieuKhamLs` thay vì `MaHangDoi`
- Backend sẽ fail hoặc tạo lượt khám sai!

**✅ SỬA:** Nên tạo lượt khám SAU KHI gọi vào khám (trong Examination.jsx)

---

## ✅ BƯỚC 2: THU PHÍ (Backend: ClinicalService.TaoPhieuKhamAsync())

### Logic thu phí:

**✅ ĐÚNG:**
```csharp
// Không thu phí nếu:
// 1. Hình thức tiếp nhận = service_return (quay lại sau CLS)
// 2. Tái khám (LoaiHen = "tai_kham") VÀ không đến muộn (phanLoaiDen != "den_muon")

var shouldCharge = true;
if (hinhThucTiepNhan == "service_return") shouldCharge = false;
if (taiKham && phanLoaiDen != "den_muon") shouldCharge = false;

// Thu phí khám LS nếu shouldCharge = true
if (!skipBilling && shouldCharge && loaded.DichVuKham is not null)
{
    await _billing.TaoHoaDonAsync(invoiceReq);
}
```

**✅ Logic đúng, không có vấn đề.**

---

## ✅ BƯỚC 3: ĐẨY VÀO HÀNG ĐỢI (Backend: QueueService.ThemVaoHangDoiAsync())

### Backend Flow:
```csharp
// ClinicalService.TaoPhieuKhamAsync() dòng 333
await _queue.ThemVaoHangDoiAsync(enqueueRequest);
```

**✅ ĐÚNG:**
- Tự động tạo hàng đợi khi tạo phiếu khám
- Kiểm tra duplicate (theo MaPhieuKham)
- Tính độ ưu tiên và phân loại đến
- Cập nhật trạng thái bệnh nhân → "cho_kham"

**✅ Logic đúng, không có vấn đề.**

---

## ⚠️ BƯỚC 4: GỌI VÀO KHÁM → TẠO LƯỢT KHÁM

### Frontend: Examination.jsx

**⚠️ VẤN ĐỀ PHÁT HIỆN:**

#### VẤN ĐỀ 2: Examination.jsx có vẻ đang tạo lượt khám
Nhưng cần kiểm tra xem có đúng MaHangDoi không:

```javascript
// Examination.jsx - cần kiểm tra
const maHangDoiForVisit = 
  queueItem?.MaHangDoi ?? raw.MaHangDoi ?? key ?? null;

if (!maHangDoiForVisit) {
  throw new Error("Thiếu MaHangDoi khi tạo lượt khám");
}

await createHistoryVisitMut.mutateAsync({
  MaHangDoi: maHangDoiForVisit, // ✅ Đúng
  // ...
});
```

**✅ CÓ VẺ ĐÚNG:** Examination.jsx đang dùng MaHangDoi để tạo lượt khám.

**❌ VẤN ĐỀ:** PatientModal.jsx đang tạo lượt khám TRƯỚC, không có MaHangDoi!

---

## ✅ BƯỚC 5: CHỈ ĐỊNH ĐI KHÁM CLS (nếu có)

**⏸️ CHƯA KIỂM TRA** - User yêu cầu check sau.

---

## ✅ BƯỚC 6: TẠO PHIẾU CHẨN ĐOÁN VÀ PHÁT THUỐC

**✅ ĐÃ HOÀN TẤT** - Flow mới:
1. Xuất chẩn đoán → `da_lap_chan_doan`
2. Xử lý chẩn đoán → fetch và hiển thị
3. Hoàn tất → `CompleteExamAsync()` → `da_hoan_tat`

---

## 📋 TÓM TẮT CÁC VẤN ĐỀ PHÁT HIỆN:

### ❌ VẤN ĐỀ NGHIÊM TRỌNG 1: Duplicate History Visit

**Vị trí:** `PatientModal.jsx` dòng 1568-1582

**Vấn đề:**
- Frontend đang tạo lượt khám NGAY sau khi tạo phiếu khám
- Gọi `createHistoryVisitMut.mutateAsync()` với `MaPhieuKhamLs` thay vì `MaHangDoi`
- Backend `TaoLuotKhamAsync()` yêu cầu `MaHangDoi` (bắt buộc)
- Có thể gây lỗi hoặc tạo lượt khám sai
- Examination.jsx cũng sẽ tạo lượt khám → **DUPLICATE!**

**Giải pháp:**
- ❌ **XÓA** đoạn tạo lượt khám trong `PatientModal.jsx`
- ✅ **GIỮ** tạo lượt khám trong `Examination.jsx` (sau khi gọi vào khám)

---

## 🔍 CẦN KIỂM TRA THÊM:

1. **Examination.jsx** - Xem logic tạo lượt khám có đúng không
2. **QueueService.LayTiepTheoTrongPhongAsync()** - Xem logic gọi vào khám có đúng không
3. **Flow CLS** - Chỉ định CLS và tạo phiếu CLS
4. **Frontend navigation** - Sau khi tạo phiếu, có navigate đúng không

