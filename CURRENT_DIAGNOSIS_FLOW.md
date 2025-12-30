# LUỒNG HIỆN TẠI: KHI BÁC SĨ CLICK "XUẤT CHẨN ĐOÁN"

## 📍 ĐIỂM BẮT ĐẦU

**Frontend:** `src/components/exam/ExamDetail.jsx`
- Button: **"Xuất phiếu chẩn đoán"** (dòng 963)
- Handler: `handleExportDiagnosisLS()` (dòng 313)

---

## 🔄 LUỒNG CHI TIẾT

### **BƯỚC 1: Frontend - ExamDetail.jsx**

```javascript
// File: src/components/exam/ExamDetail.jsx
// Dòng: 313-346

async function handleExportDiagnosisLS() {
  // 1. Validate: phải có chẩn đoán
  if (!hasDx) return;
  
  // 2. Validate: không cho chọn đồng thời "Cho về" + "Tái khám"
  if (dxFlags.choVe && dxFlags.taiKham) {
    setDxFlagError('Không thể chọn đồng thời "Cho về" và "Tái khám".');
    return;
  }
  
  // 3. Build payload từ form data
  const payload = buildPayloadCommon();
  payload.services = payload.orderRows.map((r) => r.id);
  
  // 4. Gọi callback từ parent (Examination.jsx)
  if (onExportDiagnosis) {
    await onExportDiagnosis(patient, {
      dx: payload.dx,        // Chẩn đoán
      rxRows: payload.rxRows, // Đơn thuốc
      services: payload.services, // Dịch vụ CLS
    });
    return;
  }
  
  // Fallback: gọi trực tiếp mutation (nếu không có callback)
  await dxMut.mutateAsync({...});
}
```

**Payload gửi đi:**
```javascript
{
  dx: {
    pre: "Chẩn đoán sơ bộ",
    final: "Chẩn đoán xác định",
    plan: "Phác đồ điều trị",
    advice: "Lời khuyên",
    note: "Nội dung khám",
    flags: {
      choVe: true/false,
      choThuocVe: true/false,
      taiKham: true/false
    }
  },
  rxRows: [
    {
      code: "MaThuoc",
      name: "Tên thuốc",
      dose: "Liều dùng",
      qty: 10,
      price: 50000
    }
  ],
  services: ["MaDichVu1", "MaDichVu2"]
}
```

---

### **BƯỚC 2: Frontend - Examination.jsx**

```javascript
// File: src/routes/Examination.jsx
// Dòng: 556-629

async function handleExportDiagnosis(patient, payload) {
  // 1. Lấy mã phiếu khám
  const maPhieuKham = patient?.MaPhieuKham || ...;
  
  // 2. Map đơn thuốc từ rxRows
  const donThuoc = (payload?.rxRows || []).map((r) => ({
    MaThuoc: r.code,
    SoLuong: r.qty,
    ChiDinhSuDung: r.dose,
    ThanhTien: r.price * r.qty,
  }));
  
  // 3. Map hướng xử trí từ flags
  const huongXuTriArr = [];
  if (flags.choVe) huongXuTriArr.push("Cho về");
  if (flags.choThuocVe) huongXuTriArr.push("Cho thuốc về");
  if (flags.taiKham) huongXuTriArr.push("Tái khám");
  const huongXuTri = huongXuTriArr.join("; ");
  
  // 4. Build final payload theo FinalDiagnosisCreateRequest
  const finalPayload = {
    MaPhieuKham: maPhieuKham,
    MaLuotKham: patient?.MaLuotKham || ...,
    MaHangDoi: patient?.MaHangDoi || ...,
    TrangThaiLuot: "hoan_tat",  // ⚠️ Đóng lượt ngay
    ThoiGianKetThuc: new Date().toISOString(),
    MaDonThuoc: null,  // Sẽ được tạo ở backend
    MaBacSiKeDon: patient?.MaBacSiKham || ...,
    ChanDoanSoBo: payload?.dx?.pre || "",
    ChanDoanCuoi: payload?.dx?.final || "",
    NoiDungKham: payload?.dx?.note || "",
    HuongXuTri: huongXuTri,
    LoiKhuyen: payload?.dx?.advice || "",
    PhatDoDieuTri: payload?.dx?.plan || "",
    DonThuoc: donThuoc,  // Danh sách thuốc
  };
  
  // 5. Gọi API
  await dxMut.mutateAsync(finalPayload);
  
  // 6. Cleanup UI
  setInProgress((prev) => {
    const s = new Set(prev);
    s.delete(key);
    return s;
  });
  setActive(null);  // Đóng màn hình chi tiết
}
```

