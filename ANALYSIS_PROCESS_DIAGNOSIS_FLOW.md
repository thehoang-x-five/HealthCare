# PHÂN TÍCH: LUỒNG CLICK "XỬ LÝ & CHẨN ĐOÁN"

## ❓ CÂU HỎI

**Khi click nút "Xử lý & chẩn đoán" (status = `cho_xu_ly`), có đang call API để lấy phiếu chẩn đoán và fill vào form chưa?**

---

## 🔍 PHÂN TÍCH LUỒNG HIỆN TẠI

### **BƯỚC 1: Click nút trong PatientsTable.jsx**

```javascript
// File: src/components/patients/PatientsTable.jsx
// Dòng: 532-539

{accountActive && showProcessBtn && (
  <button
    onClick={() => onAction?.("process", p)}
    className="..."
  >
    Xử lý & chẩn đoán
  </button>
)}
```

**→ Gọi:** `onAction("process", p)` với `p` là patient object từ table

---

### **BƯỚC 2: Patients.jsx - handleAction("process")**

```javascript
// File: src/routes/Patients.jsx
// Dòng: 516-557

if (type === "process") {
  // 1. Mở modal với mode="process"
  setModal({ open: true, mode: "process", patient: p });

  if (pid) {
    try {
      // 2. Tìm phiếu khám đang thực hiện
      const clinicalList = await searchClinicalRaw({
        MaBenhNhan: pid,
        TrangThai: "dang_thuc_hien",  // ⚠️ Tìm với trạng thái "dang_thuc_hien"
      });

      if (Array.isArray(clinicalList) && clinicalList.length > 0) {
        const latestClinical = clinicalList[0];
        const maPhieuKham = 
          latestClinical?.MaPhieuKham ||
          latestClinical?.maPhieuKham ||
          latestClinical?.id ||
          null;

        if (maPhieuKham) {
          // 3. GỌI API getFinalDiagnosis
          try {
            await getFinalDiagnosis(maPhieuKham);  // ⚠️ CHỈ GỌI, KHÔNG LƯU KẾT QUẢ
          } catch (err) {
            console.error("Không lấy được chẩn đoán cuối:", err);
            toast.warn("Không thể tải chẩn đoán cuối. Vui lòng thử lại.");
          }
        }
      }
    } catch (err) {
      console.error("Lỗi khi tìm phiếu khám để lấy chẩn đoán:", err);
      toast.error("Không thể tải thông tin phiếu khám. Vui lòng thử lại.");
    }
  }

  return;
}
```

**⚠️ VẤN ĐỀ 1:**
- Code gọi `getFinalDiagnosis(maPhieuKham)` nhưng **KHÔNG LƯU KẾT QUẢ** vào đâu cả
- Comment nói "Modal sẽ tự xử lý khi cần" → Không chắc modal có tự fetch không

**⚠️ VẤN ĐỀ 2:**
- Tìm phiếu khám với `TrangThai: "dang_thuc_hien"` 
- Nhưng khi bệnh nhân có status `cho_xu_ly`, phiếu khám có thể đã là `da_lap_chan_doan` hoặc `da_hoan_tat` rồi
- → Có thể không tìm thấy phiếu khám

**⚠️ VẤN ĐỀ 3:**
- Không lưu `maPhieuKham` vào `patient` object trước khi mở modal
- → Modal không có `maPhieuKham` để fetch

---

### **BƯỚC 3: PatientModal.jsx - useEffect khi mode === "process"**

```javascript
// File: src/components/patients/PatientModal.jsx
// Dòng: 570-586

if (patient && mode === "process") {
  setDiagnosisData((prev) => prev ?? DIAG_INIT);
  const svcItems = Array.isArray(patient?.serviceOrder?.items)
    ? patient.serviceOrder.items
    : [];
  setSvcResults(
    svcItems.map((s) => ({
      service: s,
      result: "",
      note: "",
      attachments: [],
    }))
  );

  // ⚠️ CHỈ FETCH NẾU: isWaitingProcess && maPhieuKhamCurrent
  if (isWaitingProcess && maPhieuKhamCurrent) {
    fetchFinalDiagnosis();
  }
}
```

