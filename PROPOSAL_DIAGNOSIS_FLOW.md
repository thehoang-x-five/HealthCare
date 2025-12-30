# ĐỀ XUẤT CẢI THIỆN LUỒNG: KHÁM BỆNH → LẬP CHẨN ĐOÁN → XỬ LÝ CHẨN ĐOÁN

## 📋 VẤN ĐỀ HIỆN TẠI

### Luồng hiện tại (SAI):
```
1. Khám bệnh: da_lap → dang_kham
2. Lập chẩn đoán: 
   ❌ Ngay lập tức đóng TẤT CẢ:
   - Phiếu khám → da_hoan_tat
   - Lượt khám → hoan_tat  
   - Hàng đợi → da_phuc_vu
   - Bệnh nhân → cho_xu_ly
```

**Vấn đề:**
- Không có bước trung gian "xử lý chẩn đoán"
- Bệnh nhân chuyển sang `cho_xu_ly` nhưng không rõ cần xử lý gì
- Không thể theo dõi tiến trình xử lý sau chẩn đoán

---

## ✅ LUỒNG ĐỀ XUẤT (ĐÚNG)

### Luồng mới:
```
1. Khám bệnh: 
   da_lap → dang_kham

2. Lập chẩn đoán (TaoChanDoanCuoi):
   ✅ Chỉ lưu chẩn đoán
   ✅ Phiếu khám → da_lap_chan_doan (MỚI)
   ✅ Lượt khám → vẫn dang_kham (chưa đóng)
   ✅ Hàng đợi → vẫn dang_thuc_hien (chưa đóng)
   ✅ Bệnh nhân → cho_xu_ly_chan_doan (MỚI)

3. Xử lý chẩn đoán (ProcessDiagnosis):
   - Nếu có CLS → chờ CLS hoàn tất
   - Nếu có đơn thuốc → chờ lấy thuốc
   - Nếu có thanh toán → chờ thanh toán
   - Nếu tái khám → chờ tái khám
   ✅ Phiếu khám → vẫn da_lap_chan_doan
   ✅ Bệnh nhân → cho_xu_ly_chan_doan

4. Hoàn tất (CompleteExam):
   ✅ Phiếu khám → da_hoan_tat
   ✅ Lượt khám → hoan_tat
   ✅ Hàng đợi → da_phuc_vu
   ✅ Bệnh nhân → da_xu_ly_xong (hoặc null)
```

---

## 🔧 THAY ĐỔI CHI TIẾT

### 1. Thêm trạng thái mới

#### Entity: PhieuKhamLamSang
```csharp
// Trạng thái hiện tại:
// da_lap, dang_kham, da_hoan_tat, da_huy

// Thêm mới:
// da_lap_chan_doan  // Đã lập chẩn đoán, đang chờ xử lý
```

#### Entity: BenhNhan
```csharp
// Trạng thái hiện tại:
// cho_kham, dang_kham, cho_xu_ly, cho_xu_ly_dv, ...

// Thêm mới:
// cho_xu_ly_chan_doan  // Đã có chẩn đoán, đang chờ xử lý
```

---

### 2. Sửa ClinicalService.TaoChanDoanCuoiAsync()

**TRƯỚC (SAI):**
```csharp
// Ngay lập tức đóng tất cả
await CapNhatTrangThaiPhieuKhamAsync(
    phieu.MaPhieuKham,
    new ClinicalExamStatusUpdateRequest { TrangThai = "da_hoan_tat" });
    
phieu.BenhNhan.TrangThaiHomNay = "cho_xu_ly";
```

**SAU (ĐÚNG):**
```csharp
// Chỉ lưu chẩn đoán, chuyển sang trạng thái chờ xử lý
await CapNhatTrangThaiPhieuKhamAsync(
    phieu.MaPhieuKham,
    new ClinicalExamStatusUpdateRequest { TrangThai = "da_lap_chan_doan" });
    
phieu.BenhNhan.TrangThaiHomNay = "cho_xu_ly_chan_doan";

// KHÔNG đóng lượt khám, hàng đợi ở đây
// Lượt khám vẫn: dang_kham
// Hàng đợi vẫn: dang_thuc_hien
```

---

### 3. Thêm method mới: ProcessDiagnosisAsync()