---

### **BƯỚC 3: Frontend API Call**

```javascript
// File: src/api/examination.js
// Dòng: 246-250, 633-649

// Function: upsertFinalDiagnosis()
export async function upsertFinalDiagnosis(payload) {
  const res = await http.post(
    `${CLINICAL_BASE}/final-diagnosis`,  // POST /api/clinical/final-diagnosis
    payload
  );
  return unwrap(res);
}

// Hook: useCreateDiagnosis()
export function useCreateDiagnosis(options = {}) {
  return useMutation({
    mutationFn: async (payload) => upsertFinalDiagnosis(payload),
    onSuccess: (data, vars, ctx) => {
      // Invalidate queries để refresh UI
      qc.invalidateQueries({ queryKey: ["queue"] });
      qc.invalidateQueries({ queryKey: ["visits"] });
      qc.invalidateQueries({ queryKey: ["patients"] });
    },
  });
}
```

**API Endpoint:** 
```
POST /api/clinical/final-diagnosis
```

**Request Body (FinalDiagnosisCreateRequest):**
```json
{
  "MaPhieuKham": "PKLS-xxx",
  "MaLuotKham": "MLK-xxx",
  "MaHangDoi": "HD-xxx",
  "TrangThaiLuot": "hoan_tat",
  "ThoiGianKetThuc": "2025-01-15T10:30:00Z",
  "MaBacSiKeDon": "BS-001",
  "ChanDoanSoBo": "...",
  "ChanDoanCuoi": "...",
  "NoiDungKham": "...",
  "HuongXuTri": "Cho về; Cho thuốc về",
  "LoiKhuyen": "...",
  "PhatDoDieuTri": "...",
  "DonThuoc": [
    {
      "MaThuoc": "T-001",
      "SoLuong": 10,
      "ChiDinhSuDung": "1v x 2 lần/ngày",
      "ThanhTien": 500000
    }
  ]
}
```

---

### **BƯỚC 4: Backend Controller**

```csharp
// File: Controllers/ClinicalController.cs
// Dòng: 56-62

[HttpPost("final-diagnosis")]
[Authorize]
[RequireRole("bac_si")]
public async Task<ActionResult<FinalDiagnosisDto>> TaoHoacCapNhatChanDoan(
    [FromBody] FinalDiagnosisCreateRequest request)
{
    var result = await _service.TaoChanDoanCuoiAsync(request);
    return Ok(result);
}
```

---

### **BƯỚC 5: Backend Service - TaoChanDoanCuoiAsync()**

