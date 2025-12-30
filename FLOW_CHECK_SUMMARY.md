# 📋 TÓM TẮT KIỂM TRA FLOW

## ✅ ĐÃ KIỂM TRA VÀ SỬA LỖI

### 1. ✅ Lập phiếu khám → Tạo phiếu LS/CLS
- **Backend:** `ClinicalService.TaoPhieuKhamAsync()` ✅ ĐÚNG
  - Kiểm tra lịch hẹn và tính phân loại đến
  - Rule: 1 bệnh nhân chỉ 1 phiếu đang hoạt động
  - Tái sử dụng phiếu hoặc tạo mới
  - Xác định hình thức tiếp nhận (walkin/appointment/service_return)

### 2. ✅ Thu phí khi tạo phiếu
- **Backend:** Logic thu phí ✅ ĐÚNG
  - Không thu phí nếu: `service_return` hoặc `tái khám đúng giờ`
  - Tự động tạo hóa đơn khi cần
  - Skip billing nếu đã có hóa đơn (service_return)

### 3. ✅ Đẩy vào hàng đợi LS/CLS
- **Backend:** `QueueService.ThemVaoHangDoiAsync()` ✅ ĐÚNG
  - Tự động tạo khi tạo phiếu khám
  - Kiểm tra duplicate
  - Tính độ ưu tiên và phân loại đến
  - Cập nhật trạng thái bệnh nhân → "cho_kham"

### 4. ✅ Gọi vào khám → Tạo lượt khám
- **Frontend:** `Examination.jsx` ✅ ĐÚNG
  - Dùng `MaHangDoi` đúng cách
  - Gọi `TaoLuotKhamAsync()` với `MaHangDoi` (bắt buộc)
  - Backend tự cập nhật trạng thái queue và phiếu khám

### 5. ⏸️ Chỉ định CLS (nếu có)
- **CHƯA KIỂM TRA** - User yêu cầu check sau

### 6. ✅ Tạo chẩn đoán và phát thuốc
- **ĐÃ HOÀN TẤT** - Flow mới:
  - Xuất chẩn đoán → `da_lap_chan_doan`
  - Xử lý chẩn đoán → fetch và hiển thị
  - Hoàn tất → `CompleteExamAsync()` → `da_hoan_tat`

---

## ❌ VẤN ĐỀ ĐÃ PHÁT HIỆN VÀ SỬA

### 🔴 VẤN ĐỀ: PatientModal.jsx tạo lượt khám SAI

**Vị trí:** `my-patients/src/components/patients/PatientModal.jsx` (dòng 1567-1582)

**Vấn đề:**
- Frontend đang gọi `createHistoryVisitMut.mutateAsync()` với `MaPhieuKhamLs`
- Backend `TaoLuotKhamAsync()` yêu cầu `MaHangDoi` (bắt buộc)
- Gây lỗi 400 Bad Request hoặc tạo lượt khám sai
- Duplicate với Examination.jsx (cũng sẽ tạo lượt khám)

**✅ ĐÃ SỬA:**
- Xóa đoạn code tạo lượt khám trong `PatientModal.jsx`
- Lượt khám chỉ được tạo trong `Examination.jsx` sau khi gọi vào khám (có MaHangDoi)

---

## 📊 FLOW CHUẨN SAU KHI SỬA

```
1. Lập phiếu khám (PatientModal.jsx)
   ↓
   POST /api/clinical
   → Tạo/tái sử dụng phiếu khám
   → Tự động tạo hàng đợi
   → Thu phí (nếu cần)
   → Trạng thái: "cho_kham"
   
2. Gọi vào khám (Examination.jsx)
   ↓
   GET /api/queue/{maPhong} → Lấy hàng đợi
   → Click "Bắt đầu khám"
   → POST /api/history/visits (với MaHangDoi)
   → Tạo lượt khám
   → Cập nhật queue → "dang_thuc_hien"
   → Cập nhật phiếu khám → "dang_kham"
   
3. Khám bệnh (Examination.jsx → ExamDetail.jsx)
   → Nhập thông tin khám
   → Chỉ định CLS (nếu có)
   
4. Xuất chẩn đoán (Examination.jsx)
   ↓
   POST /api/clinical/final-diagnosis
   → Tạo chẩn đoán
   → Tạo đơn thuốc (nếu có)
   → Phiếu khám → "da_lap_chan_doan"
   → Bệnh nhân → "cho_xu_ly"
   
5. Xử lý chẩn đoán (Patients.jsx → PatientProcessMode.jsx)
   → Tìm phiếu khám đang hoạt động
   → Fetch chẩn đoán tự động
   → Hiển thị chẩn đoán và đơn thuốc
   
6. Hoàn tất (PatientProcessMode.jsx)
   ↓
   POST /api/clinical/{maPhieuKham}/complete
   → Đóng phiếu khám → "da_hoan_tat"
   → Đóng lượt khám → "hoan_tat"
   → Đóng hàng đợi → "da_phuc_vu"
   → Bệnh nhân → DONE
```

---

## ✅ KẾT LUẬN

- ✅ **Đã sửa:** Vấn đề duplicate lượt khám
- ✅ **Flow đúng:** Tạo phiếu → Hàng đợi → Gọi vào khám → Tạo lượt khám → Chẩn đoán → Hoàn tất
- ⏸️ **Chưa check:** Flow chỉ định CLS (sẽ check sau như user yêu cầu)

