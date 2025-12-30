# KẾ HOẠCH CHUẨN HÓA FLOW: XUẤT CHẨN ĐOÁN → XỬ LÝ → HOÀN TẤT & THU PHÍ

## 📋 MỤC TIÊU

1. ✅ Lưu `maPhieuKham` ở đâu để có thể tìm lại khi click "Xử lý & chẩn đoán"
2. ✅ Tự động fetch chẩn đoán khi mở modal process
3. ✅ Hoàn tất flow: Xử lý chẩn đoán → Hoàn tất phiếu khám → Thu phí
4. ✅ Đảm bảo: 1 bệnh nhân chỉ có 1 phiếu LS đang hoạt động

---

## 🔑 QUY TẮC QUAN TRỌNG

**Từ backend (ClinicalService.cs dòng 195-206):**
```csharp
// Rule: 1 bệnh nhân chỉ 1 phiếu LS đang hoạt động
var existingActive = await _db.PhieuKhamLamSangs
    .FirstOrDefaultAsync(p =>
        p.MaBenhNhan == request.MaBenhNhan &&
        p.TrangThai != "da_hoan_tat" &&
        p.TrangThai != "da_huy");
```

**Trạng thái phiếu khám:**
- `da_lap`: Đã lập
- `dang_kham`: Đang khám
- `da_lap_chan_doan`: **Đã lập chẩn đoán, chờ xử lý** (MỚI - cần thêm)
- `da_hoan_tat`: Đã hoàn tất
- `da_huy`: Đã hủy

**Trạng thái bệnh nhân khi cần xử lý chẩn đoán:**
- `cho_xu_ly`: Chờ xử lý (đã có chẩn đoán)

---

## 📊 LUỒNG CHUẨN

### **BƯỚC 1: Bác sĩ xuất chẩn đoán (trong Examination.jsx)**

**Hiện tại:**
- API: `POST /api/clinical/final-diagnosis`
- Service: `TaoChanDoanCuoiAsync()`
- Đang: Đóng tất cả ngay (SAI)

**Cần sửa:**
```csharp
// ClinicalService.TaoChanDoanCuoiAsync()
// CHỈ lưu chẩn đoán, KHÔNG đóng phiếu
await CapNhatTrangThaiPhieuKhamAsync(
    phieu.MaPhieuKham,
    new ClinicalExamStatusUpdateRequest { TrangThai = "da_lap_chan_doan" }); // MỚI

phieu.BenhNhan.TrangThaiHomNay = "cho_xu_ly";

// KHÔNG đóng lượt khám, hàng đợi ở đây
```

**Kết quả:**
- ✅ Chẩn đoán được lưu vào `PhieuChanDoanCuoi`
- ✅ Phiếu khám → `da_lap_chan_doan`
- ✅ Bệnh nhân → `cho_xu_ly`
- ✅ Lượt khám vẫn `dang_kham`
- ✅ Hàng đợi vẫn `dang_thuc_hien`

---

### **BƯỚC 2: Click "Xử lý & chẩn đoán" (trong PatientsTable.jsx)**

**Vấn đề hiện tại:**
- ❌ Không biết lưu `maPhieuKham` ở đâu
- ❌ Tìm phiếu với `TrangThai: "dang_thuc_hien"` → có thể không tìm thấy

**Giải pháp:**

#### **Option A: Tìm phiếu khám đang hoạt động khi mở modal (KHUYẾN NGHỊ)**

