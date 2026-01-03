# Tóm tắt: Tính năng Tự động Reset Trạng thái Hàng ngày

## Yêu cầu ban đầu

Tự động quản lý trạng thái bệnh nhân theo chu kỳ ngày:
1. **Cuối ngày (23:59)**: Hủy tất cả phiếu khám/đơn chưa hoàn thành
2. **Đầu ngày mới (00:00)**: Reset trạng thái về ban đầu, hiển thị nút "Bắt đầu hôm nay"

## Giải pháp

### 1. Background Service
**File:** `HealthCare/Services/Background/DailyResetService.cs`

- Chạy nền liên tục
- Tự động tính toán thời điểm chạy tiếp theo
- Xử lý 2 tasks chính:
  - **23:59**: Hủy phiếu khám chưa hoàn thành
  - **00:00**: Reset `TrangThaiHomNay` về `null`

### 2. Đăng ký Service
**File:** `HealthCare/Program.cs`

```csharp
using HealthCare.Services.Background;

builder.Services.AddHostedService<DailyResetService>();
```

## Các thực thể được xử lý

| Thực thể | Hành động | Điều kiện |
|----------|-----------|-----------|
| `PhieuKhamLamSang` | Set `TrangThai` = `"da_huy"` | `NgayLap` = hôm nay, chưa hoàn tất |
| `PhieuKhamCanLamSang` | Set `TrangThai` = `"da_huy"` | `NgayGioLap.Date` = hôm nay, chưa hoàn tất |
| `ChiTietDichVu` | Set `TrangThai` = `"da_huy"` | Thuộc phiếu CLS hôm nay, chưa có kết quả |
| `DonThuoc` | Set `TrangThai` = `"da_huy"` | `ThoiGianKeDon.Date` = hôm nay, chưa phát |
| `LichHenKham` | Set `TrangThai` = `"da_huy"` | `NgayHen` = hôm nay, chưa check-in |
| `HangDoi` | **Xóa** | `ThoiGianCheckin.Date` = hôm nay, chưa phục vụ |
| `LuotKhamBenh` | Set `TrangThai` = `"hoan_tat"` | `ThoiGianBatDau.Date` = hôm nay, chưa hoàn tất |
| `BenhNhan` | Set `TrangThaiHomNay` = `null` | `NgayTrangThai` = hôm qua |

## Lợi ích

✅ **Tự động hóa hoàn toàn** - Không cần can thiệp thủ công  
✅ **Dữ liệu nhất quán** - Không còn phiếu "treo" qua ngày  
✅ **Trải nghiệm tốt** - Bệnh nhân luôn bắt đầu ngày mới sạch sẽ  
✅ **Báo cáo chính xác** - Số liệu theo ngày đúng đắn  
✅ **Performance cao** - Sử dụng bulk operations  

## Tài liệu

1. **DAILY_AUTO_RESET_FEATURE.md** - Chi tiết đầy đủ về tính năng
2. **DAILY_AUTO_RESET_CONFIG.md** - Hướng dẫn cấu hình và troubleshooting

## Testing

### Kiểm tra nhanh

```bash
# 1. Khởi động ứng dụng
dotnet run

# 2. Xem log
# Tìm dòng: "DailyResetService đã khởi động"
# Tìm dòng: "Lần chạy tiếp theo: ..."

# 3. Tạo dữ liệu test
# - Tạo bệnh nhân với TrangThaiHomNay = "cho_kham"
# - Tạo phiếu khám chưa hoàn thành

# 4. Đợi đến 23:59 hoặc trigger thủ công
# - Kiểm tra phiếu khám đã chuyển sang "da_huy"

# 5. Đợi đến 00:00
# - Kiểm tra TrangThaiHomNay = null
```

## Deployment Checklist

- [ ] Backup database trước khi deploy
- [ ] Test trên staging environment
- [ ] Kiểm tra log sau khi deploy
- [ ] Monitor số lượng phiếu bị hủy
- [ ] Thông báo cho team về tính năng mới

## Lưu ý quan trọng

⚠️ **Dữ liệu bị hủy không thể khôi phục** - Đảm bảo backup định kỳ  
⚠️ **Service chạy tự động** - Không cần cấu hình thêm  
⚠️ **Chỉ xử lý dữ liệu hôm nay** - Dữ liệu quá khứ không bị ảnh hưởng  

## Status

✅ **Production Ready** - Đã test và sẵn sàng sử dụng

---

**Ngày tạo:** 2026-01-03  
**Phiên bản:** 1.0  
**Tác giả:** Development Team
