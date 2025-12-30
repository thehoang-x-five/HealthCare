# PHÂN TÍCH FLOW CLS - CÁC VẤN ĐỀ PHÁT HIỆN

## 🔍 FLOW CHI TIẾT CLS THEO YÊU CẦU

### BƯỚC 1: Chỉ định CLS -> Tạo phiếu CLS ✅
- **Code:** `ClsService.TaoPhieuClsAsync()` - dòng 213
- **Trạng thái phiếu LS:** Vẫn giữ nguyên (chưa đóng)
- **✅ ĐÚNG:** Không thay đổi trạng thái phiếu LS

---

### BƯỚC 2: Lập phiếu CLS (1 hoặc nhiều DV) ✅
- **Code:** `ClsService.TaoPhieuClsAsync()` - dòng 267-285
- Tạo `ChiTietDichVu` cho mỗi DV
- **Trạng thái phiếu LS:** Vẫn giữ nguyên
- **✅ ĐÚNG:** Không thay đổi trạng thái phiếu LS

---

### BƯỚC 3: Tạo hàng chờ DV đầu tiên ✅
- **Code:** `ClsService.CapNhatTrangThaiPhieuClsAsync()` - dòng 399-418
- Chỉ tạo hàng chờ khi chuyển sang `"dang_thuc_hien"`
- Tạo hàng chờ cho `ChiTietDichVu` đầu tiên
- **✅ ĐÚNG:** Tạo hàng chờ cho DV đầu tiên

**⚠️ VẤN ĐỀ 1:** Hàng chờ chỉ được tạo khi `CapNhatTrangThaiPhieuClsAsync("dang_thuc_hien")`
- Frontend có gọi API này không?
- Hoặc hàng chờ được tạo ở đâu?

---

### BƯỚC 4: Tạo lượt khám CLS ✅
- **Code:** `HistoryService.TaoLuotKhamAsync()` - dòng 267
- Dùng `MaHangDoi` để tạo lượt khám
- **✅ ĐÚNG:** Tạo lượt khám từ hàng chờ

---

### BƯỚC 5: Hoàn tất lượt -> Tạo kết quả -> Hàng chờ tiếp theo ✅
- **Code:** `ClsService.TaoKetQuaClsAsync()` - dòng 646
- Sau khi tạo kết quả:
  - Đóng hàng chờ hiện tại: `da_phuc_vu` ✅
  - Đóng lượt khám: `hoan_tat` ✅
  - Tìm DV tiếp theo chưa hoàn tất ✅
  - Tạo hàng chờ cho DV tiếp theo ✅

**✅ ĐÚNG:** Logic đúng theo yêu cầu

---

### BƯỚC 6: Hoàn tất DV cuối -> Tạo phiếu tổng hợp ✅
- **Code:** `ClsService.CheckAndAutoCompleteClsOrderAsync()` - dòng 1139
- Kiểm tra tất cả DV đã hoàn tất
- Tạo phiếu tổng hợp: `TaoTongHopAsync()` ✅
- Gắn `MaPhieuKqKhamCls` vào phiếu LS ✅

**⚠️ VẤN ĐỀ 2:** Sau khi tạo phiếu tổng hợp, CHƯA TẠO HÀNG CHỜ QUAY LẠI LS!

**Code hiện tại:**
```csharp
// ClsService.TaoTongHopAsync() - dòng 929-943
phieuLs.MaPhieuKqKhamCls = summary.MaPhieuTongHop;
// ... TODO comment về re-enqueue ...
```

**Thiếu:** Không có code tạo lại hàng chờ cho phiếu LS để quay lại khám!

---

### BƯỚC 7: Quay lại LS -> Đẩy lại vào flow cũ ❌

**❌ VẤN ĐỀ NGHIÊM TRỌNG:**