```csharp
// File: Services/OutpatientCare/ClinicalService.cs
// Dòng: 490-624

public async Task<FinalDiagnosisDto> TaoChanDoanCuoiAsync(
    FinalDiagnosisCreateRequest request)
{
    // ========== TRANSACTION BẮT ĐẦU ==========
    using var transaction = await _db.Database.BeginTransactionAsync();
    
    try
    {
        // 1. Load phiếu khám + các entity liên quan
        var phieu = await _db.PhieuKhamLamSangs
            .Include(p => p.BenhNhan)
            .Include(p => p.HangDois)
                .ThenInclude(h => h.LuotKhamBenh)
            .FirstOrDefaultAsync(p => p.MaPhieuKham == request.MaPhieuKham);
        
        var hangDoi = phieu.HangDois;
        var luot = hangDoi?.LuotKhamBenh;
        var maBenhNhan = phieu.MaBenhNhan;
        
        // 2. Tạo hoặc cập nhật PhieuChanDoanCuoi
        var chanDoan = await _db.PhieuChanDoanCuois
            .FirstOrDefaultAsync(c => c.MaPhieuKham == request.MaPhieuKham);
        
        if (chanDoan is null)
        {
            chanDoan = new PhieuChanDoanCuoi
            {
                MaPhieuChanDoan = $"PCD-{Guid.NewGuid():N}",
                MaPhieuKham = request.MaPhieuKham
            };
            _db.PhieuChanDoanCuois.Add(chanDoan);
        }
        
        // 3. Lưu thông tin chẩn đoán
        chanDoan.ChanDoanSoBo = request.ChanDoanSoBo;
        chanDoan.ChanDoanCuoi = request.ChanDoanCuoi;
        chanDoan.NoiDungKham = request.NoiDungKham;
        chanDoan.HuongXuTri = request.HuongXuTri;
        chanDoan.LoiKhuyen = request.LoiKhuyen;
        chanDoan.PhatDoDieuTri = request.PhatDoDieuTri;
        
        // ========== ⚠️ ĐÓNG TẤT CẢ NGAY LẬP TỨC ==========
        
        // 4. Đóng lượt khám
        if (luot is not null)
        {
            luot.TrangThai = "hoan_tat";  // ⚠️
            luot.ThoiGianKetThuc = request.ThoiGianKetThuc ?? DateTime.Now;
        }
        
        // 5. Đóng hàng đợi
        if (hangDoi is not null)
        {
            hangDoi.TrangThai = "da_phuc_vu";  // ⚠️
            await _queue.CapNhatTrangThaiHangDoiAsync(
                hangDoi.MaHangDoi,
                new QueueStatusUpdateRequest { TrangThai = "da_phuc_vu" });
        }
        
        // 6. Đóng phiếu khám
        await CapNhatTrangThaiPhieuKhamAsync(
            phieu.MaPhieuKham,
            new ClinicalExamStatusUpdateRequest { TrangThai = "da_hoan_tat" });  // ⚠️
        
        // 7. Cập nhật trạng thái bệnh nhân
        phieu.BenhNhan.TrangThaiHomNay = "cho_xu_ly";  // ⚠️
        
        await _db.SaveChangesAsync();
        
        // 8. Tạo đơn thuốc (nếu có)
        PrescriptionDto? donThuocDto = null;
        if (request.DonThuoc is not null && request.DonThuoc.Count > 0)
        {
            var prescriptionReq = new PrescriptionCreateRequest
            {
                MaBenhNhan = maBenhNhan,
                MaBacSiKeDon = request.MaBacSiKeDon ?? luot?.MaNhanSuThucHien ?? phieu.MaBacSiKham,
                MaPhieuChanDoanCuoi = chanDoan.MaPhieuChanDoan,
                TongTienDon = 0m,
                Items = request.DonThuoc
            };
            
            donThuocDto = await _pharmacy.TaoDonThuocAsync(prescriptionReq);
            chanDoan.MaDonThuoc = donThuocDto.MaDonThuoc;
            await _db.SaveChangesAsync();
        }
        
        // ========== COMMIT TRANSACTION ==========
        await transaction.CommitAsync();
        
        // ========== BROADCAST REALTIME ==========
        var dto = new FinalDiagnosisDto { ... };
        
        // 9. Broadcast chẩn đoán đã thay đổi
        await _realtime.BroadcastFinalDiagnosisChangedAsync(dto);
        
        // 10. Cập nhật trạng thái bệnh nhân (lần nữa)
        await _patients.CapNhatTrangThaiBenhNhanAsync(maBenhNhan, new PatientStatusUpdateRequest
        {
            TrangThaiHomNay = "cho_xu_ly"
        });
        
        // 11. Broadcast dashboard
        var dashboard = await _dashboard.LayDashboardHomNayAsync();
        await _realtime.BroadcastDashboardTodayAsync(dashboard);
        
        // 12. Tạo thông báo
        await TaoThongBaoPhieuChuanDoanAsync(dto, phieu);
        
        return dto;
    }
    catch (Exception)
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

---

## 📊 TÓM TẮT NHỮNG GÌ ĐANG XẢY RA

### ✅ **NHỮNG VIỆC ĐÚNG:**
1. ✅ Lưu chẩn đoán vào `PhieuChanDoanCuoi`
2. ✅ Tạo đơn thuốc (nếu có)
3. ✅ Broadcast realtime để cập nhật UI
4. ✅ Tạo thông báo cho y tá
5. ✅ Cập nhật dashboard

### ⚠️ **NHỮNG VIỆC SAI (VẤN ĐỀ):**
1. ❌ **Đóng lượt khám ngay** (`hoan_tat`) - Nên giữ `dang_kham` cho đến khi hoàn tất
2. ❌ **Đóng hàng đợi ngay** (`da_phuc_vu`) - Nên giữ `dang_thuc_hien` cho đến khi hoàn tất
3. ❌ **Đóng phiếu khám ngay** (`da_hoan_tat`) - Nên chuyển sang `da_lap_chan_doan` trước
4. ❌ **Bệnh nhân → `cho_xu_ly`** - Không rõ cần xử lý gì, nên là `cho_xu_ly_chan_doan`

### 🔄 **LUỒNG HIỆN TẠI (SAI):**
```
Lập chẩn đoán 
  ↓