```javascript
// File: src/routes/Patients.jsx
// Sửa handleAction("process")

if (type === "process") {
  // Tìm phiếu khám ĐANG HOẠT ĐỘNG (không filter trạng thái, chỉ loại trừ đã hoàn tất/hủy)
  let patientWithExam = { ...p };
  
  if (pid) {
    try {
      // Tìm phiếu khám đang hoạt động (theo rule: không "da_hoan_tat" và không "da_huy")
      // Có thể tìm theo:
      // 1. Không filter trạng thái (lấy mới nhất)
      // 2. Hoặc filter: ["da_lap", "dang_kham", "da_lap_chan_doan"]
      
      const clinicalList = await searchClinicalRaw({
        MaBenhNhan: pid,
        // BỎ TrangThai filter - lấy tất cả, BE sẽ trả về theo thứ tự mới nhất
      });

      if (Array.isArray(clinicalList) && clinicalList.length > 0) {
        // Lấy phiếu đầu tiên (mới nhất)
        const latestClinical = clinicalList[0];
        const maPhieuKham = 
          latestClinical?.MaPhieuKham ||
          latestClinical?.maPhieuKham ||
          latestClinical?.id ||
          null;

        if (maPhieuKham) {
          // ✅ LƯU maPhieuKham vào patient object
          patientWithExam = {
            ...patientWithExam,
            MaPhieuKham: maPhieuKham,
            maPhieuKham: maPhieuKham,
            MaPhieuKhamLs: maPhieuKham,
            maPhieuKhamLs: maPhieuKham,
          };
        }
      }
    } catch (err) {
      console.error("Lỗi khi tìm phiếu khám:", err);
      toast.warn("Không thể tải thông tin phiếu khám. Vui lòng thử lại.");
    }
  }

  // ✅ Mở modal với patient đã có maPhieuKham
  setModal({ open: true, mode: "process", patient: patientWithExam });
  return;
}
```

**Lý do chọn Option A:**
- ✅ Đơn giản, không cần thay đổi backend
- ✅ Luôn lấy phiếu mới nhất đang hoạt động
- ✅ Không cần lưu vào database/state phức tạp

---

### **BƯỚC 3: Tự động fetch chẩn đoán khi mở modal (PatientModal.jsx)**

```javascript
// File: src/components/patients/PatientModal.jsx
// Sửa useEffect khi mode === "process"

useEffect(() => {
  // ... existing code ...
  
  if (patient && mode === "process") {
    setDiagnosisData((prev) => prev ?? DIAG_INIT);
    // ... existing svcResults code ...
    
    // ✅ TỰ ĐỘNG FETCH nếu có maPhieuKham
    const maPhieuKham = 
      patient?.MaPhieuKham ||
      patient?.maPhieuKham ||
      patient?.MaPhieuKhamLs ||
      patient?.maPhieuKhamLs ||
      form?.MaPhieuKham ||
      form?.maPhieuKham ||
      null;
    
    if (isWaitingProcess && maPhieuKham) {
      // ✅ GỌI NGAY khi mở modal
      fetchFinalDiagnosis();
    } else if (isWaitingProcess && !maPhieuKham) {
      // Nếu không có maPhieuKham, thử tìm lại
      (async () => {
        const pid = patient?.id || patient?.pid || patient?.MaBenhNhan || patient?.maBenhNhan;
        if (!pid) return;
        
        try {
          const clinicalList = await searchClinicalRaw({
            MaBenhNhan: pid,
            // Không filter trạng thái
          });
          
          if (Array.isArray(clinicalList) && clinicalList.length > 0) {
            const latestClinical = clinicalList[0];
            const maPhieuKham = 
              latestClinical?.MaPhieuKham ||
              latestClinical?.maPhieuKham ||
              null;
            
            if (maPhieuKham) {
              // Lưu vào patient object để fetchFinalDiagnosis dùng
              setForm(prev => ({
                ...prev,
                MaPhieuKham: maPhieuKham,
                maPhieuKham: maPhieuKham,
              }));
              
              // Gọi lại fetchFinalDiagnosis sau khi có maPhieuKham
              // (cần dùng ref hoặc state để trigger)
              setTimeout(() => {
                fetchFinalDiagnosis();
              }, 100);
            }
          }
        } catch (err) {
          console.error("Lỗi khi tìm phiếu khám tự động:", err);
        }
      })();
    }
  }
}, [open, patient, patientForView, mode, today, isDirty, isWaitingProcess]);
```

---

### **BƯỚC 4: Hoàn tất & Thu phí (PatientModal.jsx → handleFinishDoctor)**

**Hiện tại (SAI):**
```javascript
async function handleFinishDoctor() {
  // Chỉ cập nhật status bệnh nhân → DONE
  onMutatePatient?.(pid, { status: STATUSES.DONE });
  // KHÔNG gọi API hoàn tất phiếu khám
}
```

**Cần sửa:**