```csharp
/// <summary>
/// Xử lý chẩn đoán: kiểm tra và cập nhật các bước xử lý sau chẩn đoán
/// </summary>
public async Task<DiagnosisProcessDto> ProcessDiagnosisAsync(
    string maPhieuKham,
    DiagnosisProcessRequest request)
{
    var phieu = await _db.PhieuKhamLamSangs
        .Include(p => p.PhieuChanDoanCuoi)
        .Include(p => p.PhieuKhamCanLamSang)
        .Include(p => p.BenhNhan)
        .FirstOrDefaultAsync(p => p.MaPhieuKham == maPhieuKham)
        ?? throw new InvalidOperationException("Không tìm thấy phiếu khám");

    if (phieu.TrangThai != "da_lap_chan_doan")
        throw new InvalidOperationException("Phiếu khám chưa có chẩn đoán");

    var processStatus = new DiagnosisProcessDto
    {
        MaPhieuKham = maPhieuKham,
        // Kiểm tra các bước xử lý
        CoClsChuaHoanTat = await CheckClsPendingAsync(phieu),
        CoDonThuocChuaLay = await CheckPrescriptionPendingAsync(phieu),
        CoThanhToanChuaXong = await CheckBillingPendingAsync(phieu),
        CoTaiKham = request.CoTaiKham ?? false
    };

    // Cập nhật trạng thái xử lý (nếu cần)
    // Nhưng vẫn giữ phiếu ở da_lap_chan_doan

    return processStatus;
}
```

---

### 4. Thêm method mới: CompleteExamAsync()

```csharp
/// <summary>
/// Hoàn tất phiếu khám: chỉ gọi khi đã xử lý xong tất cả
/// </summary>
public async Task<ClinicalExamDto> CompleteExamAsync(
    string maPhieuKham,
    CompleteExamRequest request)
{
    using var transaction = await _db.Database.BeginTransactionAsync();
    try
    {
        var phieu = await _db.PhieuKhamLamSangs
            .Include(p => p.BenhNhan)
            .Include(p => p.HangDois)
                .ThenInclude(h => h.LuotKhamBenh)
            .FirstOrDefaultAsync(p => p.MaPhieuKham == maPhieuKham)
            ?? throw new InvalidOperationException("Không tìm thấy phiếu khám");

        if (phieu.TrangThai != "da_lap_chan_doan")
            throw new InvalidOperationException("Phiếu khám chưa có chẩn đoán hoặc đã hoàn tất");

        // Kiểm tra các bước xử lý đã xong chưa
        var hasPendingCls = await CheckClsPendingAsync(phieu);
        var hasPendingPrescription = await CheckPrescriptionPendingAsync(phieu);
        var hasPendingBilling = await CheckBillingPendingAsync(phieu);

        if (hasPendingCls && !request.ForceComplete)
            throw new InvalidOperationException("Còn dịch vụ CLS chưa hoàn tất");

        if (hasPendingPrescription && !request.ForceComplete)
            throw new InvalidOperationException("Còn đơn thuốc chưa lấy");

        if (hasPendingBilling && !request.ForceComplete)
            throw new InvalidOperationException("Còn thanh toán chưa xong");

        // Bây giờ mới đóng tất cả
        var hangDoi = phieu.HangDois;
        var luot = hangDoi?.LuotKhamBenh;

        if (luot is not null)
        {
            luot.TrangThai = "hoan_tat";
            luot.ThoiGianKetThuc = DateTime.Now;
        }

        if (hangDoi is not null)
        {
            hangDoi.TrangThai = "da_phuc_vu";
            await _queue.CapNhatTrangThaiHangDoiAsync(
                hangDoi.MaHangDoi,
                new QueueStatusUpdateRequest { TrangThai = "da_phuc_vu" });
        }

        await CapNhatTrangThaiPhieuKhamAsync(
            phieu.MaPhieuKham,
            new ClinicalExamStatusUpdateRequest { TrangThai = "da_hoan_tat" });

        phieu.BenhNhan.TrangThaiHomNay = null; // hoặc "da_xu_ly_xong"

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        var dto = await LayPhieuKhamAsync(maPhieuKham);
        await _realtime.BroadcastClinicalExamUpdatedAsync(dto);
        
        return dto;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

private async Task<bool> CheckClsPendingAsync(PhieuKhamLamSang phieu)
{
    if (phieu.PhieuKhamCanLamSang == null) return false;
    
    var cls = phieu.PhieuKhamCanLamSang;
    return cls.TrangThai != "da_hoan_tat";
}

private async Task<bool> CheckPrescriptionPendingAsync(PhieuKhamLamSang phieu)
{
    var chanDoan = phieu.PhieuChanDoanCuoi;
    if (chanDoan?.MaDonThuoc == null) return false;
    
    var donThuoc = await _db.DonThuocs
        .FirstOrDefaultAsync(d => d.MaDonThuoc == chanDoan.MaDonThuoc);
    
    return donThuoc?.TrangThai != "da_lay";
}

private async Task<bool> CheckBillingPendingAsync(PhieuKhamLamSang phieu)
{
    // Kiểm tra có hóa đơn chưa thanh toán không
    var hoaDon = await _db.HoaDonThanhToans
        .FirstOrDefaultAsync(h => h.MaPhieuKham == phieu.MaPhieuKham);
    
    return hoaDon?.TrangThai != "da_thanh_toan";
}
```

