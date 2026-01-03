# Tính năng Tự động Reset Trạng thái Hàng ngày

## Tổng quan

Hệ thống tự động quản lý trạng thái bệnh nhân và phiếu khám theo chu kỳ ngày:

### 1. Cuối ngày (23:59)
Tự động **hủy tất cả phiếu khám/đơn chưa hoàn thành** của bệnh nhân có trạng thái khác "Hoàn thành"

### 2. Đầu ngày mới (00:00)
Tự động **reset trạng thái hôm nay** về `null` để hiển thị nút "Bắt đầu hôm nay"

---

## Chi tiết hoạt động

### Task 1: Hủy phiếu khám chưa hoàn thành (23:59)

**Điều kiện:**
- Bệnh nhân có `TrangThaiHomNay` khác `null` và khác `"da_hoan_tat"`
- `NgayTrangThai` = hôm nay

**Các thao tác tự động:**

1. **Phiếu khám lâm sàng** (`PhieuKhamLamSang`)
   - Điều kiện: `NgayLap` = hôm nay, `TrangThai` ≠ `"da_hoan_tat"` và ≠ `"da_huy"`
   - Hành động: Set `TrangThai` = `"da_huy"`

2. **Phiếu khám CLS** (`PhieuKhamCanLamSang`)
   - Điều kiện: `NgayGioLap.Date` = hôm nay, `TrangThai` ≠ `"da_hoan_tat"`
   - Hành động: Set `TrangThai` = `"da_huy"`

3. **Chi tiết dịch vụ CLS** (`ChiTietDichVu`)
   - Điều kiện: Thuộc phiếu CLS hôm nay, `TrangThai` ≠ `"da_co_ket_qua"`
   - Hành động: Set `TrangThai` = `"da_huy"`

4. **Đơn thuốc** (`DonThuoc`)
   - Điều kiện: `ThoiGianKeDon.Date` = hôm nay, `TrangThai` ≠ `"da_phat"`
   - Hành động: Set `TrangThai` = `"da_huy"`

5. **Lịch hẹn khám** (`LichHenKham`)
   - Điều kiện: `NgayHen` = hôm nay, `TrangThai` ≠ `"da_checkin"` và ≠ `"da_huy"`
   - Hành động: Set `TrangThai` = `"da_huy"`

6. **Hàng đợi** (`HangDoi`)
   - Điều kiện: `ThoiGianCheckin.Date` = hôm nay, `TrangThai` ≠ `"da_phuc_vu"`
   - Hành động: **Xóa** khỏi hệ thống

7. **Lượt khám bệnh** (`LuotKhamBenh`)
   - Điều kiện: `ThoiGianBatDau.Date` = hôm nay, `TrangThai` ≠ `"hoan_tat"`
   - Hành động: Set `TrangThai` = `"hoan_tat"`, `ThoiGianKetThuc` = thời gian hiện tại
   - Lưu ý: `LuotKhamBenh` không có trạng thái `"da_huy"`, chỉ có `"dang_thuc_hien"` và `"hoan_tat"`

**Logging:**
```
Đã hủy X phiếu khám lâm sàng
Đã hủy Y phiếu khám CLS
Đã hủy Z chi tiết dịch vụ CLS
Đã hủy W đơn thuốc
Đã hủy V lịch hẹn khám
Đã xóa U hàng đợi
Đã đóng T lượt khám bệnh
```

---

### Task 2: Reset trạng thái hôm nay (00:00)

**Điều kiện:**
- Bệnh nhân có `TrangThaiHomNay` ≠ `null`
- `NgayTrangThai` = ngày hôm qua

**Hành động:**
- Set `TrangThaiHomNay` = `null`
- Set `NgayTrangThai` = hôm nay

**Kết quả:**
- Frontend sẽ hiển thị nút "Bắt đầu hôm nay" cho bệnh nhân
- Bệnh nhân có thể bắt đầu quy trình khám mới

**Logging:**
```
Đã reset trạng thái hôm nay cho X bệnh nhân
```

---

## Cấu hình

### Background Service

Service: `DailyResetService`
- Loại: `BackgroundService` (chạy nền liên tục)
- Đăng ký: `Program.cs` → `builder.Services.AddHostedService<DailyResetService>()`

### Thời gian chạy

| Thời điểm | Task | Mô tả |
|-----------|------|-------|
| 23:59 | Hủy phiếu khám | Hủy tất cả phiếu/đơn chưa hoàn thành |
| 00:00 | Reset trạng thái | Reset `TrangThaiHomNay` về `null` |

### Tự động tính toán lần chạy tiếp theo