```javascript
// File: src/components/patients/PatientModal.jsx
// Sửa handleFinishDoctor()

async function handleFinishDoctor() {
  const pid = form?.id;
  if (!pid) {
    toast.error("Thiếu mã bệnh nhân.");
    return;
  }
  
  // ✅ 1. Lấy maPhieuKham
  const maPhieuKham = 
    diagnosisData?.MaPhieuKham ||
    diagnosisData?.maPhieuKham ||
    form?.MaPhieuKham ||
    form?.maPhieuKham ||
    patient?.MaPhieuKham ||
    patient?.maPhieuKham ||
    null;
  
  if (!maPhieuKham) {
    toast.error("Thiếu mã phiếu khám. Không thể hoàn tất.");
    return;
  }
  
  try {
    // ✅ 2. Gọi API hoàn tất phiếu khám
    // API: POST /api/clinical/{maPhieuKham}/complete
    // Payload: CompleteExamRequest { ForceComplete: false }
    
    await completeExam(maPhieuKham, {
      ForceComplete: false, // Không force, kiểm tra đầy đủ các bước
      GhiChu: "Hoàn tất từ tab xử lý chẩn đoán",
    });
    
    toast.success("Đã hoàn tất phiếu khám.");
    
    // ✅ 3. Cập nhật trạng thái bệnh nhân → DONE
    await onMutatePatient?.(pid, { status: STATUSES.DONE });
    
    // ✅ 4. Nếu có tái khám, tạo lịch hẹn
    const d = diagnosisData || {};
    if (/tái khám/i.test(d.followup || "")) {
      const date = (d.followupDate || "").slice(0, 10);
      const time = d.followupTime || "";
      if (date) {
        await createFollowupHold({
          pid,
          patient: form?.name || pid,
          date,
          time,
          dept: booking.dept || exam.dept || "",
          doctor: booking.doctor || "",
          note: d.advice || "Hẹn tái khám",
        });
        await onMutatePatient?.(pid, { status: STATUSES.SCHEDULED_FUP });
      }
    }
    
    // ✅ 5. Đóng modal
    onClose?.();
    
    // ✅ 6. Refresh danh sách
    qc.invalidateQueries({ queryKey: ["patients"] });
    qc.invalidateQueries({ queryKey: ["queue"] });
    
  } catch (err) {
    const msg =
      err?.response?.data?.message ||
      err?.response?.data?.Message ||
      err?.message ||
      "Không thể hoàn tất phiếu khám. Vui lòng thử lại.";
    toast.error(msg);
    console.error("Lỗi khi hoàn tất phiếu khám:", err);
  }
}
```

**Thêm API mới:**

```javascript
// File: src/api/examination.js

// Hoàn tất phiếu khám
export async function completeExam(maPhieuKham, payload = {}) {
  const res = await http.post(
    `${CLINICAL_BASE}/${maPhieuKham}/complete`,
    payload
  );
  return unwrap(res);
}

// Hook
export function useCompleteExam() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ maPhieuKham, ...payload }) => 
      completeExam(maPhieuKham, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["clinical"] });
      qc.invalidateQueries({ queryKey: ["queue"] });
      qc.invalidateQueries({ queryKey: ["patients"] });
    },
  });
}
```

---

### **BƯỚC 5: Backend - Thêm endpoint CompleteExam**

**Cần thêm vào ClinicalController:**

```csharp
[HttpPost("{maPhieuKham}/complete")]
[Authorize]
[RequireRole("bac_si", "y_ta")]
public async Task<ActionResult<ClinicalExamDto>> CompleteExam(
    string maPhieuKham,
    [FromBody] CompleteExamRequest request)
{
    var result = await _service.CompleteExamAsync(maPhieuKham, request);
    return Ok(result);
}
```

**Cần thêm vào IClinicalService & ClinicalService:**

