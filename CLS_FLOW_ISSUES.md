# 🔴 CÁC VẤN ĐỀ FLOW CLS - CHI TIẾT

## ❌ VẤN ĐỀ NGHIÊM TRỌNG 1: Chưa tạo hàng chờ quay lại LS sau khi hoàn tất tất cả DV CLS

### Vị trí:
- File: `HealthCare/Services/OutpatientCare/ClsService.cs`
- Method: `TaoTongHopAsync()` - dòng 862-946

### Vấn đề:
Sau khi tạo phiếu tổng hợp và gắn `MaPhieuKqKhamCls` vào phiếu LS, **KHÔNG TẠO LẠI HÀNG CHỜ** cho phiếu LS để quay lại khám.

**Code hiện tại:**
```csharp
// ClsService.TaoTongHopAsync() - dòng 929-944
phieuLs.MaPhieuKqKhamCls = summary.MaPhieuTongHop;

// ❌ CHỈ CÓ COMMENT, KHÔNG CÓ CODE THỰC TẾ
// await RequeueClinicalExamToClinicAsync(phieuLs); // TODO
```

### Hậu quả:
- ❌ Bệnh nhân không có hàng chờ để quay lại khám LS
- ❌ Phải tạo phiếu khám mới để có hàng chờ (nhưng sẽ tái sử dụng phiếu cũ nếu có `MaPhieuKqKhamCls`)
- ❌ Không đúng flow: Bệnh nhân phải "tự tạo" hàng chờ thay vì tự động

### Giải pháp:
**THÊM CODE** vào `TaoTongHopAsync()` để tạo lại hàng chờ cho phiếu LS:

```csharp
// Sau khi gắn MaPhieuKqKhamCls vào phiếu LS
phieuLs.MaPhieuKqKhamCls = summary.MaPhieuTongHop;
await _db.SaveChangesAsync();

// ✅ THÊM: Tạo lại hàng chờ cho phiếu LS để quay lại khám
var queueExisting = await _db.HangDois
    .FirstOrDefaultAsync(h => h.MaPhieuKham == phieuLs.MaPhieuKham);

if (queueExisting is not null)
{
    // Cập nhật hàng chờ hiện có: chuyển về "cho_goi", Nguon = "service_return"
    await _queue.CapNhatThongTinHangDoiAsync(queueExisting.MaHangDoi, new QueueEnqueueRequest
    {
        MaBenhNhan = phieuLs.MaBenhNhan,
        MaPhong = queueExisting.MaPhong, // Giữ phòng khám LS
        LoaiHangDoi = "kham_lam_sang",
        Nguon = "service_return",
        Nhan = null,
        CapCuu = false,
        DoUuTien = 0,
        ThoiGianLichHen = null,
        MaPhieuKham = phieuLs.MaPhieuKham,
        MaChiTietDv = null,
        PhanLoaiDen = null
    });
}
else
{
    // Tạo hàng chờ mới nếu chưa có (trường hợp hiếm)
    var maPhongKham = phieuLs.DichVuKham?.MaPhongThucHien;
    if (!string.IsNullOrWhiteSpace(maPhongKham))
    {
        await _queue.ThemVaoHangDoiAsync(new QueueEnqueueRequest
        {
            MaBenhNhan = phieuLs.MaBenhNhan,
            MaPhong = maPhongKham,
            LoaiHangDoi = "kham_lam_sang",
            Nguon = "service_return",
            Nhan = null,
            CapCuu = false,
            DoUuTien = 0,
            ThoiGianLichHen = null,
            MaPhieuKham = phieuLs.MaPhieuKham,
            MaChiTietDv = null,
            PhanLoaiDen = null
        });
    }
}
```

---

## ⚠️ VẤN ĐỀ 2: Hàng chờ LS có bị đóng khi tạo hàng chờ CLS không?

### Phân tích:
- ✅ **Hàng chờ LS KHÔNG bị đóng** khi tạo hàng chờ CLS
- ✅ Mỗi hàng chờ là độc lập (1 hàng chờ cho phiếu LS, nhiều hàng chờ cho các DV CLS)
- ✅ Hàng chờ LS vẫn tồn tại, chỉ thay đổi trạng thái: `cho_goi` → `dang_thuc_hien` → `da_phuc_vu` (khi hoàn tất)

### Kết luận:
✅ **KHÔNG CÓ VẤN ĐỀ** - Hàng chờ LS không bị đóng

---

## ⚠️ VẤN ĐỀ 3: Lượt khám LS có bị đóng khi tạo lượt khám CLS không?