Service tự động tính toán thời điểm chạy tiếp theo:
- Nếu chưa đến 23:59 hôm nay → chạy lúc 23:59
- Nếu đã qua 23:59 → chạy lúc 00:00 ngày mai

---

## Lợi ích

### 1. Tự động dọn dẹp dữ liệu
- Không còn phiếu khám "treo" qua ngày
- Dữ liệu luôn nhất quán và chính xác

### 2. Cải thiện trải nghiệm người dùng
- Bệnh nhân luôn bắt đầu ngày mới với trạng thái sạch
- Không cần can thiệp thủ công

### 3. Báo cáo chính xác
- Số liệu thống kê theo ngày chính xác
- Dễ dàng phân tích hiệu suất

### 4. Giảm tải cho nhân viên
- Không cần nhân viên hủy phiếu thủ công cuối ngày
- Tự động hóa quy trình quản lý

---

## Monitoring & Logging

### Log Level: Information

Service ghi log chi tiết:
- Thời điểm bắt đầu/kết thúc mỗi task
- Số lượng bản ghi được xử lý
- Lỗi nếu có

### Ví dụ log:

```
[2026-01-03 23:59:00] DailyResetService đã khởi động
[2026-01-03 23:59:00] Lần chạy tiếp theo: 2026-01-03 23:59:00
[2026-01-03 23:59:01] Bắt đầu hủy phiếu khám chưa hoàn thành...
[2026-01-03 23:59:01] Tìm thấy 15 bệnh nhân chưa hoàn thành
[2026-01-03 23:59:02] Đã hủy 12 phiếu khám lâm sàng
[2026-01-03 23:59:02] Đã hủy 8 phiếu khám CLS
[2026-01-03 23:59:02] Đã hủy 25 chi tiết dịch vụ CLS
[2026-01-03 23:59:02] Đã hủy 10 đơn thuốc
[2026-01-03 23:59:02] Đã hủy 3 lịch hẹn khám
[2026-01-03 23:59:02] Đã xóa 15 hàng đợi
[2026-01-03 23:59:02] Đã đóng 18 lượt khám bệnh
[2026-01-03 23:59:02] Hoàn thành hủy phiếu khám
[2026-01-04 00:00:00] Bắt đầu reset trạng thái hôm nay...
[2026-01-04 00:00:01] Đã reset trạng thái hôm nay cho 15 bệnh nhân
```

---

## Xử lý lỗi

### Retry mechanism
- Nếu có lỗi, service đợi 1 phút rồi thử lại
- Không crash toàn bộ ứng dụng

### Transaction safety
- Mỗi task chạy trong scope riêng
- Đảm bảo database context được dispose đúng cách

---

## Testing

### Test thủ công

1. **Test hủy phiếu khám:**
   - Tạo bệnh nhân với `TrangThaiHomNay` = `"cho_kham"`
   - Tạo phiếu khám chưa hoàn thành
   - Đợi đến 23:59 hoặc trigger thủ công
   - Kiểm tra phiếu khám đã chuyển sang `"da_huy"`

2. **Test reset trạng thái:**
   - Tạo bệnh nhân với `TrangThaiHomNay` = `"cho_kham"`
   - Set `NgayTrangThai` = hôm qua
   - Đợi đến 00:00 hoặc trigger thủ công
   - Kiểm tra `TrangThaiHomNay` = `null`

### Test tự động (Unit test)

```csharp
// TODO: Viết unit test cho DailyResetService
```

---

## Lưu ý quan trọng

⚠️ **Service chạy tự động khi ứng dụng khởi động**
- Không cần cấu hình thêm
- Chạy nền liên tục cho đến khi ứng dụng dừng

⚠️ **Dữ liệu bị hủy không thể khôi phục**
- Đảm bảo logic nghiệp vụ phù hợp trước khi deploy
- Cân nhắc backup dữ liệu định kỳ

⚠️ **Performance**
- Service sử dụng `ExecuteUpdateAsync` và `ExecuteDeleteAsync` (bulk operations)
- Hiệu suất cao, không load toàn bộ entities vào memory

---

## Tương lai

### Cải tiến có thể thêm:

1. **Cấu hình thời gian linh hoạt**
   - Cho phép admin cấu hình giờ chạy qua appsettings.json

2. **Notification**
   - Gửi thông báo cho admin về số lượng phiếu bị hủy

3. **Archive thay vì Delete**
   - Lưu trữ dữ liệu bị hủy vào bảng lịch sử

4. **Dashboard**
   - Hiển thị thống kê về số phiếu bị hủy hàng ngày

---

## Tác giả

- **Ngày tạo:** 2026-01-03
- **Phiên bản:** 1.0
- **Trạng thái:** Production Ready ✅