```csharp
// Interface
Task<ClinicalExamDto> CompleteExamAsync(string maPhieuKham, CompleteExamRequest request);

// Implementation
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

        // Chỉ cho phép hoàn tất nếu đã có chẩn đoán
        if (phieu.TrangThai != "da_lap_chan_doan")
            throw new InvalidOperationException(
                "Phiếu khám chưa có chẩn đoán hoặc đã hoàn tất.");

        // Kiểm tra các bước xử lý đã xong chưa (nếu không force)
        if (!request.ForceComplete)
        {
            var hasPendingCls = await CheckClsPendingAsync(phieu);
            var hasPendingPrescription = await CheckPrescriptionPendingAsync(phieu);
            var hasPendingBilling = await CheckBillingPendingAsync(phieu);

            if (hasPendingCls)
                throw new InvalidOperationException("Còn dịch vụ CLS chưa hoàn tất.");
            
            if (hasPendingPrescription)
                throw new InvalidOperationException("Còn đơn thuốc chưa lấy.");
            
            if (hasPendingBilling)
                throw new InvalidOperationException("Còn thanh toán chưa xong.");
        }

        // Đóng tất cả
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
        
        var dashboard = await _dashboard.LayDashboardHomNayAsync();
        await _realtime.BroadcastDashboardTodayAsync(dashboard);
        
        return dto;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

---

## 📝 CHECKLIST TRIỂN KHAI

### **BACKEND:**

- [ ] 1. Thêm trạng thái `da_lap_chan_doan` vào entity `PhieuKhamLamSang`
- [ ] 2. Sửa `TaoChanDoanCuoiAsync()` - chỉ lưu chẩn đoán, không đóng phiếu
- [ ] 3. Thêm DTO `CompleteExamRequest`
- [ ] 4. Thêm method `CompleteExamAsync()` vào `IClinicalService` & `ClinicalService`
- [ ] 5. Thêm endpoint `POST /api/clinical/{maPhieuKham}/complete` vào `ClinicalController`
- [ ] 6. Migration database (nếu cần)

### **FRONTEND:**

- [ ] 7. Sửa `Patients.jsx` - `handleAction("process")`: Tìm phiếu khám đang hoạt động, lưu vào patient object
- [ ] 8. Sửa `PatientModal.jsx` - Tự động fetch chẩn đoán khi mở modal process
- [ ] 9. Thêm API `completeExam()` vào `examination.js`
- [ ] 10. Thêm hook `useCompleteExam()` vào `examination.js`
- [ ] 11. Sửa `handleFinishDoctor()` - Gọi API complete exam trước khi cập nhật status
- [ ] 12. Test toàn bộ flow

---

## 🎯 KẾT QUẢ MONG ĐỢI

### **Luồng hoàn chỉnh:**

```
1. Bác sĩ xuất chẩn đoán
   ↓
   [Lưu chẩn đoán]
   ✅ Phiếu khám → da_lap_chan_doan
   ✅ Bệnh nhân → cho_xu_ly
   ✅ Lượt khám → vẫn dang_kham
   ✅ Hàng đợi → vẫn dang_thuc_hien

2. Click "Xử lý & chẩn đoán"
   ↓
   [Tìm phiếu khám đang hoạt động]
   ✅ Lưu maPhieuKham vào patient object
   ✅ Mở modal process

3. Modal mở → Tự động fetch chẩn đoán
   ↓
   [GET /api/clinical/{maPhieuKham}/final-diagnosis]
   ✅ Hiển thị chẩn đoán + đơn thuốc

4. Click "Hoàn tất & thu phí"
   ↓
   [POST /api/clinical/{maPhieuKham}/complete]
   ✅ Đóng phiếu khám → da_hoan_tat
   ✅ Đóng lượt khám → hoan_tat
   ✅ Đóng hàng đợi → da_phuc_vu
   ✅ Bệnh nhân → DONE (hoặc SCHEDULED_FUP nếu tái khám)

5. Thu phí (nếu có đơn thuốc)
   ↓
   [Tự động tạo hóa đơn bởi BE khi complete]
   ✅ Hoàn tất
```

---

## ⚠️ LƯU Ý

1. **1 bệnh nhân chỉ có 1 phiếu LS đang hoạt động**: Rule này đã có trong BE, cần đảm bảo FE không tạo nhiều phiếu
2. **Tìm phiếu khám**: Nên tìm không filter trạng thái, lấy mới nhất (BE sẽ trả về theo thứ tự)
3. **Tái khám**: Khi hoàn tất, nếu có chỉ định tái khám thì tạo lịch hẹn và cập nhật status → `SCHEDULED_FUP`
4. **Thu phí**: Nếu có đơn thuốc, BE sẽ tự động tạo hóa đơn khi complete (hoặc có thể cần gọi riêng)