---

### 5. Thêm DTOs mới

```csharp
// DTOs/ClinicalDtos.cs

public record class DiagnosisProcessDto
{
    public string MaPhieuKham { get; set; } = default!;
    public bool CoClsChuaHoanTat { get; set; }
    public bool CoDonThuocChuaLay { get; set; }
    public bool CoThanhToanChuaXong { get; set; }
    public bool CoTaiKham { get; set; }
    public bool CoTheHoanTat => !CoClsChuaHoanTat && !CoDonThuocChuaLay && !CoThanhToanChuaXong;
}

public record class DiagnosisProcessRequest
{
    public string MaPhieuKham { get; set; } = default!;
    public bool? CoTaiKham { get; set; }
}

public record class CompleteExamRequest
{
    public string MaPhieuKham { get; set; } = default!;
    public bool ForceComplete { get; set; } = false; // Cho phép hoàn tất dù còn pending
    public string? GhiChu { get; set; }
}
```

---

### 6. Thêm Controllers mới

```csharp
// Controllers/ClinicalController.cs

[HttpPost("{maPhieuKham}/process-diagnosis")]
[Authorize]
[RequireRole("bac_si", "y_ta")]
public async Task<ActionResult<DiagnosisProcessDto>> ProcessDiagnosis(
    string maPhieuKham,
    [FromBody] DiagnosisProcessRequest request)
{
    request.MaPhieuKham = maPhieuKham;
    var result = await _service.ProcessDiagnosisAsync(maPhieuKham, request);
    return Ok(result);
}

[HttpPost("{maPhieuKham}/complete")]
[Authorize]
[RequireRole("bac_si", "y_ta")]
public async Task<ActionResult<ClinicalExamDto>> CompleteExam(
    string maPhieuKham,
    [FromBody] CompleteExamRequest request)
{
    request.MaPhieuKham = maPhieuKham;
    var result = await _service.CompleteExamAsync(maPhieuKham, request);
    return Ok(result);
}
```

---

## 📊 SO SÁNH

| Bước | Luồng CŨ (SAI) | Luồng MỚI (ĐÚNG) |
|------|----------------|------------------|
| **Lập chẩn đoán** | Đóng tất cả ngay | Chỉ lưu chẩn đoán, chuyển sang `da_lap_chan_doan` |
| **Xử lý chẩn đoán** | ❌ Không có | ✅ Có endpoint riêng để xử lý |
| **Hoàn tất** | Tự động khi lập chẩn đoán | Chỉ khi gọi `CompleteExam` |
| **Theo dõi** | ❌ Không biết cần xử lý gì | ✅ Rõ ràng từng bước |

---

## 🎯 LỢI ÍCH

1. ✅ **Rõ ràng luồng**: Tách biệt rõ 3 bước
2. ✅ **Theo dõi được**: Biết bệnh nhân đang ở bước nào
3. ✅ **Linh hoạt**: Có thể xử lý từng bước riêng
4. ✅ **Kiểm soát**: Chỉ hoàn tất khi thực sự xong
5. ✅ **Báo cáo**: Dễ dàng báo cáo số lượng đang xử lý

---

## 📝 CHECKLIST TRIỂN KHAI

- [ ] 1. Thêm trạng thái `da_lap_chan_doan` vào entity `PhieuKhamLamSang`
- [ ] 2. Thêm trạng thái `cho_xu_ly_chan_doan` vào entity `BenhNhan`
- [ ] 3. Sửa `TaoChanDoanCuoiAsync()` - không đóng lượt/queue
- [ ] 4. Thêm `ProcessDiagnosisAsync()` - kiểm tra các bước xử lý
- [ ] 5. Thêm `CompleteExamAsync()` - hoàn tất phiếu khám
- [ ] 6. Thêm DTOs mới: `DiagnosisProcessDto`, `CompleteExamRequest`
- [ ] 7. Thêm endpoints mới vào `ClinicalController`
- [ ] 8. Cập nhật Frontend để gọi các endpoint mới
- [ ] 9. Migration database (nếu cần)
- [ ] 10. Test toàn bộ luồng

---

## ⚠️ LƯU Ý

1. **Backward compatibility**: Cần xử lý các phiếu khám cũ đang ở trạng thái `da_hoan_tat` nhưng chưa có chẩn đoán
2. **Migration**: Có thể cần migration script để chuyển đổi dữ liệu cũ
3. **Frontend**: Cần cập nhật UI để hiển thị đúng các bước xử lý