### Phân tích:
- Mỗi lượt khám gắn với 1 hàng chờ
- Lượt khám LS và lượt khám CLS là **KHÁC NHAU** (gắn với hàng chờ khác nhau)
- ✅ **Lượt khám LS KHÔNG bị đóng** khi tạo lượt khám CLS

### Kết luận:
✅ **KHÔNG CÓ VẤN ĐỀ** - Lượt khám LS không bị đóng

---

## ⚠️ VẤN ĐỀ 4: Hàng chờ CLS chỉ được tạo khi chuyển sang "dang_thuc_hien"

### Phân tích:
- Hàng chờ CLS được tạo trong `CapNhatTrangThaiPhieuClsAsync("dang_thuc_hien")`
- Frontend có gọi API này: `updateClsOrderStatus(clsOrderId, "dang_thuc_hien")` (PatientModal.jsx:1393)
- ✅ **ĐÚNG:** Hàng chờ được tạo khi "lập phiếu CLS" (chuyển sang `dang_thuc_hien`)

### Kết luận:
✅ **KHÔNG CÓ VẤN ĐỀ** - Logic đúng

---

## 📋 TÓM TẮT VẤN ĐỀ

### ❌ CẦN SỬA NGAY:
1. **Thêm code tạo hàng chờ quay lại LS** trong `ClsService.TaoTongHopAsync()`
   - Sau khi tạo phiếu tổng hợp và gắn vào phiếu LS
   - Tạo lại hoặc cập nhật hàng chờ LS với `Nguon = "service_return"`, `TrangThai = "cho_goi"`

### ✅ KHÔNG CÓ VẤN ĐỀ:
1. ✅ Hàng chờ LS không bị đóng khi tạo hàng chờ CLS
2. ✅ Lượt khám LS không bị đóng khi tạo lượt khám CLS
3. ✅ Hàng chờ CLS được tạo đúng thời điểm (khi chuyển sang `dang_thuc_hien`)

---

## 🔧 CẦN IMPLEMENT

### Code cần thêm vào `ClsService.TaoTongHopAsync()`:

```csharp
// Sau dòng 929: phieuLs.MaPhieuKqKhamCls = summary.MaPhieuTongHop;

// ✅ THÊM: Tạo lại hàng chờ cho phiếu LS để quay lại khám
var queueExisting = await _db.HangDois
    .Include(h => h.PhieuKhamLamSang)
        .ThenInclude(p => p.DichVuKham)
    .FirstOrDefaultAsync(h => h.MaPhieuKham == phieuLs.MaPhieuKham);

if (queueExisting is not null)
{
    // Cập nhật hàng chờ hiện có
    var maPhongKham = phieuLs.DichVuKham?.MaPhongThucHien;
    if (!string.IsNullOrWhiteSpace(maPhongKham))
    {
        await _queue.CapNhatThongTinHangDoiAsync(queueExisting.MaHangDoi, new QueueEnqueueRequest
        {
            MaBenhNhan = phieuLs.MaBenhNhan,
            MaPhong = maPhongKham,
            LoaiHangDoi = "kham_lam_sang",
            Nguon = "service_return",
            Nhan = null,
            CapCuu = false,
            DoUuTien = 0, // Service return có độ ưu tiên cao (sẽ được tính lại trong QueueService)
            ThoiGianLichHen = null,
            MaPhieuKham = phieuLs.MaPhieuKham,
            MaChiTietDv = null,
            PhanLoaiDen = null
        });
    }
}
else
{
    // Tạo hàng chờ mới nếu chưa có (trường hợp hiếm - hàng chờ bị xóa)
    var maPhongKham = phieuLs.DichVuKham?.MaPhongThucHien;
    if (!string.IsNullOrWhiteSpace(maPhongKham))
    {
        await _queue.ThemVaoHangDoiAsync(new QueueEnqueueRequest
        {
            MaBenhNhan = phieuLs.MaBenhNhan,
            MaPhong = maPhongKham,
            LoaiHangDoi = "kham_lam_sang",
            Nguon = "service_return",
            Nhan = null,
            CapCuu = false,
            DoUuTien = 0,
            ThoiGianLichHen = null,
            MaPhieuKham = phieuLs.MaPhieuKham,
            MaChiTietDv = null,
            PhanLoaiDen = null
        });
    }
}

// Cập nhật trạng thái bệnh nhân
await _patients.CapNhatTrangThaiBenhNhanAsync(
    phieuLs.MaBenhNhan,
    new PatientStatusUpdateRequest { TrangThaiHomNay = "cho_kham" });
```