[NGAY LẬP TỨC]
  ├─ Đóng lượt khám ❌
  ├─ Đóng hàng đợi ❌
  ├─ Đóng phiếu khám ❌
  └─ Bệnh nhân → cho_xu_ly ❌
```

### ✅ **LUỒNG ĐÚNG (ĐỀ XUẤT):**
```
Lập chẩn đoán
  ↓
[CHỈ LƯU CHẨN ĐOÁN]
  ├─ Phiếu khám → da_lap_chan_doan ✅
  ├─ Lượt khám → vẫn dang_kham ✅
  ├─ Hàng đợi → vẫn dang_thuc_hien ✅
  └─ Bệnh nhân → cho_xu_ly_chan_doan ✅
  ↓
Xử lý chẩn đoán (riêng)
  ├─ CLS (nếu có)
  ├─ Đơn thuốc (nếu có)
  ├─ Thanh toán (nếu có)
  └─ Tái khám (nếu có)
  ↓
Hoàn tất (riêng)
  ├─ Đóng lượt khám ✅
  ├─ Đóng hàng đợi ✅
  ├─ Đóng phiếu khám ✅
  └─ Bệnh nhân → da_xu_ly_xong ✅
```

---

## 🎯 KẾT LUẬN

**Khi bác sĩ click "Xuất chẩn đoán":**

1. **API được gọi:** `POST /api/clinical/final-diagnosis`
2. **Service:** `ClinicalService.TaoChanDoanCuoiAsync()`
3. **Đang làm:**
   - ✅ Lưu chẩn đoán
   - ✅ Tạo đơn thuốc (nếu có)
   - ❌ **Đóng tất cả ngay lập tức** (SAI)
   - ✅ Broadcast realtime
   - ✅ Tạo thông báo

**Vấn đề:** Không có bước trung gian "xử lý chẩn đoán", mọi thứ bị đóng ngay khi lập chẩn đoán.

**Giải pháp:** Tách thành 3 bước riêng biệt như đề xuất trong `PROPOSAL_DIAGNOSIS_FLOW.md`

