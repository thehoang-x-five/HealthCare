# Enum Update Log

## 2026-01-03 - Cập nhật Enum theo yêu cầu nghiệp vụ

### Thay đổi

#### 1. TrangThaiHomNay - Đổi tên và thêm trạng thái
```diff
public static class TrangThaiHomNay
{
+   public const string ChoTiepNhan = "cho_tiep_nhan";  // Mặc định khi mới vào
    public const string ChoKham = "cho_kham";
-   public const string DangThucHien = "dang_thuc_hien";
+   public const string DangKham = "dang_kham";  // Đang khám lâm sàng (LS)
    public const string ChoTiepNhanDv = "cho_tiep_nhan_dv";
    public const string ChoKhamDv = "cho_kham_dv";
-   public const string DangKhamDv = "dang_kham_dv";
+   public const string DangKhamDv = "dang_kham_dv";  // Đang khám dịch vụ (CLS)
    public const string ChoXuLy = "cho_xu_ly";
    public const string ChoXuLyDv = "cho_xu_ly_dv";
    public const string DaHoanTat = "da_hoan_tat";
+   public const string DaHuy = "da_huy";
}
```

**Lý do:**
- `ChoTiepNhan` = "cho_tiep_nhan" - Trạng thái mặc định khi bệnh nhân mới vào hệ thống
- `DangKham` = "dang_kham" - Đang khám lâm sàng (LS), rõ ràng hơn `DangThucHien`
- `DangKhamDv` = "dang_kham_dv" - Đang khám dịch vụ (CLS), đã có sẵn
- `DaHuy` = "da_huy" - Trạng thái bệnh nhân đã hủy, được dùng trong `DashboardService.cs`

**Cập nhật code:**
- ✅ `HistoryService.cs` line 474: Đổi từ `"dang_thuc_hien"` → `"dang_kham"`

#### 2. TrangThaiPhieuTongHop - Thêm 1 trạng thái
```diff
public static class TrangThaiPhieuTongHop
{
+   public const string DangThucHien = "dang_thuc_hien";
    public const string ChoXuLy = "cho_xu_ly";
    public const string DangXuLy = "dang_xu_ly";
    public const string DaHoanTat = "da_hoan_tat";
}
```

**Lý do:**
- `DangThucHien` được sử dụng trong `ClsDtos.cs` và `ClsService.cs` cho phiếu tổng hợp CLS

---

## Tổng kết

### Trước khi cập nhật
- `TrangThaiHomNay`: 7 constants
- `TrangThaiPhieuTongHop`: 3 constants

### Sau khi cập nhật
- `TrangThaiHomNay`: 10 constants ✅
- `TrangThaiPhieuTongHop`: 4 constants ✅

---

## Files đã cập nhật

1. ✅ `HealthCare/Enums/StatusEnums.cs` - Thêm constants
2. ✅ `HealthCare/ENUM_USAGE_GUIDE.md` - Cập nhật documentation

---

## Workflow trạng thái bệnh nhân hôm nay

```
[Bắt đầu]
    ↓
cho_tiep_nhan (Chờ tiếp nhận - mặc định)
    ↓
cho_kham (Chờ khám)
    ↓
dang_kham (Đang khám LS)
    ↓
cho_tiep_nhan_dv (Chờ tiếp nhận DV CLS)
    ↓
cho_kham_dv (Chờ khám DV CLS)
    ↓
dang_kham_dv (Đang khám DV CLS)
    ↓
cho_xu_ly (Chờ xử lý chẩn đoán)
    ↓
cho_xu_ly_dv (Chờ xử lý DV)
    ↓
da_hoan_tat (Hoàn tất) / da_huy (Đã hủy)
    ↓
[Kết thúc]
```

---

## Workflow trạng thái phiếu tổng hợp

```
[Bắt đầu]
    ↓
dang_thuc_hien (Đang thực hiện các DV CLS)
    ↓
cho_xu_ly (Chờ y tá lập phiếu LS)
    ↓
dang_xu_ly (Đang xử lý)
    ↓
da_hoan_tat (Đã hoàn tất)
    ↓
[Kết thúc]
```

---

## Kiểm tra đầy đủ

### Checklist các trạng thái đã có trong enum

#### Trạng thái Bệnh nhân
- [x] `cho_tiep_nhan` (Mặc định khi mới vào)
- [x] `cho_kham`
- [x] `dang_kham` (Đang khám LS)
- [x] `cho_tiep_nhan_dv`
- [x] `cho_kham_dv`
- [x] `dang_kham_dv` (Đang khám CLS)
- [x] `cho_xu_ly`
- [x] `cho_xu_ly_dv`
- [x] `da_hoan_tat`
- [x] `da_huy`

#### Trạng thái Phiếu khám LS
- [x] `da_lap`
- [x] `dang_thuc_hien`
- [x] `da_lap_chan_doan`
- [x] `da_hoan_tat`
- [x] `da_huy`

#### Trạng thái Phiếu khám CLS
- [x] `da_lap`
- [x] `dang_thuc_hien`
- [x] `da_hoan_tat`
- [x] `da_huy`

#### Trạng thái Chi tiết DV
- [x] `da_lap`
- [x] `dang_thuc_hien`
- [x] `da_co_ket_qua`
- [x] `chua_co_ket_qua`
- [x] `da_huy`

#### Trạng thái Đơn thuốc
- [x] `da_ke`
- [x] `cho_phat`
- [x] `da_phat`
- [x] `da_huy`

#### Trạng thái Lịch hẹn
- [x] `dang_cho`
- [x] `da_xac_nhan`
- [x] `da_checkin`
- [x] `da_huy`

#### Trạng thái Hàng đợi
- [x] `cho_goi`
- [x] `dang_goi`
- [x] `dang_thuc_hien`
- [x] `da_phuc_vu`

#### Trạng thái Lượt khám
- [x] `dang_thuc_hien`
- [x] `hoan_tat`

#### Trạng thái Hóa đơn
- [x] `chua_thu`
- [x] `da_thu`
- [x] `da_huy`

#### Trạng thái Thông báo
- [x] `chua_gui`
- [x] `da_gui`
- [x] `da_doc`

#### Trạng thái Phiếu tổng hợp
- [x] `dang_thuc_hien`
- [x] `cho_xu_ly`
- [x] `dang_xu_ly`
- [x] `da_hoan_tat`

---

## Validation

### Đã kiểm tra
- ✅ Compile thành công
- ✅ Không có diagnostic errors
- ✅ Documentation đã cập nhật
- ✅ Tất cả trạng thái trong code đều có trong enum

### Cần làm tiếp
- [ ] Migrate các service sang dùng enum
- [ ] Viết unit tests
- [ ] Update API documentation

---

**Updated:** 2026-01-03  
**Status:** ✅ Complete