**Điều kiện để fetch:**
1. `isWaitingProcess = true`
   ```javascript
   const isWaitingProcess =
     (patientForView?.status || "") === STATUSES.WAIT_PROC ||
     (patientForView?.status || "") === STATUSES.WAIT_PROC_SVC;
   ```

2. `maPhieuKhamCurrent` phải có giá trị
   ```javascript
   const maPhieuKhamCurrent =
     patient?.MaPhieuKham ||
     patient?.maPhieuKham ||
     patient?.MaPhieuKhamLs ||
     patient?.maPhieuKhamLs ||
     form?.MaPhieuKham ||
     form?.maPhieuKham ||
     localStorage.getItem("last-clinical-exam-id") ||  // Fallback từ localStorage
     null;
   ```

**⚠️ VẤN ĐỀ 4:**
- `maPhieuKhamCurrent` được lấy từ `patient` object
- Nhưng trong `handleAction("process")`, không lưu `maPhieuKham` vào `patient` object
- → `maPhieuKhamCurrent` sẽ là `null` (trừ khi có trong localStorage)
- → `fetchFinalDiagnosis()` **KHÔNG BAO GIỜ ĐƯỢC GỌI** (trừ trường hợp đặc biệt)

---

### **BƯỚC 4: fetchFinalDiagnosis() - Nếu được gọi**

```javascript
// File: src/components/patients/PatientModal.jsx
// Dòng: 1266-1305

const fetchFinalDiagnosis = async () => {
  if (loadingFinalDiagnosis) return;
  if (!maPhieuKhamCurrent) {
    toast.error("Thiếu mã phiếu khám.");
    return;
  }
  try {
    setLoadingFinalDiagnosis(true);
    const dxRes = await getFinalDiagnosis(maPhieuKhamCurrent);
    if (!dxRes) {
      toast.error("Không tìm thấy chẩn đoán cuối.");
      return;
    }
    // ✅ FILL VÀO diagnosisData
    setDiagnosisData((prev) => ({
      ...prev,
      MaPhieuChanDoan: dxRes.MaPhieuChanDoan || dxRes.maPhieuChanDoan,
      MaPhieuKham: dxRes.MaPhieuKham || dxRes.maPhieuKham,
      MaDonThuoc: dxRes.MaDonThuoc || dxRes.maDonThuoc,
      dxPrimary: dxRes.ChanDoanSoBo || dxRes.dxPrimary || "",
      dxSecondary: dxRes.ChanDoanCuoi || dxRes.dxSecondary || "",
      summary: dxRes.NoiDungKham || dxRes.summary || "",
      orders: dxRes.PhatDoDieuTri || dxRes.orders || "",
      advice: dxRes.LoiKhuyen || dxRes.advice || "",
      followup: dxRes.HuongXuTri || dxRes.followup || "",
      prescriptionCode: dxRes.MaDonThuoc || dxRes.maDonThuoc || "",
    }));
    toast.success("Đã tải chẩn đoán cuối.");
  } catch (err) {
    // Error handling
  } finally {
    setLoadingFinalDiagnosis(false);
  }
};
```

**✅ Nếu được gọi:** Function này sẽ fill đúng vào `diagnosisData` và hiển thị trong `PatientProcessMode`

---

## 🎯 KẾT LUẬN

### **TRẠNG THÁI HIỆN TẠI: ❌ KHÔNG HOẠT ĐỘNG**

1. ❌ **Không tự động fetch khi mở modal:**
   - `maPhieuKhamCurrent` thường là `null` (vì không lưu vào patient object)
   - Điều kiện `isWaitingProcess && maPhieuKhamCurrent` → `false`
   - → `fetchFinalDiagnosis()` **KHÔNG BAO GIỜ ĐƯỢC GỌI TỰ ĐỘNG**

2. ✅ **Có nút "Tải chẩn đoán cuối" (manual):**
   - User phải click nút này trong `PatientProcessMode` (dòng 215-224)
   - Nút gọi `handleFetchFinalDiagnosis()` → `fetchFinalDiagnosis()`
   - Nhưng vẫn cần `maPhieuKhamCurrent` → có thể fail nếu không có

3. ⚠️ **Logic tìm phiếu khám sai:**
   - Tìm với `TrangThai: "dang_thuc_hien"`
   - Nhưng khi status bệnh nhân = `cho_xu_ly`, phiếu khám có thể đã là `da_lap_chan_doan` hoặc `da_hoan_tat`

