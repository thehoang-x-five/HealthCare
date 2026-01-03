# NURSE ROLE CLARIFICATION - 2025-01-03

## 🎯 CẤU TRÚC VAI TRÒ Y TÁ

Trong hệ thống này, có **3 loại y tá** (phân biệt qua `LoaiYTa`):

### 1. Y tá Hành chính (`hanhchinh`)
**Nhiệm vụ:**
- ✅ Tiếp nhận bệnh nhân
- ✅ Quản lý lịch hẹn
- ✅ Lập phiếu khám
- ✅ **Thu ngân** (xử lý hóa đơn, thanh toán)
- ✅ **Phát thuốc** (quản lý kho thuốc, phát thuốc cho bệnh nhân)

**Quyền hạn:**
- Toàn quyền: Tiếp nhận (Lịch hẹn, Bệnh nhân, Lập phiếu khám)
- Xem: Khám bệnh (danh sách hàng chờ)
- Không: Gọi vào khám, Chẩn đoán, CLS

### 2. Y tá Lâm sàng (`phong_kham`)
**Nhiệm vụ:**
- ✅ Hỗ trợ bác sĩ trong phòng khám
- ✅ Gọi bệnh nhân vào khám
- ✅ Cập nhật trạng thái khám
- ✅ Tạo chẩn đoán (quyền = Bác sĩ)

**Quyền hạn:**
- Toàn quyền: Khám bệnh LS (Gọi vào, Chẩn đoán, Chỉ định CLS)
- Xem: Tất cả trang khác
- Không: Tiếp nhận (Tạo lịch hẹn, Tạo BN, Sửa thông tin BN)

### 3. Y tá Cận lâm sàng (`can_lam_sang`)
**Nhiệm vụ:**
- ✅ Thực hiện xét nghiệm
- ✅ Siêu âm, X-quang
- ✅ Cập nhật kết quả CLS
- ✅ Tạo phiếu tổng hợp (quyền = Kỹ thuật viên)

**Quyền hạn:**
- Toàn quyền: Khám bệnh CLS (Gọi vào, Cập nhật kết quả)
- Xem: Tất cả trang khác
- Không: Tiếp nhận (Tạo lịch hẹn, Tạo BN, Sửa thông tin BN)

---

## 📋 REALTIME BROADCAST STRATEGY

### Về mặt SignalR Groups:
Tất cả 3 loại y tá đều join vào **cùng một group: `role:y_ta`**

```csharp
// Backend
private static readonly string NurseRoleGroupName = RealtimeHub.GetRoleGroupName("y_ta");

// Frontend
await conn.invoke("JoinRoleAsync", "y_ta");  // Tất cả y tá
```

### Filtering theo context:
- **Clinical Exams (LS)**: Gửi cho bác sĩ + y tá trong **phòng khám** (room group)
- **CLS Orders**: Gửi cho bác sĩ + y tá trong **phòng CLS** (room group)
- **Invoices**: Gửi cho **TẤT CẢ y tá** (y tá hành chính sẽ xử lý)
- **Prescriptions**: Gửi cho bác sĩ kê đơn + **TẤT CẢ y tá** (y tá hành chính sẽ phát thuốc)
- **Appointments**: Gửi cho bác sĩ được chỉ định + **TẤT CẢ y tá** (y tá hành chính quản lý)

**Lý do broadcast rộng cho Invoice/Prescription/Appointment:**
- Y tá hành chính có thể ở bất kỳ phòng nào (không cố định)
- Cần đảm bảo tất cả y tá hành chính đều nhận được thông báo
- Frontend sẽ filter hiển thị dựa trên `LoaiYTa` của user

---

## 🔧 DATABASE STRUCTURE

### Bảng NhanSu (Staff):
```csharp
public class NhanSu
{
    public string MaNhanSu { get; set; }
    public string VaiTro { get; set; }        // "bac_si" | "y_ta" | "ky_thuat_vien"
    public string? LoaiYTa { get; set; }      // "hanhchinh" | "phong_kham" | "can_lam_sang"
    public string? MaPhong { get; set; }      // Phòng làm việc (nếu có)
}
```

### Enums:
```csharp
public static class VaiTro
{
    public const string BacSi = "bac_si";
    public const string YTa = "y_ta";
    public const string KyThuatVien = "ky_thuat_vien";
    public const string QuanTri = "quan_tri";
}

public static class LoaiYTa
{
    public const string HanhChinh = "hanhchinh";
    public const string PhongKham = "phong_kham";
    public const string CanLamSang = "can_lam_sang";
}
```

---

## 📊 REALTIME FILTERING SUMMARY

### ✅ Filtered by Room (chỉ gửi cho phòng cụ thể):
- Clinical Exams → Bác sĩ + Y tá LS trong phòng khám
- CLS Orders → Bác sĩ + Y tá CLS trong phòng CLS
- Queue → Nhân sự trong phòng

### ✅ Broadcast to All Nurses (gửi cho tất cả y tá):
- Invoices → Y tá hành chính xử lý
- Prescriptions → Y tá hành chính phát thuốc
- Appointments → Y tá hành chính quản lý
- Dashboard/KPI → Tất cả nhân sự

### ✅ Filtered by Doctor (chỉ gửi cho bác sĩ cụ thể):
- Clinical Exams → Bác sĩ được chỉ định
- Prescriptions → Bác sĩ kê đơn
- Appointments → Bác sĩ được chỉ định
- Visits → Bác sĩ khám

---

## ⚠️ LƯU Ý QUAN TRỌNG

### Về Y tá Hành chính:
- **KHÔNG CÓ** vai trò riêng "thu_ngan" hay "phat_thuoc"
- Y tá hành chính **KIÊM LUÔN** thu ngân và phát thuốc
- Tất cả đều dùng `VaiTro = "y_ta"` và `LoaiYTa = "hanhchinh"`

### Về Realtime Groups:
- Tất cả y tá join vào **cùng một group**: `role:y_ta`
- Filtering chi tiết hơn dựa trên **room groups**: `room:{maPhong}`
- Frontend tự filter hiển thị dựa trên `LoaiYTa` của user

### Về Backward Compatibility:
- Code vẫn xử lý được "thu_ngan" và "phat_thuoc" nếu có data cũ
- Nhưng tất cả đều map về group `y_ta`
- Không tạo group riêng cho thu_ngan hay phat_thuoc

---

## ✅ KẾT LUẬN

Hệ thống đã được làm rõ:
- ✅ Có **3 loại y tá**: Hành chính, Lâm sàng, Cận lâm sàng
- ✅ Y tá hành chính **KIÊM** thu ngân và phát thuốc
- ✅ Tất cả y tá join vào **cùng một group** `role:y_ta`
- ✅ Filtering chi tiết hơn dựa trên **room groups**
- ✅ Comments và documentation đã được cập nhật chính xác

**Ngày cập nhật:** 2025-01-03  
**Người thực hiện:** Kiro AI Assistant

