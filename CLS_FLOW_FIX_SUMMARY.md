# ✅ TỔNG KẾT SỬA FLOW CLS

## 🔴 VẤN ĐỀ ĐÃ PHÁT HIỆN VÀ SỬA

### ❌ VẤN ĐỀ: Chưa tạo hàng chờ quay lại LS sau khi hoàn tất tất cả DV CLS

**Vị trí:** `ClsService.TaoTongHopAsync()` - dòng 862-972

**Vấn đề:**
- Sau khi tạo phiếu tổng hợp và gắn `MaPhieuKqKhamCls` vào phiếu LS
- **KHÔNG TẠO LẠI HÀNG CHỜ** cho phiếu LS để quay lại khám
- Chỉ có comment TODO, chưa có code thực tế

**✅ ĐÃ SỬA:**
1. Thêm code tìm hàng chờ hiện có của phiếu LS
2. Nếu có: Cập nhật hàng chờ với `Nguon = "service_return"`, `TrangThai = "cho_goi"`
3. Nếu không: Tạo hàng chờ mới
4. Cập nhật trạng thái bệnh nhân → `cho_kham`

**Code đã thêm:**
```csharp
// ===== Tạo lại hàng chờ cho phiếu LS để quay lại khám =====
var queueExisting = await _db.HangDois
    .Include(h => h.PhieuKhamLamSang)
        .ThenInclude(p => p.DichVuKham)
    .FirstOrDefaultAsync(h => h.MaPhieuKham == phieuLs.MaPhieuKham);

var maPhongKham = phieuLs.DichVuKham?.MaPhongThucHien;

if (queueExisting is not null && !string.IsNullOrWhiteSpace(maPhongKham))
{
    // Cập nhật hàng chờ hiện có
    await _queue.CapNhatThongTinHangDoiAsync(queueExisting.MaHangDoi, ...);
    await _queue.CapNhatTrangThaiHangDoiAsync(
        queueExisting.MaHangDoi,
        new QueueStatusUpdateRequest { TrangThai = "cho_goi" });
}
else if (!string.IsNullOrWhiteSpace(maPhongKham))
{
    // Tạo hàng chờ mới nếu chưa có
    await _queue.ThemVaoHangDoiAsync(...);
}

// Cập nhật trạng thái bệnh nhân
await _patients.CapNhatTrangThaiBenhNhanAsync(
    phieuLs.MaBenhNhan,
    new PatientStatusUpdateRequest { TrangThaiHomNay = "cho_kham" });
```

---

## ✅ CÁC PHẦN ĐÚNG (KHÔNG CẦN SỬA)

### 1. ✅ Hàng chờ LS không bị đóng khi tạo hàng chờ CLS
- Mỗi hàng chờ là độc lập
- Hàng chờ LS và hàng chờ CLS là khác nhau

### 2. ✅ Lượt khám LS không bị đóng khi tạo lượt khám CLS
- Mỗi lượt khám gắn với 1 hàng chờ
- Lượt khám LS và lượt khám CLS là khác nhau

### 3. ✅ Phiếu LS không bị đóng trong quá trình CLS
- Trạng thái phiếu LS giữ nguyên trong quá trình CLS

### 4. ✅ Hàng chờ CLS được tạo đúng thời điểm
- Được tạo khi `CapNhatTrangThaiPhieuClsAsync("dang_thuc_hien")`
- Frontend đã gọi đúng API

### 5. ✅ Flow hoàn tất lượt -> Tạo kết quả -> Hàng chờ tiếp theo
- Logic đúng: Đóng hàng chờ hiện tại → Tìm DV tiếp theo → Tạo hàng chờ mới

### 6. ✅ Flow hoàn tất DV cuối -> Tạo phiếu tổng hợp
- Tự động tạo phiếu tổng hợp khi tất cả DV hoàn tất
- Gắn `MaPhieuKqKhamCls` vào phiếu LS

---

## 📊 FLOW HOÀN CHỈNH SAU KHI SỬA

### BƯỚC 1: Chỉ định CLS -> Tạo phiếu CLS ✅
- `ClsService.TaoPhieuClsAsync()`
- Tạo `PhieuKhamCanLamSang` + `ChiTietDichVu` (1 hoặc nhiều)
- **Phiếu LS:** Vẫn giữ nguyên trạng thái

### BƯỚC 2: Lập phiếu CLS -> Tạo hàng chờ DV đầu tiên ✅
- `ClsService.CapNhatTrangThaiPhieuClsAsync("dang_thuc_hien")`
- Tạo hàng chờ cho `ChiTietDichVu` đầu tiên
- **Hàng chờ LS:** Vẫn tồn tại, không bị đóng

### BƯỚC 3: Tạo lượt khám CLS ✅
- `HistoryService.TaoLuotKhamAsync(MaHangDoi)`
- Tạo lượt khám từ hàng chờ CLS
- **Lượt khám LS:** Vẫn tồn tại, không bị đóng

### BƯỚC 4: Hoàn tất lượt -> Tạo kết quả -> Hàng chờ tiếp theo ✅
- `ClsService.TaoKetQuaClsAsync()`
- Đóng hàng chờ hiện tại → Đóng lượt khám hiện tại
- Tìm DV tiếp theo chưa hoàn tất → Tạo hàng chờ mới
- Lặp lại cho đến khi hết DV

### BƯỚC 5: Hoàn tất DV cuối -> Tạo phiếu tổng hợp ✅
- `ClsService.CheckAndAutoCompleteClsOrderAsync()`
- Tự động gọi `TaoTongHopAsync()` khi tất cả DV hoàn tất
- Gắn `MaPhieuKqKhamCls` vào phiếu LS

### BƯỚC 6: Tạo lại hàng chờ quay lại LS ✅ (MỚI SỬA)
- Trong `TaoTongHopAsync()`
- Tìm hàng chờ hiện có của phiếu LS
- Cập nhật: `Nguon = "service_return"`, `TrangThai = "cho_goi"`
- Hoặc tạo mới nếu chưa có
- Cập nhật trạng thái BN → `cho_kham`

### BƯỚC 7: Bệnh nhân quay lại khám LS ✅
- Hàng chờ LS đã được tạo lại với `Nguon = "service_return"`
- Bác sĩ có thể gọi vào khám từ hàng chờ này
- **Phiếu LS, hàng chờ LS, lượt khám LS:** Vẫn sống suốt quá trình CLS

---

## ✅ KẾT LUẬN

✅ **Tất cả các vấn đề đã được sửa!**

Flow CLS hiện tại:
- ✅ Đúng bản chất từng bước
- ✅ Data được persist đúng lúc
- ✅ Hàng chờ và lượt khám LS sống suốt quá trình
- ✅ Tự động tạo hàng chờ quay lại LS sau khi hoàn tất CLS
- ✅ Bệnh nhân có thể quay lại khám LS ngay sau khi có kết quả CLS