---

## 🔧 GIẢI PHÁP ĐỀ XUẤT

### **Option 1: Sửa Patients.jsx - Lưu maPhieuKham vào patient object**

```javascript
// File: src/routes/Patients.jsx
// Sửa handleAction("process")

if (type === "process") {
  if (pid) {
    try {
      // Tìm phiếu khám MỚI NHẤT (không filter theo trạng thái)
      const clinicalList = await searchClinicalRaw({
        MaBenhNhan: pid,
        // Bỏ TrangThai filter, hoặc tìm cả "dang_thuc_hien", "da_lap_chan_doan", "da_hoan_tat"
      });

      if (Array.isArray(clinicalList) && clinicalList.length > 0) {
        const latestClinical = clinicalList[0];
        const maPhieuKham = 
          latestClinical?.MaPhieuKham ||
          latestClinical?.maPhieuKham ||
          latestClinical?.id ||
          null;

        // ✅ LƯU maPhieuKham vào patient object
        p = {
          ...p,
          MaPhieuKham: maPhieuKham,
          maPhieuKham: maPhieuKham,
          MaPhieuKhamLs: maPhieuKham,
          maPhieuKhamLs: maPhieuKham,
        };
      }
    } catch (err) {
      console.error("Lỗi khi tìm phiếu khám:", err);
    }
  }

  // ✅ Mở modal với patient đã có maPhieuKham
  setModal({ open: true, mode: "process", patient: p });
  return;
}
```

### **Option 2: Sửa PatientModal.jsx - Tự động tìm maPhieuKham nếu thiếu**

```javascript
// File: src/components/patients/PatientModal.jsx
// Thêm useEffect để tự động fetch nếu thiếu maPhieuKham

useEffect(() => {
  if (!open || mode !== "process") return;
  if (maPhieuKhamCurrent) return; // Đã có rồi thì không cần fetch
  if (!isWaitingProcess) return;

  const pid = patient?.id || patient?.pid || patient?.MaBenhNhan || patient?.maBenhNhan;
  if (!pid) return;

  // Tự động tìm phiếu khám và fetch chẩn đoán
  (async () => {
    try {
      const clinicalList = await searchClinicalRaw({
        MaBenhNhan: pid,
        // Tìm phiếu mới nhất, không filter trạng thái
      });

      if (Array.isArray(clinicalList) && clinicalList.length > 0) {
        const latestClinical = clinicalList[0];
        const maPhieuKham = 
          latestClinical?.MaPhieuKham ||
          latestClinical?.maPhieuKham ||
          null;

        if (maPhieuKham) {
          // Lưu vào localStorage để fetchFinalDiagnosis dùng
          try {
            localStorage.setItem("last-clinical-exam-id", maPhieuKham);
          } catch {}
          
          // Gọi fetchFinalDiagnosis
          // Cần truyền maPhieuKham vào hoặc dùng ref/state
          fetchFinalDiagnosis();
        }
      }
    } catch (err) {
      console.error("Lỗi khi tìm phiếu khám tự động:", err);
    }
  })();
}, [open, mode, isWaitingProcess, patient, maPhieuKhamCurrent]);
```

### **Option 3: Sửa logic tìm phiếu khám (quan trọng)**

```javascript
// Trong Patients.jsx handleAction("process")

// ❌ SAI: Chỉ tìm "dang_thuc_hien"
const clinicalList = await searchClinicalRaw({
  MaBenhNhan: pid,
  TrangThai: "dang_thuc_hien",
});

// ✅ ĐÚNG: Tìm phiếu mới nhất (không filter trạng thái), hoặc tìm cả các trạng thái có thể có
const clinicalList = await searchClinicalRaw({
  MaBenhNhan: pid,
  // Bỏ filter TrangThai, hoặc tìm: ["dang_thuc_hien", "da_lap_chan_doan", "da_hoan_tat"]
});
```

---

## ✅ RECOMMENDATION

**Kết hợp Option 1 + Option 3:**
1. Sửa `handleAction("process")` trong `Patients.jsx`:
   - Tìm phiếu khám mới nhất (không filter trạng thái)
   - Lưu `maPhieuKham` vào patient object trước khi mở modal
2. Modal sẽ tự động fetch vì đã có `maPhieuKham` trong patient object

