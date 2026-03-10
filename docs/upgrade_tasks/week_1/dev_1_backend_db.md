# Hướng dẫn hoàn thành Tuần 1 - Nền tảng Hạ tầng & Trạng thái

> **File:** `UPGRADE_IMPLEMENTATION_PLAN.md` (Mục 1.1, 1.2, 2.1)

## Nhiệm vụ 1: Thiết lập MongoDB
1. **Cài đặt thư viện**: Chạy lệnh cài đặt gói `MongoDB.Driver` cho `HealthCare.csproj`.
2. **Cấu hình appsettings**: 
   - Mở `appsettings.json` và thêm cấu hình `"MongoDb": { "ConnectionString": "mongodb://localhost:27017", "DatabaseName": "healthcare_plus" }`.
3. **Mã nguồn**:
   - Tạo `MongoDbContext.cs` trong folder `Data` hoặc `Infrastructure`. Dùng Singleton/Scoped DI cho MongoClient trong `Program.cs`.

## Nhiệm vụ 2: Chạy SQL Migration cho các bảng mới/cập nhật
1. **Entity Updates**:
   - Mở `BenhNhan.cs`: Thêm `MaCha` (string), `MaMe` (string), `CCCD` (string), `NgayTao`, `NgayCapNhat`.
   - Mở các Entity khác (`KetQuaDichVu`, `PhieuChanDoanCuoi`, `HoaDonThanhToan`, `DonThuoc`, `ChiTietDonThuoc`, `HangDoi`, `LuotKhamBenh`) để thêm các thuộc tính tương ứng nêu trong Gap Analysis.
   - Thêm 2 bảng mới: `LichSuXuatKho.cs` và `ThongBaoMau.cs`.
2. **Tạo Migration**:
   - Mở Terminal/Package Manager: Chạy `dotnet ef migrations add Phase1Upgrade`.
   - Review file migration, nếu ổn chạy tiếp `dotnet ef database update`.

## Nhiệm vụ 3: Đặt Lịch SERIALIZABLE (Stored Procedure)
1. **Tạo Stored Procedure (MySQL)**:
   - Viết lệnh SQL tạo Procedure `sp_BookAppointment` kiểm tra trùng lịch (sử dụng isolation level `SERIALIZABLE`). Chạy lệnh này trên DB server.
2. **Cập nhật Backend (`AppointmentService.cs`)**:
   - Giữ nguyên `FindConflictsForConfirmedAsync` làm Validate bước 1.
   - Khi insert lịch, thay vì dùng `_context.LichHen.Add`, gọi qua `_context.Database.ExecuteSqlRawAsync("CALL sp_BookAppointment(...)")`.