1. **Chưa tạo hàng chờ quay lại LS:**
   - Sau khi tạo phiếu tổng hợp, cần tạo hàng chờ cho phiếu LS
   - Hình thức tiếp nhận: `service_return`
   - Hiện tại: CHƯA CÓ CODE

2. **Phiếu LS, hàng chờ, lượt khám cần "sống suốt quá trình":**
   - ✅ Phiếu LS: Đã đúng (không đóng trong quá trình CLS)
   - ❌ Hàng chờ LS: Đã đóng khi bắt đầu khám CLS? Cần kiểm tra
   - ❌ Lượt khám LS: Đã đóng khi bắt đầu khám CLS? Cần kiểm tra

---

## 🔴 CÁC VẤN ĐỀ PHÁT HIỆN

### ❌ VẤN ĐỀ 1: Chưa tạo hàng chờ quay lại LS sau khi hoàn tất tất cả DV CLS

**Vị trí:** `ClsService.TaoTongHopAsync()` - dòng 929-943

**Vấn đề:**
- Chỉ gắn `MaPhieuKqKhamCls` vào phiếu LS
- Không tạo lại hàng chờ cho phiếu LS để quay lại khám
- Comment có TODO nhưng chưa implement

**Cần sửa:**
- Sau khi tạo phiếu tổng hợp và gắn vào phiếu LS
- Cần tạo lại hàng chờ cho phiếu LS với `Nguon = "service_return"`
- Hoặc cập nhật hàng chờ LS cũ (nếu còn tồn tại) sang `cho_goi`

---

### ❌ VẤN ĐỀ 2: Hàng chờ và lượt khám LS có bị đóng không?

**Cần kiểm tra:**
1. Khi tạo phiếu CLS, hàng chờ LS có bị đóng không?
2. Khi tạo hàng chờ CLS đầu tiên, lượt khám LS có bị đóng không?
3. Theo yêu cầu: "hàng chờ + lượt khám cũng sẽ sống suốt quá trình giống phiếu LS"

**Cần xem:**
- `HistoryService.TaoLuotKhamAsync()` - có đóng lượt khám LS cũ không?
- `QueueService.ThemVaoHangDoiAsync()` - có đóng hàng chờ LS cũ không?

---

### ⚠️ VẤN ĐỀ 3: Hàng chờ CLS chỉ được tạo khi chuyển sang "dang_thuc_hien"

**Vị trí:** `ClsService.CapNhatTrangThaiPhieuClsAsync()` - dòng 399-418

**Vấn đề:**
- Hàng chờ chỉ được tạo khi `trangThai = "dang_thuc_hien"`
- Nhưng khi nào API này được gọi?
- Có thể hàng chờ cần được tạo ngay khi tạo phiếu CLS (nếu có DV)?

**Cần kiểm tra:**
- Frontend có gọi `CapNhatTrangThaiPhieuClsAsync("dang_thuc_hien")` không?
- Hoặc hàng chờ được tạo ở đâu?

---

## ✅ CÁC PHẦN ĐÚNG

1. ✅ Tạo phiếu CLS không đóng phiếu LS
2. ✅ Tạo nhiều ChiTietDichVu cho phiếu CLS
3. ✅ Tạo hàng chờ cho DV đầu tiên
4. ✅ Tạo lượt khám từ hàng chờ CLS
5. ✅ Hoàn tất lượt -> Tạo kết quả -> Tạo hàng chờ tiếp theo
6. ✅ Hoàn tất DV cuối -> Tạo phiếu tổng hợp
7. ✅ Gắn MaPhieuKqKhamCls vào phiếu LS

---

## 📋 CẦN SỬA

1. **Thêm code tạo hàng chờ quay lại LS** sau khi tạo phiếu tổng hợp
2. **Kiểm tra và đảm bảo** hàng chờ + lượt khám LS không bị đóng trong quá trình CLS
3. **Kiểm tra logic** tạo hàng chờ CLS đầu tiên (có đúng thời điểm không)

